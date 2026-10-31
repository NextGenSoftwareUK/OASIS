using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosAvatarRepeatLifecycleTests
{
    private sealed class Pair
    {
        public IAvatar? Profile=new Avatar{Id=Guid.NewGuid(),Username="owned-profile",Email="owned@example.invalid",IsActive=true};
        public IAvatarDetail? Detail;
        public int ProfileWrites,DetailWrites,ProfileDeletes,DetailDeletes;
        public Action? AfterSelect;
        public readonly AzureCosmosDBOASIS Provider;
        public Pair()
        {
            Detail=new AvatarDetail{Id=Profile!.Id,Username=Profile.Username,Email=Profile.Email,IsActive=true};
            var profiles=new Mock<IAvatarRepository>();var details=new Mock<IAvatarDetailRepository>();
            profiles.Setup(r=>r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(()=>Profile!);
            profiles.Setup(r=>r.GetByIdAsync(It.IsAny<string>(),0)).ReturnsAsync(()=>Profile!);
            profiles.Setup(r=>r.GetListAsync(0)).ReturnsAsync(()=>{
                var rows=Profile==null?new List<IAvatar>():new(){Profile};
                var action=AfterSelect;AfterSelect=null;action?.Invoke();return rows;});
            details.Setup(r=>r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(()=>Detail!);
            details.Setup(r=>r.GetByIdAsync(It.IsAny<string>(),0)).ReturnsAsync(()=>Detail!);
            profiles.Setup(r=>r.ReplaceIfUnchangedAsync(It.IsAny<IAvatar>(),It.IsAny<IAvatar>()))
                .ReturnsAsync((IAvatar expected,IAvatar proposal)=>{CheckProfile(expected);ProfileWrites++;return Profile=proposal;});
            details.Setup(r=>r.ReplaceIfUnchangedAsync(It.IsAny<IAvatarDetail>(),It.IsAny<IAvatarDetail>()))
                .ReturnsAsync((IAvatarDetail expected,IAvatarDetail proposal)=>{CheckDetail(expected);DetailWrites++;return Detail=proposal;});
            profiles.Setup(r=>r.DeleteIfUnchangedAsync(It.IsAny<IAvatar>())).Callback<IAvatar>(expected=>{
                CheckProfile(expected);ProfileDeletes++;Profile=null;}).Returns(Task.CompletedTask);
            details.Setup(r=>r.DeleteIfUnchangedAsync(It.IsAny<IAvatarDetail>())).Callback<IAvatarDetail>(expected=>{
                CheckDetail(expected);DetailDeletes++;Detail=null;}).Returns(Task.CompletedTask);
            Provider=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new()){IsProviderActivated=true};
            typeof(AzureCosmosDBOASIS).GetField("avatarRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(Provider,profiles.Object);
            typeof(AzureCosmosDBOASIS).GetField("avatarDetailRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(Provider,details.Object);
        }
        private void CheckProfile(IAvatar expected)
        {
            if(Profile==null||!StoredRecordPayloadEquality.Equivalent(StoredRecordJson.CaptureInputToken<IAvatar>(expected),StoredRecordJson.CaptureInputToken<IAvatar>(Profile)))
                throw new Core.Helpers.StorageLifecycleConflictException("Owned profile comparison rejected a changed observation.");
        }
        private void CheckDetail(IAvatarDetail expected)
        {
            if(Detail==null||!StoredRecordPayloadEquality.Equivalent(StoredRecordJson.CaptureInputToken<IAvatarDetail>(expected),StoredRecordJson.CaptureInputToken<IAvatarDetail>(Detail)))
                throw new Core.Helpers.StorageLifecycleConflictException("Owned detail comparison rejected a changed observation.");
        }
        public Task<OASISResult<bool>> Delete(bool sync,int route,bool soft,Guid id)
            =>route==0?(sync?Task.FromResult(Provider.DeleteAvatar(id,soft)):Provider.DeleteAvatarAsync(id,soft))
                :route==1?(sync?Task.FromResult(Provider.DeleteAvatarByUsername("owned-profile",soft)):Provider.DeleteAvatarByUsernameAsync("owned-profile",soft))
                :(sync?Task.FromResult(Provider.DeleteAvatarByEmail("owned@example.invalid",soft)):Provider.DeleteAvatarByEmailAsync("owned@example.invalid",soft));
    }
    public static IEnumerable<object[]> Cases()
    {
        foreach(var sync in new[]{false,true})foreach(var route in new[]{0,1,2})
            foreach(var purge in new[]{false,true})yield return new object[]{sync,route,purge};
    }
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task PairedSelectorsCanRepeatSoftDeleteAndPurgeRetiredRecords(bool sync,int route,bool purge)
    {
        var pair=new Pair();var id=pair.Profile!.Id;
        var first=await pair.Delete(sync,route,true,id);Assert.IsFalse(first.IsError,first.Exception?.ToString());
        Assert.AreEqual(1,pair.ProfileWrites);Assert.AreEqual(1,pair.DetailWrites);
        Assert.IsFalse(pair.Profile!.IsActive);Assert.IsFalse(pair.Detail!.IsActive);
        var date=pair.Profile.DeletedDate;Assert.AreEqual(date,pair.Detail.DeletedDate);
        Assert.IsTrue((await pair.Provider.LoadAvatarAsync(id)).IsError);
        Assert.IsTrue((await pair.Provider.LoadAvatarDetailAsync(id)).IsError);
        var second=await pair.Delete(sync,route,!purge,id);
        Assert.IsFalse(second.IsError,second.Message+second.Exception);Assert.IsTrue(second.Result);Assert.IsTrue(second.IsDeleted);
        Assert.AreEqual(1,pair.ProfileWrites);Assert.AreEqual(1,pair.DetailWrites);
        Assert.AreEqual(purge?1:0,pair.ProfileDeletes);Assert.AreEqual(purge?1:0,pair.DetailDeletes);
        if(purge){Assert.IsNull(pair.Profile);Assert.IsNull(pair.Detail);}
        else{Assert.IsFalse(second.IsSaved);Assert.AreEqual(date,pair.Profile.DeletedDate);Assert.AreEqual(date,pair.Detail.DeletedDate);}
    }
    [DataTestMethod][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task ChangedProfileObservationConflictsBeforeAnyPairedDeletion(bool sync,int route)
    {
        var pair=new Pair();var id=pair.Profile!.Id;
        pair.AfterSelect=()=>{pair.Profile=new Avatar{Id=id,Username="renamed",Email="renamed@example.invalid",IsActive=true};};
        var result=await pair.Delete(sync,route,false,id);
        Assert.IsTrue(result.IsError);Assert.AreEqual(Core.Helpers.StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
        Assert.AreEqual(0,pair.ProfileDeletes);Assert.AreEqual(0,pair.DetailDeletes);
        Assert.IsNotNull(pair.Profile);Assert.IsNotNull(pair.Detail);
    }
}
