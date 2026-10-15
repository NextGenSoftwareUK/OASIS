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
    [DataRow(false)] [DataRow(true)]
    public async Task ReplacementFreezesPayloadAndRetainsOriginalFailure(bool uncertain)
    {
        var entity=new Avatar {Id=Guid.NewGuid(),FirstName="submitted",ProviderUniqueStorageKey=null!};
        var id=entity.Id;object? sent=null;
        Exception cause=uncertain ? new CosmosMutationReconciliationException(id.ToString(),new IOException("lost reply"))
            :new CosmosException("owned404",System.Net.HttpStatusCode.NotFound,1002,"owned",0);
        var client=new Mock<ICosmosDbClient>();
        client.Setup(c=>c.ReplaceDocumentAsync(id.ToString(),It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>()))
            .Returns((string _,object value,PartitionKey? __,CancellationToken ___)=> {sent=value;entity.FirstName="changed";return Task.FromException(cause);});
        var factory=new Mock<ICosmosDbClientFactory>();factory.Setup(f=>f.GetClient("avatarItems")).Returns(client.Object);
        var repository=new AvatarRepository(factory.Object);
        Exception failed=uncertain ? await Assert.ThrowsExceptionAsync<CosmosMutationReconciliationException>(()=>repository.UpdateAsync(entity))
            :await Assert.ThrowsExceptionAsync<CosmosException>(()=>repository.UpdateAsync(entity));
        Assert.AreSame(cause,failed);Assert.IsNull(entity.ProviderUniqueStorageKey);Assert.AreEqual("submitted",((Avatar)sent!).FirstName);
    }

    [TestMethod]
    public async Task EntityDeleteUsesCanonicalIdentityNotStaleProviderKey()
    {
        var id=Guid.NewGuid();var client=new Mock<ICosmosDbClient>();
        client.Setup(c=>c.DeleteDocumentAsync(id.ToString(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var factory=new Mock<ICosmosDbClientFactory>();factory.Setup(f=>f.GetClient("avatarItems")).Returns(client.Object);
        await new AvatarRepository(factory.Object).DeleteAsync(new Avatar {Id=id,ProviderUniqueStorageKey=null!});
        client.Verify(c=>c.DeleteDocumentAsync(id.ToString(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>()),Times.Once);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task FailedWriteDoesNotPublishGeneratedIdentityOrProviderKey(bool create)
    {
        var entity=new Avatar {Id=Guid.Empty,FirstName="submitted",ProviderUniqueStorageKey=null!};
        object? sent=null;var cause=new IOException("owned SDK failure");
        var client=new Mock<ICosmosDbClient>();
        Task<string> Fail(object value) {sent=value;entity.FirstName="changed";return Task.FromException<string>(cause);}
        client.Setup(c=>c.CreateDocumentAsync(It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>())).Returns((object value,PartitionKey? _,CancellationToken __)=>Fail(value));
        client.Setup(c=>c.UpsertDocumentAsync(It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>())).Returns((object value,PartitionKey? _,CancellationToken __)=>Fail(value));
        var factory=new Mock<ICosmosDbClientFactory>();factory.Setup(f=>f.GetClient("avatarItems")).Returns(client.Object);
        var repository=new AvatarRepository(factory.Object);
        var failed=await Assert.ThrowsExceptionAsync<IOException>(async()=> {if(create) await repository.AddAsync(entity);else await repository.UpsertAsync(entity);});
        Assert.AreSame(cause,failed);Assert.AreEqual(Guid.Empty,entity.Id);Assert.IsNull(entity.ProviderUniqueStorageKey);
        Assert.IsNotNull(sent);Assert.AreNotSame(entity,sent);var snapshot=(Avatar)sent;
        Assert.AreNotEqual(Guid.Empty,snapshot.Id);Assert.AreEqual("submitted",snapshot.FirstName);
    }

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
