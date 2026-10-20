using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosIdentityTests
{
    [DataTestMethod]
    [DataRow("id","malformed")] [DataRow("id","typed")] [DataRow("id","type-name")]
    [DataRow("field","malformed")] [DataRow("field","typed")] [DataRow("field","type-name")]
    [DataRow("list","malformed")] [DataRow("list","typed")] [DataRow("list","type-name")]
    public async Task StoredJsonFailuresAreTerminalIntegrityErrors(string reader,string corruption)
    {
        var id=Guid.NewGuid();
        var extra=corruption=="typed" ? ",\"Version\":\"not-an-integer\"":"";
        var typeName=corruption=="type-name" ? "\"$type\":\"System.IO.FileInfo, System.Private.CoreLib\",":"";
        var json=corruption=="malformed" ? "not-json":$"{{{typeName}\"Id\":\"{id}\",\"id\":\"{id}\"{extra}}}";
        var client=new Mock<ICosmosDbClient>();
        client.Setup(c=>c.ReadDocumentAsync(It.IsAny<string>(),It.IsAny<PartitionKey?>(),It.IsAny<CancellationToken>())).ReturnsAsync(json);
        client.Setup(c=>c.ReadDocumentByField(It.IsAny<string>(),It.IsAny<string>(),0)).Returns(json);
        client.Setup(c=>c.ReadAllDocuments()).Returns(new List<string> {json});
        var factory=new Mock<ICosmosDbClientFactory>();factory.Setup(f=>f.GetClient(It.IsAny<string>())).Returns(client.Object);
        async Task Probe<T>(CosmosDbRepository<T> repository) where T:class,NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase
        {
            var error=await Assert.ThrowsExceptionAsync<InvalidDataException>(async()=> {
                if(reader=="id") await repository.GetByIdAsync(id.ToString());
                else if(reader=="field") repository.GetByField("Username","owned");
                else repository.GetList();
            });
            Assert.IsInstanceOfType(error.InnerException,typeof(Newtonsoft.Json.JsonException));
        }
        await Probe(new AvatarRepository(factory.Object));
        await Probe(new AvatarDetailRepository(factory.Object));
        await Probe(new HolonRepository(factory.Object));
    }

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
