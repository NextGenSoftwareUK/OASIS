using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaAvatarMappingTests
{
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AvatarDeletionFlagHasInverseActiveMeaning(bool active)
    {
        var avatar = new Avatar { Id = Guid.NewGuid(), IsActive = active, Version = 3 };
        var dto = avatar.GetSolanaAvatarDto();
        Assert.AreEqual(!active, dto.IsDeleted);
        Assert.AreEqual(active, dto.GetAvatar().IsActive);
        Assert.AreEqual(avatar.Id, dto.GetAvatar().Id);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void DetailRetainsIdentityProgressAndActiveState(bool active)
    {
        var detail = new AvatarDetail { Id = Guid.NewGuid(), IsActive = active,
            Username = "synthetic-avatar", Email = "fixture@example.invalid", XP = 321, Karma = 654,
            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var dto = detail.GetSolanaAvatarDetailDto();
        Assert.AreEqual(!active, dto.IsDeleted);
        var loaded = dto.GetAvatarDetail();
        Assert.AreEqual(active, loaded.IsActive);
        Assert.AreEqual(detail.Id, loaded.Id);
        Assert.AreEqual(detail.Username, loaded.Username);
        Assert.AreEqual(detail.Email, loaded.Email);
        Assert.AreEqual(detail.Level, loaded.Level);
        Assert.AreEqual(detail.XP, loaded.XP);
        Assert.AreEqual(detail.Karma, loaded.Karma);
        Assert.AreEqual(detail.CreatedDate, loaded.CreatedDate);
    }
}
