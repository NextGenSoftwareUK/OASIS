using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;
[TestClass]
public class CallerPublicationTests
{
    private class IdentityChangingBackend : IKeyValueBackend
    {
        public readonly InMemoryBackend Store = new();
        public Action AfterWrite;
        public Action AfterRead;
        public Action AfterDelete;
        public async Task<string> GetAsync(string key,System.Threading.CancellationToken ct=default){var value=await Store.GetAsync(key,ct);AfterRead?.Invoke();return value;}
        public async Task PutAsync(string key,string value,System.Threading.CancellationToken ct=default)
        { await Store.PutAsync(key,value,ct); await Task.Yield(); AfterWrite?.Invoke(); }
        public async Task DeleteAsync(string key,System.Threading.CancellationToken ct=default){await Store.DeleteAsync(key,ct);AfterDelete?.Invoke();}
        public Task<System.Collections.Generic.IReadOnlyList<string>> ListKeysAsync(string prefix,System.Threading.CancellationToken ct=default)=>Store.ListKeysAsync(prefix,ct);
        public Task VerifyAsync(System.Threading.CancellationToken ct=default)=>Store.VerifyAsync(ct);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task RetiredIndexDeletionRemainsInAcknowledgedKeysWhenCallerChanges(bool synchronous)
    {
        var backend=new IdentityChangingBackend();var id=Guid.NewGuid();var seed=new TestProvider(backend.Store);
        Assert.IsFalse((await seed.SaveAvatarAsync(new Avatar{Id=id,Username="old-owned",Email="old-owned@example.invalid"})).IsError);
        var before=backend.Store.Items.Keys.ToArray();var source=new Avatar{Id=id,Username="new-owned",Email="new-owned@example.invalid"};
        backend.AfterDelete=()=>source.Version=999;var provider=new TestProvider(backend);
        var result=synchronous?provider.SaveAvatar(source):await provider.SaveAvatarAsync(source);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(999,source.Version);
        var deleted=before.Except(backend.Store.Items.Keys).Single();
        var partial=result.Exception as KeyValuePartialMutationException;Assert.IsNotNull(partial);Assert.IsTrue(partial.AcknowledgedKeys.Contains(deleted));
    }
    // Controlled preconditions over the same test store, not a native CAS certification.
    private sealed class ConditionalIdentityChangingBackend:IdentityChangingBackend,IConditionalKeyValueBackend
    {
        public async Task<KeyValueObservation> ReadObservedAsync(string key,System.Threading.CancellationToken ct=default){var value=await GetAsync(key,ct);return value==null?null:new KeyValueObservation(value,"owned");}
        public async Task CreateOnlyAsync(string key,string value,System.Threading.CancellationToken ct=default){if(Store.Items.ContainsKey(key))throw new StorageLifecycleConflictException("Controlled key exists.");await PutAsync(key,value,ct);}
        public async Task ReplaceUnchangedAsync(string key,string value,string revision,System.Threading.CancellationToken ct=default){if(revision!="owned"||!Store.Items.ContainsKey(key))throw new StorageLifecycleConflictException("Controlled revision mismatch.");await PutAsync(key,value,ct);}
        public async Task DeleteUnchangedAsync(string key,string revision,System.Threading.CancellationToken ct=default){if(revision!="owned"||!Store.Items.ContainsKey(key))throw new StorageLifecycleConflictException("Controlled revision mismatch.");await DeleteAsync(key,ct);AfterWrite?.Invoke();}
    }

