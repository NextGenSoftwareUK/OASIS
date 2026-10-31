using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS.UnitTests;

[TestClass]
public class SqlServerCqlTests
{
    private static CosmicQuery<Holon> Query(Guid[] ids)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SQLServerDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(
                Guid.NewGuid(), ProviderType.SQLServerDBOASIS, ids).Result).Result;

    [TestMethod]
    public void CapturedBusinessKeysCannotEscapeOrMutateAuthorityCatalogue()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var expected = ids[0];
        var selected = new[] { ids[0], ids[2] };
        var query = Query(ids.Take(2).ToArray()).Where(h => selected.Contains(h.Id)).Result;
        selected[0] = Guid.NewGuid();
        ids[0] = Guid.NewGuid();

        var result = SqlServerCqlCompiler.Compile(query, ProviderType.SQLServerDBOASIS);
        Assert.IsFalse(result.IsError, result.Message);
        CollectionAssert.AreEqual(new[] { expected }, result.Result.ToArray());
        Assert.IsTrue(SqlServerCqlCompiler.Compile(query, ProviderType.PolygonOASIS).IsError);
        Assert.AreEqual(0, SqlServerCqlCompiler.Compile(Query(Array.Empty<Guid>()), ProviderType.SQLServerDBOASIS).Result.Count);
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

        var result = SqlServerCqlCompiler.Compile(query, ProviderType.SQLServerDBOASIS);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task AdapterRejectsUnsupportedQueryBeforeActivationAndHonoursPreCancellation()
    {
        var provider = new SQLServerDBOASIS("Server=127.0.0.1,1;Database=synthetic;Integrated Security=true;Connect Timeout=1");
        var query = Query(new[] { Guid.NewGuid() });
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.SQLServerDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SQLServerDBOASIS, provider.QueryCapabilities) });
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
    public void DerivedAliasesKeepTheirOwnIdentityAndCannotInheritSqlServerCertification()
    {
        var provider = new SQLServerDBOASIS("Server=127.0.0.1,1;Database=synthetic;Integrated Security=true;Connect Timeout=1");
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
    public async Task EmptyCatalogueNeedsNoNativeConnectionAndDeactivationClearsAdmission()
    {
        // This endpoint cannot serve SQL. Empty authorized selection must still
        // return an empty page, while provider activation remains mandatory.
        var provider = new SQLServerDBOASIS("Server=127.0.0.1,1;Database=synthetic;Integrated Security=true;Connect Timeout=1")
        { IsProviderActivated = true };
        var plan = CosmicQueryPlanner.Plan(Query(Array.Empty<Guid>()), ProviderType.SQLServerDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SQLServerDBOASIS, provider.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var empty = await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.IsFalse(empty.IsError, empty.Message);
        Assert.AreEqual(0, empty.Result.Count);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        var inactive = await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", inactive.ErrorCode);
    }

    [TestMethod]
    public void NativeContentUsesParameterizedObjectKeysAndOrdinalTextPredicates()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var text = "coffee%_' OR 1=1 --";
        var key = "category]'; DROP TABLE OASISHolons;--";
        var query = Query(ids).Where(h => (h.Name == text || h.Description.Contains(text)) &&
            CosmicQueryMetadata.Value<string>(h, key) == text).Result.Skip(1).Result.Take(1).Result;
        var compiled = SqlServerCqlCompiler.CompileNative(query, ProviderType.SQLServerDBOASIS);
        Assert.IsFalse(compiled.IsError, compiled.Message);
        using var command = compiled.Result.Command;
        Assert.AreEqual(2, compiled.Result.SourceCount);
        Assert.IsFalse(command.CommandText.Contains(text));
        Assert.IsFalse(command.CommandText.Contains(key));
        StringAssert.Contains(command.CommandText, "OPENJSON");
        StringAssert.Contains(command.CommandText, "[type]=5");
        StringAssert.Contains(command.CommandText, "[type]=1");
        StringAssert.Contains(command.CommandText, "CHARINDEX");
        StringAssert.Contains(command.CommandText, "DATALENGTH");
        StringAssert.Contains(command.CommandText, "Latin1_General_100_BIN2");
        Assert.AreEqual(3, command.Parameters.Cast<Microsoft.Data.SqlClient.SqlParameter>().Count(parameter => Equals(parameter.Value, text)));
        Assert.IsTrue(command.Parameters.Cast<Microsoft.Data.SqlClient.SqlParameter>().Any(parameter => Equals(parameter.Value, "metaData")));
        Assert.IsTrue(command.Parameters.Cast<Microsoft.Data.SqlClient.SqlParameter>().Any(parameter => Equals(parameter.Value, key)));
        CollectionAssert.AreEquivalent(ids, command.Parameters.Cast<Microsoft.Data.SqlClient.SqlParameter>()
            .Where(parameter => parameter.ParameterName.StartsWith("@id")).Select(parameter => (Guid)parameter.Value).ToArray());
    }

    [DataTestMethod]
    [DataRow("numeric")]
    [DataRow("order")]
    [DataRow("null")]
    [DataRow("nul-value")]
    public void UnprovedContentShapesRejectBeforeNativeExecution(string shape)
    {
        var query = Query(new[] { Guid.NewGuid() });
        query = shape switch {
            "numeric" => query.Where(h => CosmicQueryMetadata.Value<int>(h, "score") > 1).Result,
            "order" => query.OrderBy(h => h.Name).Result,
            "null" => query.Where(h => h.Name == null).Result,
            _ => query.Where(h => h.Name == "invalid\0value").Result
        };
        var compiled = SqlServerCqlCompiler.CompileNative(query, ProviderType.SQLServerDBOASIS);
        Assert.IsTrue(compiled.IsError);
        Assert.IsNull(compiled.Result);
    }

    [TestMethod]
    public void LongStringEqualityDoesNotUseJsonValueOrBoundedSqlParameters()
    {
        var value = new string('x', 4001);
        var compiled = SqlServerCqlCompiler.CompileNative(Query(new[] { Guid.NewGuid() })
            .Where(h => CosmicQueryMetadata.Value<string>(h, "long") == value).Result, ProviderType.SQLServerDBOASIS);
        Assert.IsFalse(compiled.IsError, compiled.Message);
        using var command = compiled.Result.Command;
        Assert.IsFalse(command.CommandText.Contains("JSON_VALUE"));
        var literal = command.Parameters.Cast<Microsoft.Data.SqlClient.SqlParameter>().Single(parameter => Equals(parameter.Value, value));
        Assert.AreEqual(-1, literal.Size);
        Assert.AreEqual(System.Data.SqlDbType.NVarChar, literal.SqlDbType);
    }

    [TestMethod]
    public void ContentOrDoesNotWidenTheServerCatalogue()
    {
        var allowed = Guid.NewGuid();
        var forbidden = Guid.NewGuid();
        var compiled = SqlServerCqlCompiler.CompileNative(Query(new[] { allowed })
            .Where(h => h.Id == forbidden || h.Name.Contains("coffee")).Result, ProviderType.SQLServerDBOASIS);
        Assert.IsFalse(compiled.IsError, compiled.Message);
        using var command = compiled.Result.Command;
        Assert.AreEqual(1, compiled.Result.SourceCount);
        Assert.AreEqual(allowed, command.Parameters["@id0"].Value);
        StringAssert.Contains(command.CommandText, "WHERE (Id IN (@id0)) AND IsDeleted=0 AND (");
    }

    [TestMethod]
    public void OversizedNeedlesRejectAtTheSharedCaptureBoundary()
    {
        var needle = new string('x', CosmicQueryExpression.MaximumStringLength + 1);
        var captured = Query(new[] { Guid.NewGuid() }).Where(h => h.Name.Contains(needle));
        Assert.IsTrue(captured.IsError);
        Assert.IsNull(captured.Result);
    }

}
