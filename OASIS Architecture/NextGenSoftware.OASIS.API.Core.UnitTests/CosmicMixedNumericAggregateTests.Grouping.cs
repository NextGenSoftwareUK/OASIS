using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicMixedNumericAggregateTests
{
    private static CosmicGroupedSelection<Holon, CosmicProjectionRecord, CosmicProjectionRecord> IntegerGroups(
        Fixture f, int maximumGroups = 25, int maximumBytes = 1048576, bool keySelected = true)
    {
        var key = CosmicProjectionSelection<Holon>.Create("Key", h => (int?)h.MetaData["amount"]).Result;
        var g = f.Query.Source.GroupByColumns(new[] { key },
            CosmicGroupingOptions.Create(maximumGroups, maximumBytes).Result).Result;
        var bindings = new List<CosmicGroupedBinding<Holon, CosmicProjectionRecord>>();
        if (keySelected) bindings.Add(CosmicGroupedBinding<Holon, CosmicProjectionRecord>.Key(g, "Key", 0).Result);
        bindings.Add(CosmicGroupedBinding<Holon, CosmicProjectionRecord>.Aggregate(g, "Rows", g.Source.LongCount().Result).Result);
        bindings.Add(CosmicGroupedBinding<Holon, CosmicProjectionRecord>.Aggregate(g, "IntRows", g.Source.Count().Result).Result);
        bindings.Add(CosmicGroupedBinding<Holon, CosmicProjectionRecord>.CountValues(g, "Populated", h => (int?)h.MetaData["amount"]).Result);
        bindings.Add(CosmicGroupedBinding<Holon, CosmicProjectionRecord>.Aggregate(g, "Sum",
            g.Source.Sum(h => (int?)h.MetaData["amount"]).Result).Result);
        bindings.Add(CosmicGroupedBinding<Holon, CosmicProjectionRecord>.Aggregate(g, "Mean",
            g.Source.Average(h => (int?)h.MetaData["amount"]).Result).Result);
        bindings.Add(CosmicGroupedBinding<Holon, CosmicProjectionRecord>.Aggregate(g, "Minimum",
            g.Source.Min(h => (int?)h.MetaData["amount"]).Result).Result);
        bindings.Add(CosmicGroupedBinding<Holon, CosmicProjectionRecord>.Aggregate(g, "Maximum",
            g.Source.Max(h => (int?)h.MetaData["amount"]).Result).Result);
        var selected = g.SelectColumns(bindings); Assert.False(selected.IsError, selected.Message);
        return selected.Result;
    }

    [Fact]
    public async Task MixedGroupsReconcileChangedKeysBeforeAllCountsAndNumericReductions()
    {
        var f = await Create(typeof(int), true, new object[] { 100, 2, 3, null! }, new object[] { 2, 4, null! }, overlap: 1);
        var grouped = IntegerGroups(f);
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, grouped);
        Assert.False(plan.IsError, plan.Message);
        var r = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(r.IsError, r.Message); Assert.True(r.IsLoaded); Assert.Equal(4, r.Result.Count);
        Assert.Null(r.Result[0]["Key"]); Assert.Equal(2L, r.Result[0]["Rows"]); Assert.Equal(0L, r.Result[0]["Populated"]);
        Assert.Equal(0, r.Result[0]["Sum"]); Assert.Null(r.Result[0]["Mean"]); Assert.Null(r.Result[0]["Minimum"]);
        var two = r.Result.Single(row => Equals(row["Key"], 2));
        Assert.Equal(2L, two["Rows"]); Assert.Equal(2, two["IntRows"]); Assert.Equal(2L, two["Populated"]);
        Assert.Equal(4, two["Sum"]); Assert.Equal(2m, two["Mean"]); Assert.Equal(2, two["Minimum"]); Assert.Equal(2, two["Maximum"]);
        Assert.DoesNotContain(r.Result, row => Equals(row["Key"], 100));
    }

    [Fact]
    public async Task MixedHavingThenSelectedOrderAndOnePageUseTheReconciledGroups()
    {
        var f = await Create(typeof(int), true, new object[] { 100, 2, 3, null! }, new object[] { 2, 4, null! }, overlap: 1);
        var grouped = IntegerGroups(f).Where(row => (long)row["Rows"] >= 2)
            .OrderByDescending(row => (long)row["Rows"]).Skip(1).Take(1);
        Assert.False(grouped.IsError, grouped.Message);
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, grouped.Result);
        Assert.False(plan.IsError, plan.Message);
        var r = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(r.IsError, r.Message); var row = Assert.Single(r.Result);
        Assert.Equal(2, row["Key"]); Assert.Equal(4, row["Sum"]);
    }

    [Fact]
    public async Task HiddenGroupKeysStillSupplyStableFinalTies()
    {
        var f = await Create(typeof(int), true, new object[] { 4, 2, 3 }, Array.Empty<object>());
        var grouped = IntegerGroups(f, keySelected: false).OrderBy(row => (long)row["Rows"]).Skip(1).Take(1).Result;
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, grouped);
        Assert.False(plan.IsError, plan.Message);
        var r = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(r.IsError, r.Message); Assert.Equal(3, Assert.Single(r.Result)["Sum"]);
    }

    [Fact]
    public async Task MixedGroupsCanExceedOneBranchWindowWithoutReapplyingThatWindowToTheUnion()
    {
        var f = await Create(typeof(int), true, Enumerable.Repeat((object)2, 99).ToArray(),
            Enumerable.Repeat((object)2, 99).ToArray());
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, IntegerGroups(f));
        Assert.False(plan.IsError, plan.Message);
        var r = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(r.IsError, r.Message); var row = Assert.Single(r.Result);
        Assert.Equal(198L, row["Rows"]); Assert.Equal(396, row["Sum"]); Assert.Equal(2m, row["Mean"]);
    }

    [Fact]
    public async Task EmptyMixedInputHasNoSyntheticEmptyGroup()
    {
        var f = await Create(typeof(int), true, Array.Empty<object>(), Array.Empty<object>());
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, IntegerGroups(f));
        var r = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(r.IsError, r.Message); Assert.True(r.IsLoaded); Assert.Empty(r.Result);
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("mapping")]
    [InlineData("duplicate")]
    [InlineData("full")]
    [InlineData("shortened")]
    [InlineData("payload")]
    [InlineData("cancelled")]
    [InlineData("null-plan")]
    [InlineData("groups")]
    [InlineData("output-bytes")]
    public async Task MixedGroupingPreservesCompleteInputAccessAndFullGroupBudgets(string invalid)
    {
        var f = await Create(typeof(int), true, new object[] { 3 },
            invalid == "full" ? Enumerable.Repeat((object)2, 100).ToArray() : new object[] { 2 },
            readLimit: invalid == "shortened" ? 1 : 100,
            sourceActor: invalid == "actor" ? Guid.NewGuid() : null);
        var selected = IntegerGroups(f, maximumGroups: invalid == "groups" ? 1 : 25,
            maximumBytes: invalid == "output-bytes" ? 1 : 1048576);
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, selected);
        Assert.False(plan.IsError, plan.Message);
        using var cancellation = new CancellationTokenSource();
        if (invalid == "cancelled") cancellation.Cancel();
        var r = CosmicMixedProjectionResults.Group(invalid == "null-plan" ? null! : plan.Result, Actor,
            invalid == "mapping" ? Guid.NewGuid() : f.Mapping, f.Stored,
            invalid == "duplicate" ? new[] { f.Live[0], f.Live[0] } : f.Live,
            invalid == "payload" ? 1 : CosmicMixedProjectionResults.MaximumInputBytes, cancellation.Token);
        Assert.True(r.IsError);
        Assert.Equal(invalid switch { "actor" => "CQL_ACCESS_INVALID", "full" or "shortened" => "CQL_VIRTUAL_SOURCE_LIMIT",
            "null-plan" => "CQL_MIXED_QUERY_INVALID", "payload" or "groups" or "output-bytes" => "CQL_RESULT_LIMIT",
            "cancelled" => "CQL_QUERY_CANCELLED", _ => "CQL_PROVIDER_RESULT_INVALID" }, r.ErrorCode);
    }

    [Fact]
    public async Task GroupPlanRequiresTheSameSourceAndEveryExactProjectedPath()
    {
        var f = await Create(typeof(int), true, new object[] { 2 }, new object[] { 3 });
        var other = await Create(typeof(int), true, new object[] { 2 }, new object[] { 3 });
        Assert.Equal("CQL_MIXED_QUERY_INVALID", CosmicMixedProjectionResults.PrepareGrouping(f.Query, IntegerGroups(other)).ErrorCode);
        var g = f.Query.Source.GroupByColumns(new[] {
            CosmicProjectionSelection<Holon>.Create("Name", h => h.Name).Result }).Result;
        var selected = g.SelectColumns(new[] {
            CosmicGroupedBinding<Holon, CosmicProjectionRecord>.Key(g, "Name", 0).Result }).Result;
        Assert.Equal("CQL_MIXED_QUERY_INVALID", CosmicMixedProjectionResults.PrepareGrouping(f.Query, selected).ErrorCode);
    }

    [Fact]
    public async Task GroupPlansKeepTheCapturedHavingLiteral()
    {
        var f = await Create(typeof(int), true, new object[] { 2 }, new object[] { 3 });
        long threshold = 1;
        var selected = IntegerGroups(f).Where(row => (long)row["Rows"] >= threshold).Result;
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, selected); threshold = 100;
        var r = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(r.IsError, r.Message); Assert.Equal(2, r.Result.Count);
    }

    public sealed class GroupConstructorProbe
    {
        public static int Calls;
        public long Rows { get; set; }
        public int? Sum { get; set; }
        public GroupConstructorProbe() { Calls++; }
    }

    [Fact]
    public async Task OffPageGroupOverflowCannotPublishAnyDtoConstructor()
    {
        var f = await Create(typeof(int), true, new object[] { 1, int.MaxValue, int.MaxValue }, Array.Empty<object>());
        var g = f.Query.Source.GroupByColumns(new[] {
            CosmicProjectionSelection<Holon>.Create("Key", h => (int?)h.MetaData["amount"]).Result }).Result;
        var selected = g.Select(group => new GroupConstructorProbe {
            Rows = group.LongCount(), Sum = group.Sum(h => (int?)h.MetaData["amount"]) }).Take(1);
        Assert.False(selected.IsError, selected.Message);
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, selected.Result);
        Assert.False(plan.IsError, plan.Message); GroupConstructorProbe.Calls = 0;
        var r = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.True(r.IsError); Assert.Equal("CQL_SCALAR_OVERFLOW", r.ErrorCode);
        Assert.Equal(0, GroupConstructorProbe.Calls);
    }

    [Fact]
    public async Task CompositeKeysAndHavingUseTheExistingOrdinalNullAndUnicodeRules()
    {
        var f = await Create(typeof(string), true, new object[] { "A", "a", "\uE000", null! },
            new object[] { "A", "😀", null! }, overlap: 1);
        var g = f.Query.Source.GroupByColumns(new[] {
            CosmicProjectionSelection<Holon>.Create("First", h => (string)h.MetaData["amount"]).Result,
            CosmicProjectionSelection<Holon>.Create("Second", h => (string)h.MetaData["amount"]).Result }).Result;
        var selected = g.SelectColumns(new[] {
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Key(g,"Text",0).Result,
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Aggregate(g,"Rows",g.Source.LongCount().Result).Result
        }).Where(row => (string)row["Text"] == null || ((string)row["Text"]).Contains("a") || (string)row["Text"] == "😀").Result;
        var plan = CosmicMixedProjectionResults.PrepareGrouping(f.Query, selected);
        Assert.False(plan.IsError, plan.Message);
        var r = CosmicMixedProjectionResults.Group(plan.Result, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(r.IsError, r.Message); Assert.Equal(3, r.Result.Count);
        Assert.Null(r.Result[0]["Text"]); Assert.Equal("a", r.Result[1]["Text"]); Assert.Equal("😀", r.Result[2]["Text"]);
    }
    [Fact]
    public async Task IndependentCategoryMovesWithTheLiveWinnerBeforeGrouping()
    {
        var f = await Create(typeof(int), true, new object[] {100,3}, new object[] {2}, overlap:1,
            storedKeys:new object[] {"old","stable"}, nativeKeys:new object[] {"new"});
        var g=f.Query.Source.GroupByColumns(new[] {
            CosmicProjectionSelection<Holon>.Create("Category",h=>(string)h.MetaData["category"]).Result }).Result;
        var selected=g.SelectColumns(new[] {
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Key(g,"Category",0).Result,
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Aggregate(g,"Sum",g.Source.Sum(h=>(int?)h.MetaData["amount"]).Result).Result }).Result;
        var plan=CosmicMixedProjectionResults.PrepareGrouping(f.Query,selected);
        Assert.False(plan.IsError,plan.Message);
        var r=CosmicMixedProjectionResults.Group(plan.Result,Actor,f.Mapping,f.Stored,f.Live);
        Assert.False(r.IsError,r.Message); Assert.Equal(2,r.Result.Count);
        Assert.Equal("new",r.Result[0]["Category"]); Assert.Equal(2,r.Result[0]["Sum"]);
        Assert.DoesNotContain(r.Result,row=>Equals(row["Category"],"old"));
    }

    [Fact]
    public async Task GroupedWideInt64SumAndAverageStayExactAcrossDistinctInputs()
    {
        var f=await Create(typeof(long),true,new object[] {9007199254740993L},new object[] {9007199254740995L},
            storedKeys:new object[] {"one"},nativeKeys:new object[] {"one"});
        var g=f.Query.Source.GroupByColumns(new[] {
            CosmicProjectionSelection<Holon>.Create("Category",h=>(string)h.MetaData["category"]).Result }).Result;
        var selected=g.SelectColumns(new[] {
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Aggregate(g,"Sum",g.Source.Sum(h=>(long?)h.MetaData["amount"]).Result).Result,
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Aggregate(g,"Mean",g.Source.Average(h=>(long?)h.MetaData["amount"]).Result).Result }).Result;
        var plan=CosmicMixedProjectionResults.PrepareGrouping(f.Query,selected);
        Assert.False(plan.IsError,plan.Message);
        var r=CosmicMixedProjectionResults.Group(plan.Result,Actor,f.Mapping,f.Stored,f.Live);
        Assert.False(r.IsError,r.Message);var row=Assert.Single(r.Result);
        Assert.Equal(18014398509481988L,row["Sum"]);Assert.Equal(9007199254740994m,row["Mean"]);
    }

    [Fact]
    public async Task GroupedDoubleRoundsOnceAcrossCatastrophicCancellation()
    {
        var f=await Create(typeof(double),true,new object[] {1e16,1d},new object[] {-1e16},
            storedKeys:new object[] {"one","one"},nativeKeys:new object[] {"one"});
        var g=f.Query.Source.GroupByColumns(new[] {
            CosmicProjectionSelection<Holon>.Create("Category",h=>(string)h.MetaData["category"]).Result }).Result;
        var selected=g.SelectColumns(new[] {
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Aggregate(g,"Sum",g.Source.Sum(h=>(double?)h.MetaData["amount"]).Result).Result,
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Aggregate(g,"Mean",g.Source.Average(h=>(double?)h.MetaData["amount"]).Result).Result }).Result;
        var plan=CosmicMixedProjectionResults.PrepareGrouping(f.Query,selected);
        Assert.False(plan.IsError,plan.Message);
        var r=CosmicMixedProjectionResults.Group(plan.Result,Actor,f.Mapping,f.Stored,f.Live);
        Assert.False(r.IsError,r.Message);var row=Assert.Single(r.Result);
        Assert.Equal(1d,row["Sum"]);Assert.Equal(1d/3d,row["Mean"]);
    }

    [Fact]
    public async Task PopulatedCategoryWithNullValuesRemainsARealGroup()
    {
        var f=await Create(typeof(int),true,new object[] {null!},new object[] {null!},
            storedKeys:new object[] {"one"},nativeKeys:new object[] {"one"});
        var g=f.Query.Source.GroupByColumns(new[] {
            CosmicProjectionSelection<Holon>.Create("Category",h=>(string)h.MetaData["category"]).Result }).Result;
        var selected=g.SelectColumns(new[] {
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Aggregate(g,"Rows",g.Source.LongCount().Result).Result,
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.CountValues(g,"Populated",h=>(int?)h.MetaData["amount"]).Result,
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Aggregate(g,"Mean",g.Source.Average(h=>(int?)h.MetaData["amount"]).Result).Result }).Result;
        var plan=CosmicMixedProjectionResults.PrepareGrouping(f.Query,selected);
        Assert.False(plan.IsError,plan.Message);
        var r=CosmicMixedProjectionResults.Group(plan.Result,Actor,f.Mapping,f.Stored,f.Live);
        Assert.False(r.IsError,r.Message);var row=Assert.Single(r.Result);
        Assert.Equal(2L,row["Rows"]);Assert.Equal(0L,row["Populated"]);Assert.Null(row["Mean"]);
    }

    [Fact]
    public async Task NullableBooleanGroupsKeepFalseTrueAndNullSeparate()
    {
        var f=await Create(typeof(bool),true,new object[] {false,true,null!},new object[] {false});
        var g=f.Query.Source.GroupByColumns(new[] {
            CosmicProjectionSelection<Holon>.Create("Flag",h=>(bool?)h.MetaData["amount"]).Result }).Result;
        var selected=g.SelectColumns(new[] {
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Key(g,"Flag",0).Result,
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Aggregate(g,"Rows",g.Source.LongCount().Result).Result }).Result;
        var plan=CosmicMixedProjectionResults.PrepareGrouping(f.Query,selected);
        Assert.False(plan.IsError,plan.Message);
        var r=CosmicMixedProjectionResults.Group(plan.Result,Actor,f.Mapping,f.Stored,f.Live);
        Assert.False(r.IsError,r.Message);Assert.Equal(3,r.Result.Count);
        Assert.Null(r.Result[0]["Flag"]);Assert.Equal(false,r.Result[1]["Flag"]);Assert.Equal(2L,r.Result[1]["Rows"]);
        Assert.Equal(true,r.Result[2]["Flag"]);
    }

}
