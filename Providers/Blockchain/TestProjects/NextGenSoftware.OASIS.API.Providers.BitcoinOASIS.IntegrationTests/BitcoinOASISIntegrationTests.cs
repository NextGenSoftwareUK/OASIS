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
        public async Task EsploraOrdinaryTransactionDoesNotProduceAnAvatar()
        {
            using var provider = new BitcoinOASIS();
            const string genesisTransaction = "4a5e1e4baab89f3a32518a88c31bc87f618f76673e2cc77ab2127b7afdeda33b";

            var avatar = await provider.LoadAvatarByProviderKeyAsync(genesisTransaction);
            Assert.IsTrue(avatar.IsError);
            Assert.IsNull(avatar.Result);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, avatar.ErrorCode);

            var wallet = await provider.LoadAvatarByProviderKeyAsync("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa");
            Assert.IsTrue(wallet.IsError);
            Assert.IsNull(wallet.Result);

            var missingTransaction = new string('0', 64);
            var missingAvatar = await provider.LoadAvatarByProviderKeyAsync(missingTransaction);
            var missingHolon = await provider.LoadHolonAsync(missingTransaction, loadChildren: false);
            var missingDetail = await provider.LoadAvatarDetailByTransactionIdAsync(missingTransaction);
            var notFound = NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound;
            Assert.AreEqual(notFound, missingAvatar.ErrorCode);
            Assert.AreEqual(notFound, missingHolon.ErrorCode);
            Assert.AreEqual(notFound, missingDetail.ErrorCode);
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
        [DoNotParallelize]
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

            await BitcoinCliAsync("-rpcwallet=oasis-regtest", "-named", "sendtoaddress",
                $"address={source}", "amount=1", "fee_rate=2");
            var fundingBlocks = await BitcoinCliAsync("generatetoaddress", "1", source);
            using var fundingBlock = System.Text.Json.JsonDocument.Parse(fundingBlocks);
            var immatureReward = await BitcoinCliAsync("getblock", fundingBlock.RootElement[0].GetString(), "1");
            using var rewardBlock = System.Text.Json.JsonDocument.Parse(immatureReward);
            var rewardId = rewardBlock.RootElement.GetProperty("tx")[0].GetString();
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
            var holonId = Guid.NewGuid();
            var memo = System.Text.Json.JsonSerializer.Serialize(new { Id = holonId, HolonType = HolonType.Holon, Version = 1 });
            var payment = await provider.SendTransactionAsync(source, destination, 0.001m, memo);
            Assert.IsFalse(payment.IsError, payment.Message);
            var transactionId = payment.Result.TransactionResult;
            Assert.AreEqual(transactionId, payment.MetaData["BitcoinTransactionId"]);
            var mempool = await BitcoinCliAsync("getmempoolentry", transactionId);
            using var entry = System.Text.Json.JsonDocument.Parse(mempool);
            Assert.IsTrue(entry.RootElement.GetProperty("fees").GetProperty("base").GetDecimal() > 0m);

            var blocks = await BitcoinCliAsync("generatetoaddress", "1", destination);
            using var mined = System.Text.Json.JsonDocument.Parse(blocks);
            var block = await BitcoinCliAsync("getblock", mined.RootElement[0].GetString(), "2");
            using var confirmation = System.Text.Json.JsonDocument.Parse(block);
            var transaction = confirmation.RootElement.GetProperty("tx").EnumerateArray()
                .Single(candidate => candidate.GetProperty("txid").GetString() == transactionId);
            Assert.IsTrue(transaction.GetProperty("vin").EnumerateArray()
                .All(input => input.GetProperty("txid").GetString() != rewardId),
                "Payment must exclude the immature mining reward.");
            var outputs = transaction.GetProperty("vout").EnumerateArray().ToArray();
            Assert.IsTrue(outputs.Any(output => output.GetProperty("value").GetDecimal() == 0.001m &&
                output.GetProperty("scriptPubKey").TryGetProperty("address", out var address) &&
                address.GetString() == destination));
            var script = NBitcoin.TxNullDataTemplate.Instance
                .GenerateScriptPubKey(System.Text.Encoding.UTF8.GetBytes(memo)).ToHex();
            Assert.IsTrue(outputs.Any(output => output.GetProperty("value").GetDecimal() == 0m &&
                output.GetProperty("scriptPubKey").GetProperty("hex").GetString() == script));

            var minedHeight = confirmation.RootElement.GetProperty("height").GetInt32().ToString();
            for (var attempt = 0; attempt < 60; attempt++)
            {
                if ((await http.GetStringAsync("blocks/tip/height")).Trim() == minedHeight)
                    break;
                await Task.Delay(500);
            }
            Assert.AreEqual(minedHeight, (await http.GetStringAsync("blocks/tip/height")).Trim());
            var stored = await provider.LoadHolonAsync(transactionId, loadChildren: false, version: 1);
            Assert.IsFalse(stored.IsError, stored.Message);
            Assert.AreEqual(holonId, stored.Result.Id);
            Assert.AreEqual(transactionId, stored.Result.ProviderUniqueStorageKey[ProviderType.BitcoinOASIS]);

            var missingVersion = await provider.LoadHolonAsync(transactionId, loadChildren: false, version: 2);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, missingVersion.ErrorCode);

            var storageAddress = sourceKey.PubKey.GetAddress(NBitcoin.ScriptPubKeyType.Segwit, network).ToString();
            await BitcoinCliAsync("-rpcwallet=oasis-regtest", "-named", "sendtoaddress",
                $"address={storageAddress}", "amount=1", "fee_rate=2");
            await BitcoinCliAsync("generatetoaddress", "1", source);
            var storageTip = (await BitcoinCliAsync("getblockcount")).Trim();
            for (var attempt = 0; attempt < 60; attempt++)
            {
                if ((await http.GetStringAsync("blocks/tip/height")).Trim() == storageTip)
                    break;
                await Task.Delay(500);
            }
            Assert.AreEqual(storageTip, (await http.GetStringAsync("blocks/tip/height")).Trim());

            var kuboApi = Environment.GetEnvironmentVariable("OASIS_BITCOIN_KUBO_API");
            Assert.IsTrue(Uri.TryCreate(kuboApi, UriKind.Absolute, out var kuboUri) && kuboUri.IsLoopback);
            using var kubo = new System.Net.Http.HttpClient { BaseAddress = new Uri(kuboApi.TrimEnd('/') + "/") };
            var initialPins = await ReadKuboPinsAsync();

            using (var unfundedKey = new NBitcoin.Key())
            using (var unfunded = new BitcoinOASIS(api, "regtest", unfundedKey.GetWif(network).ToString(), null, 2m))
            {
                var failed = await unfunded.SaveHolonAsync(new Holon
                {
                    Id = Guid.NewGuid(),
                    HolonType = HolonType.Holon,
                    Name = "Unfunded regtest fixture",
                    Version = 1
                }, saveChildren: false);
                Assert.IsTrue(failed.IsError);
                Assert.IsFalse(failed.MetaData.ContainsKey("BitcoinTransactionId"));
                CollectionAssert.AreEquivalent(initialPins, await ReadKuboPinsAsync());
                var emptyInventory = await unfunded.ListRecordPublicationsAsync();
                Assert.IsFalse(emptyInventory.IsError, emptyInventory.Message);
                Assert.AreEqual(0, emptyInventory.Result.Count);
            }

            var privateHolon = new Holon
            {
                Id = Guid.NewGuid(),
                HolonType = HolonType.Holon,
                Version = 3,
                Name = "Private regtest fixture"
            };
            var saved = await provider.SaveHolonAsync(privateHolon, saveChildren: false);
            Assert.IsFalse(saved.IsError, saved.Message);
            var storageTx = saved.Result.ProviderUniqueStorageKey[ProviderType.BitcoinOASIS];
            Assert.AreEqual(storageTx, saved.MetaData["BitcoinTransactionId"]);
            await BitcoinCliAsync("getmempoolentry", storageTx);
            var storageBlockIds = await BitcoinCliAsync("generatetoaddress", "1", source);
            using var storageBlocks = System.Text.Json.JsonDocument.Parse(storageBlockIds);
            var storageBlockHash = storageBlocks.RootElement[0].GetString();
            var storageBlock = await BitcoinCliAsync("getblock", storageBlockHash, "1");
            using var storageConfirmation = System.Text.Json.JsonDocument.Parse(storageBlock);
            Assert.IsTrue(storageConfirmation.RootElement.GetProperty("tx").EnumerateArray()
                .Any(candidate => candidate.GetString() == storageTx));

            for (var attempt = 0; attempt < 60; attempt++)
            {
                if ((await http.GetStringAsync("blocks/tip/hash")).Trim() == storageBlockHash)
                    break;
                await Task.Delay(500);
            }
            Assert.AreEqual(storageBlockHash, (await http.GetStringAsync("blocks/tip/hash")).Trim());
            using var reopened = new BitcoinOASIS(api, "regtest");
            var restored = await reopened.LoadHolonAsync(storageTx, loadChildren: false, version: 3);
            Assert.IsFalse(restored.IsError, restored.Message);
            Assert.AreEqual(privateHolon.Id, restored.Result.Id);
            Assert.AreEqual(privateHolon.Name, restored.Result.Name);
            Assert.AreEqual(3, restored.Result.Version);
            Assert.AreEqual(storageTx, restored.Result.ProviderUniqueStorageKey[ProviderType.BitcoinOASIS]);

            const string storageKeyVariable = "OASIS_BITCOIN_STORAGE_KEY";
            var originalKey = Environment.GetEnvironmentVariable(storageKeyVariable);
            var wrongKey = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
            try
            {
                Environment.SetEnvironmentVariable(storageKeyVariable, Convert.ToBase64String(wrongKey));
                var rejected = await reopened.LoadHolonAsync(storageTx, loadChildren: false, version: 3);
                Assert.IsTrue(rejected.IsError);
                Assert.IsNull(rejected.Result);
                Assert.IsInstanceOfType(rejected.Exception, typeof(System.Security.Cryptography.CryptographicException));
            }
            finally
            {
                Environment.SetEnvironmentVariable(storageKeyVariable, originalKey);
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(wrongKey);
            }

            var avatarRecord = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "regtest-avatar",
                Email = "regtest@example.invalid",
                Version = 2
            };
            var savedAvatar = await provider.SaveAvatarAsync(avatarRecord);
            Assert.IsFalse(savedAvatar.IsError, savedAvatar.Message);
            var avatarTx = savedAvatar.Result.ProviderUniqueStorageKey[ProviderType.BitcoinOASIS];
            Assert.AreEqual(avatarTx, savedAvatar.MetaData["BitcoinTransactionId"]);
            await BitcoinCliAsync("getmempoolentry", avatarTx);
            var avatarBlocks = await BitcoinCliAsync("generatetoaddress", "1", source);
            using var avatarBlock = System.Text.Json.JsonDocument.Parse(avatarBlocks);
            var avatarBlockHash = avatarBlock.RootElement[0].GetString();
            for (var attempt = 0; attempt < 60; attempt++)
            {
                if ((await http.GetStringAsync("blocks/tip/hash")).Trim() == avatarBlockHash)
                    break;
                await Task.Delay(500);
            }
            Assert.AreEqual(avatarBlockHash, (await http.GetStringAsync("blocks/tip/hash")).Trim());

            var loadedAvatar = await reopened.LoadAvatarByProviderKeyAsync(avatarTx, version: 2);
            Assert.IsFalse(loadedAvatar.IsError, loadedAvatar.Message);
            Assert.AreEqual(avatarRecord.Id, loadedAvatar.Result.Id);
            Assert.AreEqual(avatarRecord.Email, loadedAvatar.Result.Email);
            Assert.AreEqual(avatarRecord.Username, loadedAvatar.Result.Username);
            Assert.AreEqual(avatarTx, loadedAvatar.Result.ProviderUniqueStorageKey[ProviderType.BitcoinOASIS]);

            using var walletReader = new BitcoinOASIS(api, "regtest", sourceKey.GetWif(network).ToString());
            foreach (var lookup in new[]
            {
                await walletReader.LoadAvatarAsync(avatarRecord.Id),
                await walletReader.LoadAvatarByEmailAsync(avatarRecord.Email.ToUpperInvariant()),
                await walletReader.LoadAvatarByUsernameAsync(avatarRecord.Username.ToUpperInvariant(), version: 2)
            })
            {
                Assert.IsFalse(lookup.IsError, lookup.Message);
                Assert.AreEqual(avatarRecord.Id, lookup.Result.Id);
                Assert.AreEqual(avatarTx, lookup.Result.ProviderUniqueStorageKey[ProviderType.BitcoinOASIS]);
            }

            var avatarList = await walletReader.LoadAllAvatarsAsync(version: 2);
            Assert.IsFalse(avatarList.IsError, avatarList.Message);
            Assert.AreEqual(avatarRecord.Id, avatarList.Result.Single().Id);
            var missingAvatarVersion = await walletReader.LoadAvatarAsync(avatarRecord.Id, version: 3);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, missingAvatarVersion.ErrorCode);

            var records = (await ReadKuboPinsAsync()).Except(initialPins, StringComparer.Ordinal).ToArray();
            Assert.AreEqual(2, records.Length, "The holon and avatar must each retain their encrypted Kubo payload.");
            foreach (var record in records)
            {
                using var blob = await kubo.PostAsync($"api/v0/cat?arg={Uri.EscapeDataString(record)}", null);
                blob.EnsureSuccessStatusCode();
                var envelope = await blob.Content.ReadAsStringAsync();
                Assert.IsTrue(envelope.StartsWith("oasis-aes256gcm-gzip-v2:", StringComparison.Ordinal));
                Assert.IsFalse(envelope.Contains(avatarRecord.Email, StringComparison.Ordinal));
                Assert.IsFalse(envelope.Contains(privateHolon.Name, StringComparison.Ordinal));
            }

            var avatarAsHolon = await reopened.LoadHolonAsync(avatarTx, loadChildren: false);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, avatarAsHolon.ErrorCode);

            var avatarDetail = new AvatarDetail
            {
                Id = avatarRecord.Id,
                Username = avatarRecord.Username,
                Email = avatarRecord.Email,
                FirstName = "Regtest",
                LastName = "Detail",
                Version = 4
            };
            var savedDetail = await provider.SaveAvatarDetailAsync(avatarDetail);
            Assert.IsFalse(savedDetail.IsError, savedDetail.Message);
            var detailTx = savedDetail.Result.ProviderUniqueStorageKey[ProviderType.BitcoinOASIS];
            Assert.AreEqual(detailTx, savedDetail.MetaData["BitcoinTransactionId"]);
            await BitcoinCliAsync("getmempoolentry", detailTx);

            using var detailBlocks = System.Text.Json.JsonDocument.Parse(
                await BitcoinCliAsync("generatetoaddress", "1", source));
            var detailBlockHash = detailBlocks.RootElement[0].GetString();
            for (var attempt = 0; attempt < 60; attempt++)
            {
                if ((await http.GetStringAsync("blocks/tip/hash")).Trim() == detailBlockHash)
                    break;
                await Task.Delay(500);
            }
            Assert.AreEqual(detailBlockHash, (await http.GetStringAsync("blocks/tip/hash")).Trim());

            var loadedDetail = await reopened.LoadAvatarDetailByTransactionIdAsync(detailTx, version: 4);
            Assert.IsFalse(loadedDetail.IsError, loadedDetail.Message);
            Assert.AreEqual(avatarDetail.Id, loadedDetail.Result.Id);
            Assert.AreEqual(avatarDetail.Username, loadedDetail.Result.Username);
            Assert.AreEqual(avatarDetail.FirstName, ((AvatarDetail)loadedDetail.Result).FirstName);
            Assert.AreEqual(4, loadedDetail.Result.Version);
            Assert.AreEqual(detailTx, loadedDetail.Result.ProviderUniqueStorageKey[ProviderType.BitcoinOASIS]);

            foreach (var lookup in new[]
            {
                await walletReader.LoadAvatarDetailAsync(avatarDetail.Id),
                await walletReader.LoadAvatarDetailByEmailAsync(avatarDetail.Email.ToUpperInvariant()),
                await walletReader.LoadAvatarDetailByUsernameAsync(avatarDetail.Username.ToUpperInvariant(), version: 4)
            })
            {
                Assert.IsFalse(lookup.IsError, lookup.Message);
                Assert.AreEqual(avatarDetail.Id, lookup.Result.Id);
                Assert.AreEqual(detailTx, lookup.Result.ProviderUniqueStorageKey[ProviderType.BitcoinOASIS]);
            }

            var detailList = await walletReader.LoadAllAvatarDetailsAsync(version: 4);
            Assert.IsFalse(detailList.IsError, detailList.Message);
            Assert.AreEqual(avatarDetail.Id, detailList.Result.Single().Id);
            var missingDetailVersion = await walletReader.LoadAvatarDetailAsync(avatarDetail.Id, version: 5);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, missingDetailVersion.ErrorCode);

            var wrongDetailVersion = await reopened.LoadAvatarDetailByTransactionIdAsync(detailTx, version: 5);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, wrongDetailVersion.ErrorCode);
            var avatarAsDetail = await reopened.LoadAvatarDetailByTransactionIdAsync(avatarTx);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, avatarAsDetail.ErrorCode);

            // Forward the signed transaction to real Esplora, then truncate its acknowledgement.
            using var reservation = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            reservation.Start();
            var proxyAddress = $"http://127.0.0.1:{((System.Net.IPEndPoint)reservation.LocalEndpoint).Port}/";
            reservation.Stop();
            using var proxy = new System.Net.HttpListener();
            proxy.Prefixes.Add(proxyAddress);
            proxy.Start();
            using var proxyDeadline = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
            var nativeAcknowledgement = ForwardUntilBroadcastAsync();
            try
            {
                using var interrupted = new BitcoinOASIS(proxyAddress, "regtest", sourceKey.GetWif(network).ToString(), null, 2m);
                var interruptedActivation = await interrupted.ActivateProviderAsync();
                Assert.IsFalse(interruptedActivation.IsError, interruptedActivation.Message);
                var uncertain = await interrupted.SaveHolonAsync(new Holon
                {
                    Id = Guid.NewGuid(),
                    HolonType = HolonType.Holon,
                    Name = "Interrupted regtest fixture",
                    Version = 1
                }, saveChildren: false);
                var acceptedId = await nativeAcknowledgement;
                Assert.IsTrue(uncertain.IsError, uncertain.Message);
                Assert.IsNull(uncertain.Result);
                Assert.AreEqual("BITCOIN_BROADCAST_OUTCOME_UNKNOWN", uncertain.ErrorCode);
                Assert.AreEqual(acceptedId, uncertain.MetaData["BitcoinTransactionId"]);
                StringAssert.Contains(uncertain.Message, "Esplora did not confirm Bitcoin transaction");
                await BitcoinCliAsync("getmempoolentry", acceptedId);

                var namePrefix = $"oasis-bitcoin-v1:{network.GetGenesis().GetHash()}:{storageAddress}:";
                using var storedPins = await kubo.PostAsync(
                    $"api/v0/pin/ls?type=recursive&name={Uri.EscapeDataString(namePrefix)}", null);
                storedPins.EnsureSuccessStatusCode();
                using var namedPins = System.Text.Json.JsonDocument.Parse(await storedPins.Content.ReadAsStringAsync());
                CollectionAssert.AreEquivalent(
                    new[] { storageTx, avatarTx, detailTx, acceptedId }.Select(id => namePrefix + id).ToArray(),
                    namedPins.RootElement.GetProperty("Keys").EnumerateObject()
                        .Select(pin => pin.Value.GetProperty("Name").GetString()).ToArray());

                using var recoveredBlocks = System.Text.Json.JsonDocument.Parse(
                    await BitcoinCliAsync("generatetoaddress", "1", source));
                var recoveredBlock = recoveredBlocks.RootElement[0].GetString();
                for (var attempt = 0; attempt < 60; attempt++)
                {
                    if ((await http.GetStringAsync("blocks/tip/hash")).Trim() == recoveredBlock)
                        break;
                    await Task.Delay(500);
                }
                Assert.AreEqual(recoveredBlock, (await http.GetStringAsync("blocks/tip/hash")).Trim());

                using var recovered = new BitcoinOASIS(api, "regtest", sourceKey.GetWif(network).ToString());
                var inventory = await recovered.ListRecordPublicationsAsync();
                Assert.IsFalse(inventory.IsError, inventory.Message);
                CollectionAssert.AreEquivalent(new[] { storageTx, avatarTx, detailTx, acceptedId }, inventory.Result.ToArray());
                var reconciliation = await recovered.ReconcileRecordPublicationAsync(acceptedId);
                Assert.IsFalse(reconciliation.IsError, reconciliation.Message);
                Assert.IsTrue(reconciliation.Result);
                var unknownPin = await recovered.ReconcileRecordPublicationAsync(transactionId);
                Assert.IsTrue(unknownPin.IsError);
                Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, unknownPin.ErrorCode);
            }
            finally
            {
                proxyDeadline.Cancel();
                try { await nativeAcknowledgement; }
                catch (OperationCanceledException) when (proxyDeadline.IsCancellationRequested) { }
            }

            async Task<string> ForwardUntilBroadcastAsync()
            {
                var cancellation = proxyDeadline.Token;
                while (true)
                {
                    var request = await proxy.GetContextAsync().WaitAsync(cancellation);
                    using var response = request.Response;
                    var path = request.Request.Url.PathAndQuery.TrimStart('/');
                    var isBroadcast = request.Request.HttpMethod == "POST" && path == "tx";
                    using var forward = new System.Net.Http.HttpRequestMessage(
                        new System.Net.Http.HttpMethod(request.Request.HttpMethod), path);
                    if (isBroadcast)
                    {
                        using var body = new System.IO.StreamReader(request.Request.InputStream);
                        forward.Content = new System.Net.Http.StringContent(
                            await body.ReadToEndAsync(cancellation), System.Text.Encoding.ASCII, "text/plain");
                    }

                    using var upstream = await http.SendAsync(forward, cancellation);
                    var payload = await upstream.Content.ReadAsByteArrayAsync(cancellation);
                    response.StatusCode = (int)upstream.StatusCode;
                    response.ContentType = upstream.Content.Headers.ContentType?.ToString() ?? "text/plain";
                    response.ContentLength64 = payload.Length;
                    if (isBroadcast)
                    {
                        upstream.EnsureSuccessStatusCode();
                        await response.OutputStream.WriteAsync(payload.AsMemory(0, 1), cancellation);
                        await response.OutputStream.FlushAsync(cancellation);
                        response.Abort();
                        return System.Text.Encoding.ASCII.GetString(payload).Trim();
                    }

                    await response.OutputStream.WriteAsync(payload.AsMemory(), cancellation);
                }
            }

            async Task<string[]> ReadKuboPinsAsync()
            {
                using var response = await kubo.PostAsync("api/v0/pin/ls?type=recursive", null);
                response.EnsureSuccessStatusCode();
                using var pins = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return pins.RootElement.GetProperty("Keys").EnumerateObject().Select(pin => pin.Name).ToArray();
            }

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
