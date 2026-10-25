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
    public async Task AcknowledgedDepthFrontierCannotAuthorizeMissingDescendants(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=new Holon();var child=new Holon();var leaf=new Holon();
        root.Children.Add(child);child.Children.Add(leaf);
        (await provider.SaveHolonAsync(root)).IsSaved.Should().BeTrue();
        var saved=synchronous ? provider.SaveHolon(root,maxChildDepth:1)
            :await provider.SaveHolonAsync(root,maxChildDepth:1);
        saved.IsError.Should().BeFalse(saved.Message);saved.IsSaved.Should().BeTrue();
        HolonChangeTracking.ValidateChildSelectionForSave(saved.Result);
        var frontier=saved.Result.Children.Single();frontier.Children.Should().BeEmpty();
        Action admit=()=>HolonChangeTracking.ValidateChildSelectionForSave(frontier);
        admit.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AcknowledgedGraphPublishesMatchingLogicalAndNativeIdentities(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach(var batch in new[] {false,true})
        {
            var root=new Holon();var child=new Holon();root.Children=new List<IHolon> {child};
            IHolon acknowledged;
            if(batch)
            {
                var saved=synchronous ? provider.SaveHolons(new[] {root}):await provider.SaveHolonsAsync(new[] {root});
                saved.IsSaved.Should().BeTrue();acknowledged=saved.Result.Single();
            }
            else
            {
                var saved=synchronous ? provider.SaveHolon(root):await provider.SaveHolonAsync(root);
                saved.IsSaved.Should().BeTrue();acknowledged=saved.Result;
            }
            Action admitAcknowledgement=()=>HolonChangeTracking.ValidateChildSelectionForSave(acknowledged);
            admitAcknowledgement.Should().NotThrow("a complete native acknowledgement must retain its selected membership observation");
            var cold=Core.Serialization.StoredRecordJson.Project<Holon>(acknowledged);
            Action admitSerialized=()=>HolonChangeTracking.ValidateChildSelectionForSave(cold);
            admitSerialized.Should().Throw<InvalidOperationException>();
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
