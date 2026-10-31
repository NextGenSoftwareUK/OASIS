using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jMetadataQueryTests
{
    [DataTestMethod]
    [DataRow(MetaKeyValuePairMatchMode.All)] [DataRow(MetaKeyValuePairMatchMode.Any)]
    public void KeysAndValuesNeverBecomeCypherText(MetaKeyValuePairMatchMode mode)
    {
        const string key = "key`}) DETACH DELETE h //";
        const string value = "'unsafe'";
        var plan = Provider.MetadataLookupQuery(new() { [key] = value }, mode, HolonType.All);
        Assert.IsFalse(plan.Query.Contains(key)); Assert.IsFalse(plan.Query.Contains(value));
        StringAssert.Contains(plan.Query, mode == MetaKeyValuePairMatchMode.All ? "all(token" : "any(token");
        Assert.AreEqual(1, ((List<string>)plan.Parameters["tokens"]).Count);
    }

    [TestMethod]
    public void EmptyBlankAndInvalidModesAreRejectedBeforeNativeQuery()
    {
        Assert.ThrowsException<ArgumentException>(() => Provider.MetadataLookupQuery(new(), MetaKeyValuePairMatchMode.All, HolonType.All));
        Assert.ThrowsException<ArgumentException>(() => Provider.MetadataLookupQuery(new() { [" "] = "x" }, MetaKeyValuePairMatchMode.All, HolonType.All));
        Assert.ThrowsException<ArgumentException>(() => Provider.MetadataLookupQuery(new() { ["a"] = "b" }, (MetaKeyValuePairMatchMode)int.MaxValue, HolonType.All));
    }
}
