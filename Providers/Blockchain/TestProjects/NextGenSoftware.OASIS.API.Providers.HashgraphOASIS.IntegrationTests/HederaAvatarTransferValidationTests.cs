using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaAvatarTransferValidationTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NamedRoutesRejectInvalidAmountIdentityAndTokenBeforeWalletLookup(bool byEmail)
    {
        var provider = new HashgraphOASIS();
        async Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses.ITransactionResponse>> Send(
            string from, string to, decimal amount, string token)
            => byEmail ? await provider.SendTransactionByEmailAsync(from, to, amount, token)
                : await provider.SendTransactionByUsernameAsync(from, to, amount, token);
        var amount = await Send("sender", "recipient", 0m, "HBAR");
        Assert.IsInstanceOfType(amount.Exception, typeof(ArgumentOutOfRangeException));
        foreach (var pair in new[] { ("", "recipient"), ("sender", ""), ("same", "SAME") })
        {
            var result = await Send(pair.Item1, pair.Item2, 1m, "HBAR");
            Assert.IsTrue(result.IsError);
            Assert.IsInstanceOfType(result.Exception, typeof(ArgumentException));
        }
        var invalidToken = await Send("sender", "recipient", 1m, "not-token");
        Assert.IsInstanceOfType(invalidToken.Exception, typeof(ArgumentException));
        var native = byEmail ? await provider.SendTransactionByEmailAsync("same", "SAME", 1m)
            : await provider.SendTransactionByUsernameAsync("same", "SAME", 1m);
        Assert.IsInstanceOfType(native.Exception, typeof(ArgumentException));
    }

    [TestMethod]
    public async Task InvalidTokenAndDefaultWalletAmountsFailBeforeWalletLookup()
    {
        var provider = new HashgraphOASIS();
        foreach (var token in new[] { "", "ETH", "not-token" })
        {
            var result = await provider.SendTransactionByIdAsync(Guid.NewGuid(), Guid.NewGuid(), 1m, token);
            Assert.IsTrue(result.IsError);
            Assert.IsInstanceOfType(result.Exception, typeof(ArgumentException));
        }
        var invalidAmount = await provider.SendTransactionByDefaultWalletAsync(Guid.NewGuid(), Guid.NewGuid(), 0m);
        Assert.IsTrue(invalidAmount.IsError);
        Assert.IsInstanceOfType(invalidAmount.Exception, typeof(ArgumentOutOfRangeException));
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public async Task NonpositiveAmountFailsBeforeWalletLookup(int amount)
    {
        var result = await new HashgraphOASIS().SendTransactionByIdAsync(Guid.NewGuid(), Guid.NewGuid(), amount);
        Assert.IsTrue(result.IsError);
        Assert.IsInstanceOfType(result.Exception, typeof(ArgumentOutOfRangeException));
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task MissingOrSameAvatarIdentityFailsBeforeWalletLookup()
    {
        var provider = new HashgraphOASIS();
        var id = Guid.NewGuid();
        foreach (var pair in new[] { (Guid.Empty, id), (id, Guid.Empty), (id, id) })
        {
            var result = await provider.SendTransactionByIdAsync(pair.Item1, pair.Item2, 1m);
            Assert.IsTrue(result.IsError);
            Assert.IsInstanceOfType(result.Exception, typeof(ArgumentException));
            Assert.IsNull(result.Result);
        }
    }
}
