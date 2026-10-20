using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class ConditionalHolonRecordTests
{
    // Controlled native-precondition schedules, not a real-provider or multi-key transaction claim.
    private sealed class Store:IConditionalKeyValueBackend
    {
        public readonly Dictionary<string,KeyValueObservation> Rows=new(StringComparer.Ordinal);
        public Action<string> AfterRead;public Action<string> AfterMutation;public int Mutations;private int _revision;
        public void Seed(string key,string value)=>Rows[key]=new(value,"native-"+(++_revision));
        public Task<KeyValueObservation> ReadObservedAsync(string key,CancellationToken token=default){
            token.ThrowIfCancellationRequested();Rows.TryGetValue(key,out var observed);AfterRead?.Invoke(key);return Task.FromResult(observed);
        }
        public Task CreateOnlyAsync(string key,string value,CancellationToken token=default){
            token.ThrowIfCancellationRequested();if(Rows.ContainsKey(key))throw new StorageLifecycleConflictException("controlled create conflict");
            Seed(key,value);Mutations++;AfterMutation?.Invoke(key);return Task.CompletedTask;
        }
        public Task ReplaceUnchangedAsync(string key,string value,string revision,CancellationToken token=default){
            token.ThrowIfCancellationRequested();if(!Rows.TryGetValue(key,out var row)||row.Revision!=revision)throw new StorageLifecycleConflictException("controlled replace conflict");
            Seed(key,value);Mutations++;AfterMutation?.Invoke(key);return Task.CompletedTask;
        }
        public Task DeleteUnchangedAsync(string key,string revision,CancellationToken token=default){
            token.ThrowIfCancellationRequested();if(!Rows.TryGetValue(key,out var row)||row.Revision!=revision)throw new StorageLifecycleConflictException("controlled delete conflict");
            Rows.Remove(key);Mutations++;AfterMutation?.Invoke(key);return Task.CompletedTask;
        }
        public Task<string> GetAsync(string key,CancellationToken token=default)=>throw new InvalidOperationException("save must retain its native observation");
        public Task PutAsync(string key,string value,CancellationToken token=default)=>throw new InvalidOperationException("unconditional overwrite forbidden in this schedule");
        public Task DeleteAsync(string key,CancellationToken token=default)=>throw new InvalidOperationException("not used by this save schedule");
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix,CancellationToken token=default)=>Task.FromResult<IReadOnlyList<string>>(Rows.Keys.Where(key=>key.StartsWith(prefix,StringComparison.Ordinal)).ToArray());
        public Task VerifyAsync(CancellationToken token=default)=>Task.CompletedTask;
    }

    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(true,true)]
    public async Task ConcurrentWinnerCannotBeOverwrittenAndPartialHistoryIsExplicit(bool existing,bool history)
    {
        var id=Guid.NewGuid();var key=$"oasis/holon/{id:N}";var store=new Store();
        var original=new Holon{Id=id,Name="original",Version=3,VersionId=Guid.NewGuid()};
        if(existing)store.Seed(key,OasisJson.Serialize<IHolon>(original));
        var winner=OasisJson.Serialize<IHolon>(new Holon{Id=id,Name="independent native winner",Version=4});
        store.AfterRead=observed=>{if(observed==key){store.AfterRead=null;store.Seed(key,winner);}};
        var provider=new TestProvider(store){IsVersionControlEnabled=history};
        var caller=new Holon{Id=id,Name="must not overwrite winner",Version=9};var originalKeys=caller.ProviderUniqueStorageKey;
        var result=await provider.SaveHolonAsync(caller,saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
        Assert.AreEqual(winner,store.Rows[key].Value);Assert.AreEqual(9,caller.Version);Assert.AreSame(originalKeys,caller.ProviderUniqueStorageKey);
        Assert.AreEqual(history?1:0,store.Mutations,result.Exception?.ToString());Assert.AreEqual(history,NativeMutationReconciliation.IsRequired(result.Exception));
        if(history){
            var partial=(KeyValuePartialMutationException)result.Exception;
            CollectionAssert.AreEqual(new[]{$"oasis/version/holon/{id:N}/0000000003"},partial.AcknowledgedKeys.ToArray());
            Assert.IsInstanceOfType<StorageLifecycleConflictException>(partial.InnerException);
        }else Assert.IsInstanceOfType<StorageLifecycleConflictException>(result.Exception,result.Exception?.ToString());
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ExistingHistoryMustMatchAndIsNeverOverwritten(bool matching)
    {
        var id=Guid.NewGuid();var key=$"oasis/holon/{id:N}";var historyKey=$"oasis/version/holon/{id:N}/0000000003";
        var store=new Store();var original=OasisJson.Serialize<IHolon>(new Holon{Id=id,Name="original",Version=3,VersionId=Guid.NewGuid()});
        store.Seed(key,original);store.Seed(historyKey,matching?original:"different historical payload");var archive=store.Rows[historyKey];
        var provider=new TestProvider(store){IsVersionControlEnabled=true};var caller=new Holon{Id=id,Name="next"};
        var result=await provider.SaveHolonAsync(caller,saveChildren:false);
        Assert.AreSame(archive,store.Rows[historyKey]);Assert.AreEqual(matching?1:0,store.Mutations,result.Exception?.ToString());
        Assert.AreEqual(!matching,result.IsError);Assert.AreEqual(matching,result.IsSaved);
        if(matching)Assert.AreEqual(4,caller.Version);
        else {Assert.IsInstanceOfType<System.IO.InvalidDataException>(result.Exception);Assert.AreEqual(original,store.Rows[key].Value);Assert.AreEqual(0,caller.Version);}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ParentIndexRacePreservesWinnerAndReturnsAcknowledgedPartialKeys(bool retireOld)
    {
        var id=Guid.NewGuid();var oldParent=Guid.NewGuid();var nextParent=Guid.NewGuid();var recordKey=$"oasis/holon/{id:N}";
        var oldKey=$"oasis/holon-parent/{oldParent:N}/{id:N}";var nextKey=$"oasis/holon-parent/{nextParent:N}/{id:N}";
        var store=new Store();store.Seed(recordKey,OasisJson.Serialize<IHolon>(new Holon{Id=id,Name="original",ParentHolonId=oldParent,Version=3}));store.Seed(oldKey,id.ToString());
        KeyValueObservation winner=null;
        store.AfterMutation=key=>{if(key==recordKey){store.AfterMutation=null;var changed=retireOld?oldKey:nextKey;store.Seed(changed,id.ToString());winner=store.Rows[changed];}};
        var provider=new TestProvider(store);var caller=new Holon{Id=id,Name="next",ParentHolonId=nextParent,Version=9};
        var result=await provider.SaveHolonAsync(caller,saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.AreEqual(9,caller.Version);
        var partial=(KeyValuePartialMutationException)result.Exception;Assert.IsTrue(NativeMutationReconciliation.IsRequired(partial));
        CollectionAssert.AreEqual(retireOld?new[]{recordKey,nextKey}:new[]{recordKey},partial.AcknowledgedKeys.ToArray());
        Assert.IsInstanceOfType<StorageLifecycleConflictException>(partial.InnerException);Assert.AreSame(winner,store.Rows[retireOld?oldKey:nextKey]);
        Assert.AreEqual(retireOld?2:1,store.Mutations);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task CorruptParentIndexCannotBeRepairedBySave(bool corruptOld)
    {
        var id=Guid.NewGuid();var oldParent=Guid.NewGuid();var nextParent=Guid.NewGuid();var recordKey=$"oasis/holon/{id:N}";
        var store=new Store();store.Seed(recordKey,OasisJson.Serialize<IHolon>(new Holon{Id=id,ParentHolonId=oldParent}));
        var index=$"oasis/holon-parent/{(corruptOld?oldParent:nextParent):N}/{id:N}";store.Seed(index,Guid.NewGuid().ToString());
        var original=store.Rows[recordKey];var corrupt=store.Rows[index];var provider=new TestProvider(store){IsVersionControlEnabled=true};
        var result=await provider.SaveHolonAsync(new Holon{Id=id,ParentHolonId=nextParent},saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<System.IO.InvalidDataException>(result.Exception);
        Assert.AreSame(original,store.Rows[recordKey]);Assert.AreSame(corrupt,store.Rows[index]);Assert.AreEqual(0,store.Mutations);
    }

    [TestMethod]
    public async Task SuccessfulParentMoveAcknowledgesCanonicalCreateAndRetirement()
    {
        var id=Guid.NewGuid();var oldParent=Guid.NewGuid();var nextParent=Guid.NewGuid();var recordKey=$"oasis/holon/{id:N}";
        var oldKey=$"oasis/holon-parent/{oldParent:N}/{id:N}";var nextKey=$"oasis/holon-parent/{nextParent:N}/{id:N}";
        var store=new Store();store.Seed(recordKey,OasisJson.Serialize<IHolon>(new Holon{Id=id,ParentHolonId=oldParent,Version=3}));store.Seed(oldKey,id.ToString());
        var caller=new Holon{Id=id,ParentHolonId=nextParent};var result=await new TestProvider(store).SaveHolonAsync(caller,saveChildren:false);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);Assert.AreEqual(4,caller.Version);
        Assert.IsFalse(store.Rows.ContainsKey(oldKey));Assert.AreEqual(id.ToString(),store.Rows[nextKey].Value);Assert.AreEqual(3,store.Mutations);
    }

    [DataTestMethod][DataRow(false,false,false)][DataRow(false,false,true)][DataRow(true,false,false)]
    [DataRow(true,false,true)][DataRow(true,true,false)][DataRow(true,true,true)]
    public async Task AvatarDetailConcurrentWinnerPreservesKarmaXpAndCaller(bool existing,bool history,bool synchronous)
    {
        var id=Guid.NewGuid();var recordKey=$"oasis/avatardetail/{id:N}";var store=new Store();
        if(existing)store.Seed(recordKey,OasisJson.Serialize<IAvatarDetail>(new AvatarDetail{Id=id,Karma=11,XP=22,Version=3,VersionId=Guid.NewGuid()}));
        var winner=OasisJson.Serialize<IAvatarDetail>(new AvatarDetail{Id=id,Karma=77,XP=88,Version=4});
        store.AfterRead=key=>{if(key==recordKey){store.AfterRead=null;store.Seed(key,winner);}};
        var caller=new AvatarDetail{Id=id,Karma=33,XP=44,Version=9};var keys=caller.ProviderUniqueStorageKey;
        var provider=new TestProvider(store){IsVersionControlEnabled=history};
        var result=synchronous?provider.SaveAvatarDetail(caller):await provider.SaveAvatarDetailAsync(caller);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
        Assert.AreEqual(winner,store.Rows[recordKey].Value);Assert.AreEqual(9,caller.Version);Assert.AreSame(keys,caller.ProviderUniqueStorageKey);
        Assert.AreEqual(33L,caller.Karma);Assert.AreEqual(44L,caller.XP);Assert.AreEqual(history?1:0,store.Mutations);
        Assert.AreEqual(history,NativeMutationReconciliation.IsRequired(result.Exception));
        if(history){
            var partial=(KeyValuePartialMutationException)result.Exception;
            CollectionAssert.AreEqual(new[]{$"oasis/version/avatardetail/{id:N}/0000000003"},partial.AcknowledgedKeys.ToArray());
            Assert.IsInstanceOfType<StorageLifecycleConflictException>(partial.InnerException);
        }else Assert.IsInstanceOfType<StorageLifecycleConflictException>(result.Exception);
    }

    private static string IdentityKey(bool email,string value)=>"oasis/avatar-"+(email?"email/":"username/")+
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant())).TrimEnd('=').Replace('+','-').Replace('/','_');

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task AvatarIdentityClaimRaceCannotOverwriteCompetingOwner(bool email)
    {
        var id=Guid.NewGuid();var key=$"oasis/avatar/{id:N}";var value=email?"new@example.invalid":"new-user";var index=IdentityKey(email,value);
        var store=new Store();var other=Guid.NewGuid();KeyValueObservation winner=null;
        store.AfterMutation=written=>{if(written==key){store.AfterMutation=null;store.Seed(index,other.ToString());winner=store.Rows[index];}};
        var caller=new Avatar{Id=id,Username=email?null:value,Email=email?value:null,Version=9};
        var result=await new TestProvider(store).SaveAvatarAsync(caller);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.AreEqual(9,caller.Version);
        Assert.AreSame(winner,store.Rows[index]);Assert.AreEqual(1,store.Mutations);
        var partial=(KeyValuePartialMutationException)result.Exception;
        CollectionAssert.AreEqual(new[]{key},partial.AcknowledgedKeys.ToArray());Assert.IsInstanceOfType<StorageLifecycleConflictException>(partial.InnerException);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task RetiredAvatarIdentityIndexMustBelongToObservedAvatar(bool email)
    {
        var id=Guid.NewGuid();var key=$"oasis/avatar/{id:N}";var old=email?"old@example.invalid":"old-user";var index=IdentityKey(email,old);
        var store=new Store();store.Seed(key,OasisJson.Serialize<IAvatar>(new Avatar{Id=id,Username=email?null:old,Email=email?old:null}));store.Seed(index,Guid.NewGuid().ToString());
        var original=store.Rows[key];var corrupt=store.Rows[index];
        var caller=new Avatar{Id=id,Username=email?null:"next-user",Email=email?"next@example.invalid":null};
        var result=await new TestProvider(store){IsVersionControlEnabled=true}.SaveAvatarAsync(caller);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<System.IO.InvalidDataException>(result.Exception);
        Assert.AreSame(original,store.Rows[key]);Assert.AreSame(corrupt,store.Rows[index]);Assert.AreEqual(0,store.Mutations);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NormalizedAvatarIdentityChangeCannotRetireCurrentIndex(bool email)
    {
        var id=Guid.NewGuid();var key=$"oasis/avatar/{id:N}";var value=email?"same@example.invalid":"same-user";var index=IdentityKey(email,value);
        var store=new Store();store.Seed(key,OasisJson.Serialize<IAvatar>(new Avatar{Id=id,Username=email?null:value,Email=email?value:null,Version=3}));store.Seed(index,id.ToString());
        var caller=new Avatar{Id=id,Username=email?null:" SAME-USER ",Email=email?" SAME@EXAMPLE.INVALID ":null};
        var result=await new TestProvider(store).SaveAvatarAsync(caller);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);
        Assert.AreEqual(id.ToString(),store.Rows[index].Value);Assert.AreEqual(2,store.Mutations);Assert.AreEqual(4,caller.Version);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task DetailDeletionCannotReplaceOrRemoveChangedCurrentRecord(bool soft,bool synchronous)
    {
        var id=Guid.NewGuid();var key=$"oasis/avatardetail/{id:N}";var store=new Store();
        store.Seed(key,OasisJson.Serialize<IAvatarDetail>(new AvatarDetail{Id=id,IsActive=true,Karma=12,Version=1}));
        var winner=OasisJson.Serialize<IAvatarDetail>(new AvatarDetail{Id=id,IsActive=true,Karma=99,Version=2});
        store.AfterRead=read=>{if(read==key){store.AfterRead=null;store.Seed(key,winner);}};
        var provider=new TestProvider(store);var result=synchronous?provider.DeleteAvatarDetail(id,soft):await provider.DeleteAvatarDetailAsync(id,soft);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);
        Assert.IsInstanceOfType<StorageLifecycleConflictException>(result.Exception);Assert.AreEqual(winner,store.Rows[key].Value);Assert.AreEqual(0,store.Mutations);
    }

    [TestMethod]
    public async Task DetailSoftDeletionArchivesOriginalNotItsMutatedProposal()
    {
        var id=Guid.NewGuid();var key=$"oasis/avatardetail/{id:N}";var store=new Store();
        store.Seed(key,OasisJson.Serialize<IAvatarDetail>(new AvatarDetail{Id=id,IsActive=true,Karma=12,Version=1,VersionId=Guid.NewGuid()}));
        var result=await new TestProvider(store){IsVersionControlEnabled=true}.DeleteAvatarDetailAsync(id,true);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsDeleted);
        var archived=OasisJson.DeserializeStored<IAvatarDetail>(store.Rows[$"oasis/version/avatardetail/{id:N}/0000000001"].Value);
        var current=OasisJson.DeserializeStored<IAvatarDetail>(store.Rows[key].Value);
        Assert.IsTrue(archived.IsActive);Assert.IsFalse(archived.IsDeleted);Assert.AreEqual(1,archived.Version);
        Assert.IsFalse(current.IsActive);Assert.IsTrue(current.IsDeleted);Assert.AreEqual(2,current.Version);Assert.AreEqual(2,store.Mutations);
    }

    [TestMethod]
    public async Task DetailHardDeletionPreservesChangedHistoryAfterCurrentAcknowledgement()
    {
        var id=Guid.NewGuid();var key=$"oasis/avatardetail/{id:N}";var history=$"oasis/version/avatardetail/{id:N}/0000000001";var store=new Store();
        store.Seed(key,OasisJson.Serialize<IAvatarDetail>(new AvatarDetail{Id=id,Version=2}));
        var original=OasisJson.Serialize<IAvatarDetail>(new AvatarDetail{Id=id,Version=1});store.Seed(history,original);KeyValueObservation winner=null;
        store.AfterMutation=written=>{if(written==key){store.AfterMutation=null;store.Seed(history,original);winner=store.Rows[history];}};
        var result=await new TestProvider(store).DeleteAvatarDetailAsync(id,false);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);
        Assert.IsFalse(store.Rows.ContainsKey(key));Assert.AreSame(winner,store.Rows[history]);Assert.AreEqual(1,store.Mutations);
        var partial=(KeyValuePartialMutationException)result.Exception;CollectionAssert.AreEqual(new[]{key},partial.AcknowledgedKeys.ToArray());
        Assert.IsInstanceOfType<StorageLifecycleConflictException>(partial.InnerException);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task HolonDeletionCannotRemoveChangedCurrentOrAnyAuxiliaryKeys(bool synchronous)
    {
        var id=Guid.NewGuid();var parent=Guid.NewGuid();var key=$"oasis/holon/{id:N}";var link=$"oasis/holon-parent/{parent:N}/{id:N}";var history=$"oasis/version/holon/{id:N}/0000000001";
        var store=new Store();store.Seed(key,OasisJson.Serialize<IHolon>(new Holon{Id=id,ParentHolonId=parent,Version=2}));store.Seed(link,id.ToString());store.Seed(history,OasisJson.Serialize<IHolon>(new Holon{Id=id,Version=1}));
        var winner=OasisJson.Serialize<IHolon>(new Holon{Id=id,ParentHolonId=parent,Name="winner",Version=3});
        store.AfterRead=read=>{if(read==key){store.AfterRead=null;store.Seed(key,winner);}};
        var oldLink=store.Rows[link];var oldHistory=store.Rows[history];var provider=new TestProvider(store);
        var result=synchronous?provider.DeleteHolon(id):await provider.DeleteHolonAsync(id);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);Assert.IsNull(result.Result);Assert.IsInstanceOfType<StorageLifecycleConflictException>(result.Exception);
        Assert.AreEqual(winner,store.Rows[key].Value);Assert.AreSame(oldLink,store.Rows[link]);Assert.AreSame(oldHistory,store.Rows[history]);Assert.AreEqual(0,store.Mutations);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task HolonDeletionPreservesChangedParentOrChildLinkAfterCurrentAcknowledgement(bool child)
    {
        var id=Guid.NewGuid();var parent=Guid.NewGuid();var childId=Guid.NewGuid();var key=$"oasis/holon/{id:N}";
        var link=child?$"oasis/holon-parent/{id:N}/{childId:N}":$"oasis/holon-parent/{parent:N}/{id:N}";var owner=child?childId:id;
        var store=new Store();store.Seed(key,OasisJson.Serialize<IHolon>(new Holon{Id=id,ParentHolonId=child?Guid.Empty:parent}));store.Seed(link,owner.ToString());KeyValueObservation winner=null;
        store.AfterMutation=written=>{if(written==key){store.AfterMutation=null;store.Seed(link,owner.ToString());winner=store.Rows[link];}};
        var result=await new TestProvider(store).DeleteHolonAsync(id);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);Assert.IsFalse(store.Rows.ContainsKey(key));Assert.AreSame(winner,store.Rows[link]);Assert.AreEqual(1,store.Mutations);
        var partial=(KeyValuePartialMutationException)result.Exception;CollectionAssert.AreEqual(new[]{key},partial.AcknowledgedKeys.ToArray());Assert.IsInstanceOfType<StorageLifecycleConflictException>(partial.InnerException);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task HolonDeletionRefusesCorruptLinkOrHistoryBeforeMutation(bool history)
    {
        var id=Guid.NewGuid();var parent=Guid.NewGuid();var key=$"oasis/holon/{id:N}";var store=new Store();store.Seed(key,OasisJson.Serialize<IHolon>(new Holon{Id=id,ParentHolonId=history?Guid.Empty:parent}));
        var corruptKey=history?$"oasis/version/holon/{id:N}/0000000001":$"oasis/holon-parent/{parent:N}/{id:N}";
        store.Seed(corruptKey,history?OasisJson.Serialize<IHolon>(new Holon{Id=Guid.NewGuid(),Version=1}):Guid.NewGuid().ToString());
        var original=store.Rows[key];var corrupt=store.Rows[corruptKey];var result=await new TestProvider(store).DeleteHolonAsync(id);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<System.IO.InvalidDataException>(result.Exception);Assert.AreEqual(0,store.Mutations);Assert.AreSame(original,store.Rows[key]);Assert.AreSame(corrupt,store.Rows[corruptKey]);
    }

    [TestMethod]
    public async Task HolonDeletionRemovesObservedLinksAndHistoryButNotChildRecords()
    {
        var id=Guid.NewGuid();var parent=Guid.NewGuid();var child=Guid.NewGuid();var key=$"oasis/holon/{id:N}";var childKey=$"oasis/holon/{child:N}";var store=new Store();
        store.Seed(key,OasisJson.Serialize<IHolon>(new Holon{Id=id,ParentHolonId=parent,Version=2}));store.Seed(childKey,OasisJson.Serialize<IHolon>(new Holon{Id=child,ParentHolonId=id}));
        store.Seed($"oasis/holon-parent/{parent:N}/{id:N}",id.ToString());store.Seed($"oasis/holon-parent/{id:N}/{child:N}",child.ToString());store.Seed($"oasis/version/holon/{id:N}/0000000001",OasisJson.Serialize<IHolon>(new Holon{Id=id,Version=1}));
        var originalChild=store.Rows[childKey];var result=await new TestProvider(store).DeleteHolonAsync(id);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsDeleted);Assert.AreEqual(id,result.Result.Id);Assert.AreEqual(4,store.Mutations);
        Assert.AreEqual(1,store.Rows.Count);Assert.AreSame(originalChild,store.Rows[childKey]);
    }
}
