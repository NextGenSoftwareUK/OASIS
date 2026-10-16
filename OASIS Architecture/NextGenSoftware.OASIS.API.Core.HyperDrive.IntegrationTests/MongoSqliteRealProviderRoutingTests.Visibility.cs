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
                foreach(var table in new[] {"Holon","Avatar","AvatarDetail"})
                    foreach(var column in new[] {"IsPublic","VersionId","PreviousVersionId","CustomHolonType"})
                        control.Database.ExecuteSqlRaw($"ALTER TABLE \"{table}\" DROP COLUMN \"{column}\"");
            }
            var activated=synchronous ? sqlite.ActivateProvider():await sqlite.ActivateProviderAsync();
            activated.IsError.Should().BeFalse(activated.Message);
            (await sqlite.LoadAvatarAsync(id)).Result.IsPublic.Should().BeFalse();
            (await sqlite.LoadAvatarDetailAsync(id)).Result.IsPublic.Should().BeFalse();
            (await sqlite.LoadHolonAsync(holonId,loadChildren:false)).Result.IsPublic.Should().BeFalse();
            foreach(var old in new NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase[] {
                (await sqlite.LoadAvatarAsync(id)).Result,(await sqlite.LoadAvatarDetailAsync(id)).Result,
                (await sqlite.LoadHolonAsync(holonId,loadChildren:false)).Result})
            {
                old.VersionId.Should().BeEmpty();old.PreviousVersionId.Should().BeEmpty();old.CustomHolonType.Should().BeNull();
            }
            var version=Guid.NewGuid();var previous=Guid.NewGuid();
            foreach(var visible in new[] {true,false})
            {
                avatar.IsPublic=visible;detail.IsPublic=visible;holon.IsPublic=visible;
                foreach(var incoming in new NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase[] {avatar,detail,holon})
                {
                    incoming.VersionId=version;incoming.PreviousVersionId=previous;incoming.CustomHolonType="owned-schema-type";
                }
                (synchronous ? sqlite.SaveAvatar(avatar):await sqlite.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
                (synchronous ? sqlite.SaveAvatarDetail(detail):await sqlite.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
                (synchronous ? sqlite.SaveHolon(holon,saveChildren:false):await sqlite.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
                var loaded=synchronous ? sqlite.LoadAvatar(id):await sqlite.LoadAvatarAsync(id);
                loaded.Result.IsPublic.Should().Be(visible);loaded.Result.FirstName.Should().Be("retained");
                var loadedDetail=synchronous ? sqlite.LoadAvatarDetail(id):await sqlite.LoadAvatarDetailAsync(id);
                loadedDetail.Result.IsPublic.Should().Be(visible);loadedDetail.Result.Karma.Should().Be(31);loadedDetail.Result.XP.Should().Be(47);
                (await sqlite.LoadHolonAsync(holonId,loadChildren:false)).Result.IsPublic.Should().Be(visible);
                foreach(var retained in new NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase[] {loaded.Result,loadedDetail.Result,
                    (await sqlite.LoadHolonAsync(holonId,loadChildren:false)).Result})
                {
                    retained.VersionId.Should().Be(version);retained.PreviousVersionId.Should().Be(previous);retained.CustomHolonType.Should().Be("owned-schema-type");
                }
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
