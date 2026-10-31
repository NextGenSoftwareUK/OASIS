using System.Linq.Expressions;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task NativeCqlFieldComparisonCountsBothPathsAgainstParameterBudgetBeforeIO()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var query = CosmicQuery<Holon>.Create();
        for (var i=0;i<30;i++)
        {
            var next=query.Where(h=>
                CosmicQueryMetadata.Value<long>(h,"a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p") ==
                CosmicQueryMetadata.Value<long>(h,"q","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p"));
            next.IsError.Should().BeFalse(next.Message); query=next.Result;
        }
        var plan=CosmicQueryPlanner.Plan(query,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS,
            new[]{new CosmicQueryTarget(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS,SQLLiteDBOASIS.CqlCapabilities)});
        plan.IsError.Should().BeFalse(plan.Message);
        var result=await provider.ExecuteCosmicQueryAsync(plan.Result);
        result.ErrorCode.Should().Be("CQL_QUERY_LIMIT");
        File.Exists(_databasePath).Should().BeFalse();
    }

    [Fact]
    public async Task NativeCqlFieldComparisonPreservesStringGuidBooleanAndExactDateSemantics()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var guid = Guid.NewGuid(); var other = Guid.NewGuid();
        var instant = new DateTime(2026,10,20,12,0,0,DateTimeKind.Utc).AddTicks(1);
        var rows = new[] {
            new Holon { Id=Guid.NewGuid(), Name="same", Description="same", MetaData=new() {
                ["sl"]="same", ["sr"]="same", ["gl"]=guid, ["gr"]=guid,
                ["bl"]=true, ["br"]=true, ["dl"]=instant, ["dr"]=instant.AddTicks(1) } },
            new Holon { Id=Guid.NewGuid(), Name="Case", Description="case", MetaData=new() {
                ["sl"]="Case", ["sr"]="case", ["gl"]=guid, ["gr"]=other,
                ["bl"]=true, ["br"]=false, ["dl"]=instant.AddTicks(1), ["dr"]=instant } },
            new Holon { Id=Guid.NewGuid(), Name=null, Description=null, MetaData=new() {
                ["sl"]=null!, ["sr"]=null!, ["gl"]=null!, ["gr"]=null!,
                ["bl"]=null!, ["br"]=null!, ["dl"]=null!, ["dr"]=null! } } };
        foreach (var row in rows)
        {
            row.MetaData["owner"]=row.Id;
            (await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();
        }
        async Task Check(Expression<Func<Holon,bool>> predicate, params int[] selected)
        {
            var result=await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
            result.IsError.Should().BeFalse(result.Message+result.Exception);
            result.Result.Select(h=>h.Id).OrderBy(id=>id).Should().Equal(selected.Select(i=>rows[i].Id).OrderBy(id=>id));
        }
        await Check(h=>h.Name==h.Description,0,2);
        await Check(h=>h.Name!=h.Description,1);
        await Check(h=>h.CreatedByAvatarId==h.ModifiedByAvatarId,0,1,2);
        await Check(h=>CosmicQueryMetadata.Value<Guid>(h,"owner")==h.Id,0,1,2);
        await Check(h=>CosmicQueryMetadata.Value<string>(h,"sl")==CosmicQueryMetadata.Value<string>(h,"sr"),0,2);
        await Check(h=>CosmicQueryMetadata.Value<string>(h,"sl")==h.Name,0,1,2);
        await Check(h=>CosmicQueryMetadata.Value<Guid?>(h,"gl")==CosmicQueryMetadata.Value<Guid?>(h,"gr"),0,2);
        await Check(h=>CosmicQueryMetadata.Value<Guid>(h,"gl")==CosmicQueryMetadata.Value<Guid>(h,"gr"),0);
        await Check(h=>CosmicQueryMetadata.Value<bool?>(h,"bl")==CosmicQueryMetadata.Value<bool?>(h,"br"),0,2);
        await Check(h=>CosmicQueryMetadata.Value<DateTime?>(h,"dl")<CosmicQueryMetadata.Value<DateTime?>(h,"dr"),0);
        await Check(h=>CosmicQueryMetadata.Value<DateTime?>(h,"dl")==CosmicQueryMetadata.Value<DateTime?>(h,"dr"),2);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public async Task NativeCqlFieldComparisonPreservesIndependentPathsAndNullableDomains(int selection)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        object?[] left = { 1L, 2L, 3L, null, null, 1L, "2", long.MaxValue, long.MinValue };
        object?[] right = { 2L, 2L, 2L, null, 2L, null, 2L, long.MaxValue - 1, long.MinValue + 1 };
        var ids = new List<Guid>();
        const string key = "a.b'; $x";
        for (var i = 0; i < left.Length + 1; i++)
        {
            var row = new Holon { Id = Guid.NewGuid(), MetaData = i == left.Length ? new() : new() {
                ["left"] = new Dictionary<string, object> { [key] = left[i]! },
                ["right"] = new Dictionary<string, object> { [key] = right[i]! } } };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse(); ids.Add(row.Id);
        }
        foreach (var metadata in new[] {
            new Dictionary<string,object> { ["left"]=new Dictionary<string,object>{{key,1L}}, ["right"]=new Dictionary<string,object>{{key,"1"}} },
            new Dictionary<string,object> { ["left"]=new Dictionary<string,object>{{key,1L}} },
            new Dictionary<string,object> { ["right"]=new Dictionary<string,object>{{key,1L}} } })
        {
            var row=new Holon { Id=Guid.NewGuid(), MetaData=metadata };
            (await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse(); ids.Add(row.Id);
        }
        Expression<Func<Holon,bool>> predicate = selection switch {
            0 => h => CosmicQueryMetadata.Value<long?>(h,"left",key) == CosmicQueryMetadata.Value<long?>(h,"right",key),
            1 => h => CosmicQueryMetadata.Value<long?>(h,"left",key) != CosmicQueryMetadata.Value<long?>(h,"right",key),
            2 => h => CosmicQueryMetadata.Value<long?>(h,"left",key) < CosmicQueryMetadata.Value<long?>(h,"right",key),
            3 => h => CosmicQueryMetadata.Value<long?>(h,"left",key) <= CosmicQueryMetadata.Value<long?>(h,"right",key),
            4 => h => CosmicQueryMetadata.Value<long?>(h,"left",key) > CosmicQueryMetadata.Value<long?>(h,"right",key),
            5 => h => CosmicQueryMetadata.Value<long?>(h,"left",key) >= CosmicQueryMetadata.Value<long?>(h,"right",key),
            _ => h => !(CosmicQueryMetadata.Value<long?>(h,"left",key) == CosmicQueryMetadata.Value<long?>(h,"right",key)) };
        int[] selected = selection switch { 0 => new[]{1,3}, 1 => new[]{0,2,4,5,7,8}, 2 => new[]{0,8},
            3 => new[]{0,1,8}, 4 => new[]{2,7}, 5 => new[]{1,2,7}, _ => new[]{0,2,4,5,6,7,8,9,10,11,12} };
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Select(h=>h.Id).OrderBy(id=>id).Should().Equal(selected.Select(i=>ids[i]).OrderBy(id=>id));
    }
}
