using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

[TestClass]
public sealed class CqlJoinSqlTests
{
    [DataTestMethod]
    [DataRow(false, false, 4)]
    [DataRow(true, false, 5)]
    [DataRow(false, true, 0)]
    [DataRow(true, true, 1)]
    public async Task NativeMatchingPreservesFanoutAndNeverMatchesNull(bool outer, bool nullKey, int expected)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await Seed(connection);
        var scope = CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result;
        var left = CosmicQuery<Holon>.Create(scope).Where(h => h.Description == "left");
        var right = CosmicQuery<Holon>.Create(scope).Where(h => h.Description == "right");
        if (nullKey) left = left.Where(h => h.Name == null);
        var join = outer ? left.LeftJoin(right, h => h.Name, h => h.Name)
            : left.Join(right, h => h.Name, h => h.Name);
        var selected = join.Select((l, r) => new { l.Name, Present = r != null }).Result;
        var sql = Compile(selected, Limits());
        var pairs = await ReadPairs(connection, sql);
        Assert.AreEqual(expected, pairs.Count);
        Assert.AreEqual(outer ? 1 : 0, pairs.Count(pair => pair.Right == null));
        if (!nullKey) Assert.AreEqual(4, pairs.Count(pair => pair.Right != null));
    }

    [TestMethod]
    public async Task EachSourceWindowRunsBeforeMatching()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await Seed(connection);
        var scope = CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result;
        var left = CosmicQuery<Holon>.Create(scope).Where(h => h.Description == "left")
            .OrderBy(h => h.Version).Skip(1).Take(1);
        var right = CosmicQuery<Holon>.Create(scope).Where(h => h.Description == "right" && h.Name == "coffee")
            .OrderByDescending(h => h.Version).Take(1);
        var selected = left.Join(right, h => h.Name, h => h.Name)
            .Select((l, r) => new { Left = l.Version, Right = r.Version }).Result;
        var sql = Compile(selected, Limits());
        var pairs = await ReadPairs(connection, sql);
        Assert.AreEqual(1, pairs.Count);
        Assert.AreEqual(2L, pairs[0].Left);
        Assert.AreEqual(1L, pairs[0].Right);
    }

    [DataTestMethod]
    [DataRow(0, 1)]
    [DataRow(1, 2)]
    public async Task PairBudgetKeepsAnOverflowDetectionRow(int remaining, int expected)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await Seed(connection);
        var scope = CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result;
        var source = CosmicQuery<Holon>.Create(scope).Where(h => h.Name == "coffee");
        var selected = source.Join(source, h => h.Name, h => h.Name).Select((l, r) => l.Name).Result;
        var sql = Compile(selected, new CosmicJoinExecutionLimits(10, 10, remaining, 10, 10000, 10000, 10000));
        Assert.AreEqual(expected, (await ReadPairs(connection, sql)).Count);
    }

    private static CosmicJoinExecutionLimits Limits() => new(10, 10, 100, 100, 10000, 10000, 10000);

    private static object Compile<TResult>(CosmicJoinSelection<Holon, Holon, string, TResult> query, CosmicJoinExecutionLimits limits)
    {
        Assert.IsNotNull(query);
        return typeof(HolonRepository).GetMethod("CompileJoinSql", BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(Holon), typeof(Holon), typeof(string), typeof(TResult))
            .Invoke(null, new object[] { query, limits })!;
    }

    private static async Task<List<(long Left, long? Right)>> ReadPairs(SqliteConnection connection, object compiled)
    {
        // Install the production UTF-8 guards and native progress/length limits.
        var budgetType = typeof(HolonRepository).Assembly.GetType(
            "NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories.SqliteCqlReadBudget")!;
        using var budget = (IDisposable)Activator.CreateInstance(budgetType,
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object?[] { connection, CancellationToken.None, 50000000L, null }, null)!;
        object Field(string name) => compiled.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(compiled)!;
        await using var command = connection.CreateCommand();
        command.CommandText = (string)Field("Pairs");
        foreach (var parameter in (List<SqliteParameter>)Field("Parameters"))
            command.Parameters.Add(new SqliteParameter(parameter.ParameterName, parameter.Value));
        var rows = new List<(long, long?)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) rows.Add((reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetInt64(1)));
        return rows;
    }

    private static async Task Seed(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Holon(Id TEXT,Name TEXT,Description TEXT,Version INTEGER,IsPublic INTEGER,CreatedByAvatarId TEXT);
            INSERT INTO Holon VALUES
            ('00000000-0000-0000-0000-000000000001','coffee','left',1,1,NULL),
            ('00000000-0000-0000-0000-000000000002','coffee','left',2,1,NULL),
            ('00000000-0000-0000-0000-000000000003',NULL,'left',3,1,NULL),
            ('00000000-0000-0000-0000-000000000004','coffee','right',4,1,NULL),
            ('00000000-0000-0000-0000-000000000005','coffee','right',5,1,NULL),
            ('00000000-0000-0000-0000-000000000006',NULL,'right',6,1,NULL);
            """;
        await command.ExecuteNonQueryAsync();
    }
}
