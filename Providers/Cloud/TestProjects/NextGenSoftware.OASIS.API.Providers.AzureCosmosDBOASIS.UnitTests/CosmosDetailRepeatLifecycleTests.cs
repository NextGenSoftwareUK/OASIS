using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosDetailRepeatLifecycleTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task AliasDeletionBindsItsOriginalObservationAcrossConcurrentRename(bool sync,bool email)
    {
        var original=new AvatarDetail{Id=Guid.NewGuid(),Username="original",Email="original@example.invalid",IsActive=true};
        var changed=new AvatarDetail{Id=original.Id,Username="changed",Email="changed@example.invalid",IsActive=true};
        IAvatarDetail stored=original;var repository=new Mock<IAvatarDetailRepository>();var deletes=0;
        repository.Setup(value=>value.GetListAsync(0)).ReturnsAsync(()=>{
            var selected=stored;stored=changed;return new List<IAvatarDetail>{selected};});
        repository.Setup(value=>value.GetByIdAsync(original.Id.ToString())).ReturnsAsync(()=>stored);
        repository.Setup(value=>value.DeleteIfUnchangedAsync(It.IsAny<IAvatarDetail>())).Callback<IAvatarDetail>(expected=>{
            if(expected.Username!=stored.Username||expected.Email!=stored.Email)
                throw new Core.Helpers.StorageLifecycleConflictException("Owned native comparison rejected a changed original.");
            deletes++;}).Returns(Task.CompletedTask);
        var concrete=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new()){IsProviderActivated=true};
        typeof(AzureCosmosDBOASIS).GetField("avatarDetailRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(concrete,repository.Object);
        IOASISStorageProvider provider=concrete;
        var result=email
            ?(sync?provider.DeleteAvatarDetailByEmail(original.Email,false):await provider.DeleteAvatarDetailByEmailAsync(original.Email,false))
            :(sync?provider.DeleteAvatarDetailByUsername(original.Username,false):await provider.DeleteAvatarDetailByUsernameAsync(original.Username,false));
        Assert.IsTrue(result.IsError);Assert.AreEqual(Core.Helpers.StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
        Assert.AreEqual(0,deletes);Assert.IsFalse(result.IsDeleted);Assert.AreSame(changed,stored);
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task LifecycleAliasCollisionWithTombstoneDoesNotDeleteAnotherIdentity(bool sync,bool email)
    {
        var retired=new AvatarDetail{Id=Guid.NewGuid(),Username="shared",Email="shared@example.invalid",
            IsActive=false,DeletedDate=new DateTime(2026,10,25,0,0,0,DateTimeKind.Utc)};
        var active=new AvatarDetail{Id=Guid.NewGuid(),Username=retired.Username,Email=retired.Email,IsActive=true};
        var repository=new Mock<IAvatarDetailRepository>();var deletes=0;
        repository.Setup(value=>value.GetListAsync(0)).ReturnsAsync(new List<IAvatarDetail>{retired,active});
        repository.Setup(value=>value.GetByIdAsync(active.Id.ToString())).ReturnsAsync(active);
        repository.Setup(value=>value.DeleteIfUnchangedAsync(It.IsAny<IAvatarDetail>())).Callback(()=>deletes++).Returns(Task.CompletedTask);
        var concrete=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new()){IsProviderActivated=true};
        typeof(AzureCosmosDBOASIS).GetField("avatarDetailRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(concrete,repository.Object);
        IOASISStorageProvider provider=concrete;
        var publicRead=email?await provider.LoadAvatarDetailByEmailAsync(active.Email):await provider.LoadAvatarDetailByUsernameAsync(active.Username);
        Assert.IsFalse(publicRead.IsError);Assert.AreEqual(active.Id,publicRead.Result.Id);
        var result=email
            ?(sync?provider.DeleteAvatarDetailByEmail(retired.Email,false):await provider.DeleteAvatarDetailByEmailAsync(retired.Email,false))
            :(sync?provider.DeleteAvatarDetailByUsername(retired.Username,false):await provider.DeleteAvatarDetailByUsernameAsync(retired.Username,false));
        Assert.IsTrue(result.IsError);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);
        Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,deletes,"Mutation must not pick a different active identity when a retained tombstone shares the alias.");
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach(var sync in new[]{false,true}) foreach(var route in new[]{0,1,2})
            foreach(var purge in new[]{false,true}) yield return new object[]{sync,route,purge};
    }
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task DetailLifecycleSelectorsCanRevisitTombstonesWithoutMakingThemPublic(bool sync,int route,bool purge)
    {
        var id=Guid.NewGuid();IAvatarDetail? stored=new AvatarDetail{Id=id,Username="owned-detail",Email="owned@example.invalid",IsActive=true};
        var repository=new Mock<IAvatarDetailRepository>();var replacements=0;var deletions=0;
        repository.Setup(value=>value.GetByIdAsync(id.ToString())).ReturnsAsync(()=>stored!);
        repository.Setup(value=>value.GetByIdAsync(id.ToString(),0)).ReturnsAsync(()=>stored!);
        repository.Setup(value=>value.GetListAsync(0)).ReturnsAsync(()=>stored==null?new List<IAvatarDetail>():new(){stored});
        repository.Setup(value=>value.ReplaceIfUnchangedAsync(It.IsAny<IAvatarDetail>(),It.IsAny<IAvatarDetail>()))
            .ReturnsAsync((IAvatarDetail expected,IAvatarDetail proposal)=>{
                Assert.AreSame(stored,expected);replacements++;stored=proposal;return proposal;});
        repository.Setup(value=>value.DeleteIfUnchangedAsync(It.IsAny<IAvatarDetail>()))
            .Callback<IAvatarDetail>(expected=>{Assert.AreSame(stored,expected);deletions++;stored=null;})
            .Returns(Task.CompletedTask);
        var concrete=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new()){IsProviderActivated=true};
        typeof(AzureCosmosDBOASIS).GetField("avatarDetailRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(concrete,repository.Object);
        IOASISStorageProvider provider=concrete;
        Task<OASISResult<bool>> Delete(bool soft)=>route==0
            ?(sync?Task.FromResult(provider.DeleteAvatarDetail(id,soft)):provider.DeleteAvatarDetailAsync(id,soft))
            :route==1?(sync?Task.FromResult(provider.DeleteAvatarDetailByUsername("owned-detail",soft)):provider.DeleteAvatarDetailByUsernameAsync("owned-detail",soft))
            :(sync?Task.FromResult(provider.DeleteAvatarDetailByEmail("owned@example.invalid",soft)):provider.DeleteAvatarDetailByEmailAsync("owned@example.invalid",soft));
        Assert.IsFalse((await provider.LoadAvatarDetailAsync(id)).IsError,"The active fixture must be readable before deletion.");
        var first=await Delete(true);Assert.IsFalse(first.IsError,first.Exception?.ToString());Assert.AreEqual(1,replacements);
        Assert.IsNotNull(stored);Assert.IsTrue(stored.IsDeleted);Assert.IsFalse(stored.IsActive);
        var timestamp=stored.DeletedDate;
        Assert.IsTrue((await provider.LoadAvatarDetailAsync(id)).IsError);
        Assert.IsTrue((await provider.LoadAvatarDetailByUsernameAsync("owned-detail")).IsError);
        Assert.IsTrue((await provider.LoadAvatarDetailByEmailAsync("owned@example.invalid")).IsError);
        var second=await Delete(!purge);
        Assert.IsFalse(second.IsError,second.Message+second.Exception);Assert.IsTrue(second.Result);Assert.IsTrue(second.IsDeleted);
        Assert.AreEqual(1,replacements,"Repeating soft delete or purging must not append another tombstone.");
        Assert.AreEqual(purge?1:0,deletions);
        if(purge)Assert.IsNull(stored);else{Assert.IsNotNull(stored);Assert.AreEqual(timestamp,stored.DeletedDate);Assert.IsFalse(second.IsSaved);}
    }
}
