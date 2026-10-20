using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.UnitTests;

[TestClass]
public class RadixKeyDerivationTests
{
    private static byte[] Hex(string hex) => Convert.FromHexString(hex);
    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    // SLIP-0010 test vector 1 for ed25519 (seed 000102030405060708090a0b0c0d0e0f).
    [TestMethod]
    public void Slip10MatchesThePublishedEd25519TestVector()
    {
        var seed = Hex("000102030405060708090a0b0c0d0e0f");
        var master = Slip10Ed25519.Master(seed);
        Assert.AreEqual("2b4be7f19ee27bbf30c667b642d5f4aa69fd169872f8fc3059c08ebae2eb19e7", Hex(master.Key));
        Assert.AreEqual("90046a93de5380a72b5e45010748567d5ea02bbf6522f979e05c0d8d8ca9fffb", Hex(master.ChainCode));
        Assert.AreEqual("68e0fe46dfb67e368c75379acec591dad19df3cde26e63b93a8e704f1dade7a3", Hex(Slip10Ed25519.DerivePrivateKey(seed, 0)));
        Assert.AreEqual("b1d0bad404bf35da785a64ca1ac54b2617211d2777696fbffaf208f746ae84f2", Hex(Slip10Ed25519.DerivePrivateKey(seed, 0, 1)));
    }

    [TestMethod]
    public void Slip10RejectsHardenedInputAndBadSeeds()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Slip10Ed25519.DerivePrivateKey(new byte[16], 0x80000000));
        Assert.ThrowsException<ArgumentException>(() => Slip10Ed25519.Master(new byte[8]));
    }

    [DataTestMethod]
    [DataRow((byte)1, "account_rdx1")]
    [DataRow((byte)2, "account_tdx_2_1")]
    public void MnemonicDerivesARealAccountAddressPerNetwork(byte networkId, string prefix)
    {
        var mnemonic = new Mnemonic(Wordlist.English, WordCount.TwentyFour);
        using var key = RadixBridgeHelper.GetPrivateKey(mnemonic, networkId);
        using var again = RadixBridgeHelper.GetPrivateKey(new Mnemonic(mnemonic.ToString()), networkId);
        using var next = RadixBridgeHelper.GetPrivateKey(mnemonic, networkId, 1);
        Assert.AreEqual(key.RawHex(), again.RawHex(), "same phrase, same key");
        Assert.AreNotEqual(key.RawHex(), next.RawHex(), "each index is a different account");
        var address = RadixBridgeHelper.AccountAddress(key, networkId);
        StringAssert.StartsWith(address, prefix);
        using var parsed = new RadixEngineToolkit.Address(address);
        Assert.AreEqual(networkId, parsed.NetworkId());
    }

    [TestMethod]
    public void NetworksDeriveDifferentKeysFromOnePhrase()
    {
        var mnemonic = new Mnemonic(Wordlist.English, WordCount.TwentyFour);
        using var main = RadixBridgeHelper.GetPrivateKey(mnemonic, 1);
        using var stoke = RadixBridgeHelper.GetPrivateKey(mnemonic, 2);
        Assert.AreNotEqual(main.RawHex(), stoke.RawHex());
    }

    [TestMethod]
    public async Task CreatedAccountsRestoreAndSignWithTheSameKey()
    {
        var service = new RadixService(new RadixOASISConfig { NetworkId = 2, HostUri = "https://stokenet.radixdlt.com" }, new HttpClient());
        var created = await service.CreateAccountAsync();
        Assert.IsFalse(created.IsError, created.Message);
        Assert.AreEqual(24, created.Result.SeedPhrase.Split(' ').Length);
        var restored = await service.RestoreAccountAsync(created.Result.SeedPhrase);
        Assert.AreEqual((created.Result.PublicKey, created.Result.PrivateKey), restored.Result);
        using var signer = RadixBridgeHelper.GetPrivateKeyFromHex(created.Result.PrivateKey);
        Assert.AreEqual(created.Result.PublicKey, Convert.ToHexString(signer.PublicKeyBytes()));
    }
}
