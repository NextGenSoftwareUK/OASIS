using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Native=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoJsonNumberPrecisionTests
{
    [DataTestMethod]
    [DataRow("1.000000000000000000000000001",false)] [DataRow("1.000000000000000000000000001",true)]
    [DataRow("0.1234567890123456789012345678",false)] [DataRow("0.1234567890123456789012345678",true)]
    [DataRow("1e-28",false)] [DataRow("1e-28",true)]
    [DataRow("79228162514264337593543950335",false)] [DataRow("79228162514264337593543950335",true)]
    public void JsonNumbersUseExactDecimalWhenAvailable(string text,bool nested)
    {
        using var json=JsonDocument.Parse(nested?"{\"value\":"+text+"}":text);
        var original=new Native{MetaData=new(){["number"]=json.RootElement.Clone()}};
        var native=original.ToBsonDocument();var value=nested?native["MetaData"]["number"]["value"]:native["MetaData"]["number"];
        Assert.AreEqual(BsonType.Decimal128,value.BsonType);
        var loaded=BsonSerializer.Deserialize<Native>(native.ToBson());
        var actual=nested?((Dictionary<string,object>)loaded.MetaData["number"])["value"]:loaded.MetaData["number"];
        Assert.IsTrue(StoredRecordValueEquality.Equivalent(new JRaw(text),JToken.FromObject(actual)));
    }

    [DataTestMethod]
    [DataRow("1e-1000")] [DataRow("1e1000")]
    [DataRow("0.1234567890123456789012345678901")]
    public void UnrepresentableJsonNumbersRejectInsteadOfRoundingOrUnderflowing(string text)
    {
        using var json=JsonDocument.Parse(text);
        var value=new Native{MetaData=new(){["number"]=json.RootElement.Clone()}};
        Assert.ThrowsException<BsonSerializationException>(()=>value.ToBsonDocument());
    }

    [DataTestMethod]
    [DataRow("17",BsonType.Int32)] [DataRow("2147483648",BsonType.Int64)]
    [DataRow("9223372036854775807",BsonType.Int64)] [DataRow("1e100",BsonType.Double)]
    public void ExactExistingNumericRepresentationsRemainSupported(string text,BsonType expected)
    {
        using var json=JsonDocument.Parse(text);
        var original=new Native{MetaData=new(){["number"]=json.RootElement.Clone(),["literal"]=text}};
        var native=original.ToBsonDocument();Assert.AreEqual(expected,native["MetaData"]["number"].BsonType);
        var loaded=BsonSerializer.Deserialize<Native>(native.ToBson());
        Assert.IsTrue(StoredRecordValueEquality.Equivalent(new JRaw(text),JToken.FromObject(loaded.MetaData["number"])));
        Assert.AreEqual(text,loaded.MetaData["literal"]);
    }

    [DataTestMethod]
    [DataRow("1.000000000000000000000000001",false)] [DataRow("1e-28",false)]
    [DataRow("0.1234567890123456789012345678",false)] [DataRow("79228162514264337593543950335",false)]
    [DataRow("1e-1000",true)] [DataRow("1e1000",true)] [DataRow("0.1234567890123456789012345678901",true)]
    public void LosslessRawJsonNumbersRetainNumericIdentityOrReject(string text,bool rejects)
    {
        var original=new Native{MetaData=new(){["number"]=new JRaw(text)}};
        if(rejects){Assert.ThrowsException<BsonSerializationException>(()=>original.ToBsonDocument());return;}
        var native=original.ToBsonDocument();Assert.AreEqual(BsonType.Decimal128,native["MetaData"]["number"].BsonType);
        var loaded=BsonSerializer.Deserialize<Native>(native.ToBson());
        Assert.IsTrue(StoredRecordValueEquality.Equivalent(new JRaw(text),JToken.FromObject(loaded.MetaData["number"])));
    }
}
