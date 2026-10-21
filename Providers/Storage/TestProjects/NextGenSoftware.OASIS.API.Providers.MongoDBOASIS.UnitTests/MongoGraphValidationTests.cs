using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoGraphValidationTests
{
    [DataTestMethod]
    [DataRow(true, true, true)]
    [DataRow(true, true, false)]
    [DataRow(true, false, true)]
    [DataRow(true, false, false)]
    [DataRow(false, true, true)]
    [DataRow(false, true, false)]
    [DataRow(false, false, true)]
    [DataRow(false, false, false)]
    public async Task NegativeDepthIsRejectedBeforeStorageAccess(bool synchronous, bool children, bool onProvider)
    {
        var provider = new Provider("mongodb://127.0.0.1:27017", "oasis-unit-tests");
        var child = new Holon();
        var root = new Holon { Children = new List<IHolon> { child } };
        var result = synchronous
            ? provider.SaveHolon(root, children, maxChildDepth: -1, saveChildrenOnProvider: onProvider)
            : await provider.SaveHolonAsync(root, children, maxChildDepth: -1, saveChildrenOnProvider: onProvider);
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.IsSaved);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID", result.ErrorCode);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual(Guid.Empty, root.Id);
        Assert.AreEqual(Guid.Empty, child.Id);
        Assert.AreEqual(Guid.Empty, child.ParentHolonId);
    }
    [DataTestMethod]
    [DataRow(false, false)] [DataRow(false, true)] [DataRow(true, false)] [DataRow(true, true)]
    public async Task SingleRecordCyclicMetadataFailsDuringCaptureBeforeRepositoryAccess(bool synchronous, bool list)
    {
        object cycle;
        if (list) { var values = new List<object>(); values.Add(values); cycle = values; }
        else { var values = new Dictionary<string, object>(); values["self"] = values; cycle = values; }
        var source = new Holon { MetaData = new() { ["owned"] = cycle } };
        var provider = new Provider("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1", "unactivated-cycle-only");
        var result = synchronous ? provider.SaveHolon(source, saveChildren: false)
            : await provider.SaveHolonAsync(source, saveChildren: false);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved); Assert.IsNull(result.Result);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID", result.ErrorCode);
        Assert.IsInstanceOfType<Newtonsoft.Json.JsonSerializationException>(result.Exception);
        StringAssert.Contains(result.Exception.Message, "Self referencing loop");
        Assert.AreEqual(Guid.Empty, source.Id); Assert.IsFalse(provider.IsProviderActivated);
    }
}
