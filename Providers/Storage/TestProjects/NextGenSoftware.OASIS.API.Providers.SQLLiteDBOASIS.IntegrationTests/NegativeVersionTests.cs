using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;
namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;
public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NegativeVersionFailsBeforeTouchingDisposedDatabase(bool synchronous)
    {
        var context=new DataContext(TestConnectionString);context.Dispose();
        var avatars=new AvatarRepository(context);var details=new AvatarDetailRepository(context);var holons=new HolonRepository(context);var id=Guid.NewGuid();
        void Invalid<T>(OASISResult<T> result) {result.IsError.Should().BeTrue();result.IsLoaded.Should().BeFalse();result.Result.Should().BeNull();result.Exception.Should().BeOfType<ArgumentOutOfRangeException>();}
        Invalid(synchronous ? avatars.LoadAvatar(id,-1):await avatars.LoadAvatarAsync(id,-1));
        Invalid(synchronous ? avatars.LoadAvatarByProviderKey(id.ToString(),-1):await avatars.LoadAvatarByProviderKeyAsync(id.ToString(),-1));
        Invalid(synchronous ? avatars.LoadAvatarByUsername("owned",-1):await avatars.LoadAvatarByUsernameAsync("owned",-1));
        Invalid(synchronous ? avatars.LoadAvatarByEmail("owned@example.invalid",-1):await avatars.LoadAvatarByEmailAsync("owned@example.invalid",-1));
        Invalid(synchronous ? avatars.LoadAllAvatars(-1):await avatars.LoadAllAvatarsAsync(-1));
        Invalid(synchronous ? details.LoadAvatarDetail(id,-1):await details.LoadAvatarDetailAsync(id,-1));
        Invalid(synchronous ? details.LoadAvatarDetailByUsername("owned",-1):await details.LoadAvatarDetailByUsernameAsync("owned",-1));
        Invalid(synchronous ? details.LoadAvatarDetailByEmail("owned@example.invalid",-1):await details.LoadAvatarDetailByEmailAsync("owned@example.invalid",-1));
        Invalid(synchronous ? details.LoadAllAvatarDetails(-1):await details.LoadAllAvatarDetailsAsync(-1));
        Invalid(synchronous ? holons.LoadHolon(id,version:-1):await holons.LoadHolonAsync(id,version:-1));
        Invalid(synchronous ? holons.LoadAllHolons(version:-1):await holons.LoadAllHolonsAsync(version:-1));
        Invalid(synchronous ? holons.LoadHolonsForParent(id,version:-1):await holons.LoadHolonsForParentAsync(id,version:-1));
    }
}
