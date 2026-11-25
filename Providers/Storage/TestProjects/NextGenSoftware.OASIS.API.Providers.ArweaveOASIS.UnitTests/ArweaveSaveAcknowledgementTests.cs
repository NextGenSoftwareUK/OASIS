using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS.UnitTests;

[TestClass]
public class ArweaveSaveAcknowledgementTests
{
    private static (ArweaveOASIS Provider, FakeArweaveService Service, Holon Row) Setup()
    {
        var s = new FakeArweaveService();
        var p = new ArweaveOASIS(s, FakeArweaveService.Wallet) { IsProviderActivated = true };
        return (p, s, new Holon { Id = Guid.NewGuid(), Name = "coffee", IsNewHolon = true });
    }

    [TestMethod]
    public async Task SaveIsAcknowledgedOnlyAfterTheNativePost()
    {
        var (p, s, row) = Setup();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        s.PostAcknowledgement = async n => { await gate.Task; return n.ToString("D43"); };
        var pending = p.SaveHolonAsync(row, saveChildren: false);
        await Task.Delay(50);
        Assert.IsFalse(pending.IsCompleted);
        gate.SetResult();
        var saved = await pending;
        Assert.IsFalse(saved.IsError, saved.Message); Assert.IsTrue(saved.IsSaved); Assert.AreEqual(1, saved.SavedCount);
        var loaded = await p.LoadHolonAsync(row.Id, loadChildren: false);
        Assert.IsFalse(loaded.IsError, loaded.Message); Assert.AreEqual("coffee", loaded.Result.Name);
    }

    [DataTestMethod][DataRow("")][DataRow("not-a-transaction")]
    public async Task InvalidReceiptFailsTheSave(string receipt)
    {
        var (p, s, row) = Setup(); s.PostAcknowledgement = _ => Task.FromResult(receipt);
        var result = await p.SaveHolonAsync(row, saveChildren: false);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved); Assert.AreEqual(0, result.SavedCount);
    }

    [TestMethod]
    public async Task RejectedPostFailsTheSave()
    {
        var (p, s, row) = Setup(); s.PostAcknowledgement = _ => Task.FromException<string>(new IOException("synthetic rejected post"));
        var result = await p.SaveHolonAsync(row, saveChildren: false);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved);
    }

    [TestMethod]
    public async Task ContinueOnErrorDoesNotTurnAllFailedWritesIntoSuccess()
    {
        var (p, s, row) = Setup(); s.PostAcknowledgement = _ => Task.FromException<string>(new IOException("synthetic rejected post"));
        var result = await p.SaveHolonsAsync(new IHolon[] { row }, saveChildren: false, continueOnError: true);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved); Assert.AreEqual(0, result.SavedCount);
    }

    [TestMethod]
    public async Task NewestSequenceWinsAndDeleteThenResaveRevives()
    {
        var (p, _, row) = Setup();
        Assert.IsFalse((await p.SaveHolonAsync(row, saveChildren: false)).IsError);
        row.Name = "tea"; row.IsNewHolon = false;
        Assert.IsFalse((await p.SaveHolonAsync(row, saveChildren: false)).IsError);
        Assert.AreEqual("tea", (await p.LoadHolonAsync(row.Id, loadChildren: false)).Result.Name);
        Assert.IsFalse((await p.DeleteHolonAsync(row.Id)).IsError);
        var missing = await p.LoadHolonAsync(row.Id, loadChildren: false);
        Assert.IsTrue(missing.IsError); Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, missing.ErrorCode);
        var fresh = new Holon { Id = row.Id, Name = "revived", IsNewHolon = true };
        Assert.IsFalse((await p.SaveHolonAsync(fresh, saveChildren: false)).IsError);
        Assert.AreEqual("revived", (await p.LoadHolonAsync(row.Id, loadChildren: false)).Result.Name);
    }

    [TestMethod]
    public async Task ChainCarriesNoPlaintextKeysOrValues()
    {
        var (p, s, row) = Setup(); row.Name = "plaintext-marker";
        Assert.IsFalse((await p.SaveHolonAsync(row, saveChildren: false)).IsError);
        foreach (var t in s.Transactions)
        {
            StringAssert.StartsWith(t.Tags["OASIS-Key"], "oasis/private/v1/");
            Assert.IsFalse(Encoding.UTF8.GetString(t.Data).Contains("plaintext-marker"));
            Assert.IsFalse(t.Tags.Values.Any(v => v.Contains(row.Id.ToString("N"))));
        }
    }

    [TestMethod]
    public void RecordKeyIsDeterministicPerWallet()
    {
        CollectionAssert.AreEqual(ArweaveOASIS.DeriveRecordKey(FakeArweaveService.Wallet), ArweaveOASIS.DeriveRecordKey(FakeArweaveService.Wallet));
        CollectionAssert.AreNotEqual(ArweaveOASIS.DeriveRecordKey(FakeArweaveService.Wallet), ArweaveOASIS.DeriveRecordKey("{\"d\":\"AQID\"}"));
        Assert.ThrowsException<InvalidOperationException>(() => ArweaveOASIS.DeriveRecordKey("{}"));
    }
}
