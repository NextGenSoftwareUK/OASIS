using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosAcknowledgedWriteTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(false,true)] [DataRow(true,false)] [DataRow(true,true)]
    public async Task AcknowledgedInvalidResponseRetainsSubmittedIdentity(bool create,bool malformed)
    {
        var id=Guid.NewGuid();var other=Guid.NewGuid();
        var json=malformed ? "not-json":$"{{\"Id\":\"{other}\",\"id\":\"{other}\"}}";
        var client=new Mock<ICosmosDbClient>();
        client.Setup(c=>c.CreateDocumentAsync(It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>())).ReturnsAsync(json);
        client.Setup(c=>c.UpsertDocumentAsync(It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>())).ReturnsAsync(json);
        var factory=new Mock<ICosmosDbClientFactory>();factory.Setup(f=>f.GetClient("avatarItems")).Returns(client.Object);
        var repository=new AvatarRepository(factory.Object);var entity=new Avatar {Id=id,ProviderUniqueStorageKey=null!};
        var failure=await Assert.ThrowsExceptionAsync<CosmosMutationReconciliationException>(async()=>
        {
            if(create) await repository.AddAsync(entity);else await repository.UpsertAsync(entity);
        });
        Assert.AreEqual(id.ToString(),failure.DocumentId);Assert.IsTrue(failure.Acknowledged);
        Assert.AreEqual(string.Empty,failure.OriginalTransactionId);Assert.IsNotNull(failure.InnerException);
        Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(failure));
    }

    [TestMethod]
    public async Task CallerIdentityChangeAfterAcknowledgementCannotReturnDifferentRecord()
    {
        var entity=new Avatar {Id=Guid.NewGuid()};var submitted=entity.Id;
        var client=new Mock<ICosmosDbClient>();
        client.Setup(c=>c.UpsertDocumentAsync(It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>()))
            .Returns(()=>{entity.Id=Guid.NewGuid();return Task.FromResult<string>(null!);});
        var factory=new Mock<ICosmosDbClientFactory>();factory.Setup(f=>f.GetClient("avatarItems")).Returns(client.Object);
        var failure=await Assert.ThrowsExceptionAsync<CosmosMutationReconciliationException>(()=>new AvatarRepository(factory.Object).UpsertAsync(entity));
        Assert.AreEqual(submitted.ToString(),failure.DocumentId);Assert.IsTrue(failure.Acknowledged);
        Assert.IsInstanceOfType(failure.InnerException,typeof(InvalidDataException));
    }
}
