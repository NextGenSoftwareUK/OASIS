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
