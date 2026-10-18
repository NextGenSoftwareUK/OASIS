using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaAvatarAuditMappingTests
{
    [DataTestMethod]
    [DataRow("avatar")][DataRow("detail")][DataRow("holon")]
    public void AuditAndParentFieldsSurviveCanonicalStoredRecord(string kind)
    {
        ISemanticHolon source=kind switch{"avatar"=>new Avatar(),"detail"=>new AvatarDetail(),_=>new Holon()};
        source.Id=Guid.NewGuid();source.CreatedByAvatarId=Guid.NewGuid();source.ModifiedByAvatarId=Guid.NewGuid();
        source.DeletedByAvatarId=Guid.NewGuid();source.DeletedDate=new DateTime(2024,2,3,4,5,6,DateTimeKind.Utc);
        source.ParentHolonId=Guid.NewGuid();source.CustomHolonType="stored-custom-type";
        ISemanticHolon loaded=kind switch{
            "avatar"=>OasisJson.Deserialize<SolanaAvatarDto>(OasisJson.Serialize(((IAvatar)source).GetSolanaAvatarDto())).GetAvatar(),
            "detail"=>OasisJson.Deserialize<SolanaAvatarDetailDto>(OasisJson.Serialize(((IAvatarDetail)source).GetSolanaAvatarDetailDto())).GetAvatarDetail(),
            _=>OasisJson.Deserialize<SolanaHolonDto>(OasisJson.Serialize(((IHolon)source).GetSolanaHolonDto())).GetHolon()};
        foreach(var field in new[]{"CreatedByAvatarId","ModifiedByAvatarId","DeletedByAvatarId","DeletedDate","ParentHolonId","CustomHolonType"}){
            Assert.AreEqual(source.GetType().GetProperty(field)!.GetValue(source),loaded.GetType().GetProperty(field)!.GetValue(loaded),kind+"."+field);
        }
    }
}
