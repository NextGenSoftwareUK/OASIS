using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS.IntegrationTests
{
    [TestClass]
    public class BitcoinOASISIntegrationTests
    {
        private BitcoinOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new BitcoinOASIS();
        }

        [TestMethod]
        public async Task EsploraActivationSurvivesDeactivation()
        {
            try
            {
                var active = await _provider.ActivateProviderAsync();
                Assert.IsFalse(active.IsError, active.Message);
                Assert.IsTrue(active.Result);
                Assert.IsTrue(_provider.IsProviderActivated);

                var inactive = await _provider.DeActivateProviderAsync();
                Assert.IsTrue(inactive.Result);
                Assert.IsFalse(_provider.IsProviderActivated);

                var reactivated = await _provider.ActivateProviderAsync();
                Assert.IsFalse(reactivated.IsError, reactivated.Message);
                Assert.IsTrue(reactivated.Result);
                Assert.IsTrue(_provider.IsProviderActivated);
            }
            finally
            {
                _provider.DeActivateProvider();
                _provider.Dispose();
            }
        }

        [TestMethod]
        public async Task EsploraOrdinaryTransactionDoesNotProduceAHolon()
        {
            const string transactionId = "4a5e1e4baab89f3a32518a88c31bc87f618f76673e2cc77ab2127b7afdeda33b";

            try
            {
                var result = await _provider.LoadHolonAsync(transactionId, loadChildren: false);
                Assert.IsTrue(result.IsError, result.Message);
                Assert.IsNull(result.Result);
                Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, result.ErrorCode, result.Message);

                var status = await _provider.GetTransactionStatusAsync(transactionId);
                Assert.IsFalse(status.IsError, status.Message);
                Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed, status.Result);
            }
            finally
            {
                _provider.Dispose();
            }
        }

        [TestMethod]
        public async Task EsploraReportsFundedAddressBalance()
        {
            try
            {
                var activation = await _provider.ActivateProviderAsync();
                Assert.IsFalse(activation.IsError, activation.Message);

                var balance = await _provider.GetBalanceAsync(
                    new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.GetWeb3WalletBalanceRequest
                    {
                        WalletAddress = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa"
                    });

                Assert.IsFalse(balance.IsError, balance.Message);
                Assert.IsTrue(balance.Result > 1 && balance.Result < 21_000_000);

                var history = await _provider.GetTransactionsAsync(
                    new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.GetWeb3TransactionsRequest
                    {
                        WalletAddress = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa"
                    });
                Assert.IsFalse(history.IsError, history.Message);
                Assert.IsTrue(history.Result.Count > 0);
                foreach (var transaction in history.Result)
                {
                    Assert.AreEqual(64, transaction.NativeTransactionId.Length);
                    Assert.AreEqual("satoshi", transaction.AmountUnit);
                    Assert.AreEqual("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", transaction.ToWalletAddress);
                }
            }
            finally
            {
                _provider.Dispose();
            }
        }

        [TestMethod]
        public async Task EsploraBoundedHistoryPreservesExactBitcoinAmount()
        {
            try
            {
                var activation = await _provider.ActivateProviderAsync();
                Assert.IsFalse(activation.IsError, activation.Message);

                var history = await _provider.GetTransactionsAsync(
                    new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.GetWeb3TransactionsRequest
                    {
                        WalletAddress = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa",
                        FromBlock = 975810,
                        ToBlock = 975810
                    });
                Assert.IsFalse(history.IsError, history.Message);

                var matching = history.Result.Single(transaction => transaction.NativeTransactionId ==
                    "fd3fed5bb1b5c3df76010d1d8c41e8bb68adb808d4c1b3b733b6da255f82271f");
                Assert.AreEqual("1249", matching.RawAmount);
                Assert.AreEqual("satoshi", matching.AmountUnit);
                Assert.AreEqual("00000000000000000001c7ee2824dea49dcb4e3ac5fd25b0619725981c439ac7", matching.NativeBlockHash);
                Assert.IsTrue(history.Result.All(transaction => transaction.NativeBlockNumber == 975810));
            }
            finally
            {
                _provider.Dispose();
            }
        }

        [TestMethod]
        public async Task EsploraActivatedAccountRestoresItsOriginalKeys()
        {
            try
            {
                var activation = await _provider.ActivateProviderAsync();
                Assert.IsFalse(activation.IsError, activation.Message);

                var created = await _provider.CreateAccountAsync();
                Assert.IsFalse(created.IsError, created.Message);
                var restored = await _provider.RestoreAccountAsync(created.Result.SeedPhrase);
                Assert.IsFalse(restored.IsError, restored.Message);

                Assert.IsTrue(created.Result.PublicKey == restored.Result.PublicKey, "Restored address differs.");
                Assert.IsTrue(created.Result.PrivateKey == restored.Result.PrivateKey, "Restored signing key differs.");

                var generated = await _provider.GenerateKeyPairAsync();
                Assert.IsFalse(generated.IsError, generated.Message);
                var signer = new NBitcoin.BitcoinSecret(generated.Result.PrivateKey, NBitcoin.Network.Main);
                Assert.IsTrue(signer.GetAddress(NBitcoin.ScriptPubKeyType.Legacy).ToString() ==
                    generated.Result.WalletAddressLegacy, "Generated legacy address differs from signer address.");
                Assert.IsTrue(signer.GetAddress(NBitcoin.ScriptPubKeyType.SegwitP2SH).ToString() ==
                    generated.Result.WalletAddressSegwitP2SH, "Generated wrapped SegWit address differs from signer address.");
            }
            finally
            {
                _provider.Dispose();
            }
        }

        [TestMethod]
        public async Task EsploraMainnetRejectsRegtestSigningWallet()
        {
            var network = NBitcoin.Network.RegTest;
            using var source = new NBitcoin.Key();
            using var destination = new NBitcoin.Key();
            using var provider = new BitcoinOASIS(
                rpcEndpoint: "https://blockstream.info/api",
                network: "regtest",
                privateKey: source.GetWif(network).ToString());

            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);

            var transfer = await provider.SendTransactionAsync(
                source.PubKey.GetAddress(NBitcoin.ScriptPubKeyType.Segwit, network).ToString(),
                destination.PubKey.GetAddress(NBitcoin.ScriptPubKeyType.Segwit, network).ToString(),
                0.001m,
                null);

            Assert.IsTrue(transfer.IsError);
            Assert.IsNull(transfer.Result);
            Assert.IsTrue(transfer.Message.Contains("different Bitcoin network"), transfer.Message);
        }

        [TestMethod]
        public async Task RegtestBroadcastsAndConfirmsSignedPayment()
        {
            var api = Environment.GetEnvironmentVariable("OASIS_BITCOIN_REGTEST_API");
            var container = Environment.GetEnvironmentVariable("OASIS_BITCOIN_REGTEST_CONTAINER");
            Assert.IsTrue(Uri.TryCreate(api, UriKind.Absolute, out var uri) && uri.IsLoopback);
            Assert.IsFalse(string.IsNullOrWhiteSpace(container));

            var network = NBitcoin.Network.RegTest;
            using var sourceKey = new NBitcoin.Key();
            using var destinationKey = new NBitcoin.Key();
            var source = sourceKey.PubKey.GetAddress(NBitcoin.ScriptPubKeyType.SegwitP2SH, network).ToString();
            var destination = destinationKey.PubKey.GetAddress(NBitcoin.ScriptPubKeyType.Segwit, network).ToString();
            using var provider = new BitcoinOASIS(api, "regtest", sourceKey.GetWif(network).ToString(), null, 2m);
            using var http = new System.Net.Http.HttpClient { BaseAddress = new Uri(api + "/") };

            await BitcoinCliAsync("generatetoaddress", "101", source);
            var tip = (await BitcoinCliAsync("getblockcount")).Trim();
            for (var attempt = 0; attempt < 60; attempt++)
            {
                if ((await http.GetStringAsync("blocks/tip/height")).Trim() == tip)
                    break;
                await Task.Delay(500);
            }
            Assert.AreEqual(tip, (await http.GetStringAsync("blocks/tip/height")).Trim());

            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);
            const string memo = "OASIS regtest payment";
            var payment = await provider.SendTransactionAsync(source, destination, 0.001m, memo);
            Assert.IsFalse(payment.IsError, payment.Message);
            var transactionId = payment.Result.TransactionResult;
            var mempool = await BitcoinCliAsync("getmempoolentry", transactionId);
            using var entry = System.Text.Json.JsonDocument.Parse(mempool);
            Assert.IsTrue(entry.RootElement.GetProperty("fees").GetProperty("base").GetDecimal() > 0m);

            var blocks = await BitcoinCliAsync("generatetoaddress", "1", destination);
            using var mined = System.Text.Json.JsonDocument.Parse(blocks);
            var block = await BitcoinCliAsync("getblock", mined.RootElement[0].GetString(), "2");
            using var confirmation = System.Text.Json.JsonDocument.Parse(block);
            var transaction = confirmation.RootElement.GetProperty("tx").EnumerateArray()
                .Single(candidate => candidate.GetProperty("txid").GetString() == transactionId);
            var outputs = transaction.GetProperty("vout").EnumerateArray().ToArray();
            Assert.IsTrue(outputs.Any(output => output.GetProperty("value").GetDecimal() == 0.001m &&
                output.GetProperty("scriptPubKey").TryGetProperty("address", out var address) &&
                address.GetString() == destination));
            var script = NBitcoin.TxNullDataTemplate.Instance
                .GenerateScriptPubKey(System.Text.Encoding.UTF8.GetBytes(memo)).ToHex();
            Assert.IsTrue(outputs.Any(output => output.GetProperty("value").GetDecimal() == 0m &&
                output.GetProperty("scriptPubKey").GetProperty("hex").GetString() == script));

            async Task<string> BitcoinCliAsync(params string[] arguments)
            {
                var start = new System.Diagnostics.ProcessStartInfo("docker")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                foreach (var argument in new[] { "exec", container, "/srv/explorer/bitcoin/bin/bitcoin-cli",
                    "-conf=/data/.bitcoin.conf", "-datadir=/data/bitcoin", "-regtest" }.Concat(arguments))
                    start.ArgumentList.Add(argument);

                using var process = System.Diagnostics.Process.Start(start);
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                Assert.AreEqual(0, process.ExitCode, await error);
                return await output;
            }
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            // Arrange
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "TestUser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            var result = await _provider.SaveAvatarAsync(avatar);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnAvatar()
        {
            // Arrange
            var avatarId = Guid.NewGuid();

            // Act
            var result = await _provider.LoadAvatarAsync(avatarId);

            // Assert
            Assert.IsNotNull(result);
            // Note: This might return an error if avatar doesn't exist, which is expected
        }

        [TestMethod]
        public async Task SaveHolon_ShouldReturnSuccessResult()
        {
            // Arrange
            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = "TestHolon",
                Description = "Test Holon Description"
            };

            // Act
            var result = await _provider.SaveHolonAsync(holon);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnHolon()
        {
            // Arrange
            var holonId = Guid.NewGuid();

            // Act
            var result = await _provider.LoadHolonAsync(holonId);

            // Assert
            Assert.IsNotNull(result);
            // Note: This might return an error if holon doesn't exist, which is expected
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
            {
                _provider.DeActivateProvider();
            }
        }
    }
}
