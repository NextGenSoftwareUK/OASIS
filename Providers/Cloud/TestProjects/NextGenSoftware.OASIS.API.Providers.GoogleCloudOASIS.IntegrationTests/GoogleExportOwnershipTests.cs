using System.Reflection;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

public sealed partial class GoogleCloudOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(0,false)][DataRow(0,true)]
    [DataRow(1,false)][DataRow(1,true)]
    [DataRow(2,false)][DataRow(2,true)]
    public async Task AvatarExportDoesNotTreatParentIdentityAsOwnership(int selector,bool synchronous)
    {
        var owner=Guid.NewGuid();
        var avatar=new Avatar {Id=owner,IsActive=true,Username="export-"+owner.ToString("N"),Email=owner+"@example.invalid"};
        var owned=new Holon {Id=Guid.NewGuid(),IsActive=true,CreatedByAvatarId=owner,Name="owned export root"};
        var foreign=new Holon {Id=Guid.NewGuid(),IsActive=true,CreatedByAvatarId=Guid.NewGuid(),ParentHolonId=owner,Name="foreign parent-linked root"};
        Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
        Assert.IsFalse((await _provider.SaveHolonAsync(owned,saveChildren:false)).IsError);
        Assert.IsFalse((await _provider.SaveHolonAsync(foreign,saveChildren:false)).IsError);
        var exported=selector switch
        {
            0=>synchronous?_provider.ExportAllDataForAvatarById(owner):await _provider.ExportAllDataForAvatarByIdAsync(owner),
            1=>synchronous?_provider.ExportAllDataForAvatarByUsername(avatar.Username):await _provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username),
            _=>synchronous?_provider.ExportAllDataForAvatarByEmail(avatar.Email):await _provider.ExportAllDataForAvatarByEmailAsync(avatar.Email)
        };
        // Inspect and remove only these acknowledged originals before asserting the regression.
        var database=(FirestoreDb)typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(_provider)!;
        foreach(var item in new[]{("avatars",owner),("holons",owned.Id),("holons",foreign.Id)})
        {
            var reference=database.Collection(item.Item1).Document(item.Item2.ToString());
            var original=await reference.GetSnapshotAsync();
            Assert.IsTrue(original.Exists);
            Assert.AreEqual(item.Item2.ToString(),original.GetValue<string>("id"));
            await reference.DeleteAsync();
        }
        Assert.IsFalse(exported.IsError,exported.Message);
        CollectionAssert.AreEquivalent(new[]{owned.Id},exported.Result.Select(row=>row.Id).ToArray());
    }
}
