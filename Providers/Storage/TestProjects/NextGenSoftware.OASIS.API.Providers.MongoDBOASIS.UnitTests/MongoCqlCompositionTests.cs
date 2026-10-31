using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlCompositionTests
{
    private static CosmicProjectionCompositionExecutionLimits Limits(int rows = 10) => new(rows, 10000, 100, 10000, 100, 10000);
    private static CosmicProjectionCompositionPlan<Holon, TResult> Plan<TResult>(CosmicProjectionQuery<Holon, TResult> query)
    {
        var types = query.Columns.Select(column => column.Path.ValueType).Distinct().ToArray();
        var profile = new CosmicProjectionCompositionCapabilities(CosmicSelectedStageFeatures.Filter | CosmicSelectedStageFeatures.Ordering | CosmicSelectedStageFeatures.Paging,
            CosmicQueryFeatures.And | CosmicQueryFeatures.Or | CosmicQueryFeatures.Not | CosmicQueryFeatures.BooleanConstant | CosmicQueryFeatures.NullLiteral
            | CosmicQueryFeatures.StringStartsWith | CosmicQueryFeatures.StringEndsWith | CosmicQueryFeatures.StringContains | CosmicQueryFeatures.Membership,
            types.Select(type => new CosmicQueryMetadataCapability(type, Enum.GetValues<CosmicQueryComparisonOperator>(),
                type == typeof(string) ? Enum.GetValues<CosmicQueryStringOperator>() : null, supportsMembership: true)).ToArray(), types);
        var planned = CosmicProjectionCompositionPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { new CosmicProjectionCompositionTarget(ProviderType.MongoDBOASIS, MongoDBOASIS.CqlCapabilities, MongoDBOASIS.CqlProjectionCapabilities, profile) });
        Assert.IsFalse(planned.IsError, planned.Message); return planned.Result;
    }

    [TestMethod]
    public void SourceAndResultWindowsRetainTheirOriginalStageOrder()
    {
        var query = CosmicQuery<Holon>.Create().OrderBy(h => h.Name).Skip(1).Take(2)
            .Select(h => new { h.Name, h.Version }).Take(1).Where(row => row.Version > 1).Result;
        var command = new MongoCqlTranslator().CompileComposition(Plan(query), Limits());
        Assert.IsTrue(command.Source.RequiresSnapshot); Assert.AreEqual(2, command.Stages.Length);
        CollectionAssert.AreEqual(new[] { 2, 1 }, command.Stages[0].Where(stage => stage.Contains("$limit")).Select(stage => stage["$limit"].ToInt32()).ToArray());
        var selectedFilter = Array.FindLastIndex(command.Stages[1], stage => stage.Contains("$match"));
        var selectedWindow = Array.FindLastIndex(command.Stages[1], stage => stage.Contains("$limit"));
        Assert.IsTrue(selectedWindow < selectedFilter);
        Assert.IsTrue(command.Source.Pipeline.Last(stage => stage.Contains("$setWindowFields"))["$setWindowFields"]["output"].AsBsonDocument.Contains("nativeCount"));
    }

    [DataTestMethod]
    [DataRow(0, 1)] [DataRow(1, 2)]
    public void ZeroSelectedTakeDoesNotEraseSourceOverflowDetection(int remaining, int sentinel)
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(0).Result;
        var command = new MongoCqlTranslator().CompileComposition(Plan(query), Limits(remaining));
        Assert.AreEqual(sentinel, command.Source.Pipeline.Single(stage => stage.Contains("$limit"))["$limit"].ToInt32());
        Assert.IsFalse(command.Source.IsEmpty);
        Assert.IsTrue(command.Stages.Single().Any(stage => stage.Contains("$match")
            && stage["$match"].AsBsonDocument.TryGetValue("$expr", out var expr) && expr.IsBsonDocument
            && expr.AsBsonDocument.TryGetValue("$literal", out var literal) && literal.IsBoolean && !literal.AsBoolean));
        Assert.IsFalse(command.Stages.Single().Any(stage => stage.Contains("$limit") && stage["$limit"].ToInt32() == 0));
    }

    [TestMethod]
    public void MultiKeySortRunsStableNativePassesWithoutDuplicateBsonKeys()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => new { h.Name, h.Version })
            .OrderBy(row => row.Name).ThenByDescending(row => row.Version).ThenByDescending(row => row.Name).Result;
        var pipeline = new MongoCqlTranslator().CompileComposition(Plan(query), Limits()).Stages.Single();
        var passes = pipeline.Where(stage => stage.Contains("$setWindowFields")
            && stage["$setWindowFields"]["output"].AsBsonDocument.Contains("nextRank")).ToArray();
        Assert.AreEqual(3, passes.Length);
        Assert.AreEqual(-1, passes[0]["$setWindowFields"]["sortBy"]["k0.value"].ToInt32());
        Assert.AreEqual(-1, passes[1]["$setWindowFields"]["sortBy"]["k1.value"].ToInt32());
        Assert.AreEqual(1, passes[2]["$setWindowFields"]["sortBy"]["k0.value"].ToInt32());
        foreach (var pass in passes) Assert.AreEqual(1, pass["$setWindowFields"]["sortBy"]["rank"].ToInt32());
    }

    [TestMethod]
    public void ThirtyTwoSortTermsStayWithinNativeCompoundSortWidth()
    {
        var columns = Enumerable.Range(0, 32).Select(index => CosmicProjectionSelection<Holon>.Create("c" + index, h => h.Name).Result).ToArray();
        var query = CosmicQuery<Holon>.Create().SelectColumns(columns);
        Expression<Func<CosmicProjectionRecord, string>> Key(int index)
        {
            var parameter = Expression.Parameter(typeof(CosmicProjectionRecord), "row");
            var call = Expression.Call(parameter, typeof(CosmicProjectionRecord).GetProperty("Item")!.GetMethod!, Expression.Constant("c" + index));
            return Expression.Lambda<Func<CosmicProjectionRecord, string>>(Expression.Convert(call, typeof(string)), parameter);
        }
        query = query.OrderBy(Key(0));
        for (var index = 1; index < 32; index++) query = query.ThenBy(Key(index));
        Assert.IsFalse(query.IsError, query.Message);
        var pipeline = new MongoCqlTranslator().CompileComposition(Plan(query.Result), Limits()).Stages.Single();
        var passes = pipeline.Where(stage => stage.Contains("$setWindowFields") && stage["$setWindowFields"]["output"].AsBsonDocument.Contains("nextRank")).ToArray();
        Assert.AreEqual(32, passes.Length);
        foreach (var pass in passes) Assert.AreEqual(2, pass["$setWindowFields"]["sortBy"].AsBsonDocument.ElementCount);
    }

    [TestMethod]
    public void RuntimeAliasesBindNativeSlotsAndNeverBecomeBsonFieldPaths()
    {
        const string alias = "$unsafe.alias";
        var query = CosmicQuery<Holon>.Create().SelectColumns(new[] { CosmicProjectionSelection<Holon>.Create(alias, h => h.Name).Result })
            .Where(row => (string)row["$unsafe.alias"] == "coffee").Result;
        var command = new MongoCqlTranslator().CompileComposition(Plan(query), Limits());
        var json = string.Join("", command.Stages.Single().Select(stage => stage.ToJson()));
        Assert.IsFalse(json.Contains(alias, StringComparison.Ordinal)); StringAssert.Contains(json, "$k0.value");
    }

    [TestMethod]
    public void StageTransportProjectsOnlyProofAndDoesNotMutateOtherPipelines()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(1).Where(value => value == "coffee").Result;
        var command = new MongoCqlTranslator().CompileComposition(Plan(query), Limits());
        Assert.AreEqual(4, command.Stages[0].Last()["$project"].AsBsonDocument.ElementCount);
        command.Stages[0].Last()["$project"].AsBsonDocument.Add("tampered", 1);
        Assert.IsFalse(command.Stages[1].Last()["$project"].AsBsonDocument.Contains("tampered"));
        Assert.IsFalse(command.Source.Pipeline.Any(stage => stage.Contains("$project") && stage["$project"].AsBsonDocument.Contains("tampered")));
    }

    [TestMethod]
    public void DescendantSourceKeepsProtectedEnvelopeForSnapshotValidation()
    {
        var query = CosmicQuery<Holon>.Create().WhereAnyDescendant(h => h.Name == "child", CosmicQueryTraversalOptions.Create(2, 4).Result)
            .Select(h => h.Name).Take(0).Result;
        var command = new MongoCqlTranslator().CompileComposition(Plan(query), Limits());
        Assert.IsTrue(command.Source.RequiresMembership); Assert.IsTrue(command.Source.RequiresSnapshot); Assert.IsTrue(command.Source.HasTraversalEnvelope);
        Assert.IsTrue(command.Source.Pipeline.Any(stage => stage.Contains("$facet")));
        Assert.IsTrue(command.Stages.Single().Any(stage => stage.Contains("$replaceRoot")));
    }

    [TestMethod]
    public void RemainingWorkCannotUpgradeTheCapturedPlan()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(1).Result;
        Assert.ThrowsException<ArgumentException>(() => new MongoCqlTranslator().CompileComposition(Plan(query), new CosmicProjectionCompositionExecutionLimits(1001, 10000, 100, 10000, 100, 10000)));
    }
}
