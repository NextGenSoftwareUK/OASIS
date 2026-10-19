using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class NativeTransactionFollowupTests
{
    [TestMethod]
    public void OriginalReceiptAndCauseRemainDiscoverableThroughOuterErrors()
    {
        var cause=new InvalidOperationException("Followup failed.");
        var receipt=new NativeTransactionFollowupException(ProviderType.NEAROASIS,"owned-acknowledged-receipt",cause);
        var outer=new InvalidOperationException("Outer operation failed.",receipt);
        Assert.AreSame(cause,receipt.InnerException);Assert.AreEqual(ProviderType.NEAROASIS,receipt.ProviderType);
        Assert.IsTrue(NativeMutationReconciliation.IsRequired(outer));
        CollectionAssert.Contains(NativeMutationReconciliation.OriginalTransactionIds(outer).ToArray(),"owned-acknowledged-receipt");
    }

    [DataTestMethod][DataRow(null)][DataRow("")][DataRow(" ")]
    public void MissingReceiptCannotBePresentedAsAcknowledged(string receipt)
        =>Assert.ThrowsException<ArgumentException>(()=>new NativeTransactionFollowupException(ProviderType.NEAROASIS,receipt,new Exception()));
}
