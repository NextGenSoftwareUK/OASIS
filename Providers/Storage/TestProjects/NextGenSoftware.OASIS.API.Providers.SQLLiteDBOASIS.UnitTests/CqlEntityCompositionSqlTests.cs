using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

[TestClass]
public sealed partial class CqlEntityCompositionSqlTests
{
    private static object Compile(CosmicEntityCompositionQuery<Holon> query)
    {
        var capabilities = new CosmicEntityCompositionCapabilities(
            CosmicEntityStageFeatures.Filter | CosmicEntityStageFeatures.Ordering | CosmicEntityStageFeatures.Paging | CosmicEntityStageFeatures.Reverse,
            SQLLiteDBOASIS.CqlCapabilities);
        var planned = CosmicEntityCompositionPlanner.Plan(query, ProviderType.SQLLiteDBOASIS,
            new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, SQLLiteDBOASIS.CqlCapabilities), capabilities);
        Assert.IsFalse(planned.IsError, planned.Message);
        return typeof(HolonRepository).GetMethod("CompileEntityCompositionSql", BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(Holon)).Invoke(null, new object[] { planned.Result })!;
    }
    private static object Field(object value, string name) => value.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;
    private static async Task<string[]> Read(SqliteConnection connection, object compiled)
    {
        await using var command = connection.CreateCommand(); command.CommandText = (string)Field(compiled, "Rows");
        foreach (var parameter in (List<SqliteParameter>)Field(compiled, "Parameters"))
            command.Parameters.Add(new SqliteParameter(parameter.ParameterName, parameter.Value));
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) names.Add(reader.GetString(reader.GetOrdinal("Name")));
        return names.ToArray();
    }
    private static async Task<SqliteConnection> Seed()
    {
        var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE Holon(Id TEXT,Name TEXT,Version INTEGER);"
            + "INSERT INTO Holon VALUES ('1','C',1),('2','B',1),('3','A',2),('4','D',2);";
        await command.ExecuteNonQueryAsync(); return connection;
    }
    [TestMethod]
    public async Task NativeEntityFiltersAndOrderingRemainAfterSourceAndLaterWindows()
    {
        await using var connection = await Seed();
        var source = CosmicQuery<Holon>.Create().OrderBy(row => row.Id).Result;
        var firstTwo = source.Take(2).Result.Compose().Result.Where(row => row.Name == "A").Result;
        CollectionAssert.AreEqual(Array.Empty<string>(), await Read(connection, Compile(firstTwo)));
        var ordered = source.Take(3).Result.Compose().Result.OrderBy(row => row.Name).Result.Take(1).Result;
        CollectionAssert.AreEqual(new[] { "A" }, await Read(connection, Compile(ordered)));
        var laterFilter = ordered.Where(row => row.Name == "C").Result;
        CollectionAssert.AreEqual(Array.Empty<string>(), await Read(connection, Compile(laterFilter)));
    }
    [TestMethod]
    public async Task NativeEntityReorderingAndThenByPreserveEveryPagingPosition()
    {
        await using var connection = await Seed();
        var source = CosmicQuery<Holon>.Create().OrderBy(row => row.Id).Result.Take(4).Result;
        var query = source.Compose().Result.OrderByDescending(row => row.Version).Result.ThenBy(row => row.Name).Result
            .Skip(1).Result.Take(2).Result.OrderBy(row => row.Name).Result.Skip(1).Result;
        CollectionAssert.AreEqual(new[] { "D" }, await Read(connection, Compile(query)));
        var empty = query.Take(0).Result.OrderByDescending(row => row.Name).Result;
        CollectionAssert.AreEqual(Array.Empty<string>(), await Read(connection, Compile(empty)));
    }
    [TestMethod]
    public void NativeEntityValuesAreParametersAndEachStageRetainsWorkProof()
    {
        var attack = "x' OR 1=1 --";
        var source = CosmicQuery<Holon>.Create().Take(3).Result;
        var compiled = Compile(source.Compose().Result.Where(row => row.Name == attack).Result.Take(0).Result);
        Assert.IsFalse(((string)Field(compiled, "Rows")).Contains(attack));
        Assert.IsTrue(((List<SqliteParameter>)Field(compiled, "Parameters")).Any(value => Equals(value.Value, attack)));
        Assert.AreEqual(2, ((IReadOnlyList<string>)Field(compiled, "StageCounts")).Count);
        Assert.IsFalse((bool)Field(compiled, "HasRelationship"));
    }
}
