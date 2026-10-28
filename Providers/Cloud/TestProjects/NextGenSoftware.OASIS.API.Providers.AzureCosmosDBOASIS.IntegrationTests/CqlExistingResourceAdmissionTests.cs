using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
public sealed class CqlExistingResourceAdmissionTests
{
    [TestMethod]
    public async Task ExistingModeOnlyReadsDeclaredDatabaseAndAllContainerSchemas()
    {
        var client = new Mock<CosmosClient>(MockBehavior.Strict);
        var database = new Mock<Database>(MockBehavior.Strict);
        var names = new[] { "holonItems", "avatarItems", "avatarDetailItems" };
        client.Setup(value => value.GetDatabase("owned")).Returns(database.Object);
        database.Setup(value => value.ReadAsync(It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<DatabaseResponse>());
        var containers = new List<Mock<Container>>();
        foreach (var name in names)
        {
            var container = new Mock<Container>(MockBehavior.Strict);
            containers.Add(container);
            database.Setup(value => value.GetContainer(name)).Returns(container.Object);
            var response = new Mock<ContainerResponse>();
            response.SetupGet(value => value.Resource).Returns(new ContainerProperties(name, "/id"));
            container.Setup(value => value.ReadContainerAsync(It.IsAny<ContainerRequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response.Object);
        }

        var factory = new CosmosDbClientFactory(client.Object, "owned", names.ToList(), () => false,
            CosmosResourceInitializationMode.ExistingResourcesOnly);
        var result = await factory.EnsureDbSetupAsync();
        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(result.Result);
        client.VerifyAll();
        database.VerifyAll();
        database.VerifyNoOtherCalls();
        foreach (var container in containers) { container.VerifyAll(); container.VerifyNoOtherCalls(); }
        client.VerifyNoOtherCalls(); // Any create/setup/throughput request is forbidden.
    }

    [DataTestMethod]
    [DataRow("wrong-partition")]
    [DataRow("wrong-identity")]
    [DataRow("missing-container")]
    [DataRow("missing-database")]
    public async Task MissingOrIncompatibleResourcesFailWithoutProvisioning(string failure)
    {
        var client = new Mock<CosmosClient>(MockBehavior.Strict);
        var database = new Mock<Database>(MockBehavior.Strict);
        client.Setup(value => value.GetDatabase("owned")).Returns(database.Object);
        Mock<Container>? observedContainer = null;
        if (failure == "missing-database")
            database.Setup(value => value.ReadAsync(It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException("Missing owned database", HttpStatusCode.NotFound, 0, "fixture", 0));
        else
        {
            database.Setup(value => value.ReadAsync(It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Mock.Of<DatabaseResponse>());
            var container = new Mock<Container>(MockBehavior.Strict);
            database.Setup(value => value.GetContainer("holonItems")).Returns(container.Object);
            observedContainer = container;
            var read = container.Setup(value => value.ReadContainerAsync(It.IsAny<ContainerRequestOptions>(), It.IsAny<CancellationToken>()));
            if (failure == "missing-container")
                read.ThrowsAsync(new CosmosException("Missing owned container", HttpStatusCode.NotFound, 0, "fixture", 0));
            else
            {
                var response = new Mock<ContainerResponse>();
                response.SetupGet(value => value.Resource).Returns(new ContainerProperties(
                    failure == "wrong-identity" ? "foreign" : "holonItems", failure == "wrong-partition" ? "/foreign" : "/id"));
                read.ReturnsAsync(response.Object);
            }
        }
        var factory = new CosmosDbClientFactory(client.Object, "owned", new() { "holonItems" }, () => false,
            CosmosResourceInitializationMode.ExistingResourcesOnly);
        var result = await factory.EnsureDbSetupAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.Result);
        Assert.IsNotNull(result.Exception);
        client.VerifyAll();
        database.VerifyAll();
        database.VerifyNoOtherCalls();
        if (observedContainer != null) { observedContainer.VerifyAll(); observedContainer.VerifyNoOtherCalls(); }
        client.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void UnknownInitializationModeRejectsBeforeClientAccess()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new CosmosDbClientFactory(
            Mock.Of<CosmosClient>(), "owned", new(), () => false, (CosmosResourceInitializationMode)99));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new AzureCosmosDBOASIS(
            new Uri("https://owned.documents.azure.com"), "unused", "owned", new(), (CosmosResourceInitializationMode)99));
    }
}
