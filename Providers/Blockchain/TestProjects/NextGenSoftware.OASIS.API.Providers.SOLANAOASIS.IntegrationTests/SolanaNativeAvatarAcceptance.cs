using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

public sealed partial class SOLANAOASISIntegrationTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)][TestCategory("AvatarNative")]
    public async Task AvatarAndDetailPersistSelectorsRewardsAndAcknowledgements(bool synchronous)
    {
        var work=Environment.GetEnvironmentVariable("OASIS_SOLANA_NATIVE_EVIDENCE")!;
        var journal=Path.Combine(work,"avatar-native.pending.json");Assert.IsFalse(File.Exists(journal));
        var id=Guid.NewGuid();var username="owned-profile-"+id.ToString("N");var email=username+"@example.invalid";
        var profile=new Avatar{Id=id,IsActive=true,Username=username,Email=email,FirstName="Owned",AvatarType=new(AvatarType.User)};
        var detail=new AvatarDetail{Id=id,IsActive=true,Username=username,Email=email,XP=997,Karma=321};
        detail.KarmaAkashicRecords.Add(new KarmaAkashicRecord{AvatarId=id,Date=new DateTime(2026,10,21,0,0,0,DateTimeKind.Utc),Karma=321,TotalKarma=321,
            Provider=new(ProviderType.SolanaOASIS),KarmaSource=new(KarmaSourceType.Game),KarmaEarntOrLost=new(KarmaEarntOrLost.Earnt),
            KarmaTypePositive=new(KarmaTypePositive.Other),KarmaTypeNegative=new(KarmaTypeNegative.None),KarmaSourceTitle="owned native",KarmaSourceDesc="owned rewards",WebLink="https://example.invalid/rewards"});
        var intent=new Dictionary<string,object>{{"Id",id},{"Username",username},{"Email",email},{"State","before-profile"}};
        void SaveIntent()=>File.WriteAllText(journal,System.Text.Json.JsonSerializer.Serialize(intent));
        SaveIntent();
        var savedProfile=synchronous?_provider.SaveAvatar(profile):await _provider.SaveAvatarAsync(profile);
        Assert.IsFalse(savedProfile.IsError,savedProfile.Exception?.ToString());Assert.IsTrue(savedProfile.IsSaved);Assert.AreEqual(1,savedProfile.SavedCount);
        intent["ProfileSignature"]=profile.ProviderUniqueStorageKey[ProviderType.SolanaOASIS];intent["State"]="before-detail";SaveIntent();
        var savedDetail=synchronous?_provider.SaveAvatarDetail(detail):await _provider.SaveAvatarDetailAsync(detail);
        Assert.IsFalse(savedDetail.IsError,savedDetail.Exception?.ToString());Assert.IsTrue(savedDetail.IsSaved);Assert.AreEqual(1,savedDetail.SavedCount);
        intent["DetailSignature"]=detail.ProviderUniqueStorageKey[ProviderType.SolanaOASIS];intent["State"]="verify-selectors";SaveIntent();
        foreach(var selected in new[]{await _provider.LoadAvatarAsync(id),_provider.LoadAvatarByUsername(username.ToUpperInvariant()),await _provider.LoadAvatarByEmailAsync(email.ToUpperInvariant())}){
            Assert.IsFalse(selected.IsError,selected.Exception?.ToString());Assert.AreEqual(id,selected.Result.Id);Assert.AreEqual("Owned",selected.Result.FirstName);
        }
        foreach(var selected in new[]{await _provider.LoadAvatarDetailAsync(id),_provider.LoadAvatarDetailByUsername(username.ToUpperInvariant()),await _provider.LoadAvatarDetailByEmailAsync(email.ToUpperInvariant())}){
            Assert.IsFalse(selected.IsError,selected.Exception?.ToString());Assert.AreEqual(id,selected.Result.Id);Assert.AreEqual(997L,Convert.ToInt64(selected.Result.XP));Assert.AreEqual(321L,selected.Result.Karma);
            var entry=selected.Result.KarmaAkashicRecords.Single();Assert.AreEqual(id,entry.AvatarId);Assert.AreEqual(321,entry.Karma);Assert.AreEqual(321L,entry.TotalKarma);
            Assert.AreEqual(ProviderType.SolanaOASIS,entry.Provider.Value);Assert.AreEqual("owned rewards",entry.KarmaSourceDesc);Assert.AreEqual(DateTimeKind.Utc,entry.Date.Kind);
        }
        detail.XP=1001;intent["State"]="before-detail-update";SaveIntent();
        var updated=synchronous?_provider.SaveAvatarDetail(detail):await _provider.SaveAvatarDetailAsync(detail);
        Assert.IsFalse(updated.IsError,updated.Exception?.ToString());Assert.IsTrue(updated.IsSaved);Assert.AreEqual(1,updated.SavedCount);
        intent["UpdatedDetailSignature"]=detail.ProviderUniqueStorageKey[ProviderType.SolanaOASIS];intent["State"]="verify-update";SaveIntent();
        var current=await _provider.LoadAvatarDetailAsync(id);Assert.IsFalse(current.IsError,current.Exception?.ToString());
        Assert.AreEqual(1001L,Convert.ToInt64(current.Result.XP));Assert.AreEqual(321L,current.Result.Karma);Assert.AreEqual(current.Result.Version,detail.Version,"Returned caller version must match the finalized record.");
        Assert.AreEqual("Owned",(await _provider.LoadAvatarAsync(id)).Result.FirstName);
        intent["State"]="verified";SaveIntent();File.Delete(journal);
    }
}
