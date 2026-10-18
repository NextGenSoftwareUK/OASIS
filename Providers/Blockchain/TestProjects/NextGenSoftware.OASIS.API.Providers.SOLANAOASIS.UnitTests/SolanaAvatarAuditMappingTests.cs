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
    public void ProviderLinksSurviveCanonicalRecordAndDetach(string kind)
    {
        ISemanticHolon source=kind switch{"avatar"=>new Avatar(),"detail"=>new AvatarDetail(),_=>new Holon()};
        source.Id=Guid.NewGuid();
        var provider=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS;
        source.ProviderUniqueStorageKey[provider]="other-provider-key";
        source.PreviousVersionProviderUniqueStorageKey[provider]="previous-provider-key";
        source.ProviderMetaData[provider]=new Dictionary<string,string>{{"region","owned-region"}};
        SolanaBaseDto dto=kind switch{"avatar"=>((IAvatar)source).GetSolanaAvatarDto(),"detail"=>((IAvatarDetail)source).GetSolanaAvatarDetailDto(),_=>((IHolon)source).GetSolanaHolonDto()};
        var wire=OasisJson.Serialize(dto);
        ISemanticHolon loaded=kind switch{
            "avatar"=>OasisJson.Deserialize<SolanaAvatarDto>(wire).GetAvatar(),
            "detail"=>OasisJson.Deserialize<SolanaAvatarDetailDto>(wire).GetAvatarDetail(),
            _=>OasisJson.Deserialize<SolanaHolonDto>(wire).GetHolon()};
        Assert.AreEqual("other-provider-key",loaded.ProviderUniqueStorageKey[provider]);
        Assert.AreEqual("previous-provider-key",loaded.PreviousVersionProviderUniqueStorageKey[provider]);
        Assert.AreEqual("owned-region",loaded.ProviderMetaData[provider]["region"]);
        source.ProviderUniqueStorageKey[provider]="changed";source.PreviousVersionProviderUniqueStorageKey.Clear();source.ProviderMetaData[provider]["region"]="changed";
        // Mapping alone, before serialization, must snapshot dictionaries rather than alias callers.
        var direct=kind switch{"avatar"=>(IHolonBase)((SolanaAvatarDto)dto).GetAvatar(),"detail"=>((SolanaAvatarDetailDto)dto).GetAvatarDetail(),_=>((SolanaHolonDto)dto).GetHolon()};
        Assert.AreEqual("other-provider-key",direct.ProviderUniqueStorageKey[provider]);
        Assert.AreEqual("previous-provider-key",direct.PreviousVersionProviderUniqueStorageKey[provider]);
        Assert.AreEqual("owned-region",direct.ProviderMetaData[provider]["region"]);
        dto.ProviderUniqueStorageKey[provider]="changed-dto";dto.PreviousVersionProviderUniqueStorageKey.Clear();dto.ProviderMetaData[provider]["region"]="changed-dto";
        Assert.AreEqual("other-provider-key",direct.ProviderUniqueStorageKey[provider]);
        Assert.AreEqual("previous-provider-key",direct.PreviousVersionProviderUniqueStorageKey[provider]);
        Assert.AreEqual("owned-region",direct.ProviderMetaData[provider]["region"]);
        var oldJson="{\"Id\":\""+source.Id+"\"}";
        IHolonBase old=kind switch{
            "avatar"=>OasisJson.Deserialize<SolanaAvatarDto>(oldJson).GetAvatar(),
            "detail"=>OasisJson.Deserialize<SolanaAvatarDetailDto>(oldJson).GetAvatarDetail(),
            _=>OasisJson.Deserialize<SolanaHolonDto>(oldJson).GetHolon()};
        Assert.AreEqual(0,old.ProviderUniqueStorageKey.Count);Assert.AreEqual(0,old.PreviousVersionProviderUniqueStorageKey.Count);
        Assert.AreEqual(0,old.ProviderMetaData.Count);
    }

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