    [DataTestMethod]
    [DataRow(0,false,false,false)][DataRow(1,false,false,false)][DataRow(2,false,false,false)]
    [DataRow(0,true,false,false)][DataRow(1,true,false,false)][DataRow(2,true,false,false)]
    [DataRow(0,false,true,false)][DataRow(1,false,true,false)][DataRow(2,false,true,false)]
    [DataRow(0,true,true,false)][DataRow(1,true,true,false)][DataRow(2,true,true,false)]
    [DataRow(0,false,false,true)][DataRow(1,false,false,true)][DataRow(2,false,false,true)]
    [DataRow(0,true,false,true)][DataRow(1,true,false,true)][DataRow(2,true,false,true)]
    [DataRow(0,false,true,true)][DataRow(1,false,true,true)][DataRow(2,false,true,true)]
    [DataRow(0,true,true,true)][DataRow(1,true,true,true)][DataRow(2,true,true,true)]
    public async Task CanonicalCallerEditStopsBeforeWriteOrFurtherIndexPublication(int kind,bool synchronous,bool duringRead,bool conditional)
    {
        IHolonBase source=kind==0?new Avatar{Username="owned",Email="owned@example.invalid"}:kind==1?new AvatarDetail{Id=Guid.NewGuid()}:new Holon();
        IdentityChangingBackend backend=conditional?new ConditionalIdentityChangingBackend():new IdentityChangingBackend();Action edit=()=>source.Version=999;
        if(duringRead)backend.AfterRead=edit;else backend.AfterWrite=edit;
        var provider=new TestProvider(backend);Exception failure;
        if(kind==0){var result=synchronous?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
        else if(kind==1){var result=synchronous?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
        else{var result=synchronous?provider.SaveHolon((IHolon)source,saveChildren:false):await provider.SaveHolonAsync((IHolon)source,saveChildren:false);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
        Assert.AreEqual(999,source.Version);Assert.AreEqual(duringRead?0:1,backend.Store.PutCalls);
        if(duringRead)Assert.IsInstanceOfType<InvalidOperationException>(failure);
        else{var partial=failure as KeyValuePartialMutationException;Assert.IsNotNull(partial);CollectionAssert.AreEquivalent(backend.Store.Items.Keys.ToArray(),partial.AcknowledgedKeys.ToArray());}
    }

    [DataTestMethod]
    [DataRow(0,false)] [DataRow(1,false)] [DataRow(2,false)]
    [DataRow(0,true)] [DataRow(1,true)] [DataRow(2,true)]
    public async Task ChangedCallerIdentityIsNotOverwrittenAfterAcknowledgedWrite(int kind,bool synchronous)
    {
        IHolonBase source=kind==0 ? new Avatar():kind==1 ? new AvatarDetail {Id=Guid.NewGuid()}:new Holon();
        var original=source.Id;var replacement=Guid.NewGuid();
        var backend=new IdentityChangingBackend {AfterWrite=()=>source.Id=replacement};
        var provider=new TestProvider(backend);Exception failure;
        if(kind==0) {var result=synchronous ? provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
        else if(kind==1) {var result=synchronous ? provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
        else {var result=synchronous ? provider.SaveHolon((IHolon)source):await provider.SaveHolonAsync((IHolon)source);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
        Assert.AreEqual(replacement,source.Id);
        Assert.IsInstanceOfType(failure,typeof(KeyValuePartialMutationException));
        var marker=(KeyValuePartialMutationException)failure;
        CollectionAssert.AreEquivalent(backend.Store.Items.Keys.ToArray(),marker.AcknowledgedKeys.ToArray());
        Assert.AreEqual(1,backend.Store.PutCalls);
        var stored=OasisJson.Deserialize<IHolonBase>(backend.Store.Items.Single().Value);
        Assert.AreNotEqual(replacement,stored.Id);
        if(original!=Guid.Empty) Assert.AreEqual(original,stored.Id);
    }

    [DataTestMethod]
    [DataRow(false,0)] [DataRow(true,0)] [DataRow(false,1)] [DataRow(true,1)] [DataRow(false,2)] [DataRow(true,2)]
    public async Task SaveRejectsMiskeyedPreviousRecordBeforeArchiveOrMutation(bool synchronous,int kind)
    {
        foreach(var history in new[] {false,true})
        {
            var backend=new InMemoryBackend();var provider=new TestProvider(backend) {IsVersionControlEnabled=history};
            var foreign=Guid.NewGuid();var id=Guid.NewGuid();var prefix="oasis/"+(kind==0 ? "avatar/":kind==1 ? "avatardetail/":"holon/");
            if(kind==0)Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar {Id=foreign,Username="foreign"})).IsError);
            else if(kind==1)Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=foreign,XP=12})).IsError);
            else Assert.IsFalse((await provider.SaveHolonAsync(new Holon {Id=foreign},saveChildren:false)).IsError);
            backend.Items[prefix+id.ToString("N")]=backend.Items[prefix+foreign.ToString("N")];
            var before=backend.Items.OrderBy(pair=>pair.Key).ToArray();
            IHolonBase input=kind==0 ? new Avatar {Id=id,Username="replacement"}:kind==1 ? new AvatarDetail {Id=id,XP=99}:new Holon {Id=id};
            var caller=OasisJson.Serialize(input);Exception failure;
            if(kind==0){var result=synchronous ? provider.SaveAvatar((IAvatar)input):await provider.SaveAvatarAsync((IAvatar)input);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
            else if(kind==1){var result=synchronous ? provider.SaveAvatarDetail((IAvatarDetail)input):await provider.SaveAvatarDetailAsync((IAvatarDetail)input);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
            else {var result=synchronous ? provider.SaveHolon((IHolon)input,saveChildren:false):await provider.SaveHolonAsync((IHolon)input,saveChildren:false);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);failure=result.Exception;}
            Assert.IsInstanceOfType(failure,typeof(System.IO.InvalidDataException));Assert.AreEqual(caller,OasisJson.Serialize(input));
            CollectionAssert.AreEqual(before,backend.Items.OrderBy(pair=>pair.Key).ToArray());
        }
    }
    private sealed class PendingBatchWrite:System.IO.IOException,INativeMutationReconciliationRequired
    {public string OriginalTransactionId=>"owned-original-pending";}

