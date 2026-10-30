using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS.UnitTests;

[TestClass]
public class ArweaveSaveAcknowledgementTests
{
    private sealed class Service : IArweaveService
    {
        public TaskCompletionSource<string> Acknowledgement = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Writes;
        public Task ProbeAsync()=>Task.CompletedTask;
        public Task<string> PostTransactionAsync(byte[] data,string contentType,Dictionary<string,string>? tags=null)
        { Writes++;return Acknowledgement.Task; }
        public Task<byte[]> GetTransactionDataAsync(string id)=>throw new AssertFailedException("No read expected");
        public Task<List<string>> QueryByTagsAsync(Dictionary<string,string> tags)=>throw new AssertFailedException("No query expected");
        public Task<List<string>> QueryOwnedByTagsAsync(Dictionary<string,string> tags)=>throw new AssertFailedException("No query expected");
    }
    private static (ArweaveOASIS Provider,Service Service,Holon Row) Setup()
    {
        var p=new ArweaveOASIS("{}","http://127.0.0.1:1","absent"){IsProviderActivated=true};var s=new Service();
        typeof(ArweaveOASIS).GetField("_arweaveService",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(p,s);
        var row=new Holon{Id=Guid.NewGuid(),Name="coffee",IsNewHolon=true};
        row.ProviderUniqueStorageKey[ProviderType.ArweaveOASIS]="original-key";
        return(p,s,row);
    }
    [TestMethod]
    public async Task SavePublishesReceiptOnlyAfterNativeAcknowledgement()
    {
        var(p,s,row)=Setup();var pending=p.SaveHolonAsync(row,saveChildren:false);
        Assert.IsFalse(pending.IsCompleted);Assert.AreEqual("original-key",row.ProviderUniqueStorageKey[ProviderType.ArweaveOASIS]);
        var tx=new string('a',43);s.Acknowledgement.SetResult(tx);var saved=await pending;
        Assert.IsFalse(saved.IsError,saved.Message);Assert.IsTrue(saved.IsSaved);Assert.AreEqual(1,saved.SavedCount);
        Assert.AreEqual(tx,row.ProviderUniqueStorageKey[ProviderType.ArweaveOASIS]);Assert.IsTrue(row.IsNewHolon,"Manager owns clean-state publication");
    }
    [DataTestMethod][DataRow("")][DataRow("not-a-transaction")]
    public async Task InvalidReceiptDoesNotPublishCallerKey(string receipt)
    {
        var(p,s,row)=Setup();s.Acknowledgement.SetResult(receipt);var result=await p.SaveHolonAsync(row,saveChildren:false);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,result.SavedCount);
        Assert.AreEqual("original-key",row.ProviderUniqueStorageKey[ProviderType.ArweaveOASIS]);
    }
    [TestMethod]
    public async Task RejectedPostDoesNotPublishCallerKey()
    {
        var(p,s,row)=Setup();s.Acknowledgement.SetException(new IOException("synthetic rejected post"));
        var result=await p.SaveHolonAsync(row,saveChildren:false);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);
        Assert.AreEqual("original-key",row.ProviderUniqueStorageKey[ProviderType.ArweaveOASIS]);
    }
    [TestMethod]
    public async Task BatchReportsAcknowledgedCount()
    {
        var(p,s,row)=Setup();s.Acknowledgement.SetResult(new string('a',43));
        var result=await p.SaveHolonsAsync(new IHolon[]{row},saveChildren:false);
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsSaved);Assert.AreEqual(1,result.SavedCount);
    }
    [TestMethod]
    public async Task ContinueOnErrorDoesNotTurnAllFailedWritesIntoSuccess()
    {
        var(p,s,row)=Setup();s.Acknowledgement.SetException(new IOException("synthetic rejected post"));
        var result=await p.SaveHolonsAsync(new IHolon[]{row},saveChildren:false,continueOnError:true);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,result.SavedCount);
    }
}
