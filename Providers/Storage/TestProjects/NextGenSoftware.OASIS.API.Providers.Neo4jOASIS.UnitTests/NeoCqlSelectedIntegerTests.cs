using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlSelectedIntegerTests
{
    private static CosmicProjectedNumericPlan<Holon, long, double> Plan()
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.Neo4jOASIS).Result)
            .Select(row => CosmicQueryMetadata.Value<long>(row, "value")).Take(2).Result;
        var plan = CosmicProjectedNumericPlanner.Plan<Holon, long, double>(query, 0, CosmicScalarOperation.Average,
            ProviderType.Neo4jOASIS, new[] { new CosmicProjectionCompositionTarget(ProviderType.Neo4jOASIS,
                Neo4jOASIS.CqlCapabilities, Neo4jOASIS.CqlProjectionCapabilities, Neo4jOASIS.CqlCompositionCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }

    [TestMethod]
    public void ReductionUsesCapturedSelectedPositionsAndCheckedPrefixes()
    {
        var plan = Plan();
        var options = plan.Composition.Options;
        var command = Neo4jOASIS.CompileCqlComposition(plan.Composition, new CosmicProjectionCompositionExecutionLimits(
            options.MaximumInputRows, options.MaximumInputPayloadBytes, options.MaximumStageRows,
            options.MaximumProofPayloadBytes, 10000, CosmicProjectionResults.MaximumPayloadLimit));
        var reduction = command.Text.Substring(command.Text.LastIndexOf("UNION ALL WITH", System.StringComparison.Ordinal));
        StringAssert.Contains(reduction, "position IN s1");
        StringAssert.Contains(reduction, "input[position].values[0].v");
        StringAssert.Contains(reduction, "CASE WHEN prefix.overflow THEN prefix");
        StringAssert.Contains(reduction, "work+size(s1)");
        Assert.IsFalse(reduction.Contains("MATCH", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void EmptyNativeWordsRetainTheRequiredMeanError()
    {
        var plan = Plan();
        var packet = Neo4jOASIS.ReadSelectedIntegerReduction(new Dictionary<string, object>
            { ["high"] = 0L, ["low"] = 0L, ["prefixOverflow"] = false }, 0, plan.Composition.NumericReduction);
        Assert.AreEqual(0L, packet.ParticipatingCount);
        Assert.AreEqual(false, packet.PrefixOverflow);
        Assert.AreEqual("CQL_EMPTY_SEQUENCE", CosmicProjectedNumericResults.FromPacket(plan.Scalar, packet).ErrorCode);
    }

    [TestMethod]
    public void FloatingWordsNumericFlagsAndExtraFieldsCannotImpersonateNativeEvidence()
    {
        var intent = Plan().Composition.NumericReduction;
        var packet = new Dictionary<string, object> { ["high"] = 0L, ["low"] = 1L, ["prefixOverflow"] = false };
        Assert.AreEqual(1L, Neo4jOASIS.ReadSelectedIntegerReduction(packet, 1, intent).Parts[1]);
        packet["low"] = 1d;
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.ReadSelectedIntegerReduction(packet, 1, intent));
        packet["low"] = 1L;
        packet["prefixOverflow"] = 0L;
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.ReadSelectedIntegerReduction(packet, 1, intent));
        packet["prefixOverflow"] = false;
        packet["extra"] = 0L;
        Assert.ThrowsException<InvalidDataException>(() => Neo4jOASIS.ReadSelectedIntegerReduction(packet, 1, intent));
    }

    [TestMethod]
    public void OrdinaryCompositionPermissionDoesNotEnrollSelectedArithmetic()
    {
        var plan = Plan();
        var current = Neo4jOASIS.CqlCompositionCapabilities;
        var legacy = new CosmicProjectionCompositionCapabilities(current.Stages, current.PredicateFeatures,
            current.FilterDomains, current.OrderingDomains, current.MaximumStages, current.SupportsAll, current.SupportsCardinality);
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", CosmicProjectionCompositionPlanner.Admit(plan.Composition,
            ProviderType.Neo4jOASIS, Neo4jOASIS.CqlCapabilities, Neo4jOASIS.CqlProjectionCapabilities, legacy).ErrorCode);
    }
}
