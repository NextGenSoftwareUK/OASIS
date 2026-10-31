using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace CosmosConsumerUnitModels { public sealed class AvatarInput:Avatar { } }

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests
{
    [TestClass]
    public class CosmosConsumerProjectionTests
    {
        [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)]
        public async Task EveryRepositoryWriteProjectsTrustedInputBeforeSdkDispatch(int operation)
        {
            var id=Guid.NewGuid();var calls=0;
            var input=new CosmosConsumerUnitModels.AvatarInput{Id=id,FirstName="canonical input"};
            var expected=new CosmosConsumerUnitModels.AvatarInput{Id=id,FirstName="previous canonical input"};
            string Reply(object value)
            {
                calls++;Assert.AreEqual(typeof(Avatar),value.GetType());
                var document=Newtonsoft.Json.Linq.JObject.Parse(OasisJson.Serialize(value));document["id"]=id.ToString();
                Assert.IsFalse(document.ToString().Contains("CosmosConsumerUnitModels",StringComparison.Ordinal));
                return document.ToString();
            }
            var client=new Mock<ICosmosDbClient>();
            client.Setup(c=>c.CreateDocumentAsync(It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>()))
                .Returns((object value,PartitionKey? _,CancellationToken __)=>Task.FromResult(Reply(value)));
            client.Setup(c=>c.UpsertDocumentAsync(It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>()))
                .Returns((object value,PartitionKey? _,CancellationToken __)=>Task.FromResult(Reply(value)));
            client.Setup(c=>c.ReplaceDocumentAsync(It.IsAny<string>(),It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>()))
                .Returns((string _,object value,PartitionKey? __,CancellationToken ___)=>Task.FromResult(Reply(value)));
            client.Setup(c=>c.ReplaceUnchangedDocumentAsync(It.IsAny<string>(),It.IsAny<object>(),It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>()))
                .Returns((string _,object original,object value,PartitionKey? __,CancellationToken ___)=>
                {Assert.AreEqual(typeof(Avatar),original.GetType());return Task.FromResult(Reply(value));});
            var factory=new Mock<ICosmosDbClientFactory>();factory.Setup(f=>f.GetClient("avatarItems")).Returns(client.Object);
            var repository=new AvatarRepository(factory.Object);
            if(operation==0)await repository.AddAsync(input);
            else if(operation==1)await repository.UpsertAsync(input);
            else if(operation==2)await repository.UpdateAsync(input);
            else await repository.ReplaceIfUnchangedAsync(expected,input);
            Assert.AreEqual(1,calls);Assert.AreEqual("canonical input",input.FirstName);Assert.AreEqual("previous canonical input",expected.FirstName);
        }
    }
}
