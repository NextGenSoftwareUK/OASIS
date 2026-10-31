using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public sealed class CosmosRepositoryDeleteTests
{
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task RepositoryDeleteOverloadsShareConditionalNativeOwner(int kind)
    {
        var source=new AvatarDetail{Id=Guid.NewGuid(),Username="owned-original",IsActive=true};
        var document=JObject.Parse(OasisJson.Serialize(source));document["id"]=source.Id.ToString();
        var client=new Mock<ICosmosDbClient>(MockBehavior.Strict);var factory=new Mock<ICosmosDbClientFactory>(MockBehavior.Strict);
        factory.Setup(value=>value.GetClient("avatarDetailItems")).Returns(client.Object);
        client.Setup(value=>value.ReadDocumentAsync(source.Id.ToString(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>())).ReturnsAsync(document.ToString());
        client.Setup(value=>value.DeleteUnchangedDocumentAsync(source.Id.ToString(),It.Is<object>(expected=>((AvatarDetail)expected).Id==source.Id&&((AvatarDetail)expected).Username==source.Username),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var repository=new AvatarDetailRepository(factory.Object);
        if(kind==0)await repository.DeleteAsync(source);else if(kind==1)await repository.DeleteAsync(source.Id);else await repository.DeleteAsync(source.Id.ToString());
        client.Verify(value=>value.DeleteUnchangedDocumentAsync(source.Id.ToString(),It.IsAny<object>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>()),Times.Once());
        client.Verify(value=>value.DeleteDocumentAsync(It.IsAny<string>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>()),Times.Never());
        client.Verify(value=>value.ReadDocumentAsync(It.IsAny<string>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>()),kind==0?Times.Never():Times.Once());
    }
}
