using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifySqliteVisibilityUpgradeAsync(SQLLiteDBOASIS sqlite,string ownedFixturePath,bool synchronous)
    {
        var id=Guid.NewGuid();var holonId=Guid.NewGuid();var name="visibility-"+id.ToString("N");
        var avatar=new Avatar {Id=id,Username=name,Email=name+"@test.local",FirstName="retained",AvatarType=new EnumValue<AvatarType>(AvatarType.User)};
        var detail=new AvatarDetail {Id=id,Username=name,Email=avatar.Email,Karma=31,XP=47};
        var holon=new Holon {Id=holonId,Name="existing private row"};
        try
        {
            (await sqlite.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
            (await sqlite.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
            (await sqlite.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
            // Only this suite's exclusively owned, synthetic DB. Simulate the
            // prior EnsureCreated schema without touching any user database.
            using(var control=new DataContext($"Data Source={ownedFixturePath};Pooling=False"))
            {
                control.Database.ExecuteSqlRaw("ALTER TABLE Holon DROP COLUMN IsPublic");
                control.Database.ExecuteSqlRaw("ALTER TABLE Avatar DROP COLUMN IsPublic");
                control.Database.ExecuteSqlRaw("ALTER TABLE AvatarDetail DROP COLUMN IsPublic");
            }
            var activated=synchronous ? sqlite.ActivateProvider():await sqlite.ActivateProviderAsync();
            activated.IsError.Should().BeFalse(activated.Message);
            (await sqlite.LoadAvatarAsync(id)).Result.IsPublic.Should().BeFalse();
            (await sqlite.LoadAvatarDetailAsync(id)).Result.IsPublic.Should().BeFalse();
            (await sqlite.LoadHolonAsync(holonId,loadChildren:false)).Result.IsPublic.Should().BeFalse();
            foreach(var visible in new[] {true,false})
            {
                avatar.IsPublic=visible;detail.IsPublic=visible;holon.IsPublic=visible;
                (synchronous ? sqlite.SaveAvatar(avatar):await sqlite.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
                (synchronous ? sqlite.SaveAvatarDetail(detail):await sqlite.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
                (synchronous ? sqlite.SaveHolon(holon,saveChildren:false):await sqlite.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
                var loaded=synchronous ? sqlite.LoadAvatar(id):await sqlite.LoadAvatarAsync(id);
                loaded.Result.IsPublic.Should().Be(visible);loaded.Result.FirstName.Should().Be("retained");
                var loadedDetail=synchronous ? sqlite.LoadAvatarDetail(id):await sqlite.LoadAvatarDetailAsync(id);
                loadedDetail.Result.IsPublic.Should().Be(visible);loadedDetail.Result.Karma.Should().Be(31);loadedDetail.Result.XP.Should().Be(47);
                (await sqlite.LoadHolonAsync(holonId,loadChildren:false)).Result.IsPublic.Should().Be(visible);
            }
            // Reopening an already upgraded schema is idempotent.
            (synchronous ? sqlite.ActivateProvider():await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        }
        finally
        {
            (await sqlite.DeleteAvatarAsync(id,softDelete:false)).IsError.Should().BeFalse();
            (await sqlite.DeleteHolonAsync(holonId)).IsError.Should().BeFalse();
        }
    }
}
