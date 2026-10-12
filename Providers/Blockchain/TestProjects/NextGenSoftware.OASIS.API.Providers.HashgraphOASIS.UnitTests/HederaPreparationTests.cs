using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

[TestClass]
public class HederaPreparationTests
{
    [TestMethod]
    public async Task PreparationCannotInventExecutionOrTrustedPublicKey()
    {
        var provider=new HashgraphOASIS();
        Assert.ThrowsException<ArgumentNullException>(()=>provider.CreateNativePreparationServer(null!,"attempt","testnet","0.0.1001","key"));
        using var evidence=JsonDocument.Parse("{}");
        await Assert.ThrowsExceptionAsync<ArgumentException>(()=>provider.VerifyOriginalSignedPreparationAsync(evidence.RootElement,""));
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
