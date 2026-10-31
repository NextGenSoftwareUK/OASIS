using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlPostWindowElementTests
{
    private static CosmicElementPlan<Holon> Plan(CosmicElementQuery<Holon> element)
    {
        var planned = CosmicElementPlanner.Plan(element, ProviderType.MongoDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.MongoDBOASIS, MongoDBOASIS.CqlCapabilities) });
        Assert.IsFalse(planned.IsError, planned.Message);
        return planned.Result;
    }

    [DataTestMethod]
    [DataRow(CosmicElementOperation.First)]
    [DataRow(CosmicElementOperation.Single)]
    [DataRow(CosmicElementOperation.Last)]
    public void CompleteTerminalErrorWindowPrecedesEveryElementLimit(CosmicElementOperation operation)
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
            .OrderBy(row => row.Name).Take(3).Result;
        var bounds = CosmicQueryTraversalOptions.Create(2, 1).Result;
        var element = operation == CosmicElementOperation.First
            ? source.First(row => row.Name == "first" || CosmicQueryRelations.AnyDescendant(row, child => true, bounds)).Result
            : operation == CosmicElementOperation.Single
            ? source.Single(row => row.Name == "first" || CosmicQueryRelations.AnyDescendant(row, child => true, bounds)).Result
            : source.Last(row => row.Name == "first" || CosmicQueryRelations.AnyDescendant(row, child => true, bounds)).Result;
        var plan = Plan(element);
        var command = new MongoCqlTranslator().CompilePostWindowElement(plan.Rows, operation == CosmicElementOperation.Single ? 2 : 1);
        Assert.IsTrue(command.RequiresMembership);
        Assert.IsTrue(command.HasTraversalEnvelope);
        var global = command.Pipeline.Single(stage => stage.Contains("$setWindowFields"))["$setWindowFields"]["output"].AsBsonDocument;
        CollectionAssert.AreEqual(new[] { "unbounded", "unbounded" }, global["invalid"]["window"]["documents"].AsBsonArray.Select(value => value.AsString).ToArray());
        var windowIndex = System.Array.FindIndex(command.Pipeline, stage => stage.Contains("$setWindowFields"));
        var lastLimit = System.Array.FindLastIndex(command.Pipeline, stage => stage.Contains("$limit"));
        Assert.IsTrue(windowIndex < lastLimit);
        Assert.AreEqual(operation == CosmicElementOperation.Single ? 2 : 1, command.Pipeline[lastLimit]["$limit"].AsInt32);
        var sort = command.Pipeline.Last(stage => stage.Contains("$sort"))["$sort"].AsBsonDocument;
        Assert.AreEqual(operation == CosmicElementOperation.Last ? -1 : 1, sort["row.HolonId"].AsInt32);
    }

    [TestMethod]
    public void EmptyPageKeepsSourceErrorEnvelopeAndOneCanonicalOuterOutcome()
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
            .WhereAnyDescendant(row => true, CosmicQueryTraversalOptions.Create(2, 1).Result).Take(0).Result;
        var command = new MongoCqlTranslator().CompilePostWindowElement(Plan(source.FirstOrDefault(row => true).Result).Rows, 1);
        Assert.IsTrue(command.HasTraversalEnvelope);
        Assert.AreEqual(0, command.FetchCount);
        Assert.IsTrue(command.Pipeline.Any(stage => stage.Contains("$facet")));
        StringAssert.Contains(command.Pipeline.Last().ToJson(), "$row.error");
        StringAssert.Contains(command.Pipeline.Last().ToJson(), "$row.row");
    }

    [TestMethod]
    public void OrdinaryPagingPermissionCannotEnrollPostWindowElements()
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result).Take(2).Result;
        var plan = Plan(source.First(row => true).Result);
        var current = MongoDBOASIS.CqlCapabilities;
        var legacy = new CosmicQueryCapabilities(current.Features & ~CosmicQueryFeatures.PostWindowElementPredicate,
            current.CanonicalFields.ToArray(), current.MetadataTypes.ToArray(), current.Traversal);
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", legacy.AdmitPlan(plan.Rows).ErrorCode);
    }
}
