using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass]
public class SuiAvatarTransferValidationTests
{
    [DataTestMethod]
    [DataRow("", "recipient", 1)]
    [DataRow("sender", "", 1)]
    [DataRow("sender", "SENDER", 1)]
    [DataRow("sender", "recipient", 0)]
    [DataRow("sender", "recipient", -1)]
    public async Task InvalidIdentityOrAmountFailsBeforeWalletLookup(string sender, string recipient, int amount)
    {
        using var provider = new SuiOASIS();
        var results = new[] { await provider.SendTransactionByUsernameAsync(sender, recipient, amount),
            await provider.SendTransactionByEmailAsync(sender, recipient, amount),
            provider.SendTransactionByUsername(sender, recipient, amount), provider.SendTransactionByEmail(sender, recipient, amount) };
        foreach (var result in results) { Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); }
    }

    [TestMethod]
    public async Task UnsupportedTokenAndInexactAmountFailBeforeWalletLookup()
    {
        using var provider = new SuiOASIS();
        var results = new[] { await provider.SendTransactionByUsernameAsync("sender", "recipient", 1, "bad"),
            await provider.SendTransactionByEmailAsync("sender", "recipient", 1, "bad"),
            await provider.SendTransactionByUsernameAsync("sender", "recipient", 0.0000000001m) };
        foreach (var result in results) { Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); }
    }
}
