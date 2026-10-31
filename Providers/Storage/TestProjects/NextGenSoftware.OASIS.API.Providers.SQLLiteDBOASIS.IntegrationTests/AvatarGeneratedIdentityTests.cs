using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;
using FluentAssertions;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;
public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task GeneratedAvatarIdentitiesPublishOnlyAfterAcknowledgement(bool synchronous,bool detail)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=new HashSet<Guid>();
        for(var index=0;index<2;index++)
        {
            var username="owned-generated-"+Guid.NewGuid().ToString("N");
            if(detail)
            {
                var source=new AvatarDetail {Username=username,XP=index+10,IsActive=true};
                var saved=synchronous ? provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);
                saved.IsError.Should().BeFalse(saved.Message);source.Id.Should().NotBeEmpty();saved.Result.Id.Should().Be(source.Id);
                ids.Add(source.Id);(await provider.LoadAvatarDetailAsync(source.Id)).Result.XP.Should().Be(index+10);
            }
            else
            {
                var source=new Avatar {Username=username,Email=username+"@example.invalid",IsActive=true,
                    AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)};
                var saved=synchronous ? provider.SaveAvatar(source):await provider.SaveAvatarAsync(source);
                saved.IsError.Should().BeFalse(saved.Message);source.Id.Should().NotBeEmpty();saved.Result.Id.Should().Be(source.Id);
                saved.Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.SQLLiteDBOASIS].Should().Be(source.Id.ToString());
                ids.Add(source.Id);(await provider.LoadAvatarAsync(source.Id)).Result.Username.Should().Be(username);
            }
        }
        ids.Count.Should().Be(2);
        var badDate=DateTime.SpecifyKind(new DateTime(2026,10,16),DateTimeKind.Unspecified);
        if(detail)
        {
            var source=new AvatarDetail {Username="owned-rejected",DeletedDate=badDate};
            var failed=synchronous ? provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);
            failed.IsError.Should().BeTrue();source.Id.Should().BeEmpty();failed.Result.Should().BeNull();
        }
        else
        {
            var source=new Avatar {Username="owned-rejected",DeletedDate=badDate,
                AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)};
            var failed=synchronous ? provider.SaveAvatar(source):await provider.SaveAvatarAsync(source);
            failed.IsError.Should().BeTrue();source.Id.Should().BeEmpty();failed.Result.Should().BeNull();
        }
        using var control=new DataContext(TestConnectionString);
        if(detail) {control.AvatarDetails.Count().Should().Be(2);control.AvatarDetails.Any(row=>row.Id==Guid.Empty.ToString()).Should().BeFalse();}
        else {control.Avatars.Count().Should().Be(2);control.Avatars.Any(row=>row.Id==Guid.Empty.ToString()).Should().BeFalse();}
    }
}