    [DataTestMethod]
    [DataRow(false,false,false)] [DataRow(true,false,false)] [DataRow(false,false,true)] [DataRow(true,false,true)]
    [DataRow(false,true,false)] [DataRow(true,true,false)] [DataRow(false,true,true)] [DataRow(true,true,true)]
    public async Task BatchRetainsEarlierAcknowledgementsOnLaterFailure(bool synchronous,bool unknown,bool continueOnError)
    {
        var first=new Holon {Id=Guid.NewGuid()};var second=new Holon {Id=Guid.NewGuid()};var last=new Holon {Id=Guid.NewGuid()};
        var key="oasis/holon/"+second.Id.ToString("N");Exception cause=unknown ? new PendingBatchWrite():new System.IO.IOException("owned later read failure");
        var backend=new InMemoryBackend();if(unknown) {backend.FailPutKey=key;backend.FailPutException=cause;} else {backend.FailGetKey=key;backend.FailGetException=cause;}
        var provider=new TestProvider(backend);var result=synchronous ? provider.SaveHolons(new[] {first,second,last},continueOnError:continueOnError)
            :await provider.SaveHolonsAsync(new[] {first,second,last},continueOnError:continueOnError);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
        var marker=(KeyValuePartialMutationException)result.Exception;Assert.AreSame(cause,marker.InnerException);
        var expected=new System.Collections.Generic.List<string> {"oasis/holon/"+first.Id.ToString("N")};
        if(!unknown && continueOnError) expected.Add("oasis/holon/"+last.Id.ToString("N"));
        CollectionAssert.AreEquivalent(expected,marker.AcknowledgedKeys.ToArray());
        Assert.IsTrue(backend.Items.ContainsKey(expected[0]));Assert.IsFalse(backend.Items.ContainsKey(key));
        Assert.AreEqual(!unknown && continueOnError,backend.Items.ContainsKey("oasis/holon/"+last.Id.ToString("N")));
        if(unknown) CollectionAssert.AreEqual(new[] {"owned-original-pending"},NativeMutationReconciliation.OriginalTransactionIds(marker).ToArray());
    }

