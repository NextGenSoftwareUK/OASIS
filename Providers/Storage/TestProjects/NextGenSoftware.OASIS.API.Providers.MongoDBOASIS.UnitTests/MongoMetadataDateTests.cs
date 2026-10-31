using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Serializers;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoMetadataDateTests
{
    public sealed class ComplexMetadataNode
    {
        public string Name{get;set;}="owned";
        public ComplexMetadataNode? Child{get;set;}
    }
    [TestMethod]
    public void CyclicComplexMetadataCannotBeSavedAsTruncatedJson()
    {
        var node=new ComplexMetadataNode();node.Child=node;
        var document=new BsonDocument();using var writer=new BsonDocumentWriter(document);
        var error=Assert.ThrowsException<BsonSerializationException>(()=>new MetaDataDictionarySerializer().Serialize(
            BsonSerializationContext.CreateRoot(writer),new BsonSerializationArgs{NominalType=typeof(Dictionary<string,object>)},
            new Dictionary<string,object>{{"complex",node}}));
        Assert.IsNotNull(error.InnerException);
    }
    [TestMethod]
    public void AcyclicComplexMetadataRetainsExistingJsonTextContract()
    {
        var node=new ComplexMetadataNode{Child=new ComplexMetadataNode{Name="child"}};
        var document=new BsonDocument();using var writer=new BsonDocumentWriter(document);
        new MetaDataDictionarySerializer().Serialize(BsonSerializationContext.CreateRoot(writer),
            new BsonSerializationArgs{NominalType=typeof(Dictionary<string,object>)},new Dictionary<string,object>{{"complex",node}});
        using var json=System.Text.Json.JsonDocument.Parse(document["complex"].AsString);
        Assert.AreEqual("owned",json.RootElement.GetProperty("Name").GetString());
        Assert.AreEqual("child",json.RootElement.GetProperty("Child").GetProperty("Name").GetString());
    }
    [DataTestMethod]
    [DataRow(0,false)][DataRow(1,false)][DataRow(2,false)][DataRow(3,false)][DataRow(4,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)][DataRow(3,true)][DataRow(4,true)]
    public void UnsupportedNativeMetadataTypesFailInsteadOfBecomingNull(int kind,bool nested)
    {
        BsonValue value=kind switch{
            0=>new BsonRegularExpression("owned","i"),1=>new BsonTimestamp(123),
            2=>BsonMinKey.Value,3=>BsonMaxKey.Value,_=>BsonUndefined.Value};
        var values=new BsonDocument("unsupported",value);
        var document=nested?new BsonDocument("nested",values):values;
        using var reader=new BsonDocumentReader(document);
        var error=Assert.ThrowsException<BsonSerializationException>(()=>new MetaDataDictionarySerializer().Deserialize(
            BsonDeserializationContext.CreateRoot(reader),new BsonDeserializationArgs{NominalType=typeof(Dictionary<string,object>)}));
        StringAssert.Contains(error.Message,value.BsonType.ToString());
    }
    [DataTestMethod]
    [DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public void BsonUuidMetadataRetainsIdentityForStandardAndExistingCSharpLegacy(bool nested,bool legacy)
    {
        _=new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS("mongodb://127.0.0.1:27028/?serverSelectionTimeoutMS=100","owned-uuid-unit");
        var identity=Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var representation=legacy?GuidRepresentation.CSharpLegacy:GuidRepresentation.Standard;
        var values=new BsonDocument("identity",new BsonBinaryData(identity,representation));
        var document=nested?new BsonDocument("nested",values):values;
        using var reader=new BsonDocumentReader(document);
        var result=new MetaDataDictionarySerializer().Deserialize(BsonDeserializationContext.CreateRoot(reader),
            new BsonDeserializationArgs{NominalType=typeof(Dictionary<string,object>)});
        var metadata=nested?(Dictionary<string,object>)result["nested"]:result;
        Assert.AreEqual(identity.ToString(),metadata["identity"]);
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void BsonDatesRetainUtcTypeWithoutInferringNumbersOrStrings(bool nested)
    {
        // Match application bootstrap before BSON's process-global object serializer is first resolved.
        // Construction configures serialization only; do not activate or contact this endpoint.
        _=new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS("mongodb://127.0.0.1:27028/?serverSelectionTimeoutMS=100","owned-date-unit");
        var when=new DateTime(2020,1,2,3,4,5,DateTimeKind.Utc);
        var milliseconds=new DateTimeOffset(when).ToUnixTimeMilliseconds();
        var values=new BsonDocument {{"when",new BsonDateTime(when)},{"number",new BsonInt64(milliseconds)},{"text",when.ToString("O")}};
        var document=nested ? new BsonDocument("nested",values):values;
        using var reader=new BsonDocumentReader(document);
        var result=new MetaDataDictionarySerializer().Deserialize(BsonDeserializationContext.CreateRoot(reader),
            new BsonDeserializationArgs {NominalType=typeof(Dictionary<string,object>)});
        var metadata=nested ? (Dictionary<string,object>)result["nested"]:result;
        Assert.IsInstanceOfType(metadata["when"],typeof(DateTime));
        Assert.AreEqual(when,(DateTime)metadata["when"]);Assert.AreEqual(DateTimeKind.Utc,((DateTime)metadata["when"]).Kind);
        Assert.IsInstanceOfType(metadata["number"],typeof(long));Assert.AreEqual(milliseconds,(long)metadata["number"]);
        Assert.IsInstanceOfType(metadata["text"],typeof(string));Assert.AreEqual(when.ToString("O"),metadata["text"]);
    }
}
