using System.Collections.ObjectModel;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;
public partial class SQLLiteDBOASISIntegrationTests
{
    public sealed class NativeRoleMember:Holon { }
    public sealed class NativeRoleOwner:Holon
    {
        public List<NativeRoleMember> Components {get;set;}=new();
        public override ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Components).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection(nameof(Components),Components,members=>Components=members.Cast<NativeRoleMember>().ToList())}).ToArray();
        public override IReadOnlyList<HolonChildRoleBinding> GetChildRoleBindings()=>base.GetChildRoleBindings()
            .Concat(new[]{HolonChildRoleBinding.Create<NativeRoleMember>(nameof(Components),members=>Components=members.ToList(),
                source=>Core.Helpers.Mapper<IHolon,NativeRoleMember>.MapBaseHolonProperties(source))}).ToArray();
    }

    [Theory]
    [InlineData(false,"all")] [InlineData(true,"all")]
    [InlineData(false,"search")] [InlineData(true,"search")]
    [InlineData(false,"metadata")] [InlineData(true,"metadata")]
    [InlineData(false,"pairs")] [InlineData(true,"pairs")]
    [InlineData(false,"parent")] [InlineData(true,"parent")]
    [InlineData(false,"parent-key")] [InlineData(true,"parent-key")]
    public async Task TypedManagerQueriesRestoreRolesAndConditionallyResave(bool synchronous,string route)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=true};
        using var other=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=true};
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await other.ActivateProviderAsync()).IsError.Should().BeFalse();
        var writer=CosmicManager(provider,true);var reader=CosmicManager(other,true);var actor=Guid.NewGuid();
        var parent=new Holon{Name="query parent"};
        const string parentKey="typed-query-parent";
        parent.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS]=parentKey;
        var root=new NativeRoleOwner{Name="typed query owner",HolonType=HolonType.Quest};
        root.MetaData["typed-kind"]="query-owner";
        root.Components.Add(new NativeRoleMember{Name="query member"});
        parent.Children.Add(root);
        var saved=await writer.SaveHolonAsync(parent,actor,new CosmicWriteOptions(Guid.NewGuid()));
        saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        var originalVersion=root.VersionId;
        var pairs=new Dictionary<string,string>{{"typed-kind","query-owner"}};
        var loaded=route switch
        {
            "search"=>synchronous ? reader.SearchHolons<NativeRoleOwner>("typed query owner",actor,searchOnlyForCurrentAvatar:false,holonType:HolonType.Quest,providerType:ProviderType.SQLLiteDBOASIS)
                :await reader.SearchHolonsAsync<NativeRoleOwner>("typed query owner",actor,searchOnlyForCurrentAvatar:false,holonType:HolonType.Quest,providerType:ProviderType.SQLLiteDBOASIS),
            "all"=>synchronous ? reader.LoadAllHolons<NativeRoleOwner>(holonType:HolonType.Quest,cache:false,providerType:ProviderType.SQLLiteDBOASIS)
                :await reader.LoadAllHolonsAsync<NativeRoleOwner>(holonType:HolonType.Quest,providerType:ProviderType.SQLLiteDBOASIS),
            "metadata"=>synchronous ? reader.LoadHolonsByMetaData<NativeRoleOwner>("typed-kind","query-owner",providerType:ProviderType.SQLLiteDBOASIS)
                :await reader.LoadHolonsByMetaDataAsync<NativeRoleOwner>("typed-kind","query-owner",providerType:ProviderType.SQLLiteDBOASIS),
            "pairs"=>synchronous ? reader.LoadHolonsByMetaData<NativeRoleOwner>(pairs,MetaKeyValuePairMatchMode.All,providerType:ProviderType.SQLLiteDBOASIS)
                :await reader.LoadHolonsByMetaDataAsync<NativeRoleOwner>(pairs,MetaKeyValuePairMatchMode.All,providerType:ProviderType.SQLLiteDBOASIS),
            "parent"=>synchronous ? reader.LoadHolonsForParent<NativeRoleOwner>(parent.Id,providerType:ProviderType.SQLLiteDBOASIS)
                :await reader.LoadHolonsForParentAsync<NativeRoleOwner>(parent.Id,providerType:ProviderType.SQLLiteDBOASIS),
            _=>synchronous ? reader.LoadHolonsForParent<NativeRoleOwner>(parentKey,providerType:ProviderType.SQLLiteDBOASIS)
                :await reader.LoadHolonsForParentAsync<NativeRoleOwner>(parentKey,providerType:ProviderType.SQLLiteDBOASIS)
        };
        loaded.IsError.Should().BeFalse(loaded.Message+loaded.Exception);
        var result=loaded.Result.Should().ContainSingle().Subject;
        result.Id.Should().Be(root.Id);result.Children.Should().BeEmpty();result.VersionId.Should().Be(originalVersion);
        result.Components.Should().ContainSingle().Which.Name.Should().Be("query member");
        result.HasHolonChanged().Should().BeFalse();
        result.Components[0].Name="query member edited";
        var resaved=await reader.SaveHolonAsync(result,actor,new CosmicWriteOptions(Guid.NewGuid()));
        resaved.IsError.Should().BeFalse(resaved.Message+resaved.Exception);
        result.PreviousVersionId.Should().Be(originalVersion);
        var independentlyLoaded=await writer.LoadHolonAsync<NativeRoleOwner>(root.Id,providerType:ProviderType.SQLLiteDBOASIS);
        independentlyLoaded.IsError.Should().BeFalse(independentlyLoaded.Message+independentlyLoaded.Exception);
        independentlyLoaded.Result.Components.Should().ContainSingle().Which.Name.Should().Be("query member edited");
    }

    [Theory]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task TypedManagerLoadsRestoreRolesAndKeepRevisionLineageOnResave(bool synchronous,bool byKey)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=true};
        using var other=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=true};
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();(await other.ActivateProviderAsync()).IsError.Should().BeFalse();
        var writer=CosmicManager(provider,true);var reader=CosmicManager(other,true);var actor=Guid.NewGuid();
        var source=new NativeRoleOwner{Name="typed root"};const string key="typed-role-root-key";
        source.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS]=key;
        var child=new NativeRoleMember{Name="typed component"};child.Children.Add(new Holon{Name="nested child"});source.Components.Add(child);
        var created=await writer.SaveHolonAsync(source,actor,new CosmicWriteOptions(Guid.NewGuid()));
        created.IsError.Should().BeFalse(created.Message+created.Exception);
        var rootVersion=source.VersionId;var childVersion=child.VersionId;
        var loaded=byKey
            ? synchronous ? reader.LoadHolon<NativeRoleOwner>(key,providerType:ProviderType.SQLLiteDBOASIS)
                :await reader.LoadHolonAsync<NativeRoleOwner>(key,providerType:ProviderType.SQLLiteDBOASIS)
            : synchronous ? reader.LoadHolon<NativeRoleOwner>(source.Id,providerType:ProviderType.SQLLiteDBOASIS)
                :await reader.LoadHolonAsync<NativeRoleOwner>(source.Id,providerType:ProviderType.SQLLiteDBOASIS);
        loaded.IsError.Should().BeFalse(loaded.Message+loaded.Exception);loaded.Result.Children.Should().BeEmpty();
        var component=loaded.Result.Components.Should().ContainSingle().Subject;
        component.Name.Should().Be("typed component");component.Children.Should().ContainSingle().Which.Name.Should().Be("nested child");
        loaded.Result.VersionId.Should().Be(rootVersion);component.VersionId.Should().Be(childVersion);loaded.Result.HasHolonChanged().Should().BeFalse();
        component.Name="edited component";
        var saved=synchronous ? reader.SaveHolon(loaded.Result,actor,new CosmicWriteOptions(Guid.NewGuid()))
            :await reader.SaveHolonAsync(loaded.Result,actor,new CosmicWriteOptions(Guid.NewGuid()));
        saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        loaded.Result.PreviousVersionId.Should().Be(rootVersion);component.PreviousVersionId.Should().Be(childVersion);
        var independent=await writer.LoadHolonAsync<NativeRoleOwner>(source.Id,providerType:ProviderType.SQLLiteDBOASIS);
        independent.IsError.Should().BeFalse(independent.Message+independent.Exception);
        independent.Result.Components.Should().ContainSingle().Which.Name.Should().Be("edited component");
        independent.Result.Children.Should().BeEmpty();
    }
}
