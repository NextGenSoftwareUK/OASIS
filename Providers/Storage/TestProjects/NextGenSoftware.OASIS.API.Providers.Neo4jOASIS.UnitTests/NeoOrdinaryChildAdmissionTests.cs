using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoOrdinaryChildAdmissionTests
{
    [DataTestMethod]
    [DataRow(false, false, false)] [DataRow(false, false, true)]
    [DataRow(false, true, false)] [DataRow(false, true, true)]
    [DataRow(true, false, false)] [DataRow(true, false, true)]
    [DataRow(true, true, false)] [DataRow(true, true, true)]
    public Task PartialSelectionCannotReachNativeSession(bool synchronous, bool batch, bool acceptChanges)
        => Probe(synchronous, batch, acceptChanges, true);

    [DataTestMethod]
    [DataRow(false, false)] [DataRow(false, true)]
    [DataRow(true, false)] [DataRow(true, true)]
    public Task PartialRootOnlySaveRemainsAdmitted(bool synchronous, bool batch)
        => Probe(synchronous, batch, true, false);

    private static async Task Probe(bool synchronous, bool batch, bool acceptChanges, bool children)
    {
        var driver = DispatchProxy.Create<IDriver, CosmicQueryAdmissionTests.NoIo>();
        var provider = new Neo4jOASIS("bolt://owned.invalid", "owned", "owned", () => driver)
            { IsProviderActivated = true };
        var root = new Holon { Id = Guid.NewGuid() };
        HolonChangeTracking.MarkIncompleteChildSelection(root);
        if (acceptChanges) root.AcceptChanges();
        NextGenSoftware.OASIS.Common.OASISResult<IHolon>? single = null;
        NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolon>>? many = null;
        if (batch)
            many = synchronous ? provider.SaveHolons(new[] { root }, saveChildren: children)
                : await provider.SaveHolonsAsync(new[] { root }, saveChildren: children);
        else
            single = synchronous ? provider.SaveHolon(root, saveChildren: children)
                : await provider.SaveHolonAsync(root, saveChildren: children);

        Assert.IsTrue(batch ? many!.IsError : single!.IsError);
        Assert.IsFalse(batch ? many!.IsSaved : single!.IsSaved);
        Assert.AreEqual(children ? 0 : 1, ((CosmicQueryAdmissionTests.NoIo)driver).Calls);
        if (children)
        {
            Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID", batch ? many!.ErrorCode : single!.ErrorCode);
            StringAssert.Contains((batch ? many!.Exception : single!.Exception).Message, "fully observed");
        }
    }
}