using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ExplicitVersionSelectsRootsButNotProviderLinkedDescendants(bool sync)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var parent=Guid.NewGuid();var root=new Holon {Id=Guid.NewGuid(),ParentHolonId=parent,Version=3,Name="root-v3"};
        var child=new Holon {Id=Guid.NewGuid(),ParentHolonId=root.Id,Version=7,Name="child-v7"};
        var grand=new Holon {Id=Guid.NewGuid(),ParentHolonId=child.Id,Version=9,Name="grand-v9"};
        foreach(var record in new[] {root,child,grand}) (await provider.SaveHolonAsync(record,saveChildren:false)).IsError.Should().BeFalse();
        void Graph(IHolon value,bool deep=true)
        {
            value.Id.Should().Be(root.Id);value.Version.Should().Be(3);
            value.Children.Should().ContainSingle();var nested=value.Children.Single();nested.Id.Should().Be(child.Id);nested.Version.Should().Be(7);
            if(deep) {nested.Children.Should().ContainSingle();nested.Children.Single().Id.Should().Be(grand.Id);nested.Children.Single().Version.Should().Be(9);}
            else nested.Children.Should().BeEmpty();
        }
        var byId=sync ? provider.LoadHolon(root.Id,loadChildrenFromProvider:true,version:3):await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true,version:3);
        byId.IsError.Should().BeFalse(byId.Message);Graph(byId.Result);
        var byKey=sync ? provider.LoadHolon(root.Id.ToString(),loadChildrenFromProvider:true,version:3):await provider.LoadHolonAsync(root.Id.ToString(),loadChildrenFromProvider:true,version:3);
        byKey.IsError.Should().BeFalse(byKey.Message);Graph(byKey.Result);
        var all=sync ? provider.LoadAllHolons(HolonType.All,loadChildrenFromProvider:true,version:3):await provider.LoadAllHolonsAsync(HolonType.All,loadChildrenFromProvider:true,version:3);
        all.Result.Should().ContainSingle();Graph(all.Result.Single());
        var roots=sync ? provider.LoadHolonsForParent(parent,HolonType.All,loadChildrenFromProvider:true,version:3):await provider.LoadHolonsForParentAsync(parent,HolonType.All,loadChildrenFromProvider:true,version:3);
        roots.Result.Should().ContainSingle();Graph(roots.Result.Single());
        var shallow=sync ? provider.LoadHolon(root.Id,recursive:false,loadChildrenFromProvider:true,version:3):await provider.LoadHolonAsync(root.Id,recursive:false,loadChildrenFromProvider:true,version:3);
        Graph(shallow.Result,false);
        var bounded=sync ? provider.LoadHolon(root.Id,maxChildDepth:1,loadChildrenFromProvider:true,version:3):await provider.LoadHolonAsync(root.Id,maxChildDepth:1,loadChildrenFromProvider:true,version:3);
        Graph(bounded.Result,false);
        var absent=await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true,version:7);
        absent.Result.Should().BeNull();
    }
}
