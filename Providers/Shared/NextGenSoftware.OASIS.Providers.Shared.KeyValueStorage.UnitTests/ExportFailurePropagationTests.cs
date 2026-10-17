using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;
[TestClass]
public class ExportFailurePropagationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task AvatarExportRetainsIncompleteCollectionWarning(bool synchronous)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);var owner=Guid.NewGuid();var missing=Guid.NewGuid();var good=Guid.NewGuid();
        Assert.IsFalse((await provider.SaveHolonAsync(new Holon {Id=good,CreatedByAvatarId=owner,Name="owned available"},saveChildren:false)).IsError);
        Assert.IsFalse((await provider.SaveHolonAsync(new Holon {Id=missing,CreatedByAvatarId=owner,Name="owned unavailable"},saveChildren:false)).IsError);
        Assert.IsFalse((await provider.SaveHolonAsync(new Holon {Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),Name="other owner"},saveChildren:false)).IsError);
        backend.FailGetKey="oasis/holon/"+missing.ToString("N");backend.FailGetException=new System.IO.IOException("owned partial export read");
        var result=synchronous ? provider.ExportAllDataForAvatarById(owner):await provider.ExportAllDataForAvatarByIdAsync(owner);
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsWarning);Assert.AreEqual(good,result.Result.Single().Id);
        Assert.AreEqual("true",result.MetaData["partialHolonReads"]);Assert.AreEqual("1",result.MetaData["failedHolonReadCount"]);
        Assert.AreEqual(1,result.LoadedCount);
    }
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task ExportSelectorRetainsAbsenceAndCorruption(bool synchronous,bool email)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        async Task<OASISResult<System.Collections.Generic.IEnumerable<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>>> Export()
            =>email ? synchronous ? provider.ExportAllDataForAvatarByEmail("owned@example.invalid"):await provider.ExportAllDataForAvatarByEmailAsync("owned@example.invalid")
                :synchronous ? provider.ExportAllDataForAvatarByUsername("owned"):await provider.ExportAllDataForAvatarByUsernameAsync("owned");
        var missing=await Export();Assert.IsTrue(missing.IsError);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);Assert.IsNull(missing.Result);
        Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar {Id=Guid.NewGuid(),Username="owned",Email="owned@example.invalid"})).IsError);
        backend.FailGetKey=backend.Items.Keys.Single(key=>key.StartsWith(email ? "oasis/avatar-email/":"oasis/avatar-username/",StringComparison.Ordinal));
        backend.FailGetException=new System.IO.InvalidDataException("owned corrupt export identity index");
        var corrupt=await Export();Assert.IsTrue(corrupt.IsError);Assert.IsNull(corrupt.Result);Assert.AreSame(backend.FailGetException,corrupt.Exception);
    }
}
