using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosIdentityTests
{
    [DataTestMethod]
    [DataRow("mismatch")] [DataRow("other-record")] [DataRow("empty")]
    public async Task GuidReaderRejectsMismatchedStoredOrRequestedIdentity(string mode)
    {
        var requested=Guid.NewGuid();var logical=mode=="other-record" ? Guid.NewGuid():mode=="empty" ? Guid.Empty:requested;
        var outer=mode=="mismatch" ? Guid.NewGuid():logical;
        var client=new Mock<ICosmosDbClient>();
        client.Setup(c=>c.ReadDocumentAsync(It.IsAny<string>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync($"{{\"Id\":\"{logical}\",\"id\":\"{outer}\"}}");
        var factory=new Mock<ICosmosDbClientFactory>();factory.Setup(f=>f.GetClient("avatarItems")).Returns(client.Object);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>new AvatarRepository(factory.Object).GetByIdAsync(requested.ToString()));
    }

    [TestMethod]
    public async Task GuidReaderNormalizesRequestAndValidatesBeforeSdk()
    {
        var id=Guid.NewGuid();var client=new Mock<ICosmosDbClient>();
        client.Setup(c=>c.ReadDocumentAsync(id.ToString(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync($"{{\"Id\":\"{id}\",\"id\":\"{id}\",\"FirstName\":\"owned\"}}");
        var factory=new Mock<ICosmosDbClientFactory>();factory.Setup(f=>f.GetClient("avatarItems")).Returns(client.Object);
        var repository=new AvatarRepository(factory.Object);
        Assert.AreEqual("owned",(await repository.GetByIdAsync(id.ToString().ToUpperInvariant())).FirstName);
        factory.Invocations.Clear();client.Invocations.Clear();
        await Assert.ThrowsExceptionAsync<ArgumentException>(()=>repository.GetByIdAsync("bad-key"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(()=>repository.GetByIdAsync(Guid.Empty.ToString()));
        factory.VerifyNoOtherCalls();
    }
}
