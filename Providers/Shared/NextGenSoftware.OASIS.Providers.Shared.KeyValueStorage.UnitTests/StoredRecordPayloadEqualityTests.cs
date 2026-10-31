using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class StoredRecordPayloadEqualityTests
{
    [DataTestMethod]
    [DataRow("1e9223372036854775808","10e9223372036854775807",true)]
    [DataRow("1e-9223372036854775809","10e-9223372036854775810",true)]
    [DataRow("1e9223372036854775808","1e9223372036854775809",false)]
    public void SharedEntryPointUsesExactUnboundedExponentOwner(string left,string right,bool equal)
        =>Assert.AreEqual(equal,StoredRecordPayloadEquality.Equivalent(new JRaw(left),new JRaw(right)));

    [TestMethod]
    public void FiniteNumericFingerprintRetainsExistingCanonicalBytes()
    {
        var content=new JObject{["c"]=new JRaw("1e100"),["b"]=new JArray(new JValue(1000),new JValue(-12.5m)),["a"]=new JRaw("1.00")};
        const string canonical="{\"a\":1,\"b\":[1e3,-125e-1],\"c\":1e100}";
        var hash=System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical)));
        Assert.AreEqual(hash,ConditionalHolonPlan.ComputeFingerprint(content));
    }

    [DataTestMethod]
    [DataRow("1.00", "1", true)]
    [DataRow("1e100", "1E+100", true)]
    [DataRow("1.0000000000000000000000000001", "1.0000000000000000000000000002", false)]
    [DataRow("-0.00", "0", true)]
    [DataRow("\"1\"", "1", false)]
    public void RawNumericMetadataUsesExactValueMeaning(string left,string right,bool equal)
        =>Assert.AreEqual(equal,StoredRecordPayloadEquality.Equivalent(
            new JObject { ["value"]=new JRaw(left) },
            new JObject { ["value"]=new JRaw(right) }));

    [TestMethod]
    public void RawNumericMetadataMatchesClrDecimalButNotText()
    {
        Assert.IsTrue(StoredRecordPayloadEquality.Equivalent(new JRaw("1.00"),new JValue(1m)));
        Assert.IsFalse(StoredRecordPayloadEquality.Equivalent(new JRaw("1"),new JValue("1")));
    }

    [DataTestMethod]
    [DataRow("151.0","151",true)][DataRow("1e3","1000",true)]
    [DataRow("0.0","-0.0",true)][DataRow("-12.5","-12.50",true)]
    [DataRow("9007199254740993","9007199254740992.0",false)]
    [DataRow("151.0","152",false)][DataRow("1e300","1e299",false)]
    [DataRow("{\"x\":1.0}","{\"x\":1}",true)]
    [DataRow("{\"x\":1}","{\"X\":1}",false)]
    [DataRow("{\"x\":null}","{}",false)]
    [DataRow("[1,2]","[2,1]",false)][DataRow("[1,2.0]","[1.0,2]",true)]
    [DataRow("\"151\"","151",false)]
    public void JsonValuesPreserveFieldsOrderAndExactNumericMeaning(string expected,string actual,bool equal)
        =>Assert.AreEqual(equal,StoredRecordPayloadEquality.Equivalent(JToken.Parse(expected),JToken.Parse(actual)));
}
