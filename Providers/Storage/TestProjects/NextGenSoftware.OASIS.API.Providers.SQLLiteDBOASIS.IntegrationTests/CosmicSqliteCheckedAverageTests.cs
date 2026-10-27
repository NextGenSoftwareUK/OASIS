using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public sealed class CosmicSqliteCheckedAverageTests
{
    [Fact]
    public void NativeAggregateOrderControlsPrefixOverflowAfterPaging()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var owner = new SqliteCqlCheckedInt64Average(connection, () => { });
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE inputs(position INTEGER PRIMARY KEY,value INTEGER);"
            + "INSERT INTO inputs VALUES(0,9223372036854775807),(1,1),(2,-9223372036854775807);";
        command.ExecuteNonQuery();

        long Run(string order, string page = "")
        {
            // Insertion order is deliberately independent of aggregate order.
            command.CommandText = "SELECT " + SqliteCqlCheckedInt64Average.Function
                + "(value ORDER BY position " + order + ") FROM (SELECT * FROM inputs ORDER BY position "
                + order + page + ")";
            var result = (long)command.ExecuteScalar()!;
            owner.Check();
            return result;
        }

        Assert.Equal(1L, Run("ASC"));
        Assert.Equal(0L, Run("DESC"));
        Assert.Equal(0L, Run("ASC", " LIMIT 2 OFFSET 1"));
        Assert.Equal(0L, Run("ASC", " LIMIT 0"));
    }

    [Fact]
    public void NullInputsDoNotParticipateAndNegativeOverflowStaysLatched()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var owner = new SqliteCqlCheckedInt64Average(connection, () => { });
        using var command = connection.CreateCommand();
        command.CommandText = "WITH inputs(position,value) AS (VALUES(0,NULL),(1,-9223372036854775808),"
            + "(2,-1),(3,9223372036854775807)) SELECT " + SqliteCqlCheckedInt64Average.Function
            + "(value ORDER BY position) FROM inputs";
        Assert.Equal(1L, (long)command.ExecuteScalar()!);
        owner.Check();
        command.CommandText = "SELECT " + SqliteCqlCheckedInt64Average.Function + "(NULL)";
        Assert.Equal(0L, (long)command.ExecuteScalar()!);
        owner.Check();
    }

    [Fact]
    public void CancellationIsPreservedAndDisposalRemovesTheOwnedHook()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var cancellation = new OperationCanceledException("owned test cancellation");
        using (var owner = new SqliteCqlCheckedInt64Average(connection, () => throw cancellation))
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT " + SqliteCqlCheckedInt64Average.Function + "(1)";
            command.ExecuteScalar();
            Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(owner.Check));
        }
        using var missing = connection.CreateCommand();
        missing.CommandText = "SELECT " + SqliteCqlCheckedInt64Average.Function + "(1)";
        Assert.Throws<SqliteException>(() => missing.ExecuteScalar());
    }
}
