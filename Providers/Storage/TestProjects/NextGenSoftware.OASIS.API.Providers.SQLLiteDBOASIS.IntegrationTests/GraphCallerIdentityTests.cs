using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;
using FluentAssertions;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;
public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AcknowledgedGraphPublishesMatchingLogicalAndNativeIdentities(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach(var batch in new[] {false,true})
        {
            var root=new Holon();var child=new Holon();root.Children=new List<IHolon> {child};
            if(batch) (synchronous ? provider.SaveHolons(new[] {root}):await provider.SaveHolonsAsync(new[] {root})).IsSaved.Should().BeTrue();
            else (synchronous ? provider.SaveHolon(root):await provider.SaveHolonAsync(root)).IsSaved.Should().BeTrue();
            root.Id.Should().NotBeEmpty();child.Id.Should().NotBeEmpty();child.ParentHolonId.Should().Be(root.Id);
            foreach(var source in new[] {root,child})
            {
                var loaded=await provider.LoadHolonAsync(source.Id,loadChildren:false);
                loaded.IsError.Should().BeFalse();loaded.Result.Id.Should().Be(source.Id);
                loaded.Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.SQLLiteDBOASIS].Should().Be(source.Id.ToString());
                using var control=new DataContext(TestConnectionString);
                control.Holons.Single(row=>row.Id==source.Id.ToString()).LocalProviderKey.Should().Be(source.Id.ToString());
            }
        }
    }

    [Theory]
    [InlineData(false,"null")] [InlineData(true,"null")]
    [InlineData(false,"cycle")] [InlineData(true,"cycle")]
    [InlineData(false,"parent")] [InlineData(true,"parent")]
    [InlineData(false,"duplicate")] [InlineData(true,"duplicate")]
    [InlineData(false,"write")] [InlineData(true,"write")]
    public async Task FailedGraphDoesNotPublishCallerIdentities(bool synchronous,string invalid)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach(var batch in new[] {false,true})
        {
            var root=new Holon();var child=new Holon {ParentHolonId=invalid=="parent" ? Guid.NewGuid():Guid.Empty};
            var originalParent=child.ParentHolonId;
            root.Children=new List<IHolon> {child};
            if(invalid=="null") root.Children.Add(null);
            if(invalid=="cycle") child.Children=new List<IHolon> {root};
            if(invalid=="duplicate") root.Children.Add(child);
            if(invalid=="write") root.DeletedDate=DateTime.SpecifyKind(new DateTime(2026,10,16),DateTimeKind.Unspecified);
            bool failed;
            if(batch) failed=(synchronous ? provider.SaveHolons(new[] {root}):await provider.SaveHolonsAsync(new[] {root})).IsError;
            else failed=(synchronous ? provider.SaveHolon(root):await provider.SaveHolonAsync(root)).IsError;
            failed.Should().BeTrue();root.Id.Should().BeEmpty();child.Id.Should().BeEmpty();
            child.ParentHolonId.Should().Be(originalParent);
            using var control=new DataContext(TestConnectionString);control.Holons.Count().Should().Be(0);
        }
    }
}
