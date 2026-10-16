using System.Reflection;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
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
