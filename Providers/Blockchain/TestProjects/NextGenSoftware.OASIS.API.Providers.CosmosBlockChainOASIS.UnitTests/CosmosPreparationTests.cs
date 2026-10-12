using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class CosmosPreparationTests
{
    [TestMethod]
    public void FactoryCannotInventAnExecution()
    {
        using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1");
        Assert.ThrowsException<ArgumentNullException>(()=>provider.CreateNativePreparationServer(null!,"attempt","chain","sender"));
        Assert.IsFalse(provider.IsProviderActivated);
    }
    [TestMethod]
    public async Task InvalidSignedEvidenceFailsWithoutActivatingProvider()
    {
        using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1");
        using var invalid=JsonDocument.Parse("{\"transactionHash\":\"invalid\",\"sender\":\"invalid\",\"signedTransactionBase64\":\"AQID\"}");
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>provider.VerifyOriginalSignedPreparationAsync(invalid.RootElement));
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
