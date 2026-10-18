using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public static IEnumerable<object[]> RelationshipValidationCases()=>from kind in new[]{"provider","dimension","celestial"}
        from synchronous in new[]{false,true} from existing in new[]{false,true} select new object[]{kind,synchronous,existing};

    [Theory][MemberData(nameof(RelationshipValidationCases))]
    public async Task RejectedRelationshipIdentityCannotLeakThroughLaterUnrelatedSave(string kind,bool synchronous,bool existing)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var original=new AvatarDetail{Id=Guid.NewGuid(),Username="original-links",XP=47,Karma=83};
        original.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]="original";
        original.DimensionLevelIds[DimensionLevel.First]=Guid.NewGuid();original.ParentStarId=Guid.NewGuid();
        (await provider.SaveAvatarDetailAsync(original)).IsError.Should().BeFalse();
        var proposed=new AvatarDetail{Id=existing?original.Id:Guid.Empty,Username="rejected-links",XP=999,Karma=999};
        if(kind=="provider")proposed.ProviderUniqueStorageKey[(ProviderType)999]="invalid";
        if(kind=="dimension")proposed.DimensionLevelIds[(DimensionLevel)999]=Guid.NewGuid();
        if(kind=="celestial")proposed.DimensionLevel=(DimensionLevel)999;
        var rejected=synchronous?provider.SaveAvatarDetail(proposed):await provider.SaveAvatarDetailAsync(proposed);
        rejected.IsError.Should().BeTrue();rejected.Result.Should().BeNull();rejected.IsSaved.Should().BeFalse();rejected.Exception.Should().NotBeNull();
        proposed.Id.Should().Be(existing?original.Id:Guid.Empty,"a rejected insert must not publish a generated identity");
        var unrelated=new AvatarDetail{Id=Guid.NewGuid(),Username="unrelated-links",XP=13};
        var later=synchronous?provider.SaveAvatarDetail(unrelated):await provider.SaveAvatarDetailAsync(unrelated);later.IsError.Should().BeFalse(later.Exception?.ToString());
        using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var restored=await reader.LoadAvatarDetailAsync(original.Id);restored.IsError.Should().BeFalse(restored.Exception?.ToString());
        restored.Result.Username.Should().Be(original.Username);restored.Result.XP.Should().Be(47);restored.Result.Karma.Should().Be(83);
        restored.Result.ProviderUniqueStorageKey.Should().BeEquivalentTo(original.ProviderUniqueStorageKey);
        restored.Result.DimensionLevelIds.Should().BeEquivalentTo(original.DimensionLevelIds);restored.Result.ParentStarId.Should().Be(original.ParentStarId);
        using var native=new DataContext(TestConnectionString);
        native.AvatarDetails.AsNoTracking().Count().Should().Be(2);native.AvatarDetails.AsNoTracking().Any(row=>row.Username=="rejected-links").Should().BeFalse();
    }
}
