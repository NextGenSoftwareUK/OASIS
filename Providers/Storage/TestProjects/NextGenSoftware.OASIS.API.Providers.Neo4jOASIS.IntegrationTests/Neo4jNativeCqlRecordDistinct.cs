using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public partial class Neo4jNativeCqlTests
{
    [TestMethod]
    public Task NativeRecordDistinctUsesVisibleTuplesAndOrderedStages()
        =>WithNativeGraph(async fixture=>{
            var source=CosmicQuery<Holon>.Create().OrderBy(h=>h.Id).Take(20).Result;
            var selected=source.SelectColumns(new[]{
                CosmicProjectionSelection<Holon>.Create("name",h=>h.Name).Result,
                CosmicProjectionSelection<Holon>.Create("description",h=>h.Description).Result,
                CosmicProjectionSelection<Holon>.Create("hidden",h=>h.Id).Result}).Result;
            var tuple=selected.SelectOutputColumns(new[]{
                new CosmicProjectionOutputBinding("Label","name"),
                new CosmicProjectionOutputBinding("Detail","description")}).Result.Distinct().Result;
            var adapter=(ICosmicQueryProjectionCompositionStorageProvider)fixture.Provider;
            async Task<CosmicProjectionRecord[]> Read(CosmicProjectionQuery<Holon,CosmicProjectionRecord> query)
            {
                var plan=CosmicProjectionCompositionPlanner.Plan(query,adapter.QueryProviderType,new[]{
                    new CosmicProjectionCompositionTarget(adapter.QueryProviderType,adapter.QueryCapabilities,
                        adapter.ProjectionCapabilities,adapter.CompositionCapabilities)});
                if(plan.IsError)throw new Exception(plan.Message);
                var result=await GraphManager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(plan.Result,new[]{fixture.Provider},maximumResults:20);
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
        },width:4,configure:root=>{
            var all=new[]{root}.Concat(root.Children).ToArray();
            for(var index=0;index<all.Length;index++){
                all[index].Name=index<3?"coffee":"tea";
                all[index].Description=index==2?"b":index<3?"a":null;
                all[index].IsPublic=true;
            }
        });
}
