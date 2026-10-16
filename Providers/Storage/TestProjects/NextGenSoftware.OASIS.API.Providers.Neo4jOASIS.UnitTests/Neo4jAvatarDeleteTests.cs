using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jAvatarDeleteTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task InvalidHolonDeleteIdentityFailsBeforeNativeDispatch(bool synchronous,bool byKey)
    {
        var driver=System.Reflection.DispatchProxy.Create<Neo4j.Driver.IDriver,Neo4jLifecycleTests.DriverProbe>();
        var provider=new Provider("bolt://127.0.0.1:17687","neo4j","owned",()=>driver);
        var result=byKey ? synchronous ? provider.DeleteHolon(" "):await provider.DeleteHolonAsync(" ")
            :synchronous ? provider.DeleteHolon(Guid.Empty):await provider.DeleteHolonAsync(Guid.Empty);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsDeleted);
        Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
        Assert.AreEqual(0,((Neo4jLifecycleTests.DriverProbe)driver).Verifications);
    }

    [TestMethod]
    public void SoftDeletePreservesRecordAndNeverPlansNodeRemoval()
    {
        var avatar = new Avatar { Id = Guid.NewGuid(), FirstName = "preserve", Username = "owned", IsActive = true };
        avatar.MetaData["seed"] = "oak";
        var original = OasisJson.Serialize(avatar);
        var now = DateTime.UtcNow;
        var plan = Provider.PlanAvatarDelete(original, avatar.Id.ToString(), true, now);
        Assert.IsFalse(plan.Query.Contains("DELETE", StringComparison.Ordinal));
        StringAssert.Contains(plan.Query, "a.SnapshotJson = $expected");
        Assert.IsTrue(plan.Query.IndexOf("REMOVE a.__oasisAvatarLock", StringComparison.Ordinal)
            < plan.Query.IndexOf("WHERE a.SnapshotJson", StringComparison.Ordinal));
        var deleted = Provider.ReadAvatarSnapshot((string)plan.Parameters["snapshot"], avatar.Id.ToString());
        Assert.IsFalse(deleted.IsActive); Assert.IsTrue(deleted.IsDeleted);
        Assert.AreEqual(now, deleted.DeletedDate); Assert.AreEqual("preserve", deleted.FirstName);
        Assert.AreEqual("oak", deleted.MetaData["seed"]); Assert.IsTrue(avatar.IsActive);
        Assert.AreEqual(original, plan.Parameters["expected"]);
        var repeated = Provider.PlanAvatarDelete((string)plan.Parameters["snapshot"], avatar.Id.ToString(), true, now.AddDays(1));
        Assert.AreEqual(now, repeated.Parameters["deletedDate"]);
    }

    [TestMethod]
    public void HardDeleteIsExplicitAndRetainsCompareGuard()
    {
        var avatar = new Avatar { Id = Guid.NewGuid() };
        var plan = Provider.PlanAvatarDelete(OasisJson.Serialize(avatar), avatar.Id.ToString(), false, DateTime.UtcNow);
        StringAssert.Contains(plan.Query, "DETACH DELETE a");
        StringAssert.Contains(plan.Query, "a.SnapshotJson = $expected");
        Assert.IsFalse(plan.Parameters.ContainsKey("snapshot"));
    }

    [DataTestMethod]
    [DataRow("Id")] [DataRow("ProviderKey")] [DataRow("Email")] [DataRow("Username")]
    public void SelectorValuesRemainParameterized(string field)
        => StringAssert.Contains(Provider.AvatarDeleteSelection(field), ": $value");

    [TestMethod]
    public void UnrecognizedSelectorCannotBecomeCypher()
        => Assert.ThrowsException<ArgumentException>(() => Provider.AvatarDeleteSelection("Id}) DETACH DELETE a //"));

    [TestMethod]
    public void UncertainMutationRetainsCauseWithoutInventingTransactionId()
    {
        var cause = new IOException("owned lost acknowledgement");
        var failure = new NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jMutationReconciliationException(cause);
        Assert.AreSame(cause, failure.InnerException);
        Assert.AreEqual(string.Empty, failure.OriginalTransactionId);
        Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(failure));
    }
}
