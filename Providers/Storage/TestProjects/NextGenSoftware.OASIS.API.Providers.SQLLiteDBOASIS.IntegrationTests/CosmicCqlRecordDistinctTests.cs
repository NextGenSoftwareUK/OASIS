using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;
public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task NativeRecordDistinctUsesVisibleTuplesAndOrderedStages()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        var activated=await provider.ActivateProviderAsync();
        if(activated.IsError)throw new Exception(activated.Message);
        var seed=Enumerable.Range(0,5).Select(index=>new Holon{
            Name=index<3?"coffee":"tea",Description=index==2?"b":index<3?"a":null,IsPublic=true}).ToArray();
        var saved=await provider.SaveHolonsAsync(seed,saveChildren:false);
        if(saved.IsError)throw new Exception(saved.Message);
            var source=CosmicQuery<Holon>.Create().OrderBy(h=>h.Id).Take(20).Result;
            var selected=source.SelectColumns(new[]{
                CosmicProjectionSelection<Holon>.Create("name",h=>h.Name).Result,
                CosmicProjectionSelection<Holon>.Create("description",h=>h.Description).Result,
                CosmicProjectionSelection<Holon>.Create("hidden",h=>h.Id).Result}).Result;
            var tuple=selected.SelectOutputColumns(new[]{
                new CosmicProjectionOutputBinding("Label","name"),
                new CosmicProjectionOutputBinding("Detail","description")}).Result.Distinct().Result;
            var adapter=(ICosmicQueryProjectionCompositionStorageProvider)provider;
            async Task<CosmicProjectionRecord[]> Read(CosmicProjectionQuery<Holon,CosmicProjectionRecord> query)
            {
                var plan=CosmicProjectionCompositionPlanner.Plan(query,adapter.QueryProviderType,new[]{
                    new CosmicProjectionCompositionTarget(adapter.QueryProviderType,adapter.QueryCapabilities,
                        adapter.ProjectionCapabilities,adapter.CompositionCapabilities)});
                if(plan.IsError)throw new Exception(plan.Message);
                var result=await CosmicManager(provider,true).ExecuteProjectionCompositionQueryAsync(plan.Result,new[]{provider},maximumResults:20);
                if(result.IsError)throw new Exception(result.Message,result.Exception);
                return result.Result.Providers.Single().Rows.ToArray();
            }
            var rows=await Read(tuple);
            if(rows.Length!=3 || rows.Count(row=>(string)row["Label"]=="coffee")!=2
                || rows.Count(row=>(string)row["Label"]=="tea" && row["Detail"]==null)!=1
                || rows.Any(row=>!row.Aliases.SequenceEqual(new[]{"Label","Detail"})))
                throw new Exception("Native distinct lost a visible cell, retained hidden identity or mishandled null tuples.");
            var labels=tuple.SelectOutputColumns(new[]{
                new CosmicProjectionOutputBinding("Display","Label")}).Result.Distinct().Result;
            var unique=await Read(labels);
            if(unique.Length!=2 || !unique.Select(row=>(string)row["Display"]).OrderBy(value=>value).SequenceEqual(new[]{"coffee","tea"}))
                throw new Exception("Distinct must capture the current visible columns independently at every stage.");
            var filtered=await Read(labels.Where(row=>(string)row["Display"]=="coffee").Result.Take(1).Result);
            if(filtered.Length!=1 || (string)filtered[0]["Display"]!="coffee")
                throw new Exception("Native filtering/window after distinct changed the selected output.");
            var earlierWindow=await Read(selected.Take(1).Result.SelectOutputColumns(new[]{
                new CosmicProjectionOutputBinding("Display","name")}).Result.Distinct().Result);
            if(earlierWindow.Length!=1)throw new Exception("Distinct crossed an earlier result window.");
    }
}
