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
    public void UnsignedNumericExecutionRequiresExplicitInputEnrollment()
    {
        var source = CosmicQuery<Holon>.Create();
        var sum = source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "counter"));
        var average = source.Average(h => CosmicQueryMetadata.Value<ulong>(h, "counter"));
        Assert.IsFalse(sum.IsError, sum.Message);
        Assert.IsFalse(average.IsError, average.Message);
        Assert.IsFalse(Provider.CqlScalarCapabilities.Admit(sum.Result).IsError);
        Assert.IsFalse(Provider.CqlScalarCapabilities.Admit(average.Result).IsError);
        Assert.IsFalse(Provider.CqlScalarCapabilities.Admit(source.Min(h => CosmicQueryMetadata.Value<ulong>(h, "counter")).Result).IsError);

        var grouped = source.GroupBy(h => h.Name).Select(g => g.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "counter")));
        Assert.IsFalse(grouped.IsError, grouped.Message);
        Assert.IsFalse(Provider.CqlGroupedCapabilities.Admit(grouped.Result).IsError);

        // Broad read permission alone still cannot enroll unsigned arithmetic.
        var legacySigned = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, Provider.CqlProjectionCapabilities)
        });
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", legacySigned.Admit(sum.Result).ErrorCode);

        // Actual-adapter readmission remains authoritative over a caller's profile.
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
        Assert.IsFalse(actual.IsError, actual.Message);
        var restricted = CosmicScalarPlanner.Admit(plan.Result, ProviderType.Neo4jOASIS, Provider.CqlCapabilities, legacySigned);
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", restricted.ErrorCode);
    }
}
