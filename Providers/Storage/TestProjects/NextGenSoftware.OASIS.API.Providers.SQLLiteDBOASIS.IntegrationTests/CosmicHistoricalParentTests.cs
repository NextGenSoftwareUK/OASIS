using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;
public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false,false,1)] [InlineData(false,false,2)]
    [InlineData(false,true,1)] [InlineData(false,true,2)]
    [InlineData(true,false,1)] [InlineData(true,false,2)]
    [InlineData(true,true,1)] [InlineData(true,true,2)]
    public async Task HistoricalParentQueriesPreservePriorMembershipAndCurrentDescendantDepth(bool synchronous,bool byKey,int depth)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=true};
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var parent=new Holon{Name="parent-v1",Version=1};
        var key="historical-parent-key";parent.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS]=key;
        var other=new Holon{Name="other-v1",Version=1};
        var child=new Holon{Name="child-v1",Version=1,HolonType=HolonType.Planet};
        var grand=new Holon{Name="grand-v1",Version=1,HolonType=HolonType.Moon};
        parent.Children.Add(child);child.Children.Add(grand);
        (await provider.SaveHolonsAsync(new[]{parent,other})).IsError.Should().BeFalse();
        parent.Children.Remove(child);other.Children.Add(child);child.ParentHolonId=other.Id;
        foreach(var record in new[]{parent,other,child,grand}){record.Version=2;record.Name=record.Name.Replace("v1","v2");}
        (await provider.SaveHolonsAsync(new[]{parent,other})).IsError.Should().BeFalse();
        var result=byKey
            ? synchronous ? provider.LoadHolonsForParent(key,HolonType.Planet,maxChildDepth:depth,version:1)
                :await provider.LoadHolonsForParentAsync(key,HolonType.Planet,maxChildDepth:depth,version:1)
            : synchronous ? provider.LoadHolonsForParent(parent.Id,HolonType.Planet,maxChildDepth:depth,version:1)
                :await provider.LoadHolonsForParentAsync(parent.Id,HolonType.Planet,maxChildDepth:depth,version:1);
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        var loaded=result.Result.Should().ContainSingle().Subject;
        loaded.Id.Should().Be(child.Id);loaded.Name.Should().Be("child-v1");loaded.ParentHolonId.Should().Be(parent.Id);
        loaded.Children.Count.Should().Be(depth==1 ? 0:1);
        if(depth==2)loaded.Children.Single().Name.Should().Be("grand-v2");
        (await provider.LoadHolonsForParentAsync(parent.Id,version:0)).Result.Should().BeEmpty();
        (await provider.LoadHolonsForParentAsync(other.Id,version:1)).Result.Should().BeEmpty();
        using var control=new DataContext(TestConnectionString);control.Database.OpenConnection();
        using var plan=control.Database.GetDbConnection().CreateCommand();
        plan.CommandText="EXPLAIN QUERY PLAN SELECT HolonId FROM CosmicHolonRevision WHERE json_extract(RecordJson,'$.ParentHolonId')=$parent AND Version=1";
        var identity=plan.CreateParameter();identity.ParameterName="$parent";identity.Value=parent.Id.ToString("D");plan.Parameters.Add(identity);
        using var rows=plan.ExecuteReader();var details=new List<string>();while(rows.Read())details.Add(rows.GetString(3));
        details.Should().Contain(detail=>detail.Contains("IX_CosmicHolonRevision_ParentVersion"));
    }
}
