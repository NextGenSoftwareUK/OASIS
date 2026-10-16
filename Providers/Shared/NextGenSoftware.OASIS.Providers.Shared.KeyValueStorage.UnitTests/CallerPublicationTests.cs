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
