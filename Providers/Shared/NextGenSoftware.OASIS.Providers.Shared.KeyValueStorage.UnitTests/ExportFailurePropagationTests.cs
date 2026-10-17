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
