using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.UnitTests;

[TestClass]
public class PostgreSqlCqlTests
{
    private static CosmicQuery<Holon> Query(Guid[] ids)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.PostgreSQLOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(
                Guid.NewGuid(), ProviderType.PostgreSQLOASIS, ids).Result).Result;

    [TestMethod]
    public void CapturedBusinessKeysCannotEscapeOrMutateAuthorityCatalogue()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var expected = ids[0];
        var selected = new[] { ids[0], ids[2] };
        var query = Query(ids.Take(2).ToArray()).Where(h => selected.Contains(h.Id)).Result;
        selected[0] = Guid.NewGuid();
        ids[0] = Guid.NewGuid();

        var result = PostgreSqlCqlCompiler.Compile(query, ProviderType.PostgreSQLOASIS);
        Assert.IsFalse(result.IsError, result.Message);
        CollectionAssert.AreEqual(new[] { expected }, result.Result.ToArray());
        Assert.IsTrue(PostgreSqlCqlCompiler.Compile(query, ProviderType.PolygonOASIS).IsError);
        Assert.AreEqual(0, PostgreSqlCqlCompiler.Compile(Query(Array.Empty<Guid>()), ProviderType.PostgreSQLOASIS).Result.Count);
    }

    [DataTestMethod]
    [DataRow("name")]
    [DataRow("metadata")]
    [DataRow("order")]
    [DataRow("graph")]
    [DataRow("bound")]
    public void UnsupportedShapesRejectInsteadOfScanningNativeRecords(string shape)
    {
        var query = Query(Enumerable.Range(0, shape == "bound" ? 101 : 3).Select(_ => Guid.NewGuid()).ToArray());
        query = shape switch
        {
            "name" => query.Where(h => h.Name == "coffee").Result,
            "metadata" => query.Where(h => CosmicQueryMetadata.Value<string>(h, "category") == "coffee").Result,
            "order" => query.OrderBy(h => h.Id).Result,
            "graph" => query.WithChildren(CosmicQueryGraphOptions.Create(maxChildDepth: 1).Result).Result,
            _ => query
        };

        var result = PostgreSqlCqlCompiler.Compile(query, ProviderType.PostgreSQLOASIS);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task AdapterRejectsUnsupportedQueryBeforeActivationAndHonoursPreCancellation()
    {
        var provider = new PostgreSQLOASIS("Host=127.0.0.1;Port=1;Database=synthetic;Username=synthetic;Timeout=1");
        var query = Query(new[] { Guid.NewGuid() });
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.PostgreSQLOASIS,
            new[] { new CosmicQueryTarget(ProviderType.PostgreSQLOASIS, provider.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);

        var inactive = await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", inactive.ErrorCode);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var stopped = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: cancelled.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", stopped.ErrorCode);
        Assert.AreEqual(typeof(OperationCanceledException), stopped.Exception.GetType());
        Assert.IsFalse(provider.IsProviderActivated);
    }
    [TestMethod]
    public void DerivedAliasesKeepTheirOwnIdentityAndCannotInheritPostgreSqlCertification()
    {
        var provider = new PostgreSQLOASIS("Host=127.0.0.1;Port=1;Database=synthetic;Username=synthetic;Timeout=1");
        provider.ProviderType.Value = ProviderType.CockroachDBOASIS;
        Assert.AreEqual(ProviderType.CockroachDBOASIS, provider.QueryProviderType);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CockroachDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.CockroachDBOASIS,
                new[] { Guid.NewGuid() }).Result).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.CockroachDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.CockroachDBOASIS, provider.QueryCapabilities) });
        Assert.IsTrue(plan.IsError, "An unadmitted alias cannot expose PostgreSQL query capabilities.");
    }

    [TestMethod]
    public void NativeContentPredicatesKeepValuesAndJsonPathsOutOfSqlText()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var text = "coffee%_' OR TRUE --";
        var key = "category}'; DROP TABLE oasis_holons;--";
        var query = Query(ids).Where(h => (h.Name == text || h.Description.Contains(text)) &&
            CosmicQueryMetadata.Value<string>(h, key) == text).Result.Skip(1).Result.Take(1).Result;
        var compiled = PostgreSqlCqlCompiler.CompileNative(query, ProviderType.PostgreSQLOASIS);
        Assert.IsFalse(compiled.IsError, compiled.Message);
        using var command = compiled.Result;
        StringAssert.Contains(command.CommandText, "id=ANY(@ids)");
        StringAssert.Contains(command.CommandText, "strpos(");
        StringAssert.Contains(command.CommandText, "jsonb_typeof(");
        Assert.IsFalse(command.CommandText.Contains(text));
        Assert.IsFalse(command.CommandText.Contains(key));
        CollectionAssert.AreEquivalent(ids, (Guid[])command.Parameters["ids"].Value);
        Assert.AreEqual(1, command.Parameters["skip"].Value);
        Assert.AreEqual(1, command.Parameters["take"].Value);
        Assert.IsTrue(command.Parameters.Cast<Npgsql.NpgsqlParameter>().Any(parameter =>
            parameter.Value is string[] path && path.SequenceEqual(new[] { "MetaData", key })));
        Assert.AreEqual(3, command.Parameters.Cast<Npgsql.NpgsqlParameter>().Count(parameter => Equals(parameter.Value, text)));
    }

    [DataTestMethod]
    [DataRow("numeric")]
    [DataRow("starts-with")]
    [DataRow("order")]
    [DataRow("null")]
    [DataRow("nul-value")]
    public void ContentAdmissionRejectsUnprovedSemanticsBeforeOpeningAConnection(string shape)
    {
        var query = Query(new[] { Guid.NewGuid() });
        query = shape switch {
            "numeric" => query.Where(h => CosmicQueryMetadata.Value<int>(h, "score") > 1).Result,
            "starts-with" => query.Where(h => h.Name.StartsWith("C")).Result,
            "order" => query.OrderBy(h => h.Name).Result,
            "null" => query.Where(h => h.Name == null).Result,
            _ => query.Where(h => h.Name == "invalid\0value").Result
        };
        var compiled = PostgreSqlCqlCompiler.CompileNative(query, ProviderType.PostgreSQLOASIS);
        Assert.IsTrue(compiled.IsError);
        Assert.IsNull(compiled.Result);
    }

    [TestMethod]
    public void ContentCannotEscapeItsAuthorityThroughABusinessOr()
    {
        var allowed = Guid.NewGuid();
        var forbidden = Guid.NewGuid();
        var query = Query(new[] { allowed }).Where(h => h.Id == forbidden || h.Name.Contains("coffee")).Result;
        var compiled = PostgreSqlCqlCompiler.CompileNative(query, ProviderType.PostgreSQLOASIS);
        Assert.IsFalse(compiled.IsError, compiled.Message);
        using var command = compiled.Result;
        CollectionAssert.AreEqual(new[] { allowed }, (Guid[])command.Parameters["ids"].Value);
        StringAssert.Contains(command.CommandText, "AND (");
        StringAssert.Contains(command.CommandText, " OR ");
    }

    [TestMethod]
    public void CompatibleProductsDoNotInheritNewContentCapabilities()
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TimescaleDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.TimescaleDBOASIS,
                new[] { Guid.NewGuid() }).Result).Result;
        var compiled = PostgreSqlCqlCompiler.CompileNative(query, ProviderType.TimescaleDBOASIS, ProviderType.TimescaleDBOASIS);
        Assert.IsFalse(compiled.IsError, compiled.Message);
        compiled.Result.Dispose();
        Assert.IsTrue(PostgreSqlCqlCompiler.CompileNative(query.Where(h => h.Name == "coffee").Result,
            ProviderType.TimescaleDBOASIS, ProviderType.TimescaleDBOASIS).IsError);
    }

}
