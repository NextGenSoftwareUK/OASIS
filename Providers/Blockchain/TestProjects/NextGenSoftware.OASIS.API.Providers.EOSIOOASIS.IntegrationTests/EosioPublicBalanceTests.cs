using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
}
