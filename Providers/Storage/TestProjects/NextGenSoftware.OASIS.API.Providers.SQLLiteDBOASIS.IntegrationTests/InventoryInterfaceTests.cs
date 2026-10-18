using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task InventoryExtensionUpgradeRetainsOldColumnsAndSupportsNewValues(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var item=new InventoryItem{Id=Guid.NewGuid(),Name="old-column-item",Quantity=7};
        var detail=new AvatarDetail{Id=Guid.NewGuid(),Username="extension-upgrade",Inventory=new List<IInventoryItem>{item}};
        (await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))
            control.Database.ExecuteSqlRaw("ALTER TABLE AvatarInventory DROP COLUMN ExtendedValueJson");
        var activated=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();
        activated.IsError.Should().BeFalse(activated.Message);
        var restored=await provider.LoadAvatarDetailAsync(detail.Id);
        restored.IsError.Should().BeFalse(restored.Message);
        restored.Result.Inventory.Single().Name.Should().Be(item.Name);
        restored.Result.Inventory.Single().Quantity.Should().Be(7);
        restored.Result.Inventory.Single().Version.Should().Be(0);
        item.Version=13;item.VersionId=Guid.NewGuid();
        var saved=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);
        saved.IsError.Should().BeFalse(saved.Message);
        using var reader=new SQLLiteDBOASIS(TestConnectionString);
        (await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actual=(await reader.LoadAvatarDetailAsync(detail.Id)).Result.Inventory.Single();
        actual.Version.Should().Be(13);actual.VersionId.Should().Be(item.VersionId);actual.Quantity.Should().Be(7);
    }

    [Theory]
    [InlineData(true,"null")] [InlineData(false,"null")]
    [InlineData(true,"[")] [InlineData(false,"[")]
    [InlineData(true,"[]")] [InlineData(false,"[]")]
    [InlineData(true,"{\"Quantity\":99}")] [InlineData(false,"{\"Quantity\":99}")]
    [InlineData(true,"{\"quantity\":99}")] [InlineData(false,"{\"quantity\":99}")]
    [InlineData(true,"{\"MetaData\":{\"fake\":true}}")] [InlineData(false,"{\"MetaData\":{\"fake\":true}}")]
    [InlineData(true,"{\"Children\":[]}")] [InlineData(false,"{\"Children\":[]}")]
    public async Task CorruptInventoryExtensionDoesNotReturnPartialSuccess(bool synchronous,string corrupt)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var detail=new AvatarDetail{Id=Guid.NewGuid(),Username="extension-corrupt",Inventory=new List<IInventoryItem>{new InventoryItem{Id=Guid.NewGuid(),Name="corrupt-extension"}}};
        var healthy=new AvatarDetail{Id=Guid.NewGuid(),Username="extension-healthy"};
        (await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
        (await provider.SaveAvatarDetailAsync(healthy)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))
            control.Database.ExecuteSqlRaw("UPDATE AvatarInventory SET ExtendedValueJson={0} WHERE AvatarId={1}",corrupt,detail.Id.ToString());
        using var reader=new SQLLiteDBOASIS(TestConnectionString);
        (await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var failed=synchronous?reader.LoadAvatarDetail(detail.Id):await reader.LoadAvatarDetailAsync(detail.Id);
        failed.IsError.Should().BeTrue();failed.Result.Should().BeNull();
        var unaffected=synchronous?reader.LoadAvatarDetail(healthy.Id):await reader.LoadAvatarDetailAsync(healthy.Id);
        unaffected.IsError.Should().BeFalse(unaffected.Message);unaffected.Result.Id.Should().Be(healthy.Id);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task InventoryInheritedOwnedValuesSurviveIndependentRead(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var detail=new AvatarDetail{Id=Guid.NewGuid(),Username="inherited-inventory"};
        for(var revision=1;revision<=2;revision++){
            var item=new InventoryItem{Id=Guid.NewGuid(),Name="owned-"+revision,Version=revision+10,
                VersionId=Guid.NewGuid(),PreviousVersionId=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),
                CreatedDate=new DateTime(2020+revision,1,2,3,4,5,DateTimeKind.Utc),IsPublic=revision==1,
                CustomHolonType="custom-"+revision,ParentHolonId=Guid.NewGuid(),ParentHolonId2=Guid.NewGuid(),ParentHolonId3=Guid.NewGuid()};
            item.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS]="linked-"+revision;
            item.Children.Add(new Holon{Id=Guid.NewGuid(),Name="not-an-owned-navigation-value"});
            var proxy=DispatchProxy.Create<IInventoryItem,InventoryInterfaceProxy>();
            ((InventoryInterfaceProxy)(object)proxy).Item=item;
            detail.Inventory=new List<IInventoryItem>{proxy};
            var saved=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);
            saved.IsError.Should().BeFalse(saved.Message);
            using var reader=new SQLLiteDBOASIS(TestConnectionString);
            (await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
            var loaded=synchronous?reader.LoadAvatarDetail(detail.Id):await reader.LoadAvatarDetailAsync(detail.Id);
            loaded.IsError.Should().BeFalse(loaded.Message);
            var actual=loaded.Result.Inventory.Should().ContainSingle().Subject;
            foreach(var name in new[]{"Id","Name","Version","VersionId","PreviousVersionId","CreatedByAvatarId","CreatedDate",
                "IsPublic","CustomHolonType","ParentHolonId","ParentHolonId2","ParentHolonId3"}){
                var property=typeof(InventoryItem).GetProperty(name)!;
                property.GetValue(actual).Should().Be(property.GetValue(item),name);
            }
            actual.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS].Should().Be("linked-"+revision);
            actual.Children.Should().BeEmpty();
            using var control=new DataContext(TestConnectionString);
            var stored=control.Set<NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.InventoryItemModel>().AsNoTracking()
                .Single(row=>row.AvatarId==detail.Id.ToString());
            var extended=Newtonsoft.Json.Linq.JObject.Parse(stored.ExtendedValueJson);
            extended.ContainsKey("Quantity").Should().BeFalse();extended.ContainsKey("Image2D").Should().BeFalse();
            extended.ContainsKey("Children").Should().BeFalse();((int)extended["Version"]!).Should().Be(item.Version);
        }
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task LegacyInventoryPublishedPackageUpgradePreservesItem(bool synchronous)
    {
        var item=new InventoryItem {Id=Guid.NewGuid(),Name="legacy-package",Quantity=4};
        var source=new AvatarDetail {Id=Guid.NewGuid(),Username="legacy-package",Inventory=new List<IInventoryItem>{item}};
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveAvatarDetailAsync(source)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))
            control.Database.ExecuteSqlRaw("ALTER TABLE AvatarInventory DROP COLUMN PublishedSTARNETHolon");
        var activated=synchronous ? provider.ActivateProvider():await provider.ActivateProviderAsync();
        activated.IsError.Should().BeFalse(activated.Message);
        var loaded=(await provider.LoadAvatarDetailAsync(source.Id)).Result.Inventory.Should().ContainSingle().Subject;
        loaded.Id.Should().Be(item.Id);loaded.Quantity.Should().Be(4);loaded.Name.Should().Be(item.Name);
        loaded.PublishedSTARNETHolon.Should().BeNull();
        item.PublishedSTARNETHolon=new byte[] {0,255,19};
        var saved=synchronous ? provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);
        saved.IsError.Should().BeFalse(saved.Message);
        using var reader=new SQLLiteDBOASIS(TestConnectionString);
        (await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await reader.LoadAvatarDetailAsync(source.Id)).Result.Inventory.Single().PublishedSTARNETHolon.Should().Equal(item.PublishedSTARNETHolon);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task LegacyInventoryMediaUpgradePreservesItem(bool synchronous)
    {
        var item=new InventoryItem {Id=Guid.NewGuid(),Name="legacy-media-item",Quantity=7,
            Image2DURI=new Uri("https://example.test/retained.png")};
        var source=new AvatarDetail {Id=Guid.NewGuid(),Username="legacy-media",Inventory=new List<IInventoryItem>{item}};
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveAvatarDetailAsync(source)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))
            foreach(var column in new[] {"Image2D","Object3D","Object3DURI"})
                control.Database.ExecuteSqlRaw($"ALTER TABLE AvatarInventory DROP COLUMN {column}");
        var activated=synchronous ? provider.ActivateProvider():await provider.ActivateProviderAsync();
        activated.IsError.Should().BeFalse(activated.Message);
        var loaded=await provider.LoadAvatarDetailAsync(source.Id);
        loaded.IsError.Should().BeFalse(loaded.Message);
        var actual=loaded.Result.Inventory.Should().ContainSingle().Subject;
        actual.Id.Should().Be(item.Id);actual.Quantity.Should().Be(7);actual.Name.Should().Be(item.Name);
        actual.Image2D.Should().BeNull();actual.Object3D.Should().BeNull();actual.Object3DURI.Should().BeNull();
        actual.Image2DURI.Should().Be(item.Image2DURI);
    }

    public class InventoryInterfaceProxy : DispatchProxy
    {
        public InventoryItem Item=new();
        protected override object? Invoke(MethodInfo? method,object?[]? args) => method!.Invoke(Item,args);
    }

    [Theory]
    [InlineData(true,true)] [InlineData(false,true)]
    [InlineData(true,false)] [InlineData(false,false)]
    public async Task CustomInventoryInterfaceSurvivesCreateUpdateAndClear(bool synchronous,bool startEmpty)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail {Id=Guid.NewGuid(),Username="inventory-interface",Karma=71};
        var item=DispatchProxy.Create<IInventoryItem,InventoryInterfaceProxy>();
        item.Should().NotBeAssignableTo<InventoryItem>();
        item.Id=Guid.NewGuid();item.Name="owned-interface-seed";item.Description="interface payload";
        item.GameSource="Our World";item.Rarity="Rare";item.Stack=true;item.MaxQuantity=99;
        item.NftId=Guid.NewGuid();item.GeoNFTId=Guid.NewGuid();item.Weight=1.25f;
        item.IsUsable=true;item.IsTradeable=true;
        item.Properties=new Dictionary<string,object>{{"species","oak"}};
        item.MetaData=new Dictionary<string,object>{{"source","owned-fixture"}};
        foreach(var quantity in startEmpty ? new[] {0,3,7,0,2}:new[] {3,7,0,2})
        {
            item.Quantity=quantity;
            item.Image2D=quantity==7 ? null:new byte[] {0,17,255};
            item.Object3D=quantity==7 ? null:new byte[] {255,0,23,41};
            item.PublishedSTARNETHolon=quantity==7 ? null:new byte[] {19,0,255,42};
            item.Image2DURI=quantity==7 ? null:new Uri("https://example.test/seed.png?variant="+quantity);
            item.Object3DURI=quantity==7 ? null:new Uri("models/seed-"+quantity+".glb",UriKind.Relative);
            source.Inventory=quantity==0 ? new List<IInventoryItem>():new List<IInventoryItem>{item};
            var saved=synchronous ? provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);
            saved.IsError.Should().BeFalse(saved.Message);saved.IsSaved.Should().BeTrue();
            using var reader=new SQLLiteDBOASIS(TestConnectionString);
            (await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
            var loaded=(await reader.LoadAvatarDetailAsync(source.Id)).Result;
            loaded.Karma.Should().Be(71);
            if(quantity==0) loaded.Inventory.Should().BeEmpty();
            else
            {
                var actual=loaded.Inventory.Should().ContainSingle().Subject;
                actual.Id.Should().Be(item.Id);actual.Quantity.Should().Be(quantity);
                actual.Image2D.Should().BeEquivalentTo(item.Image2D,options=>options.WithStrictOrdering());
                actual.Object3D.Should().BeEquivalentTo(item.Object3D,options=>options.WithStrictOrdering());
                actual.PublishedSTARNETHolon.Should().BeEquivalentTo(item.PublishedSTARNETHolon,options=>options.WithStrictOrdering());
                (actual.Image2DURI?.OriginalString).Should().Be(item.Image2DURI?.OriginalString);
                (actual.Object3DURI?.OriginalString).Should().Be(item.Object3DURI?.OriginalString);
                actual.Name.Should().Be(item.Name);actual.Description.Should().Be(item.Description);
                actual.GameSource.Should().Be(item.GameSource);actual.Rarity.Should().Be(item.Rarity);
                actual.NftId.Should().Be(item.NftId);actual.GeoNFTId.Should().Be(item.GeoNFTId);
                actual.Weight.Should().Be(item.Weight);actual.IsTradeable.Should().BeTrue();actual.IsUsable.Should().BeTrue();
                actual.MaxQuantity.Should().Be(99);actual.Stack.Should().BeTrue();
                actual.Properties["species"].ToString().Should().Be("oak");
                actual.MetaData["source"].ToString().Should().Be("owned-fixture");
            }
        }
    }
}
