using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

[TestClass]
public sealed class CqlCompositionSqlTests
{
    private static CosmicProjectionCompositionExecutionLimits Limits(int rows = 10) => new(rows, 10000, 100, 10000, 100, 10000);
    private static CosmicProjectionCompositionPlan<Holon, TResult> Plan<TResult>(CosmicProjectionQuery<Holon, TResult> query)
    {
        var types = query.Columns.Select(column => column.Path.ValueType).Distinct().ToArray();
        var filters = types.Select(type => new CosmicQueryMetadataCapability(type, Enum.GetValues<CosmicQueryComparisonOperator>(),
            type == typeof(string) ? Enum.GetValues<CosmicQueryStringOperator>() : null, supportsMembership: true, supportsMixedNumericComparison: type == typeof(int))).ToArray();
        var features = CosmicQueryFeatures.And | CosmicQueryFeatures.Or | CosmicQueryFeatures.Not | CosmicQueryFeatures.BooleanConstant
            | CosmicQueryFeatures.NullLiteral | CosmicQueryFeatures.StringContains | CosmicQueryFeatures.StringStartsWith
            | CosmicQueryFeatures.StringEndsWith | CosmicQueryFeatures.Membership | CosmicQueryFeatures.FieldComparison | CosmicQueryFeatures.MixedNumericComparison;
        var profile = new CosmicProjectionCompositionCapabilities(CosmicSelectedStageFeatures.Filter | CosmicSelectedStageFeatures.Ordering | CosmicSelectedStageFeatures.Paging,
            features, filters, types);
        var plan = CosmicProjectionCompositionPlanner.Plan(query, ProviderType.SQLLiteDBOASIS,
            new[] { new CosmicProjectionCompositionTarget(ProviderType.SQLLiteDBOASIS, SQLLiteDBOASIS.CqlCapabilities, SQLLiteDBOASIS.CqlProjectionCapabilities, profile) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }
    private static object Compile<TResult>(CosmicProjectionQuery<Holon, TResult> query, CosmicProjectionCompositionExecutionLimits? limits = null)
        => typeof(HolonRepository).GetMethod("CompileCompositionSql", BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(Holon), typeof(TResult)).Invoke(null, new object[] { Plan(query), limits ?? Limits() })!;
    private static object Field(object compiled, string name) => compiled.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(compiled)!;
    private static IDisposable Budget(SqliteConnection connection)
        => (IDisposable)Activator.CreateInstance(typeof(HolonRepository).Assembly.GetType(
            "NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories.SqliteCqlReadBudget")!,
            BindingFlags.Instance | BindingFlags.NonPublic, null, new object?[] { connection, CancellationToken.None, 50000000L, null }, null)!;

    private static async Task<List<long[]>> Read(SqliteConnection connection, object compiled)
    {
        using var budget = Budget(connection);
        await using var command = connection.CreateCommand(); command.CommandText = (string)Field(compiled, "Proof");
        foreach (var parameter in (List<SqliteParameter>)Field(compiled, "Parameters")) command.Parameters.Add(new SqliteParameter(parameter.ParameterName, parameter.Value));
        var rows = new List<long[]>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) rows.Add(Enumerable.Range(0, 5).Select(reader.GetInt64).ToArray());
        return rows;
    }
    private static async Task Seed(SqliteConnection connection)
    {
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Holon(Id TEXT,Name TEXT,Description TEXT,Version INTEGER,IsPublic INTEGER,CreatedByAvatarId TEXT);
            INSERT INTO Holon VALUES
            ('00000000-0000-0000-0000-000000000001','C','root',2,1,NULL),
            ('00000000-0000-0000-0000-000000000002','A','root',1,1,NULL),
            ('00000000-0000-0000-0000-000000000003','B','root',2,1,NULL);
            """;
        await command.ExecuteNonQueryAsync();
    }

    [TestMethod]
    public async Task NativePipelinePreservesIndependentSourceWindowAndEveryResultStage()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await Seed(connection);
        var query = CosmicQuery<Holon>.Create().Take(3).Select(h => new { Label = h.Name, Revision = h.Version })
            .Where(row => row.Revision >= 2).OrderBy(row => row.Label).Take(1).Result;
        var rows = await Read(connection, Compile(query));
        CollectionAssert.AreEqual(new long[] { -1, 0, 0, 3, 7 }, rows[0]);
        CollectionAssert.AreEqual(new long[] { 1, 3 }, rows.Where(row => row[0] == 0).Select(row => row[1]).ToArray());
        CollectionAssert.AreEqual(new long[] { 3, 1 }, rows.Where(row => row[0] == 1).Select(row => row[1]).ToArray());
        CollectionAssert.AreEqual(new long[] { 3 }, rows.Where(row => row[0] == 2).Select(row => row[1]).ToArray());
    }

    [TestMethod]
    public async Task FilteringAfterTakeCannotMoveBeforeEitherWindow()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await Seed(connection);
        var sourcePage = CosmicQuery<Holon>.Create().OrderBy(h => h.Version).Take(1).Select(h => h.Name).Where(name => name == "B").Result;
        var first = await Read(connection, Compile(sourcePage));
        Assert.AreEqual(1L, first[0][3]); Assert.AreEqual(1L, first[0][4]); Assert.AreEqual(1, first.Count);
        var selectedPage = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(1).Where(name => name == "B").Result;
        var second = await Read(connection, Compile(selectedPage));
        Assert.AreEqual(3L, second[0][3]); Assert.AreEqual(4L, second[0][4]);
        CollectionAssert.AreEqual(new long[] { 1 }, second.Where(row => row[0] == 0).Select(row => row[1]).ToArray());
        Assert.AreEqual(0, second.Count(row => row[0] == 1));
    }

    [TestMethod]
    public async Task LaterOrderingUsesPreviousRankForEqualValues()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await Seed(connection);
        var query = CosmicQuery<Holon>.Create().Select(h => new { h.Name, h.Version }).OrderBy(row => row.Name).OrderBy(row => row.Version).Result;
        var rows = await Read(connection, Compile(query));
        CollectionAssert.AreEqual(new long[] { 2, 3, 1 }, rows.Where(row => row[0] == 1).Select(row => row[1]).ToArray());
    }

    [TestMethod]
    public async Task ThenByDescendingIsOneNativeOrderingStage()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await Seed(connection);
        var query = CosmicQuery<Holon>.Create().Select(h => new { h.Name, h.Version }).OrderBy(row => row.Version).ThenByDescending(row => row.Name).Result;
        var rows = await Read(connection, Compile(query));
        Assert.AreEqual(3L, rows[0][4]);
        CollectionAssert.AreEqual(new long[] { 2, 1, 3 }, rows.Skip(1).Select(row => row[1]).ToArray());
    }

    [DataTestMethod]
    [DataRow(0, 1)] [DataRow(1, 2)]
    public async Task RemainingInputLimitRetainsARealOverflowDetectionRow(int remaining, int expected)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await Seed(connection);
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(0).Result;
        var rows = await Read(connection, Compile(query, Limits(remaining)));
        Assert.AreEqual((long)expected, rows[0][3]); Assert.AreEqual((long)expected, rows[0][4]); Assert.AreEqual(1, rows.Count);
    }

    [TestMethod]
    public async Task EmptyStageStillReportsEarlierParticipationAndAllLaterStageProofs()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await Seed(connection);
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(0).OrderBy(value => value).Skip(1).Where(value => value == "B").Result;
        var rows = await Read(connection, Compile(query));
        CollectionAssert.AreEqual(new long[] { -1, 0, 0, 3, 3 }, rows.Single());
    }

    [TestMethod]
    public async Task RuntimeAliasIsBoundByColumnPositionRatherThanPastedIntoSql()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await Seed(connection);
        const string alias = "label\"; DROP TABLE Holon; --";
        var query = CosmicQuery<Holon>.Create().SelectColumns(new[] { CosmicProjectionSelection<Holon>.Create(alias, h => h.Name).Result })
            .Where(row => (string)row["label\"; DROP TABLE Holon; --"] == "B").Result;
        var compiled = Compile(query); Assert.IsFalse(((string)Field(compiled, "Proof")).Contains(alias, StringComparison.Ordinal));
        var rows = await Read(connection, compiled); Assert.AreEqual(3L, rows.Single(row => row[0] == 0)[1]);
        await using var count = connection.CreateCommand(); count.CommandText = "SELECT count(*) FROM Holon";
        Assert.AreEqual(3L, await count.ExecuteScalarAsync());
    }

    [TestMethod]
    public void ParametersAreSharedAcrossSourceAndAllSelectedFilters()
    {
        var names = Enumerable.Range(0, 100).Select(index => "name" + index).ToArray();
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name);
        for (var index = 0; index < 10; index++) query = query.Where(value => names.Contains(value));
        var error = Assert.ThrowsException<TargetInvocationException>(() => Compile(query.Result));
        StringAssert.Contains(error.InnerException!.Message.ToLowerInvariant(), "parameter");
    }
}
