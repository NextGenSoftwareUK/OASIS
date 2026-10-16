using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class DeleteAcknowledgementTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task DetailAndHolonDeletesRetainAcknowledgedHistory(bool synchronous,bool detail)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend) {IsVersionControlEnabled=true};var id=Guid.NewGuid();
        if(detail)
        {
            var row=new AvatarDetail {Id=id};Assert.IsFalse((await provider.SaveAvatarDetailAsync(row)).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(row)).IsError);
        }
        else
        {
            var row=new Holon {Id=id,ParentHolonId=Guid.NewGuid()};Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);
            Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);
        }
        var original=backend.Items.Keys.ToArray();var key="oasis/"+(detail ? "avatardetail/":"holon/")+id.ToString("N");backend.FailDeleteKey=key;
        Exception failure;
        if(detail)
        {var result=synchronous ? provider.DeleteAvatarDetail(id,false):await provider.DeleteAvatarDetailAsync(id,false);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);failure=result.Exception;}
        else
        {var result=synchronous ? provider.DeleteHolon(id):await provider.DeleteHolonAsync(id);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);failure=result.Exception;}
        Assert.IsTrue(NativeMutationReconciliation.IsRequired(failure));var partial=(KeyValuePartialMutationException)failure;
        Assert.IsInstanceOfType(partial.InnerException,typeof(InvalidOperationException));
        CollectionAssert.AreEquivalent(original.Where(item=>!backend.Items.ContainsKey(item)).ToArray(),partial.AcknowledgedKeys.ToArray());
        Assert.IsTrue(partial.AcknowledgedKeys.Count>0);Assert.IsTrue(backend.Items.ContainsKey(key));
    }
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task HardDeleteRetainsAcknowledgedIndexesAndHistory(bool synchronous,bool historyFailure)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend) {IsVersionControlEnabled=true};
        var avatar=new Avatar {Id=Guid.NewGuid(),Username="owned-delete",Email="owned-delete@example.invalid"};
        Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
        Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=avatar.Id})).IsError);
        // A second save creates retained profile history that hard deletion must also track.
        Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
        var original=backend.Items.Keys.ToArray();
        var record="oasis/avatar/"+avatar.Id.ToString("N");
        backend.FailDeleteKey=historyFailure ? original.Single(key=>key.Contains("version") && key.Contains(avatar.Id.ToString("N"))):record;
        var result=synchronous ? provider.DeleteAvatar(avatar.Id,false):await provider.DeleteAvatarAsync(avatar.Id,false);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
        var partial=(KeyValuePartialMutationException)result.Exception;
        Assert.IsInstanceOfType(partial.InnerException,typeof(InvalidOperationException));
        CollectionAssert.AreEquivalent(original.Where(key=>!backend.Items.ContainsKey(key)).ToArray(),partial.AcknowledgedKeys.ToArray());
        Assert.IsTrue(partial.AcknowledgedKeys.Count>=2);Assert.IsTrue(backend.Items.ContainsKey(record));
    }
}
