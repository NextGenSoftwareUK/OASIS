using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllNativeTests
{
    [Theory]
    [InlineData(false, false, 0)] [InlineData(false, false, 1)] [InlineData(false, false, 2)]
    [InlineData(false, true, 0)] [InlineData(false, true, 1)] [InlineData(false, true, 2)]
    [InlineData(true, false, 0)] [InlineData(true, false, 1)] [InlineData(true, false, 2)]
    [InlineData(true, true, 0)] [InlineData(true, true, 1)] [InlineData(true, true, 2)]
    public async Task ComposedSourceErrorsSurviveBooleanTruthAndEmptyWindows(bool nested, bool conjunction, int window)
    {
        using var context = new DataContext("Data Source=:memory:");
        await context.Database.OpenConnectionAsync();
        await Sql(context, "CREATE TABLE Holon(Id TEXT,Name TEXT,Version INTEGER,ParentHolonId TEXT,PersistentValueJson TEXT,IsPublic INTEGER,CreatedByAvatarId TEXT);"
            + "INSERT INTO Holon VALUES ('00000000-0000-0000-0000-000000000001','root',1,NULL,NULL,1,NULL),"
            + "('00000000-0000-0000-0000-000000000002','child',1,'00000000-0000-0000-0000-000000000001',NULL,1,NULL),"
            + "('00000000-0000-0000-0000-000000000003','leaf',1,'00000000-0000-0000-0000-000000000002',NULL,1,NULL),"
            + "('00000000-0000-0000-0000-000000000004','leaf',1,'00000000-0000-0000-0000-000000000002',NULL,1,NULL);");
        var native = new Native(context);
        var id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var source = Source.Where(row => row.Id == id).Result;
        var bounds = CosmicQueryTraversalOptions.Create(3, 1).Result;
        var captured = nested
            ? conjunction
                ? source.WhereAnyChild(child => child.Name == "never" && CosmicQueryRelations.AnyDescendant(child, leaf => true, bounds))
                : source.WhereAnyChild(child => child.Name == "child" || CosmicQueryRelations.AnyDescendant(child, leaf => true, bounds))
            : conjunction
                ? source.Where(row => row.Name == "never" && CosmicQueryRelations.AnyDescendant(row, leaf => true, bounds))
                : source.Where(row => row.Name == "root" || CosmicQueryRelations.AnyDescendant(row, leaf => true, bounds));
        Assert.False(captured.IsError, captured.Message);
        var query = captured.Result;
        if (window == 2) query = query.Take(0).Result;
        var selected = query.Select(row => row.Version);
        if (window == 1) selected = selected.Take(0);
        var all = selected.All(value => true).Result;
        var result = await CosmicProjectedAllExecutor.ExecuteAsync(Plan(native, all), new[] { native });
        Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode);
        Assert.Null(result.Result);
        Assert.Null(context.Database.CurrentTransaction);
        // The same source compiler must protect cardinality consumers too.
        var count = CosmicScalarPlanner.Plan(query.LongCount().Result, native.QueryProviderType,
            new[] { new CosmicScalarTarget(native.QueryProviderType, native.QueryCapabilities, SQLLiteDBOASIS.CqlScalarCapabilities) });
        Assert.False(count.IsError, count.Message);
        var counted = await new HolonRepository(context, () => false).ExecuteCosmicScalarAsync(count.Result);
        Assert.Equal("CQL_QUERY_LIMIT", counted.ErrorCode);
        Assert.False(counted.IsLoaded);
        Assert.Null(context.Database.CurrentTransaction);
        // Empty entity/projection pages use the same source validation owner.
        var repository = new HolonRepository(context, () => false);
        var empty = query.Take(0).Result;
        var rows = CosmicQueryPlanner.Plan(empty, native.QueryProviderType,
            new[] { new CosmicQueryTarget(native.QueryProviderType, native.QueryCapabilities) });
        Assert.False(rows.IsError, rows.Message);
        var loaded = await repository.ExecuteCosmicQueryAsync(rows.Result, 100, default);
        Assert.Equal("CQL_QUERY_LIMIT", loaded.ErrorCode);
        var projection = CosmicProjectionPlanner.Plan(empty.Select(row => row.Version).Result, native.QueryProviderType,
            new[] { new CosmicProjectionTarget(native.QueryProviderType, native.QueryCapabilities, native.ProjectionCapabilities) });
        Assert.False(projection.IsError, projection.Message);
        var projected = await repository.ExecuteCosmicProjectionAsync(projection.Result, 100, default);
        Assert.Equal("CQL_QUERY_LIMIT", projected.ErrorCode);
        Assert.Null(context.Database.CurrentTransaction);
        // A larger legal scope must also return the correct nonempty result.
        var sufficient = CosmicQueryTraversalOptions.Create(3, 10).Result;
        var valid = source.WhereAnyChild(child => CosmicQueryRelations.AnyDescendant(child, leaf => leaf.Name == "leaf", sufficient));
        Assert.False(valid.IsError, valid.Message);
        Assert.True(await Read(native, valid.Select(row => row.Version).All(value => value == 1).Result));
        // A failed protected scope must release its native callbacks and owner.
        Assert.True(await Read(native, source.Select(row => row.Version).All(value => value == 1).Result));
    }
}
