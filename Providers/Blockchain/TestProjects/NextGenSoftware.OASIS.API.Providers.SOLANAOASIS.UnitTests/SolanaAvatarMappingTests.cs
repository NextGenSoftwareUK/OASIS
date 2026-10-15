using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaAvatarMappingTests
{
    [TestMethod]
    public void AvatarProfileScalarsSurviveDtoSerialization()
    {
        var avatar=new Avatar {Id=Guid.NewGuid(),FirstName="Actual",LastName="Profile",Description="stored description",
            CreatedDate=new DateTime(2026,10,14,1,2,3,DateTimeKind.Utc),ModifiedDate=new DateTime(2026,10,14,2,3,4,DateTimeKind.Utc),
            AvatarType=new NextGenSoftware.Utilities.EnumValue<NextGenSoftware.OASIS.API.Core.Enums.AvatarType>(NextGenSoftware.OASIS.API.Core.Enums.AvatarType.User),
            MetaData=new Dictionary<string,object> {{"parkType","woodland"}}};
        var serialized=Newtonsoft.Json.JsonConvert.SerializeObject(avatar.GetSolanaAvatarDto());
        var loaded=Newtonsoft.Json.JsonConvert.DeserializeObject<NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models.SolanaAvatarDto>(serialized)!.GetAvatar();
        Assert.AreEqual(avatar.FirstName,loaded.FirstName); Assert.AreEqual(avatar.LastName,loaded.LastName);
        Assert.AreEqual(avatar.Description,loaded.Description); Assert.AreEqual(avatar.CreatedDate,loaded.CreatedDate); Assert.AreEqual(avatar.ModifiedDate,loaded.ModifiedDate);
        Assert.AreEqual(avatar.AvatarType.Value,loaded.AvatarType.Value); Assert.AreEqual("woodland",loaded.MetaData["parkType"].ToString());
    }

    [TestMethod]
    public void DetailContactScalarsSurviveDtoSerialization()
    {
        var detail=new AvatarDetail {Id=Guid.NewGuid(),Description="actual detail",Country="country",Town="town",County="county",Postcode="code",Landline="phone",
            ModifiedDate=new DateTime(2026,10,14,2,3,4,DateTimeKind.Utc),MetaData=new Dictionary<string,object> {{"parkType","woodland"}}};
        var serialized=Newtonsoft.Json.JsonConvert.SerializeObject(detail.GetSolanaAvatarDetailDto());
        var loaded=Newtonsoft.Json.JsonConvert.DeserializeObject<NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models.SolanaAvatarDetailDto>(serialized)!.GetAvatarDetail();
        Assert.AreEqual(detail.Country,loaded.Country); Assert.AreEqual(detail.Town,loaded.Town); Assert.AreEqual(detail.County,loaded.County);
        Assert.AreEqual(detail.Postcode,loaded.Postcode); Assert.AreEqual(detail.Landline,loaded.Landline);
        Assert.AreEqual(detail.Description,loaded.Description); Assert.AreEqual(detail.ModifiedDate,loaded.ModifiedDate);
        Assert.AreEqual("woodland",loaded.MetaData["parkType"].ToString());
    }

    [TestMethod]
    public void InvalidStoredAvatarTypeIsNotInvented()
    {
        var dto=new NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models.SolanaAvatarDto {AvatarType="not-a-type"};
        Assert.ThrowsException<InvalidOperationException>(()=>dto.GetAvatar());
    }

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
