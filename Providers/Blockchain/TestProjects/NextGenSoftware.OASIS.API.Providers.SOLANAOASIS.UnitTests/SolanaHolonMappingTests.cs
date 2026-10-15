using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaHolonMappingTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StoredHolonRetainsParentTypeAndAuditFields(bool deleted)
    {
        var holon=new Holon {Id=Guid.NewGuid(),ParentHolonId=Guid.NewGuid(),HolonType=HolonType.Holon,CustomHolonType="park-object",
            Name="actual holon",Description="stored description",CreatedByAvatarId=Guid.NewGuid(),ModifiedByAvatarId=Guid.NewGuid(),DeletedByAvatarId=Guid.NewGuid(),
            IsActive=!deleted,DeletedDate=deleted ? new DateTime(2026,10,14,1,2,3,DateTimeKind.Utc).AddTicks(123):DateTime.MinValue,
            Version=7,PreviousVersionId=Guid.NewGuid()};
        var loaded=JsonConvert.DeserializeObject<SolanaHolonDto>(JsonConvert.SerializeObject(holon.GetSolanaHolonDto()))!.GetHolon();
        Assert.AreEqual(holon.Id,loaded.Id); Assert.AreEqual(holon.ParentHolonId,loaded.ParentHolonId);
        Assert.AreEqual(holon.HolonType,loaded.HolonType); Assert.AreEqual(holon.CustomHolonType,loaded.CustomHolonType);
        Assert.AreEqual(holon.CreatedByAvatarId,loaded.CreatedByAvatarId); Assert.AreEqual(holon.ModifiedByAvatarId,loaded.ModifiedByAvatarId);
        Assert.AreEqual(holon.DeletedByAvatarId,loaded.DeletedByAvatarId); Assert.AreEqual(holon.DeletedDate,loaded.DeletedDate);
        Assert.AreEqual(holon.IsActive,loaded.IsActive); Assert.AreEqual(holon.Version,loaded.Version); Assert.AreEqual(holon.PreviousVersionId,loaded.PreviousVersionId);
    }
}
