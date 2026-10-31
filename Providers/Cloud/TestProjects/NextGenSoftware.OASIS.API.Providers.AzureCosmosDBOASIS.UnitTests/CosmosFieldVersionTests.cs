using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosFieldVersionTests
{
    [TestMethod]
    public void FieldVersionQueryUsesCanonicalCaseAndBoundParameter()
    {
        var iterator = new Mock<FeedIterator>(); iterator.SetupGet(i=>i.HasMoreResults).Returns(false);
        var container = new Mock<Container>(); QueryDefinition? selected=null;
        container.Setup(c=>c.GetItemQueryStreamIterator(It.IsAny<QueryDefinition>(),It.IsAny<string>(),It.IsAny<QueryRequestOptions>()))
            .Callback<QueryDefinition,string,QueryRequestOptions>((query,_,__)=>selected=query).Returns(iterator.Object);
        Assert.IsNull(new CosmosDbClient(container.Object).ReadDocumentByField("Username","owned",7));
        Assert.IsNotNull(selected); StringAssert.Contains(selected.QueryText,"c.Version = @version");
        Assert.IsTrue(selected.GetQueryParameters().Any(p=>p.Name=="@version" && Convert.ToInt32(p.Value)==7));
    }

    [TestMethod]
    public void NegativeFieldVersionFailsBeforeSdk()
    {
        var container = new Mock<Container>(MockBehavior.Strict);
        Assert.ThrowsException<ArgumentOutOfRangeException>(()=>new CosmosDbClient(container.Object).ReadDocumentByField("Username","owned",-1));
        container.VerifyNoOtherCalls();
    }
}
