using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class StoredRecordIdentityLookupTests
{
    [DataTestMethod][DataRow(-1)][DataRow(0)]
    public async Task InvalidIdentityArgumentsNeverReadStorage(int version)
    {
        var calls=0;
        var result=await StoredRecordIdentityLookup.SelectAsync<Avatar>(version==0?" ":"owned",version,
            _=>{calls++;throw new InvalidOperationException();},row=>row.Username);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.AreEqual(0,calls);
    }
    [DataTestMethod][DataRow(0)][DataRow(3)][DataRow(int.MaxValue)]
    public async Task ProfileHistoryPassesVersionToCollectionOwner(int version)
    {
        var row=new Avatar{Id=Guid.NewGuid(),Username=" Owned ",Version=version};var observed=-1;
        var result=await StoredRecordIdentityLookup.SelectAsync(" OWNED ",version,requested=>{
            observed=requested;return Task.FromResult(new OASISResult<IEnumerable<Avatar>>(new[]{row}));},record=>record.Username);
        Assert.IsFalse(result.IsError);Assert.AreSame(row,result.Result);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(version,observed);
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task MissingAmbiguousAndCorruptCollectionsCannotChooseArbitraryRecord(int kind)
    {
        var rows=kind==0?Array.Empty<Avatar>():kind==1?new[]{new Avatar{Username="owned"},new Avatar{Username="OWNED"}}:new Avatar[]{null!};
        var result=await StoredRecordIdentityLookup.SelectAsync("owned",0,_=>Task.FromResult(new OASISResult<IEnumerable<Avatar>>(rows)),row=>row.Username);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        if(kind==0)Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
        if(kind==1){Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);Assert.IsInstanceOfType<StorageIdentityAmbiguousException>(result.Exception);}
        if(kind==2)Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
    }
    [TestMethod]
    public async Task UpstreamFailureKeepsCauseAndErrorClassification()
    {
        var cause=new IOException("owned storage failure");var upstream=new OASISResult<IEnumerable<Avatar>>{IsError=true,IsWarning=true,Exception=cause,ErrorCode="owned-classification",Message="owned-message"};
        var result=await StoredRecordIdentityLookup.SelectAsync("owned",0,_=>Task.FromResult(upstream),row=>row.Username);
        Assert.IsTrue(result.IsError);Assert.IsTrue(result.IsWarning);Assert.IsNull(result.Result);Assert.AreSame(cause,result.Exception);Assert.AreEqual(upstream.ErrorCode,result.ErrorCode);Assert.AreEqual(upstream.Message,result.Message);
    }
}
