using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlSelectedIntegerTests
{
    private static CosmicProjectedNumericPlan<Holon, long, double> Plan()
    {
        var selected = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
            .Select(row => CosmicQueryMetadata.Value<long>(row, "value")).Take(2).Result;
        var result = CosmicProjectedNumericPlanner.Plan<Holon, long, double>(selected, 0, CosmicScalarOperation.Average,
            ProviderType.MongoDBOASIS, new[] { new CosmicProjectionCompositionTarget(ProviderType.MongoDBOASIS,
                MongoDBOASIS.CqlCapabilities, MongoDBOASIS.CqlProjectionCapabilities, MongoDBOASIS.CqlCompositionCapabilities) });
        Assert.IsFalse(result.IsError, result.Message);
        return result.Result;
    }

    private static MongoCqlCompositionCommand Compile(CosmicProjectedNumericPlan<Holon, long, double> plan)
        => new MongoCqlTranslator().CompileComposition(plan.Composition,
            new CosmicProjectionCompositionExecutionLimits(plan.Composition.Options.MaximumInputRows,
                plan.Composition.Options.MaximumInputPayloadBytes, plan.Composition.Options.MaximumStageRows,
                plan.Composition.Options.MaximumProofPayloadBytes, 10000, CosmicProjectionResults.MaximumPayloadLimit));

    private static BsonDocument Empty(MongoCqlCompositionCommand command)
        => command.Numeric.Last()["$project"]["packet"]["$ifNull"].AsBsonArray[1]["$literal"].AsBsonDocument;

    [TestMethod]
    public void CheckedPrefixesUseTheFinalSelectedRank()
    {
        var plan = Plan();
        var command = Compile(plan);
        Assert.AreSame(plan.Composition.NumericReduction, command.NumericIntent);
        var steps = command.Numeric.Single(step => step.Contains("$facet"))["$facet"]["numeric"].AsBsonArray;
        var sort = steps.Select(step => step.AsBsonDocument).Single(step => step.Contains("$setWindowFields"))
            ["$setWindowFields"]["sortBy"].AsBsonDocument;
        Assert.AreEqual(1, sort.ElementCount);
        Assert.AreEqual(1, sort["rank"].AsInt32);
    }

    [TestMethod]
    public void EmptySequenceHasAnExplicitNativeAcknowledgement()
    {
        var plan = Plan();
        var command = Compile(plan);
        var packet = HolonRepository.ReadNativeCqlSelectedNumeric(new BsonDocument("packet", Empty(command)), command.NumericIntent);
        Assert.AreEqual(0L, packet.ParticipatingCount);
        CollectionAssert.AreEqual(new[] { 0L, 0L }, packet.Parts.ToArray());
        Assert.AreEqual(false, packet.PrefixOverflow);
        Assert.AreEqual("CQL_EMPTY_SEQUENCE", CosmicProjectedNumericResults.FromPacket(plan.Scalar, packet).ErrorCode);
    }

    [TestMethod]
    public void ExactEnvelopeAndCheckedPrefixSchemaCannotBeWeakened()
    {
        var command = Compile(Plan());
        var body = (BsonDocument)Empty(command).DeepClone();
        body["inputs"] = 1L;
        body["present"] = 1L;
        body["low"] = new BsonDecimal128(new Decimal128(1));
        var envelope = new BsonDocument("packet", body);
        Assert.AreEqual(1L, HolonRepository.ReadNativeCqlSelectedNumeric(envelope, command.NumericIntent).Parts[1]);
        envelope["extra"] = 0;
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlSelectedNumeric(envelope, command.NumericIntent));
        envelope.Remove("extra");
        body["prefixOverflow"] = 0;
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlSelectedNumeric(envelope, command.NumericIntent));
        body.Remove("prefixOverflow");
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ReadNativeCqlSelectedNumeric(envelope, command.NumericIntent));
    }

    [TestMethod]
    public void LegacyCompositionPermissionDoesNotEnrollNumericReduction()
    {
        var plan = Plan();
        var current = MongoDBOASIS.CqlCompositionCapabilities;
        var legacy = new CosmicProjectionCompositionCapabilities(current.Stages, current.PredicateFeatures,
            current.FilterDomains, current.OrderingDomains, current.MaximumStages, current.SupportsAll, current.SupportsCardinality);
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", CosmicProjectionCompositionPlanner.Admit(plan.Composition,
            ProviderType.MongoDBOASIS, MongoDBOASIS.CqlCapabilities, MongoDBOASIS.CqlProjectionCapabilities, legacy).ErrorCode);
    }
}
