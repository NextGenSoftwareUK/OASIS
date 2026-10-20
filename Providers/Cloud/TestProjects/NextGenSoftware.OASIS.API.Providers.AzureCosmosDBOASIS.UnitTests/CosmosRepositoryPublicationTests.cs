using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosRepositoryPublicationTests
{
    private sealed class ObservedProjection(ICosmosDbClientFactory factory):AvatarRepository(factory)
    {
        public int Projections;
        protected override IAvatar ProjectInput(IAvatar entity){Projections++;return base.ProjectInput(entity);}
    }
    [TestMethod]
    public async Task CallerGuardPreservesExistingRepositoryProjectionHook()
    {
        var source=new Avatar{Id=Guid.NewGuid()};var client=new Mock<ICosmosDbClient>();
        client.Setup(store=>store.UpsertDocumentAsync(It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>())).Returns((object value,PartitionKey? _,CancellationToken __)=>{
            var json=JObject.Parse(OasisJson.Serialize(value));json["id"]=source.Id.ToString();return Task.FromResult(json.ToString());
        });
        var factory=new Mock<ICosmosDbClientFactory>();factory.Setup(store=>store.GetClient("avatarItems")).Returns(client.Object);
        var repository=new ObservedProjection(factory.Object);var stored=await repository.UpsertAsync(source);
        Assert.AreEqual(source.Id,stored.Id);Assert.AreEqual(1,repository.Projections);
    }

    [DataTestMethod][DataRow(0,0)][DataRow(0,1)][DataRow(0,2)]
    [DataRow(1,0)][DataRow(1,1)][DataRow(1,2)][DataRow(2,0)][DataRow(2,1)][DataRow(2,2)]
    public async Task DirectRepositoryWriteCannotOverwriteEditedCallerVersion(int kind,int operation)
    {
        IHolonBase source=kind==0?new Avatar{Id=Guid.NewGuid()}:kind==1?new AvatarDetail{Id=Guid.NewGuid()}:new Holon{Id=Guid.NewGuid()};
        var id=source.Id;var calls=0;var client=new Mock<ICosmosDbClient>();
        Task<string> Reply(object value){calls++;var json=JObject.Parse(OasisJson.Serialize(value));json["id"]=id.ToString();source.Version=999;return Task.FromResult(json.ToString());}
        client.Setup(store=>store.CreateDocumentAsync(It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>())).Returns((object value,PartitionKey? _,CancellationToken __)=>Reply(value));
        client.Setup(store=>store.UpsertDocumentAsync(It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>())).Returns((object value,PartitionKey? _,CancellationToken __)=>Reply(value));
        client.Setup(store=>store.ReplaceDocumentAsync(It.IsAny<string>(),It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>())).Returns((string _,object value,PartitionKey? __,CancellationToken ___)=>Reply(value));
        var factory=new Mock<ICosmosDbClientFactory>();factory.Setup(store=>store.GetClient(It.IsAny<string>())).Returns(client.Object);
        async Task Check<T>(CosmosDbRepository<T> repository,T value) where T:class,IHolonBase{
            var failure=await Assert.ThrowsExceptionAsync<CosmosMutationReconciliationException>(async()=>{
                if(operation==0)await repository.AddAsync(value);else if(operation==1)await repository.UpsertAsync(value);else await repository.UpdateAsync(value);
            });
            Assert.IsTrue(failure.Acknowledged);Assert.AreEqual(id.ToString(),failure.DocumentId);Assert.AreEqual(999,source.Version);Assert.AreEqual(1,calls);
        }
        if(kind==0)await Check(new AvatarRepository(factory.Object),(IAvatar)source);
        else if(kind==1)await Check(new AvatarDetailRepository(factory.Object),(IAvatarDetail)source);
        else await Check(new HolonRepository(factory.Object),(IHolon)source);
    }
}
