using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.StarknetOASIS.UnitTests
{
    [TestClass]
    public class StarknetAccountTests
    {
        private const string Address = "0x46791670e16f523da969e535a1ac1d373f7cf553d39772336acf75abc08aee8";

        private sealed class FakeSdk : IStarknetSdkBridge
        {
            public readonly List<JsonElement> Requests = new();
            public Func<JsonElement, object> Reply = _ => new { };
            public Exception Failure;

            public Task<JsonElement> InvokeAsync(object request, CancellationToken cancellationToken = default)
            {
                Requests.Add(JsonSerializer.SerializeToElement(request));
                if (Failure != null) throw Failure;
                return Task.FromResult(JsonSerializer.SerializeToElement(Reply(Requests[^1])));
            }
        }

        private static (StarknetOASIS Provider, FakeSdk Sdk) Provider(string network = "sepolia")
        {
            var sdk = new FakeSdk();
            var provider = new StarknetOASIS(network, "https://rpc.example", accountClassHash: "0xc1a55") { Sdk = sdk };
            return (provider, sdk);
        }

        private static string Op(JsonElement request) => request.GetProperty("operation").GetString();

        [DataTestMethod]
        [DataRow("sepolia", StarknetOASIS.SepoliaChainId)]
        [DataRow("SN_SEPOLIA", StarknetOASIS.SepoliaChainId)]
        [DataRow("mainnet", StarknetOASIS.MainnetChainId)]
        public void NetworkNamesMapToPinnedChainIds(string network, string chainId) => Assert.AreEqual(chainId, StarknetOASIS.ChainIdFor(network));

        [DataTestMethod]
        [DataRow("alpha-goerli")]
        [DataRow("goerli")]
        [DataRow("")]
        public void RetiredOrUnknownNetworksAreRejected(string network) =>
            Assert.ThrowsException<ArgumentException>(() => new StarknetOASIS(network, "https://rpc.example"));

        [TestMethod]
        public async Task GeneratedKeysComeFromTheSdkWithTheConfiguredAccountClass()
        {
            var (provider, sdk) = Provider();
            sdk.Reply = _ => new { mnemonic = "m", privateKey = "0x5", publicKey = "0x6", address = Address };
            var keys = await provider.GenerateKeyPairAsync();
            Assert.IsFalse(keys.IsError, keys.Message);
            Assert.AreEqual("0x5", keys.Result.PrivateKey);
            Assert.AreEqual("0x6", keys.Result.PublicKey);
            Assert.AreEqual(Address, keys.Result.WalletAddressLegacy);
            var request = sdk.Requests.Single();
            Assert.AreEqual("createAccount", Op(request));
            Assert.AreEqual("0xc1a55", request.GetProperty("classHash").GetString());
            Assert.IsFalse(request.TryGetProperty("endpoint", out _), "key generation is offline");
        }

        [TestMethod]
        public async Task CreateAndRestoreReturnRealMaterialAndRejectEmptyPhrases()
        {
            var (provider, sdk) = Provider();
            sdk.Reply = r => Op(r) == "createAccount"
                ? new { mnemonic = "twelve words", privateKey = "0x5", publicKey = "0x6", address = Address }
                : new { privateKey = "0x5", publicKey = "0x6", address = Address };
            var created = await provider.CreateAccountAsync();
            Assert.AreEqual(("0x6", "0x5", "twelve words"), created.Result);
            var restored = await provider.RestoreAccountAsync("twelve words");
            Assert.AreEqual(("0x6", "0x5"), restored.Result);
            Assert.AreEqual(0, sdk.Requests[1].GetProperty("index").GetInt32());
            Assert.IsTrue((await provider.RestoreAccountAsync("  ")).IsError);
            Assert.AreEqual(2, sdk.Requests.Count);
        }

        [TestMethod]
        public async Task NetworkOperationsCarryThePinnedChainAndEndpoint()
        {
            var (provider, sdk) = Provider("mainnet");
            sdk.Reply = _ => new { address = Address, deployed = true, classHash = "0xc1a55", nonce = "0x1" };
            var status = await provider.GetAccountStatusAsync(Address);
            Assert.AreEqual(new StarknetAccountStatus(Address, true, "0xc1a55", "0x1"), status.Result);
            var request = sdk.Requests.Single();
            Assert.AreEqual(StarknetOASIS.MainnetChainId, request.GetProperty("chainId").GetString());
            Assert.AreEqual("https://rpc.example", request.GetProperty("endpoint").GetString());
        }

        [TestMethod]
        public async Task DeploymentReportsTheOnChainOutcome()
        {
            var (provider, sdk) = Provider();
            sdk.Reply = _ => new { address = Address, transactionHash = "0xabc", executionStatus = "SUCCEEDED", finalityStatus = "ACCEPTED_ON_L2" };
            var deployed = await provider.DeployAccountAsync("0x5");
            Assert.IsFalse(deployed.IsError, deployed.Message);
            Assert.IsTrue(deployed.IsSaved);
            Assert.AreEqual(new StarknetAccountDeployment(Address, "0xabc", "SUCCEEDED", "ACCEPTED_ON_L2"), deployed.Result);
            var request = sdk.Requests.Single();
            Assert.AreEqual("deployAccount", Op(request));
            Assert.AreEqual("0x5", request.GetProperty("privateKey").GetString());
            Assert.AreEqual("0xc1a55", request.GetProperty("classHash").GetString());
        }

        [TestMethod]
        public async Task SubmittedButUnconfirmedDeploymentRequiresReconciliation()
        {
            var (provider, sdk) = Provider();
            sdk.Failure = new StarknetTransactionReconciliationException("0xabc", new InvalidOperationException("timeout"));
            var deployed = await provider.DeployAccountAsync("0x5");
            Assert.IsTrue(deployed.IsError);
            Assert.IsNull(deployed.Result);
            var reconcile = (INativeMutationReconciliationRequired)deployed.Exception;
            Assert.AreEqual("0xabc", reconcile.OriginalTransactionId);
        }

        [TestMethod]
        public async Task InterruptedDeploymentIsNeverReportedAsAPlainFailure()
        {
            var (provider, sdk) = Provider();
            sdk.Failure = new OperationCanceledException();
            var deployed = await provider.DeployAccountAsync("0x5");
            Assert.IsTrue(deployed.IsError);
            Assert.IsInstanceOfType(deployed.Exception, typeof(StarknetTransactionReconciliationException));
            Assert.AreEqual(string.Empty, ((INativeMutationReconciliationRequired)deployed.Exception).OriginalTransactionId, "no hash is invented");
        }

        [TestMethod]
        public async Task IncompleteSdkRepliesAreErrors()
        {
            var (provider, sdk) = Provider();
            sdk.Reply = _ => new { address = Address, transactionHash = "0xabc" };
            var deployed = await provider.DeployAccountAsync("0x5");
            Assert.IsTrue(deployed.IsError);
            Assert.IsInstanceOfType(deployed.Exception, typeof(InvalidDataException));
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(2)]
        public async Task AvatarDeploymentNeedsExactlyOneStarknetKey(int count)
        {
            var (provider, sdk) = Provider();
            provider.ResolveAccountKeysCore = _ => new OASISResult<List<string>>(Enumerable.Range(1, count).Select(i => "0x" + i).ToList());
            var deployed = await provider.DeployAvatarAccountAsync(Guid.NewGuid());
            Assert.IsTrue(deployed.IsError);
            Assert.AreEqual(0, sdk.Requests.Count);
        }

        [TestMethod]
        public async Task AvatarDeploymentUsesTheAvatarsKeyAndPassesKeyStoreErrorsThrough()
        {
            var (provider, sdk) = Provider();
            sdk.Reply = _ => new { address = Address, transactionHash = "0xabc", executionStatus = "SUCCEEDED", finalityStatus = "ACCEPTED_ON_L2" };
            var avatar = Guid.NewGuid();
            Guid? asked = null;
            provider.ResolveAccountKeysCore = id => { asked = id; return new OASISResult<List<string>>(new List<string> { "0x9", "0X9" }); };
            Assert.IsFalse((await provider.DeployAvatarAccountAsync(avatar)).IsError);
            Assert.AreEqual(avatar, asked);
            Assert.AreEqual("0x9", sdk.Requests.Single().GetProperty("privateKey").GetString());

            provider.ResolveAccountKeysCore = _ => new OASISResult<List<string>> { IsError = true, Message = "denied", ErrorCode = "KEYS_DENIED" };
            var denied = await provider.DeployAvatarAccountAsync(avatar);
            Assert.AreEqual("KEYS_DENIED", denied.ErrorCode);
            Assert.AreEqual(1, sdk.Requests.Count);
        }

        // ── the real pinned SDK, end to end through Node (offline operations) ─────────────────

        private static StarknetOASIS RealBridgeProvider()
        {
            var bridge = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "NextGenSoftware.OASIS.API.Providers.StarknetOASIS", "SdkBridge", "bridge.mjs"));
            Assert.IsTrue(File.Exists(bridge), bridge);
            Environment.SetEnvironmentVariable("OASIS_STARKNET_SDK_BRIDGE", bridge);
            return new StarknetOASIS("sepolia", "https://starknet-sepolia-rpc.publicnode.com");
        }

        [TestMethod]
        [TestCategory("NodeBridge")]
        public async Task RealSdkCreatesRestorableStarkCurveAccounts()
        {
            var provider = RealBridgeProvider();
            var created = await provider.CreateAccountAsync();
            Assert.IsFalse(created.IsError, created.Message);
            Assert.AreEqual(12, created.Result.SeedPhrase.Split(' ').Length);
            var restored = await provider.RestoreAccountAsync(created.Result.SeedPhrase);
            Assert.AreEqual((created.Result.PublicKey, created.Result.PrivateKey), restored.Result);

            var generator = await new StarknetOASIS("sepolia", "https://rpc.example").GetAccountAddressAsync("0x1");
            Assert.IsFalse(generator.IsError, generator.Message);
            Assert.IsTrue(generator.Result.StartsWith("0x"));
            var keys = await provider.GenerateKeyPairAsync();
            Assert.AreEqual(keys.Result.WalletAddressLegacy, (await provider.GetAccountAddressAsync(keys.Result.PrivateKey)).Result);
        }

        // ── live read-only checks against Starknet (opt-in: OASIS_STARKNET_LIVE=1) ────────────

        [TestMethod]
        [TestCategory("Live")]
        public async Task LiveSepoliaVerifiesAccountsAndEstimatesDeployFees()
        {
            if (Environment.GetEnvironmentVariable("OASIS_STARKNET_LIVE") != "1")
                Assert.Inconclusive("Set OASIS_STARKNET_LIVE=1 to run live Starknet reads.");
            var provider = RealBridgeProvider();
            var keys = await provider.GenerateKeyPairAsync();
            var status = await provider.GetAccountStatusAsync(keys.Result.WalletAddressLegacy);
            Assert.IsFalse(status.IsError, status.Message);
            Assert.IsFalse(status.Result.Deployed);
            var fee = await provider.EstimateAccountDeployFeeAsync(keys.Result.PrivateKey);
            Assert.IsFalse(fee.IsError, fee.Message);
            Assert.AreEqual(keys.Result.WalletAddressLegacy, fee.Result.Address);
            Assert.IsTrue(System.Numerics.BigInteger.Parse("0" + fee.Result.OverallFee[2..], System.Globalization.NumberStyles.HexNumber) > 0);
        }
    }
}
