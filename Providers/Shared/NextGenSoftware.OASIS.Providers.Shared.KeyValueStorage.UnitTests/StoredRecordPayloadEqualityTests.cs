using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class StoredRecordPayloadEqualityTests
{
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
