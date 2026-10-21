using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

// Actual retained Node/SDK key operations, offline. Never fund these synthetic identities.
[TestClass][DoNotParallelize]
public class NearMnemonicWalletTests
{
    private string? previousBridge;
    [TestInitialize]
    public void SelectRetainedSourceBridge()
    {
        previousBridge=Environment.GetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE");
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null){
            var bridge=Path.Combine(directory.FullName,"Providers","Blockchain","NextGenSoftware.OASIS.API.Providers.NEAROASIS","SdkBridge","bridge.mjs");
            if(File.Exists(bridge)){Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",bridge);return;}
            directory=directory.Parent;
        }
        Assert.Fail("Existing source SDK bridge is required; do not install or copy another environment.");
    }
    [TestCleanup]
    public void RestoreBridge()=>Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",previousBridge);
    private static NEAROASIS Provider()=>new("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","");

    [TestMethod]
    public async Task GeneratedMnemonicAndExplicitSecretRestoreTheSameKeysWithoutNetwork()
    {
        var provider=Provider();var created=await provider.CreateAccountAsync();
        Assert.IsFalse(created.IsError,created.Message);Assert.AreEqual(12,created.Result.SeedPhrase.Split(' ').Length);
        var restored=await provider.RestoreAccountAsync(created.Result.SeedPhrase);
        Assert.IsFalse(restored.IsError,restored.Message);
        Assert.AreEqual(created.Result.PublicKey,restored.Result.PublicKey);
        Assert.IsTrue(created.Result.PrivateKey==restored.Result.PrivateKey,"Mnemonic must restore the generated private identity.");
        var explicitKey=await provider.RestoreAccountAsync(created.Result.PrivateKey);
        Assert.IsFalse(explicitKey.IsError,explicitKey.Message);Assert.AreEqual(created.Result.PublicKey,explicitKey.Result.PublicKey);
        Assert.IsFalse(provider.IsProviderActivated,"Local key derivation must not activate network storage.");
    }

    [TestMethod]
    public async Task PublicMnemonicFixtureUsesPinnedNearSdkDerivationAndNormalization()
    {
        // Public BIP39 example, not a real wallet. Expected key pinned from existing near-seed-phrase0.2.1.
        var phrase="  ABANDON abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about  ";
        var restored=await Provider().RestoreAccountAsync(phrase);
        Assert.IsFalse(restored.IsError,restored.Message);
        Assert.AreEqual("ed25519:6j4b6zUaty6fD1awqcGCCU9JYGCWYUgdJhQrzfZhqE25",restored.Result.PublicKey);
    }

    [DataTestMethod]
    [DataRow("abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon")]
    [DataRow("not a valid recovery phrase")]
    public async Task InvalidMnemonicCannotSilentlyDeriveAnUnrecoverableIdentity(string phrase)
    {
        var restored=await Provider().RestoreAccountAsync(phrase);
        Assert.IsTrue(restored.IsError);Assert.IsNull(restored.Result.PublicKey);Assert.IsNull(restored.Result.PrivateKey);
        Assert.IsFalse(restored.Message.Contains(phrase,StringComparison.Ordinal),"Mnemonic input must not be echoed into error reports.");
    }
}
