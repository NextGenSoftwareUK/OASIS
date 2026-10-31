using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public static IEnumerable<object[]> ParentTraversalCases()
    {
        foreach (var synchronous in new[] { false, true })
        foreach (var byKey in new[] { false, true })
        foreach (var recursive in new[] { false, true })
        foreach (var depth in new[] { 0, 1, 2 })
            yield return new object[] { synchronous, byKey, recursive, depth };
    }

    [Theory]
    [MemberData(nameof(ParentTraversalCases))]
    public async Task ParentLookupCountsReturnedChildrenAtDepthOne(bool synchronous, bool byKey, bool recursive, int depth)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var grand = new Holon { Name = "grandchild" };
        var child = new Holon { Name = "child", Children = new List<IHolon> { grand } };
        var root = new Holon { Name = "root", Children = new List<IHolon> { child } };
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        var key = root.Id.ToString();
        var found = byKey
            ? synchronous ? provider.LoadHolonsForParent(key, recursive: recursive, maxChildDepth: depth)
                : await provider.LoadHolonsForParentAsync(key, recursive: recursive, maxChildDepth: depth)
            : synchronous ? provider.LoadHolonsForParent(root.Id, recursive: recursive, maxChildDepth: depth)
                : await provider.LoadHolonsForParentAsync(root.Id, recursive: recursive, maxChildDepth: depth);
        found.IsError.Should().BeFalse(found.Message + found.Exception);
        found.Result.Should().ContainSingle().Which.Id.Should().Be(child.Id);
        found.Result.Single().Children.Count().Should().Be(recursive && (depth == 0 || depth > 1) ? 1 : 0,
            "the returned child is already at depth one relative to the queried parent");
        var offset = byKey
            ? synchronous ? provider.LoadHolonsForParent(key, maxChildDepth: 2, curentChildDepth: 1)
                : await provider.LoadHolonsForParentAsync(key, maxChildDepth: 2, curentChildDepth: 1)
            : synchronous ? provider.LoadHolonsForParent(root.Id, maxChildDepth: 2, curentChildDepth: 1)
                : await provider.LoadHolonsForParentAsync(root.Id, maxChildDepth: 2, curentChildDepth: 1);
        offset.IsError.Should().BeFalse(offset.Message + offset.Exception);
        offset.Result.Single().Children.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InvalidTraversalArgumentsFailEvenWhenChildrenAreDisabled(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = new Holon { Name = "depth-validation" };
        (await provider.SaveHolonAsync(source)).IsError.Should().BeFalse();
        var single = synchronous ? provider.LoadHolon(source.Id, loadChildren: false, maxChildDepth: -1)
            : await provider.LoadHolonAsync(source.Id, loadChildren: false, maxChildDepth: -1);
        single.IsError.Should().BeTrue("negative traversal input is invalid independently of whether any rows/children exist");
        var parent = synchronous ? provider.LoadHolonsForParent(source.Id, loadChildren: false, curentChildDepth: -1)
            : await provider.LoadHolonsForParentAsync(source.Id, loadChildren: false, curentChildDepth: -1);
        parent.IsError.Should().BeTrue();
    }

    public static IEnumerable<object[]> MaximumCurrentDepthCases()
    {
        foreach(var synchronous in new[]{false,true})
        foreach(var byKey in new[]{false,true})
        foreach(var recursive in new[]{false,true})
        foreach(var loadChildren in new[]{false,true})
        foreach(var maximum in new[]{0,int.MaxValue})
            yield return new object[]{synchronous,byKey,recursive,loadChildren,maximum};
    }
    [Theory]
    [MemberData(nameof(MaximumCurrentDepthCases))]
    public async Task ParentLookupAcceptsMaximumCurrentDepthWithoutWrapOrTruncation(bool synchronous,bool byKey,
        bool recursive,bool loadChildren,int maximum)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var grand=new Holon{Name="boundary-grandchild"};
        var child=new Holon{Name="boundary-child",Children=new List<IHolon>{grand}};
        var root=new Holon{Name="boundary-root",Children=new List<IHolon>{child}};
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        var key=root.Id.ToString();
        var result=byKey
            ? synchronous ? provider.LoadHolonsForParent(key,loadChildren:loadChildren,recursive:recursive,maxChildDepth:maximum,curentChildDepth:int.MaxValue)
                : await provider.LoadHolonsForParentAsync(key,loadChildren:loadChildren,recursive:recursive,maxChildDepth:maximum,curentChildDepth:int.MaxValue)
            : synchronous ? provider.LoadHolonsForParent(root.Id,loadChildren:loadChildren,recursive:recursive,maxChildDepth:maximum,curentChildDepth:int.MaxValue)
                : await provider.LoadHolonsForParentAsync(root.Id,loadChildren:loadChildren,recursive:recursive,maxChildDepth:maximum,curentChildDepth:int.MaxValue);
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        result.Result.Should().ContainSingle().Which.Id.Should().Be(child.Id);
        result.Result.Single().Children.Count.Should().Be(loadChildren && recursive && maximum==0 ? 1:0);
        root.Children.Single().Children.Count.Should().Be(1,"reading does not alter the caller's graph");
    }
    [Theory]
    [InlineData(false,0)] [InlineData(true,0)]
    [InlineData(false,int.MaxValue)] [InlineData(true,int.MaxValue)]
    public async Task CollectionLookupAcceptsMaximumCurrentDepth(bool synchronous,int maximum)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var child=new Holon{Name="collection-boundary-child"};
        var root=new Holon{Name="collection-boundary-root",Children=new List<IHolon>{child}};
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        var result=synchronous ? provider.LoadAllHolons(maxChildDepth:maximum,curentChildDepth:int.MaxValue)
            : await provider.LoadAllHolonsAsync(maxChildDepth:maximum,curentChildDepth:int.MaxValue);
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        result.Result.Single(row=>row.Id==root.Id).Children.Count.Should().Be(maximum==0 ? 1:0);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task UnlimitedNativeHydrationPreservesADeepGraphWithoutCallStackTraversal(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var rows=Enumerable.Range(0,2048).Select(index=>new Holon{Id=Guid.NewGuid(),Name="deep-native-"+index}).ToArray();
        for(var index=1;index<rows.Length;index++)
        {rows[index].ParentHolonId=rows[index-1].Id;rows[index-1].Children.Add(rows[index]);}
        var saved=await provider.SaveHolonAsync(rows[0]);saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        var found=synchronous ? provider.LoadHolon(rows[0].Id,maxChildDepth:0)
            : await provider.LoadHolonAsync(rows[0].Id,maxChildDepth:0);
        found.IsError.Should().BeFalse(found.Message+found.Exception);
        var current=found.Result;
        for(var index=0;index<rows.Length;index++)
        {
            current.Id.Should().Be(rows[index].Id);current.Name.Should().Be(rows[index].Name);
            current.Children.Count.Should().Be(index+1<rows.Length ? 1:0);
            if(index+1<rows.Length)current=current.Children.Single();
        }
        var bounded=synchronous ? provider.LoadHolon(rows[0].Id,maxChildDepth:1)
            : await provider.LoadHolonAsync(rows[0].Id,maxChildDepth:1);
        bounded.IsError.Should().BeFalse(bounded.Message+bounded.Exception);
        bounded.Result.Children.Should().ContainSingle().Which.Children.Should().BeEmpty();
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeHydrationRejectsSelectedCyclesButHonorsTheDepthFrontier(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var child=new Holon{Name="cycle-child"};var root=new Holon{Name="cycle-root",Children=new List<IHolon>{child}};
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))
        {control.Holons.Single(row=>row.Id==root.Id.ToString()).ParentHolonId=child.Id.ToString();await control.SaveChangesAsync();}
        var invalid=synchronous ? provider.LoadHolon(root.Id):await provider.LoadHolonAsync(root.Id);
        invalid.IsError.Should().BeTrue();invalid.Result.Should().BeNull();
        invalid.Exception.Should().BeOfType<InvalidOperationException>();invalid.Message.Should().Contain("cycle");
        var bounded=synchronous ? provider.LoadHolon(root.Id,maxChildDepth:1):await provider.LoadHolonAsync(root.Id,maxChildDepth:1);
        bounded.IsError.Should().BeFalse(bounded.Message+bounded.Exception);
        bounded.Result.Children.Should().ContainSingle().Which.Children.Should().BeEmpty();
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeHydrationRetainsItsExistingDeletedChildVisibility(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var grand=new Holon{Name="retained-grandchild"};
        var child=new Holon{Name="retained-child",Children=new List<IHolon>{grand}};
        var root=new Holon{Name="visibility-root",Children=new List<IHolon>{child}};
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))
        {control.Holons.Single(row=>row.Id==child.Id.ToString()).DeletedDate=DateTime.UtcNow;await control.SaveChangesAsync();}
        var loaded=synchronous ? provider.LoadHolon(root.Id):await provider.LoadHolonAsync(root.Id);
        loaded.IsError.Should().BeFalse(loaded.Message+loaded.Exception);loaded.Result.Children.Should().BeEmpty();
        var retained=synchronous ? provider.LoadHolon(child.Id,loadChildren:false):await provider.LoadHolonAsync(child.Id,loadChildren:false);
        retained.IsError.Should().BeFalse(retained.Message+retained.Exception);retained.Result.Id.Should().Be(child.Id);
        retained.Result.DeletedDate.Should().NotBe(DateTime.MinValue);
    }
}
