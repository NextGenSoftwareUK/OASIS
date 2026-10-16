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
    public async Task AvatarSoftDeleteIncludesPairedDetailAndFencesPartialFailure(bool synchronous,bool failDetail)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);var id=Guid.NewGuid();
        Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar {Id=id,Username="paired",IsActive=true})).IsError);
        Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=id,IsActive=true,XP=123})).IsError);
        if(failDetail)backend.FailPutKey="oasis/avatardetail/"+id.ToString("N");
        var deleted=synchronous ? provider.DeleteAvatar(id,true):await provider.DeleteAvatarAsync(id,true);
        var avatar=(await provider.LoadAvatarAsync(id)).Result;var detail=(await provider.LoadAvatarDetailAsync(id)).Result;
        Assert.IsFalse(avatar.IsActive);Assert.AreNotEqual(default,avatar.DeletedDate);Assert.AreEqual(123L,detail.XP);
        if(failDetail)
        {
            Assert.IsTrue(deleted.IsError);Assert.IsFalse(deleted.IsDeleted);Assert.IsTrue(detail.IsActive);
            Assert.IsTrue(NativeMutationReconciliation.IsRequired(deleted.Exception));var partial=(KeyValuePartialMutationException)deleted.Exception;
            CollectionAssert.AreEqual(new[] {"oasis/avatar/"+id.ToString("N")},partial.AcknowledgedKeys.ToArray());
        }
        else {Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsTrue(deleted.IsDeleted);Assert.IsFalse(detail.IsActive);Assert.AreEqual(avatar.DeletedDate,detail.DeletedDate);}
    }
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task MiskeyedRootDeleteCannotRemoveForeignIndexesOrLinks(bool synchronous,bool avatar)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);var id=Guid.NewGuid();
        var key="oasis/"+(avatar ? "avatar/":"holon/")+id.ToString("N");
        var foreign=Guid.NewGuid();
        if(avatar)Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar {Id=foreign,Username="foreign"})).IsError);
        else Assert.IsFalse((await provider.SaveHolonAsync(new Holon {Id=foreign,ParentHolonId=Guid.NewGuid()},saveChildren:false)).IsError);
        backend.Items[key]=backend.Items["oasis/"+(avatar ? "avatar/":"holon/")+foreign.ToString("N")];
        var before=backend.Items.OrderBy(pair=>pair.Key).ToArray();Exception failure;
        if(avatar){var result=synchronous ? provider.DeleteAvatar(id,false):await provider.DeleteAvatarAsync(id,false);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);failure=result.Exception;}
        else {var result=synchronous ? provider.DeleteHolon(id):await provider.DeleteHolonAsync(id);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);failure=result.Exception;}
        Assert.IsInstanceOfType(failure,typeof(System.IO.InvalidDataException));CollectionAssert.AreEqual(before,backend.Items.OrderBy(pair=>pair.Key).ToArray());
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task CurrentIndexCanSelectEarlierRenamedRecord(bool email)
    {
        var provider=new TestProvider(new InMemoryBackend()) {IsVersionControlEnabled=true};
        var row=new Avatar {Id=Guid.NewGuid(),Username="old-name",Email="old@example.invalid"};
        Assert.IsFalse((await provider.SaveAvatarAsync(row)).IsError);var version=row.Version;
        row.Username="new-name";row.Email="new@example.invalid";Assert.IsFalse((await provider.SaveAvatarAsync(row)).IsError);
        var read=email ? await provider.LoadAvatarByEmailAsync(" NEW@example.invalid ",version):await provider.LoadAvatarByUsernameAsync(" NEW-NAME ",version);
        Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual("old-name",read.Result.Username);Assert.AreEqual("old@example.invalid",read.Result.Email);
    }
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task ForeignIdentityIndexCannotReadOrDeleteAnotherAvatar(bool synchronous,bool email)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        var requested=new Avatar {Id=Guid.NewGuid(),Username="requested",Email="requested@example.invalid"};
        var foreign=new Avatar {Id=Guid.NewGuid(),Username="foreign",Email="foreign@example.invalid"};
        Assert.IsFalse((await provider.SaveAvatarAsync(requested)).IsError);Assert.IsFalse((await provider.SaveAvatarAsync(foreign)).IsError);
        var prefix=email ? "oasis/avatar-email/":"oasis/avatar-username/";
        var index=backend.Items.Single(pair=>pair.Key.StartsWith(prefix,StringComparison.Ordinal) && pair.Value==requested.Id.ToString()).Key;
        backend.Items[index]=foreign.Id.ToString();var before=backend.Items.OrderBy(pair=>pair.Key).ToArray();
        var read=email ? synchronous ? provider.LoadAvatarByEmail(requested.Email):await provider.LoadAvatarByEmailAsync(requested.Email)
            :synchronous ? provider.LoadAvatarByUsername(requested.Username):await provider.LoadAvatarByUsernameAsync(requested.Username);
        Assert.IsTrue(read.IsError);Assert.IsNull(read.Result);Assert.IsInstanceOfType(read.Exception,typeof(System.IO.InvalidDataException));
        var deleted=email ? synchronous ? provider.DeleteAvatarByEmail(requested.Email,false):await provider.DeleteAvatarByEmailAsync(requested.Email,false)
            :synchronous ? provider.DeleteAvatarByUsername(requested.Username,false):await provider.DeleteAvatarByUsernameAsync(requested.Username,false);
        Assert.IsTrue(deleted.IsError);Assert.IsFalse(deleted.IsDeleted);Assert.IsInstanceOfType(deleted.Exception,typeof(System.IO.InvalidDataException));
        CollectionAssert.AreEqual(before,backend.Items.OrderBy(pair=>pair.Key).ToArray());
    }
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task SelectorDeletePreservesAbsenceCodeAndOriginalReadFailure(bool synchronous,bool email)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        async Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> Delete(bool detail)
            =>detail ? email ? synchronous ? provider.DeleteAvatarDetailByEmail("owned@example.invalid"):await provider.DeleteAvatarDetailByEmailAsync("owned@example.invalid")
                :synchronous ? provider.DeleteAvatarDetailByUsername("owned"):await provider.DeleteAvatarDetailByUsernameAsync("owned")
                :email ? synchronous ? provider.DeleteAvatarByEmail("owned@example.invalid"):await provider.DeleteAvatarByEmailAsync("owned@example.invalid")
                :synchronous ? provider.DeleteAvatarByUsername("owned"):await provider.DeleteAvatarByUsernameAsync("owned");
        foreach(var detail in new[] {false,true})
        {
            var missing=await Delete(detail);Assert.IsTrue(missing.IsError);Assert.IsFalse(missing.IsDeleted);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);
        }
        var avatar=new Avatar {Id=Guid.NewGuid(),Username="owned",Email="owned@example.invalid"};
        Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
        Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=avatar.Id})).IsError);
        var before=backend.Items.OrderBy(pair=>pair.Key).ToArray();
        backend.FailGetKey=backend.Items.Keys.Single(key=>key.StartsWith(email ? "oasis/avatar-email/":"oasis/avatar-username/",StringComparison.Ordinal));
        backend.FailGetException=new System.IO.InvalidDataException("owned corrupt selector index");
        foreach(var detail in new[] {false,true})
        {var result=await Delete(detail);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);Assert.AreSame(backend.FailGetException,result.Exception);}
        CollectionAssert.AreEqual(before,backend.Items.OrderBy(pair=>pair.Key).ToArray());
    }
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