    [DataTestMethod]
    [DataRow(true,true,0,0,3)] [DataRow(true,true,1,0,1)] [DataRow(true,true,2,0,2)]
    [DataRow(true,false,0,0,2)] [DataRow(false,true,0,0,1)] [DataRow(true,false,0,1,1)]
    public async Task SharedPreflightPreservesExistingDepthSelection(bool children,bool recursive,int limit,int initial,int expected)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        var root=new Holon();var child=new Holon();var leaf=new Holon();
        root.Children=new System.Collections.Generic.List<IHolon> {child};child.Children=new System.Collections.Generic.List<IHolon> {leaf};
        var result=await provider.SaveHolonsAsync(new[] {root},saveChildren:children,recursive:recursive,maxChildDepth:limit,curentChildDepth:initial);
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsSaved);Assert.AreNotEqual(Guid.Empty,root.Id);
        Assert.AreEqual(expected,backend.Items.Keys.Count(key=>key.StartsWith("oasis/holon/",StringComparison.Ordinal)));
        if(expected>=2) {Assert.AreNotEqual(Guid.Empty,child.Id);Assert.AreEqual(root.Id,child.ParentHolonId);} else Assert.AreEqual(Guid.Empty,child.Id);
        if(expected==3) {Assert.AreNotEqual(Guid.Empty,leaf.Id);Assert.AreEqual(child.Id,leaf.ParentHolonId);} else Assert.AreEqual(Guid.Empty,leaf.Id);
    }

    [DataTestMethod]
    [DataRow(false,"duplicate-root")] [DataRow(true,"duplicate-root")]
    [DataRow(false,"shared-child")] [DataRow(true,"shared-child")]
    [DataRow(false,"parent")] [DataRow(true,"parent")]
    [DataRow(false,"negative-depth")] [DataRow(true,"negative-depth")]
    public async Task EntireSelectedBatchIsValidatedBeforeFirstWrite(bool synchronous,string invalid)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        var first=new Holon {Id=Guid.NewGuid()};var second=new Holon {Id=invalid=="duplicate-root" ? first.Id:Guid.NewGuid()};
        var child=new Holon();
        if(invalid=="shared-child") {first.Children=new System.Collections.Generic.List<IHolon> {child};second.Children=new System.Collections.Generic.List<IHolon> {child};}
        if(invalid=="parent") {child.ParentHolonId=Guid.NewGuid();first.Children=new System.Collections.Generic.List<IHolon> {child};}
        var before=OasisJson.Serialize(child);
        var result=synchronous ? provider.SaveHolons(new[] {first,second},maxChildDepth:invalid=="negative-depth" ? -1:0)
            :await provider.SaveHolonsAsync(new[] {first,second},maxChildDepth:invalid=="negative-depth" ? -1:0);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
        Assert.AreEqual(0,backend.Items.Count);Assert.AreEqual(before,OasisJson.Serialize(child));
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task HolonParentIndexFailureRetainsRecordEvidenceWithoutPublishing(bool synchronous)
    {
        var source=new Holon {Id=Guid.NewGuid(),ParentHolonId=Guid.NewGuid(),Name="owned-parent-index-failure"};
        var index="oasis/holon-parent/"+source.ParentHolonId.ToString("N")+"/"+source.Id.ToString("N");
        var key="oasis/holon/"+source.Id.ToString("N");var cause=new System.IO.IOException("owned parent index failure");
        var backend=new InMemoryBackend {FailPutKey=index,FailPutException=cause};var provider=new TestProvider(backend);var before=OasisJson.Serialize(source);
        var result=synchronous ? provider.SaveHolon(source):await provider.SaveHolonAsync(source);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.AreEqual(before,OasisJson.Serialize(source));
        var marker=(KeyValuePartialMutationException)result.Exception;Assert.AreSame(cause,marker.InnerException);
        CollectionAssert.AreEqual(new[] {key},marker.AcknowledgedKeys.ToArray());Assert.IsTrue(backend.Items.ContainsKey(key));Assert.IsFalse(backend.Items.ContainsKey(index));
        var stored=OasisJson.Deserialize<IHolon>(backend.Items[key]);Assert.AreEqual(source.Id,stored.Id);Assert.AreEqual(source.ParentHolonId,stored.ParentHolonId);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task HolonReadFailureDoesNotPublishCandidateMetadata(bool synchronous)
    {
        var backend=new InMemoryBackend {ThrowReads=true};var provider=new TestProvider(backend);
        var source=new Holon {Name="owned-unacknowledged"};var before=OasisJson.Serialize(source);
        var result=synchronous ? provider.SaveHolon(source):await provider.SaveHolonAsync(source);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
        Assert.AreEqual(before,OasisJson.Serialize(source));Assert.AreEqual(0,backend.Items.Count);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task FailedChildDoesNotPublishParentAndRootEvidenceIsRetained(bool synchronous)
    {
        var child=new Holon {Id=Guid.NewGuid()};var root=new Holon {Id=Guid.NewGuid(),Children=new System.Collections.Generic.List<IHolon> {child}};
        var cause=new System.IO.IOException("owned child read failure before child write");
        var backend=new InMemoryBackend {FailGetKey="oasis/holon/"+child.Id.ToString("N"),FailGetException=cause};var provider=new TestProvider(backend);
        var before=OasisJson.Serialize(child);var result=synchronous ? provider.SaveHolon(root):await provider.SaveHolonAsync(root);
        Assert.IsTrue(result.IsError);Assert.AreEqual(before,OasisJson.Serialize(child));
        Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));Assert.AreSame(cause,result.Exception.InnerException);
        Assert.IsTrue(backend.Items.ContainsKey("oasis/holon/"+root.Id.ToString("N")));
        Assert.IsFalse(backend.Items.ContainsKey("oasis/holon/"+child.Id.ToString("N")));
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task FailedPreWriteReadDoesNotPublishAvatarMetadata(bool synchronous,bool detail)
    {
        var backend=new InMemoryBackend {ThrowReads=true};var provider=new TestProvider(backend);
        if(detail)
        {
            var source=new AvatarDetail {Id=Guid.NewGuid(),XP=777};var before=OasisJson.Serialize(source);
            var result=synchronous ? provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.AreEqual(before,OasisJson.Serialize(source));
        }
        else
        {
            var source=new Avatar {Username="owned"};var before=OasisJson.Serialize(source);
            var result=synchronous ? provider.SaveAvatar(source):await provider.SaveAvatarAsync(source);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.AreEqual(before,OasisJson.Serialize(source));
        }
        Assert.AreEqual(0,backend.Items.Count);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task AcknowledgedRecordWithFailedIndexRetainsPartialEvidenceWithoutPublishing(bool synchronous)
    {
        const string username="owned-index-failure";
        var index="oasis/avatar-username/"+Convert.ToBase64String(Encoding.UTF8.GetBytes(username)).TrimEnd('=').Replace('+','-').Replace('/','_');
        var cause=new System.IO.IOException("owned later index failure");
        var backend=new InMemoryBackend {FailPutKey=index,FailPutException=cause};var provider=new TestProvider(backend);
        var source=new Avatar {Username=username};var before=OasisJson.Serialize(source);
        var result=synchronous ? provider.SaveAvatar(source):await provider.SaveAvatarAsync(source);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.AreEqual(before,OasisJson.Serialize(source));
        Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));Assert.AreSame(cause,result.Exception.InnerException);
        var stored=backend.Items.Single(row=>row.Key.StartsWith("oasis/avatar/",StringComparison.Ordinal));
        var snapshot=OasisJson.Deserialize<IAvatar>(stored.Value);Assert.AreNotEqual(Guid.Empty,snapshot.Id);
        Assert.AreEqual("oasis/avatar/"+snapshot.Id.ToString("N"),stored.Key);Assert.AreEqual(username,snapshot.Username);Assert.IsFalse(backend.Items.ContainsKey(index));
        Assert.IsInstanceOfType(result.Exception,typeof(KeyValuePartialMutationException));
        CollectionAssert.AreEqual(new[] {stored.Key},((KeyValuePartialMutationException)result.Exception).AcknowledgedKeys.ToArray());
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task AcknowledgedHistoryBeforeFailedCurrentWriteRetainsCallerVersion(bool synchronous)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend) {IsVersionControlEnabled=true};
        var source=new AvatarDetail {Id=Guid.NewGuid(),XP=10};Assert.IsFalse((await provider.SaveAvatarDetailAsync(source)).IsError);
        var priorVersion=source.Version;var priorVersionId=source.VersionId;
        var key="oasis/avatardetail/"+source.Id.ToString("N");var priorJson=backend.Items[key];
        var cause=new System.IO.IOException("owned failed current write after archived version acknowledged");
        backend.FailPutKey=key;backend.FailPutException=cause;source.XP=20;var before=OasisJson.Serialize(source);
        var result=synchronous ? provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.AreEqual(before,OasisJson.Serialize(source));
        Assert.AreEqual(priorVersion,source.Version);Assert.AreEqual(priorVersionId,source.VersionId);Assert.AreEqual(priorJson,backend.Items[key]);
        var marker=(KeyValuePartialMutationException)result.Exception;Assert.AreSame(cause,marker.InnerException);
        var history=backend.Items.Single(row=>row.Key.StartsWith("oasis/version/",StringComparison.Ordinal));
        Assert.AreEqual(priorJson,history.Value);CollectionAssert.AreEqual(new[] {history.Key},marker.AcknowledgedKeys.ToArray());
    }
}
