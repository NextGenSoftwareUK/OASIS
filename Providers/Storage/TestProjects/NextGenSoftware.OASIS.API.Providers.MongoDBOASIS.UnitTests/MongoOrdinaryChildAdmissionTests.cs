using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoOrdinaryChildAdmissionTests
{
    [DataTestMethod]
    [DataRow(false, false, false)]
    [DataRow(false, false, true)]
    [DataRow(false, true, false)]
    [DataRow(false, true, true)]
    [DataRow(true, false, false)]
    [DataRow(true, false, true)]
    [DataRow(true, true, false)]
    [DataRow(true, true, true)]
    public async Task PartialChildSelectionRejectsBeforeRepositoryAccess(
        bool synchronous, bool batch, bool acceptChanges)
    {
        // A filtered/depth-limited view does not own replacement of hidden children.
        // AcceptChanges acknowledges local edits; it cannot grant store observation.
        var root = new Holon { Id = Guid.NewGuid() };
        root.Children.Add(new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id });
        HolonChangeTracking.MarkIncompleteChildSelection(root);
        if (acceptChanges) root.AcceptChanges();
        var provider = new Provider("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1",
            "unactivated-partial-child-admission-only");

        NextGenSoftware.OASIS.Common.OASISResult<IHolon>? single = null;
        NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolon>>? many = null;
        if (batch)
            many = synchronous ? provider.SaveHolons(new[] { root }, saveChildren: true)
                : await provider.SaveHolonsAsync(new[] { root }, saveChildren: true);
        else
            single = synchronous ? provider.SaveHolon(root, saveChildren: true)
                : await provider.SaveHolonAsync(root, saveChildren: true);

        Assert.IsTrue(batch ? many!.IsError : single!.IsError);
        Assert.IsFalse(batch ? many!.IsSaved : single!.IsSaved);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID", batch ? many!.ErrorCode : single!.ErrorCode);
        Assert.IsInstanceOfType<InvalidOperationException>(batch ? many!.Exception : single!.Exception);
        StringAssert.Contains((batch ? many!.Exception : single!.Exception).Message, "fully observed");
        Assert.IsFalse(provider.IsProviderActivated);
    }
}