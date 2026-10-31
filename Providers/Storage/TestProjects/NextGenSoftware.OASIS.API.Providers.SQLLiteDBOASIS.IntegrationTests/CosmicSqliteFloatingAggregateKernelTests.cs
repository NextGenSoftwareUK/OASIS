using System.Text.Json;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public sealed class CosmicSqliteFloatingAggregateKernelTests
{
    private static SqliteConnection Open()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open(); return connection;
    }
    private static string Function(bool single) => single ? SqliteCqlFloatingAggregates.SingleFunction : SqliteCqlFloatingAggregates.DoubleFunction;
    private static string Key(ulong bits, bool single) => single
        ? CosmicFloatingScalarResults.SingleOrderKey(BitConverter.Int32BitsToSingle(unchecked((int)(uint)bits)))
        : CosmicFloatingScalarResults.DoubleOrderKey(BitConverter.Int64BitsToDouble(unchecked((long)bits)));
    private static byte[] Reduce(SqliteConnection connection, bool single, params string?[] keys)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT " + Function(single) + "(value) FROM json_each($keys)";
        command.Parameters.AddWithValue("$keys", JsonSerializer.Serialize(keys));
        return (byte[])command.ExecuteScalar()!;
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void NativeFloatingKernelMatchesEveryExponentAndSeededInput(bool single)
    {
        using var connection = Open();
        using var budget = new SqliteCqlReadBudget(connection, default);
        using var aggregate = new SqliteCqlFloatingAggregates(connection, budget.Check);
        var precision = single ? 23 : 52;
        var sign = single ? 1UL << 31 : 1UL << 63;
        var maximum = single ? 0x7F7FFFFFUL : 0x7FEFFFFFFFFFFFFFUL;
        var inputs = new List<ulong> { 0, sign, 1, sign | 1, maximum, maximum | sign };
        for (var exponent=0; exponent<(single ? 255 : 2047); exponent++)
        {
            inputs.Add((ulong)exponent<<precision); inputs.Add(((ulong)exponent<<precision)|sign);
        }
        var random = new Random(20261028); var bytes = new byte[8];
        for (var index=0; index<1024; index++)
        {
            random.NextBytes(bytes);
            var bits=BitConverter.ToUInt64(bytes)&(single ? uint.MaxValue : ulong.MaxValue);
            if ((bits & ~sign)<=maximum) inputs.Add(bits);
        }
        using var command=connection.CreateCommand();
        command.CommandText="SELECT key,"+Function(single)+"(value) FROM json_each($keys) GROUP BY key ORDER BY CAST(key AS INTEGER)";
        command.Parameters.AddWithValue("$keys",JsonSerializer.Serialize(inputs.Select(bits=>Key(bits,single))));
        using var reader=command.ExecuteReader(); var position=0;
        while(reader.Read())
        {
            aggregate.Check();
            var packet=SqliteCqlFloatingAggregates.ReadPacket((byte[])reader.GetValue(1),single);
            Assert.False(packet.IsError,packet.Message); Assert.Equal(1,packet.Result.Count);
            var normalized=CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Sum,
                single ? typeof(float) : typeof(double),packet.Result.Parts,packet.Result.Count);
            Assert.False(normalized.IsError,normalized.Message);
            var actual=single ? CosmicFloatingScalarResults.SingleOrderKey(Assert.IsType<float>(normalized.Result))
                : CosmicFloatingScalarResults.DoubleOrderKey(Assert.IsType<double>(normalized.Result));
            Assert.Equal(Key(inputs[position++],single),actual);
        }
        Assert.Equal(inputs.Count,position); aggregate.Check(); budget.Check();
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void NativeFloatingKernelCancelsOversizedIntermediatesAndRetainsOverflowProof(bool single)
    {
        using var connection=Open();
        using var aggregate=new SqliteCqlFloatingAggregates(connection,()=>{});
        var maximum=single ? 0x7F7FFFFFUL : 0x7FEFFFFFFFFFFFFFUL;
        var sign=single ? 1UL<<31 : 1UL<<63;
        var raw=Reduce(connection,single,Key(maximum,single),Key(maximum,single),Key(maximum|sign,single),Key(maximum|sign,single),Key(1,single));
        aggregate.Check();
        var packet=SqliteCqlFloatingAggregates.ReadPacket(raw,single).Result;
        var sum=CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Sum,single ? typeof(float):typeof(double),packet.Parts,packet.Count);
        Assert.False(sum.IsError,sum.Message);
        Assert.Equal(Key(1,single),single ? CosmicFloatingScalarResults.SingleOrderKey((float)sum.Result):CosmicFloatingScalarResults.DoubleOrderKey((double)sum.Result));
        var overflow=Reduce(connection,single,Key(maximum,single),Key(maximum,single));
        var proof=SqliteCqlFloatingAggregates.ReadPacket(overflow,single).Result;
        Assert.Equal("CQL_SCALAR_OVERFLOW",CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Sum,single?typeof(float):typeof(double),proof.Parts,proof.Count).ErrorCode);
        var mean=CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Average,single?typeof(float):typeof(double),proof.Parts,proof.Count);
        Assert.False(mean.IsError,mean.Message);
        Assert.Equal(Key(maximum,single),single?CosmicFloatingScalarResults.SingleOrderKey((float)mean.Result):CosmicFloatingScalarResults.DoubleOrderKey((double)mean.Result));
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void NativeFloatingKeyUsesFinalTiesAndEmptyNullableContract(bool single)
    {
        using var connection=Open();
        using var aggregate=new SqliteCqlFloatingAggregates(connection,()=>{});
        using var command=connection.CreateCommand();
        command.CommandText="SELECT "+SqliteCqlFloatingAggregates.KeyFunction+"($packet,2,$domain)";
        command.Parameters.AddWithValue("$packet",Reduce(connection,single,Key(3,single),Key(0,single)));
        command.Parameters.AddWithValue("$domain",single?0:1);
        Assert.Equal(Key(2,single),command.ExecuteScalar()); aggregate.Check();
        command.Parameters["$packet"].Value=Reduce(connection,single,null,null);
        command.CommandText="SELECT "+SqliteCqlFloatingAggregates.KeyFunction+"($packet,3,$domain)";
        Assert.Equal(DBNull.Value,command.ExecuteScalar()); aggregate.Check();
        var empty=Reduce(connection,single);
        Assert.Equal(0,SqliteCqlFloatingAggregates.ReadPacket(empty,single).Result.Count);
    }

    [Fact]
    public void NativeFloatingPacketRejectsForeignShapeVersionDomainAndCount()
    {
        using var connection=Open();
        using var aggregate=new SqliteCqlFloatingAggregates(connection,()=>{});
        var packet=Reduce(connection,true,Key(1,true));
        Assert.Equal("CQL_DATA_INVALID",SqliteCqlFloatingAggregates.ReadPacket(packet,false).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID",SqliteCqlFloatingAggregates.ReadPacket(packet[..^1],true).ErrorCode);
        packet[0]=2;
        Assert.Equal("CQL_DATA_INVALID",SqliteCqlFloatingAggregates.ReadPacket(packet,true).ErrorCode);
        packet[0]=1; Array.Copy(BitConverter.GetBytes(-1L),0,packet,2,8);
        Assert.Equal("CQL_DATA_INVALID",SqliteCqlFloatingAggregates.ReadPacket(packet,true).ErrorCode);
        Array.Copy(BitConverter.GetBytes((long)int.MaxValue+1),0,packet,2,8);
        Assert.Equal("CQL_QUERY_LIMIT",SqliteCqlFloatingAggregates.ReadPacket(packet,true).ErrorCode);
    }

    [Fact]
    public void NativeSingleCannotRoundAForeignDoubleKeyIntoItsSourceDomain()
    {
        using var connection=Open();
        using var aggregate=new SqliteCqlFloatingAggregates(connection,()=>{});
        Reduce(connection,true,CosmicFloatingScalarResults.DoubleOrderKey(0.1d));
        Assert.Equal("CQL_DATA_INVALID",Assert.Throws<SqliteCqlNumericException>(()=>aggregate.Check()).Code);
    }

    [Fact]
    public void NativeKeyErrorRemainsVisibleWhenSqlFilterHidesTheOverflow()
    {
        using var connection=Open();
        using var aggregate=new SqliteCqlFloatingAggregates(connection,()=>{});
        var raw=Reduce(connection,false,Key(0x7FEFFFFFFFFFFFFFUL,false),Key(0x7FEFFFFFFFFFFFFFUL,false));
        using var command=connection.CreateCommand();
        command.CommandText="SELECT normalized FROM (SELECT "+SqliteCqlFloatingAggregates.KeyFunction+"($packet,0,1) AS normalized) WHERE normalized IS NOT NULL";
        command.Parameters.AddWithValue("$packet",raw);
        Assert.Null(command.ExecuteScalar());
        Assert.Equal("CQL_SCALAR_OVERFLOW",Assert.Throws<SqliteCqlNumericException>(()=>aggregate.Check()).Code);
    }

    [Fact]
    public void NativeBudgetFailurePreservesOriginalCauseAndConnectionCanBeReused()
    {
        using var connection=Open(); var cause=new OperationCanceledException("owned test cancellation");
        using(var aggregate=new SqliteCqlFloatingAggregates(connection,()=>throw cause))
        {
            using var command=connection.CreateCommand();
            command.CommandText="SELECT "+SqliteCqlFloatingAggregates.DoubleFunction+"($key)";
            command.Parameters.AddWithValue("$key",Key(1,false)); command.ExecuteScalar();
            Assert.Same(cause,Assert.Throws<OperationCanceledException>(()=>aggregate.Check()));
        }
        using(var fresh=new SqliteCqlFloatingAggregates(connection,()=>{}))
        {
            Assert.Equal(1,SqliteCqlFloatingAggregates.ReadPacket(Reduce(connection,false,Key(1,false)),false).Result.Count);
            fresh.Check();
        }
        using var missing=connection.CreateCommand();
        missing.CommandText="SELECT "+SqliteCqlFloatingAggregates.DoubleFunction+"('anything')";
        Assert.Throws<SqliteException>(()=>missing.ExecuteScalar());
    }
    [Fact]
    public void DisposedOwnerCannotUnregisterANewerScopeOnTheSameConnection()
    {
        using var connection=Open();
        var previous=new SqliteCqlFloatingAggregates(connection,()=>{});
        previous.Dispose();
        using var current=new SqliteCqlFloatingAggregates(connection,()=>{});
        previous.Dispose();
        var packet=SqliteCqlFloatingAggregates.ReadPacket(Reduce(connection,false,Key(1,false)),false);
        Assert.False(packet.IsError,packet.Message); Assert.Equal(1,packet.Result.Count);
        current.Check();
    }

}
