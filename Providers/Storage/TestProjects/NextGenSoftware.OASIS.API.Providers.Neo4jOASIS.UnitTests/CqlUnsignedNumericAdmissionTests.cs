using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class CqlUnsignedNumericAdmissionTests
{
    [TestMethod]
    public void UnsignedReadSupportDoesNotEnrollUnimplementedNumericExecution()
    {
        var source = CosmicQuery<Holon>.Create();
        var sum = source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "counter"));
        var average = source.Average(h => CosmicQueryMetadata.Value<ulong>(h, "counter"));
        Assert.IsFalse(sum.IsError, sum.Message);
        Assert.IsFalse(average.IsError, average.Message);
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", Provider.CqlScalarCapabilities.Admit(sum.Result).ErrorCode);
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", Provider.CqlScalarCapabilities.Admit(average.Result).ErrorCode);
        Assert.IsFalse(Provider.CqlScalarCapabilities.Admit(source.Min(h => CosmicQueryMetadata.Value<ulong>(h, "counter")).Result).IsError);

        var grouped = source.GroupBy(h => h.Name).Select(g => g.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "counter")));
        Assert.IsFalse(grouped.IsError, grouped.Message);
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", Provider.CqlGroupedCapabilities.Admit(grouped.Result).ErrorCode);

        // A caller's optimistic profile cannot upgrade the actual adapter.
        var claimed = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, Provider.CqlProjectionCapabilities, new[] { typeof(ulong) })
        });
        var plan = CosmicScalarPlanner.Plan(sum.Result, ProviderType.Neo4jOASIS, new[]
        {
            new CosmicScalarTarget(ProviderType.Neo4jOASIS, Provider.CqlCapabilities, claimed)
        });
        Assert.IsFalse(plan.IsError, plan.Message);
        var actual = CosmicScalarPlanner.Admit(plan.Result, ProviderType.Neo4jOASIS, Provider.CqlCapabilities, Provider.CqlScalarCapabilities);
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", actual.ErrorCode);
    }
}
