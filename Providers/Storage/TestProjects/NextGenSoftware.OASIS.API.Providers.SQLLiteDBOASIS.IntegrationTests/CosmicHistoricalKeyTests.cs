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
    [InlineData(false,1)] [InlineData(false,2)] [InlineData(true,1)] [InlineData(true,2)]
    public async Task HistoricalKeyReadsBindArchivedIdentityAndCurrentIndependentDescendants(bool synchronous,int depth)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=true};
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        const string oldKey="old-key-'\"/;DROP TABLE Holon;--";
        var root=new Holon{Name="root-v1",Version=1};root.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS]=oldKey;
        var child=new Holon{Name="child-v1",Version=1};var grand=new Holon{Name="grand-v1",Version=1};
        root.Children.Add(child);child.Children.Add(grand);
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        root.Name="root-v2";root.Version=2;root.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS]="new-key";
        child.Name="child-v2";child.Version=2;grand.Name="grand-v2";grand.Version=2;
        var update=await provider.SaveHolonAsync(root);update.IsError.Should().BeFalse(update.Message+update.Exception);
        using(var control=new DataContext(TestConnectionString))
        {
            control.Database.OpenConnection();using var plan=control.Database.GetDbConnection().CreateCommand();
            plan.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_CosmicHolonRevision_ProviderKeyVersion'";
            Convert.ToInt32(plan.ExecuteScalar()).Should().Be(1);
            plan.CommandText="EXPLAIN QUERY PLAN SELECT DISTINCT HolonId FROM CosmicHolonRevision WHERE json_extract(RecordJson,'$.ProviderUniqueStorageKey.SQLLiteDBOASIS')=$key AND Version=1";
            var key=plan.CreateParameter();key.ParameterName="$key";key.Value=oldKey;plan.Parameters.Add(key);
            using var rows=plan.ExecuteReader();var details=new List<string>();while(rows.Read())details.Add(rows.GetString(3));
            details.Should().Contain(detail=>detail.StartsWith("SEARCH CosmicHolonRevision USING INDEX",StringComparison.Ordinal),
                "the native optimizer may choose either suitable index for this small fixture");
        }
        var historical=synchronous ? provider.LoadHolon(oldKey,maxChildDepth:depth,version:1)
            :await provider.LoadHolonAsync(oldKey,maxChildDepth:depth,version:1);
        historical.IsError.Should().BeFalse(historical.Message+historical.Exception);
        historical.Result.Id.Should().Be(root.Id);historical.Result.Name.Should().Be("root-v1");
        var loadedChild=historical.Result.Children.Single();loadedChild.Name.Should().Be("child-v2");
        loadedChild.Children.Count.Should().Be(depth==1 ? 0:1);
        if(depth==2)loadedChild.Children.Single().Name.Should().Be("grand-v2");
        (await provider.LoadHolonAsync("new-key",version:1)).Result.Should().BeNull();
        var reused=new Holon{Name="different root",Version=1};reused.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS]=oldKey;
        (await provider.SaveHolonAsync(reused)).IsError.Should().BeFalse();
        var ambiguous=synchronous ? provider.LoadHolon(oldKey,version:1):await provider.LoadHolonAsync(oldKey,version:1);
        ambiguous.IsError.Should().BeTrue();ambiguous.Exception.Should().BeOfType<System.IO.InvalidDataException>();
        (await provider.LoadHolonAsync(oldKey,loadChildren:false)).Result.Id.Should().Be(reused.Id);
    }
}
