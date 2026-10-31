using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task NativeFloatingMixedNumericPacketsKeepAllColumnOffsets()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=new List<Guid>();
        for(var index=0;index<2;index++)
        {
            var row=new Holon {Id=Guid.NewGuid(),Name="one",MetaData=new()
                { ["stock"]=7L,["price"]=1.25m,["singleValue"]=0.1f,["value"]=2d }};
            (await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();ids.Add(row.Id);
        }
        var keys=ids.ToArray();
        var query=CosmicQuery<Holon>.Create().Where(h=>keys.Contains(h.Id)).GroupBy(h=>h.Name)
            .Select(g=>new
            {
                Integer=g.Sum(h=>CosmicQueryMetadata.Value<long>(h,"stock")),
                Decimal=g.Sum(h=>CosmicQueryMetadata.Value<decimal>(h,"price")),
                Single=g.Sum(h=>CosmicQueryMetadata.Value<float>(h,"singleValue")),
                SingleMean=g.Average(h=>CosmicQueryMetadata.Value<float>(h,"singleValue")),
                Double=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                DoubleMean=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                Minimum=g.Min(h=>CosmicQueryMetadata.Value<float>(h,"singleValue"))
            }).Result;
        var result=await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider,query),new[] {provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        var selected=result.Result.Providers.Single().Rows.Single();
        selected.Integer.Should().Be(14L);selected.Decimal.Should().Be(2.5m);
        selected.Single.Should().Be(0.2f);selected.SingleMean.Should().Be(0.1f);
        selected.Double.Should().Be(4d);selected.DoubleMean.Should().Be(2d);selected.Minimum.Should().Be(0.1f);
    }

    [Fact]
    public async Task NativeFloatingThirtyAggregateSlotsDoNotExpandIntoThousandsOfColumns()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,2d);
        var query=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).GroupBy(h=>h.Name)
            .Select(g=>new
            {
                V00=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V01=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V02=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V03=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V04=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V05=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V06=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V07=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V08=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V09=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V10=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V11=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V12=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V13=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V14=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V15=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V16=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V17=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V18=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V19=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V20=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V21=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V22=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V23=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V24=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V25=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V26=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V27=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V28=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
                V29=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value"))
            }).Where(g=>g.V00==2d).OrderBy(g=>g.V29).Take(1).Result;
        var result=await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider,query),new[] {provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        var selected=result.Result.Providers.Single().Rows.Single();
        foreach(var property in selected.GetType().GetProperties())
            ((double)property.GetValue(selected)!).Should().Be(2d);
    }
}
