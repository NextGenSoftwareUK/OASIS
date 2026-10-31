using System.Net;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosUnicodeAdmissionTests
{
    public static IEnumerable<object[]> Cases(){foreach(var record in Enumerable.Range(0,3))foreach(var field in Enumerable.Range(0,3))foreach(var kind in Enumerable.Range(0,3))yield return new object[]{record,field,kind};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task InvalidUtf16CannotBecomeAStoredReplacementCharacter(int record,int field,int kind)
    {
        var text="owned-"+(kind==0?"\uD800":kind==1?"\uDC00":"\uD83C\uDF33");
        IHolonBase source=record switch{1=>new Avatar(),2=>new AvatarDetail(),_=>new Holon()};
        source.Id=Guid.NewGuid();source.Description=field==0?text:"owned-valid";source.MetaData=new();
        var originalId=source.Id;
        if(field==1)source.MetaData[text]="value";
        if(field==2)source.MetaData["key"]=text;
        Assert.AreEqual(field==0?text:"owned-valid",source.Description,"The tested text must actually be present on the source record.");
        var writes=0;var container=new Mock<Container>(MockBehavior.Strict);
        container.Setup(value=>value.CreateItemStreamAsync(It.IsAny<Stream>(),It.IsAny<PartitionKey>(),It.IsAny<ItemRequestOptions>(),It.IsAny<CancellationToken>()))
            .Returns(async(Stream stream,PartitionKey _,ItemRequestOptions __,CancellationToken ___)=>{
                writes++;using var reader=new StreamReader(stream,Encoding.UTF8,false,1024,true);
                var payload=await reader.ReadToEndAsync();
                return new ResponseMessage(HttpStatusCode.Created){Content=new MemoryStream(Encoding.UTF8.GetBytes(payload))};
            });
        Exception? failure=null;string? result=null;
        try{result=await new CosmosDbClient(container.Object).CreateDocumentAsync(source,new PartitionKey(source.Id.ToString()));}
        catch(Exception cause){failure=cause;}
        if(kind<2){
            Assert.IsNotNull(failure,"Unpaired UTF16 must not be silently acknowledged.");
            Assert.IsFalse(failure is MockException,"An unexpected mock invocation is not validation proof.");
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(failure),"Input rejection must not masquerade as a dispatched uncertain write.");
            Assert.AreEqual(0,writes,"Reject malformed text before native mutation.");Assert.IsNull(result);
        }
        else{
            Assert.IsNull(failure,failure?.ToString());Assert.AreEqual(1,writes);
            var decoded=(IHolonBase)OasisJson.Deserialize(result!,source.GetType());
            if(field==0)Assert.AreEqual(text,decoded.Description);
            if(field==1)Assert.AreEqual("value",decoded.MetaData[text]);
            if(field==2)Assert.AreEqual(text,decoded.MetaData["key"]);
        }
        Assert.AreEqual(field==0?text:"owned-valid",source.Description);
        Assert.AreEqual(originalId,source.Id);
        if(field==1)Assert.IsTrue(source.MetaData.ContainsKey(text));
        if(field==2)Assert.AreEqual(text,source.MetaData["key"]);
    }
}
