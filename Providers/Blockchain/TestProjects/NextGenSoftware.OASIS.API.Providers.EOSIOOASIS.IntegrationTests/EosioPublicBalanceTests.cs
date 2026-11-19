using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.CurrencyBalance;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.IntegrationTests;

[TestClass]
public class EosioPublicBalanceTests
{
    [TestMethod]
    public async Task ShippingClientDecodesNonemptyVaultaBalance()
    {
        if (Environment.GetEnvironmentVariable("OASIS_ANTELOPE_PUBLIC_BALANCE_READ") != "1")
            Assert.Inconclusive("Explicitly enable the public Vaulta balance read.");

        using var client = new EosClient(new Uri("https://vaulta.greymass.com/"));
        var node = await client.GetNodeInfo();
        Assert.AreEqual("aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906", node.ChainId);

        var balances = await client.GetCurrencyBalance(new GetCurrencyBalanceRequestDto
        {
            Account = "eosio.saving",
            Code = "eosio.token",
            Symbol = "EOS"
        });

        Assert.AreEqual(1, balances.Length);
        var parts = balances[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(2, parts.Length);
        Assert.AreEqual("EOS", parts[1]);
        Assert.IsTrue(decimal.TryParse(parts[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
            out var balance) && balance > 0m);
        Console.WriteLine($"Verified EOS currency balance on Vaulta at head block {node.HeadBlockNum}.");
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task WalletFacadeReadsVaultaBalanceWithoutSigning(bool synchronous)
    {
        if (Environment.GetEnvironmentVariable("OASIS_ANTELOPE_PUBLIC_BALANCE_READ") != "1")
            Assert.Inconclusive("Explicitly enable the public Vaulta balance read.");

        using var provider = new EOSIOOASIS("https://vaulta.greymass.com/", "",
            "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906", "");
        var request = new GetWeb3WalletBalanceRequest { WalletAddress = "eosio.saving" };
        var priorCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var result = synchronous ? provider.GetBalance(request) : await provider.GetBalanceAsync(request);
            Assert.IsFalse(result.IsError, result.Exception?.Message);
            Assert.IsTrue(result.Result > 0d);
            Assert.IsFalse(provider.IsProviderActivated);
        }
        finally
        {
            CultureInfo.CurrentCulture = priorCulture;
        }
    }
}
