using System.Reflection;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task NativeCqlPayloadBudgetRejectsWithoutPublishingPartialRows()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var payload=new string('x',8*1024*1024);
        for(var i=0;i<9;i++)
            (await provider.SaveHolonAsync(new Holon{Id=Guid.NewGuid(),MetaData=new(){["payload"]=payload}},saveChildren:false)).IsError.Should().BeFalse();
        var result=await provider.ExecuteCosmicQueryAsync(WindowPlan(CosmicQuery<Holon>.Create()));
        result.ErrorCode.Should().Be("CQL_QUERY_LIMIT");
        result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();
        var next=await provider.ExecuteCosmicQueryAsync(WindowPlan(CosmicQuery<Holon>.Create().Take(1).Result));
        next.IsError.Should().BeFalse(next.Message+next.Exception);next.Result.Should().ContainSingle();
    }

    [Theory]
    [InlineData("work")] [InlineData("deadline")] [InlineData("cancel")] [InlineData("size")]
    public void NativeCqlBudgetsInterruptAndRestoreOwnedConnectionState(string reason)
    {
        using var connection=new SqliteConnection("Data Source=:memory:");connection.Open();
        var previous=SQLitePCL.raw.sqlite3_limit(connection.Handle,SQLitePCL.raw.SQLITE_LIMIT_LENGTH,-1);
        var type=typeof(SQLLiteDBOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories.SqliteCqlReadBudget");
        type.Should().NotBeNull();
        using var cancellation=new CancellationTokenSource();
        var deadline=reason=="deadline"?TimeSpan.Zero:TimeSpan.FromSeconds(30);
        var owner=Activator.CreateInstance(type!,BindingFlags.Instance|BindingFlags.NonPublic,null,
            new object?[]{connection,cancellation.Token,reason=="work"?1000L:50000000L,(TimeSpan?)deadline},null)!;
        using(var budget=(IDisposable)owner)
        {
            if(reason=="cancel")cancellation.Cancel();
            using var command=connection.CreateCommand();
            command.CommandText=reason=="size"?"SELECT zeroblob(67108865)":
                "WITH RECURSIVE n(x) AS (VALUES(0) UNION ALL SELECT x+1 FROM n WHERE x<1000000) SELECT sum(x) FROM n";
            var interrupted=Assert.Throws<SqliteException>(()=>command.ExecuteScalar());
            interrupted.SqliteErrorCode.Should().Be(reason=="size"?18:9);
            if(reason!="size")
            {
                var failure=Assert.Throws<TargetInvocationException>(()=>type!.GetMethod("Check",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(owner,null));
                failure.InnerException!.GetType().Name.Should().Be(reason switch {
                    "work"=>"SqliteCqlResourceException","deadline"=>"SqliteCqlDeadlineException",_=>nameof(OperationCanceledException)});
            }
        }
        SQLitePCL.raw.sqlite3_limit(connection.Handle,SQLitePCL.raw.SQLITE_LIMIT_LENGTH,-1).Should().Be(previous);
        using var restored=connection.CreateCommand();
        restored.CommandText="WITH RECURSIVE n(x) AS (VALUES(0) UNION ALL SELECT x+1 FROM n WHERE x<10000) SELECT count(*) FROM n";
        Convert.ToInt64(restored.ExecuteScalar()).Should().Be(10001);
    }
}
