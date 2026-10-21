using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class CosmicQueryAdmissionTests
{
    public class NoIo : DispatchProxy
    {
        public int Calls;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        { Calls++; throw new InvalidOperationException("Native I/O must not run before CQL admission."); }
    }

    [DataTestMethod]
    [DataRow("scope")] [DataRow("limit")] [DataRow("cancel")] [DataRow("inactive")]
    public async Task RejectedScopeLimitsAndCancellationDoNotOpenNativeSession(string scenario)
    {
        var driver = DispatchProxy.Create<IDriver, NoIo>();
        var provider = new Neo4jOASIS("bolt://owned.invalid", "owned", "owned", () => driver)
            { IsProviderActivated = scenario != "inactive" };
        var query = CosmicQuery<Holon>.Create().Where(h => true).Result;
        var target = scenario == "scope" ? ProviderType.SQLLiteDBOASIS : ProviderType.Neo4jOASIS;
        var planned = CosmicQueryPlanner.Plan(query, target, new[] { new CosmicQueryTarget(target, Neo4jOASIS.CqlCapabilities) });
        Assert.IsFalse(planned.IsError, planned.Message);
        using var cancellation = new CancellationTokenSource();
        if (scenario == "cancel") cancellation.Cancel();
        var result = await provider.ExecuteCosmicQueryAsync(planned.Result, scenario == "limit" ? 0 : 100, cancellation.Token);
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual(scenario switch { "scope" => "CQL_PLAN_INVALID", "limit" => "CQL_QUERY_LIMIT",
            "cancel" => "CQL_QUERY_CANCELLED", _ => "CQL_PROVIDER_INACTIVE" }, result.ErrorCode);
        Assert.AreEqual(0, ((NoIo)driver).Calls);
    }
}
