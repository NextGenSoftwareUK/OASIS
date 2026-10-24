using System.Reflection;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

public partial class GoogleConditionalWriterTests
{
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task PublicKnownOversizedRecordRejectsBeforeReadTransactionOrCommit(int kind)
    {
        var client=new Client();var provider=new GoogleCloudOASIS("owned-controlled-history",enableFirestore:true){IsProviderActivated=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,FirestoreDb.Create("owned-controlled-history",client));
        IHolonBase row=kind==0?new Avatar{Id=Guid.NewGuid()}:kind==1?new AvatarDetail{Id=Guid.NewGuid()}:new Holon{Id=Guid.NewGuid()};
        row.MetaData=new(){{"oversize",new string('x',2*1024*1024)}};
        var result=kind==0?Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IHolonBase>(await provider.SaveAvatarAsync((IAvatar)row))
            :kind==1?Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatarDetail,IHolonBase>(await provider.SaveAvatarDetailAsync((IAvatarDetail)row))
            :Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,IHolonBase>(await provider.SaveHolonAsync((IHolon)row,saveChildren:false));
        Assert.IsTrue(result.IsError);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
        Assert.AreEqual(0,client.Reads);Assert.AreEqual(0,client.Transactions);Assert.AreEqual(0,client.Writes);
    }
    [TestMethod]
    public async Task HistoryGrowthRejectsBeforeSecondCommitAndPreservesNativeOriginal()
    {
        var client=new Client();var first=await Write(client,new Holon{Id=Guid.NewGuid(),Version=1,Name=new string('x',600000)},true);
        var original=client.Stored!.Clone();var beforeWrites=client.Writes;
        await Assert.ThrowsExceptionAsync<ArgumentException>(()=>Write(client,new Holon{Id=first.Id,Version=2,PreviousVersionId=first.VersionId,VersionId=Guid.NewGuid(),Name=new string('y',600000)},true));
        Assert.AreEqual(beforeWrites,client.Writes);Assert.AreEqual(original,client.Stored);
    }
}
