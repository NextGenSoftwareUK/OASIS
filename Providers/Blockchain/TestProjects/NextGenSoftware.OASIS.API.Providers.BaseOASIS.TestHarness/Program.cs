using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Avatar = NextGenSoftware.OASIS.API.Core.Holons.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail;
using Holon = NextGenSoftware.OASIS.API.Core.Holons.Holon;

namespace NextGenSoftware.OASIS.API.Providers.BaseOASIS.TestHarness
{
    internal static class Program
    {
        // Base Sepolia Testnet Configuration
        private const string _chainUrl = "https://sepolia.base.org";
        private const string _chainPrivateKey = "YOUR_PRIVATE_KEY_HERE"; // Replace with your test private key
        private const string _contractAddress = "YOUR_CONTRACT_ADDRESS_HERE"; // Replace with your deployed contract
        private const string _accountAddress = "YOUR_ACCOUNT_ADDRESS_HERE"; // Replace with your account address

        /// <summary>
        /// Execute this example to see how Avatar CRUD works via Base provider
        /// </summary>
        private static async Task ExecuteAvatarProviderExample(string contractAddress)
        {
            BaseOASIS baseOASIS = new(_chainUrl, _chainPrivateKey, contractAddress);

            #region Create Avatar

            IAvatar avatar = new Avatar()
            {
                Username = "@base_avatar",
                FirstName = "Base",
                LastName = "User",
                AvatarId = Guid.NewGuid(),
                Id = Guid.NewGuid()
            };

            Console.WriteLine("🔄 Requesting avatar saving on Base...");
            var saveAvatarResult = await baseOASIS.SaveAvatarAsync(avatar);

            if (saveAvatarResult.IsError && !saveAvatarResult.IsSaved)
            {
                Console.WriteLine($"❌ Saving avatar failed! Error message: {saveAvatarResult.Message}");
                return;
            }
            else
            {
                Console.WriteLine("✅ Avatar saving completed successfully!");
            }

            #endregion

            #region Query Avatar

            Console.WriteLine("🔍 Querying avatar from Base...");
            var queriedAvatarResult = await baseOASIS.LoadAvatarAsync(avatar.Id);

            if (queriedAvatarResult.IsError && !queriedAvatarResult.IsLoaded)
            {
                Console.WriteLine($"❌ Avatar querying failed: {queriedAvatarResult.Message}!");
                return;
            }
            else
            {
                Console.WriteLine($"✅ Avatar queried successfully! Avatar Id: {queriedAvatarResult.Result.Id}, Name: {avatar.FullName}");
            }

            #endregion

            #region Delete Avatar

            Console.WriteLine("🗑️ Requesting avatar deletion...");
            var deletedAvatarResult = await baseOASIS.DeleteAvatarAsync(avatar.Id);
            if (deletedAvatarResult.IsError)
            {
                Console.WriteLine($"❌ Avatar deletion failed: {deletedAvatarResult.Message}!");
                return;
            }
            else
            {
                Console.WriteLine("✅ Avatar deleted successfully!");
            }

            #endregion
        }

        /// <summary>
        /// Execute this example to see how Avatar Detail CRUD works via Base provider
        /// </summary>
        private static async Task ExecuteAvatarDetailProviderExample(string contractAddress)
        {
            BaseOASIS baseOASIS = new(_chainUrl, _chainPrivateKey, contractAddress);

            #region Create Avatar Detail

            IAvatarDetail avatar = new AvatarDetail()
            {
                Username = "@base_detail",
                Address = "Base Network - L2",
                Id = Guid.NewGuid()
            };

            Console.WriteLine("🔄 Saving avatar detail on Base...");
            var saveAvatarDetailResult = await baseOASIS.SaveAvatarDetailAsync(avatar);

            if (saveAvatarDetailResult.IsError && !saveAvatarDetailResult.IsSaved)
            {
                Console.WriteLine($"❌ Saving avatar detail failed! Error: {saveAvatarDetailResult.Message}");
                return;
            }
            else
            {
                Console.WriteLine("✅ Avatar detail saved successfully!");
            }

            #endregion

            #region Query Avatar Detail

            Console.WriteLine("🔍 Querying avatar detail...");
            var queriedAvatarDetailResult = await baseOASIS.LoadAvatarDetailAsync(avatar.Id);

            if (queriedAvatarDetailResult.IsError && !queriedAvatarDetailResult.IsLoaded)
            {
                Console.WriteLine($"❌ Avatar detail query failed: {queriedAvatarDetailResult.Message}!");
                return;
            }
            else
            {
                Console.WriteLine($"✅ Avatar detail queried successfully! Id: {queriedAvatarDetailResult.Result.Id}");
            }

            #endregion
        }

        /// <summary>
        /// Execute this example to see how Holon CRUD works via Base provider
        /// </summary>
        private static async Task ExecuteHolonProviderExample(string contractAddress)
        {
            BaseOASIS baseOASIS = new(_chainUrl, _chainPrivateKey, _chainId, contractAddress);
            baseOASIS.ActivateProvider();

            #region Create Holon

            IHolon holon = new Holon
            {
                HolonType = HolonType.All,
                Description = "Base L2 Test Holon",
                Name = "BaseHolon",
                Id = Guid.NewGuid()
            };

            Console.WriteLine("🔄 Saving holon on Base...");
            var saveHolonResult = await baseOASIS.SaveHolonAsync(holon);

            if (saveHolonResult.IsError && !saveHolonResult.IsSaved)
            {
                Console.WriteLine($"❌ Holon save failed! Error: {saveHolonResult.Message}");
                return;
            }
            else
            {
                Console.WriteLine("✅ Holon saved successfully!");
            }

            #endregion

            #region Query Holon

            Console.WriteLine("🔍 Querying holon...");
            var loadHolonAsyncResult = await baseOASIS.LoadHolonAsync(holon.Id);

            if (loadHolonAsyncResult.IsError && !loadHolonAsyncResult.IsLoaded)
            {
                Console.WriteLine($"❌ Holon query failed: {loadHolonAsyncResult.Message}!");
                return;
            }
            else
            {
                Console.WriteLine($"✅ Holon queried successfully! Id: {loadHolonAsyncResult.Result.Id}, Name: {holon.Name}");
            }

            #endregion

            #region Delete Holon

            Console.WriteLine("🗑️ Deleting holon...");
            var deleteHolonAsyncResult = await baseOASIS.DeleteHolonAsync(holon.Id);
            if (deleteHolonAsyncResult.IsError)
            {
                Console.WriteLine($"❌ Holon deletion failed: {deleteHolonAsyncResult.Message}!");
                return;
            }
            else
            {
                Console.WriteLine("✅ Holon deleted successfully!");
            }

            #endregion
        }

        private static async Task ExecuteSendNFTExample()
        {
            BaseOASIS baseOASIS = new(_chainUrl, _chainPrivateKey, _contractAddress);
            baseOASIS.ActivateProvider();

            OASISResult<INFTTransactionRespone> result = await baseOASIS.SendNFTAsync(new NFTWalletTransactionRequest()
            {
                FromWalletAddress = _contractAddress,
                ToWalletAddress = _contractAddress,
                FromProvider = new EnumValue<ProviderType>(ProviderType.IPFSOASIS),
                ToProvider = new EnumValue<ProviderType>(ProviderType.BaseOASIS),
                Amount = 1m,
                MemoText = "Sending NFT on Base L2",
                TokenId = 1
            });

            Console.WriteLine($"NFT Sent: {result.IsSaved}, Message: {result.Message}");
            Console.WriteLine($"Transaction Hash: {result.Result?.TransactionResult}");
        }

        private static async Task ExecuteMintNftExample()
        {
            BaseOASIS baseOASIS = new(_chainUrl, _chainPrivateKey, _chainId, _contractAddress);
            baseOASIS.ActivateProvider();

            OASISResult<INFTTransactionRespone> result = await baseOASIS.MintNFTAsync(
                new MintNFTTransactionRequest()
                {
                    MintedByAvatarId = Guid.NewGuid(),
                    Title = "Base OASIS NFT",
                    Description = "An NFT minted on Base L2 (Coinbase) via OASIS",
                    Image = [0x01, 0x02, 0x03, 0x04],
                    ImageUrl = "https://example.com/images/base-nft.jpg",
                    Thumbnail = [0x05, 0x06, 0x07, 0x08],
                    ThumbnailUrl = "https://example.com/thumbnails/base-nft-thumb.jpg",
                    Price = 0.01m,
                    Discount = 0.001m,
                    MemoText = "Minted on Base - Powered by Coinbase!",
                    NumberToMint = 1,
                    StoreNFTMetaDataOnChain = true,
                    MetaData = new Dictionary<string, object>
                    {
                        { "Network", "Base" },
                        { "Layer", "L2" },
                        { "BackedBy", "Coinbase" },
                        { "Attributes", new Dictionary<string, string>
                            {
                                { "BackgroundColor", "Blue" },
                                { "Rarity", "Legendary" }
                            }
                        }
                    },
                    OffChainProvider = new EnumValue<ProviderType>(ProviderType.IPFSOASIS),
                    OnChainProvider = new EnumValue<ProviderType>(ProviderType.BaseOASIS),
                    JSONMetaDataURL = "https://example.com/metadata/base-nft.json"
                }
            );

            Console.WriteLine($"NFT Minted: {result.IsSaved}, Message: {result.Message}");
            Console.WriteLine($"Transaction Hash: {result.Result?.TransactionResult}");
        }

        /// <summary>
        /// Test Base wallet creation functionality
        /// </summary>
        private static async Task ExecuteWalletCreationTest()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           Base Wallet Creation Test                       ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                // Test 1: Generate Key Pair using KeyManager
                Console.WriteLine("Test 1: Generating Base key pair using KeyManager...");
                var keyPairResult = KeyManager.Instance.GenerateKeyPairWithWalletAddress(ProviderType.BaseOASIS);
                
                if (keyPairResult.IsError)
                {
                    Console.WriteLine($"❌ FAILED: {keyPairResult.Message}");
                    if (keyPairResult.Exception != null)
                    {
                        Console.WriteLine($"Exception: {keyPairResult.Exception.Message}");
                    }
                    return;
                }

                if (keyPairResult.Result == null)
                {
                    Console.WriteLine("❌ FAILED: Key pair result is null");
                    return;
                }

                Console.WriteLine($"✅ SUCCESS: Key pair generated");
                Console.WriteLine($"   Private Key: {keyPairResult.Result.PrivateKey?.Substring(0, Math.Min(20, keyPairResult.Result.PrivateKey?.Length ?? 0))}...");
                Console.WriteLine($"   Public Key: {keyPairResult.Result.PublicKey?.Substring(0, Math.Min(20, keyPairResult.Result.PublicKey?.Length ?? 0))}...");
                Console.WriteLine($"   Wallet Address: {keyPairResult.Result.WalletAddressLegacy}");
                Console.WriteLine();

                // Validate address format
                if (string.IsNullOrEmpty(keyPairResult.Result.WalletAddressLegacy))
                {
                    Console.WriteLine("⚠️  WARNING: Wallet address is empty");
                }
                else if (!keyPairResult.Result.WalletAddressLegacy.StartsWith("0x") || keyPairResult.Result.WalletAddressLegacy.Length != 42)
                {
                    Console.WriteLine($"⚠️  WARNING: Wallet address format may be incorrect: {keyPairResult.Result.WalletAddressLegacy}");
                    Console.WriteLine("   Expected: 0x followed by 40 hex characters (42 total)");
                }
                else
                {
                    Console.WriteLine($"✅ Wallet address format is valid (Ethereum/Base format)");
                }
                Console.WriteLine();

                // Test 2: Generate Key Pair Async
                Console.WriteLine("Test 2: Generating Base key pair using KeyManager (Async)...");
                var keyPairAsyncResult = await KeyManager.Instance.GenerateKeyPairWithWalletAddressAsync(ProviderType.BaseOASIS);
                
                if (keyPairAsyncResult.IsError)
                {
                    Console.WriteLine($"❌ FAILED: {keyPairAsyncResult.Message}");
                    return;
                }

                if (keyPairAsyncResult.Result == null)
                {
                    Console.WriteLine("❌ FAILED: Key pair result is null");
                    return;
                }

                Console.WriteLine($"✅ SUCCESS: Key pair generated (async)");
                Console.WriteLine($"   Wallet Address: {keyPairAsyncResult.Result.WalletAddressLegacy}");
                Console.WriteLine();

                // Test 3: Check if BaseOASIS provider is available
                Console.WriteLine("Test 3: Checking if BaseOASIS provider is registered...");
                var baseProvider = ProviderManager.Instance.GetProvider(ProviderType.BaseOASIS);
                
                if (baseProvider == null)
                {
                    Console.WriteLine("⚠️  WARNING: BaseOASIS provider is not registered");
                    Console.WriteLine("   This is expected if provider is not configured in OASIS_DNA.json");
                    Console.WriteLine("   Wallet creation will still work via KeyManager fallback");
                }
                else
                {
                    Console.WriteLine($"✅ SUCCESS: BaseOASIS provider is registered");
                    Console.WriteLine($"   Provider Type: {baseProvider.ProviderType}");
                    Console.WriteLine($"   Provider Name: {baseProvider.ProviderName}");
                    
                    // Check if it implements GenerateKeyPairAsync
                    if (baseProvider is IOASISBlockchainStorageProvider blockchainProvider)
                    {
                        var testKeyPair = await blockchainProvider.GenerateKeyPairAsync();
                        if (!testKeyPair.IsError && testKeyPair.Result != null)
                        {
                            Console.WriteLine($"✅ SUCCESS: BaseOASIS.GenerateKeyPairAsync() works");
                            Console.WriteLine($"   Generated Address: {testKeyPair.Result.WalletAddressLegacy}");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️  WARNING: BaseOASIS.GenerateKeyPairAsync() returned error: {testKeyPair.Message}");
                        }
                    }
                }
                Console.WriteLine();

                // Test 4: Create wallet without saving (simulation)
                Console.WriteLine("Test 4: Testing CreateWalletWithoutSaving for BaseOASIS...");
                var testAvatarId = Guid.NewGuid();
                var walletResult = WalletManager.Instance.CreateWalletWithoutSaving(
                    testAvatarId,
                    "Test Base Wallet",
                    "Test wallet for Base blockchain",
                    ProviderType.BaseOASIS,
                    generateKeyPair: true,
                    isDefaultWallet: true
                );

                if (walletResult.IsError || walletResult.Result == null)
                {
                    Console.WriteLine($"❌ FAILED: {walletResult.Message}");
                    if (walletResult.Exception != null)
                    {
                        Console.WriteLine($"Exception: {walletResult.Exception.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"✅ SUCCESS: Wallet created (not saved)");
                    Console.WriteLine($"   Wallet ID: {walletResult.Result.WalletId}");
                    Console.WriteLine($"   Wallet Address: {walletResult.Result.WalletAddress}");
                    Console.WriteLine($"   Provider Type: {walletResult.Result.ProviderType}");
                    Console.WriteLine($"   Is Default: {walletResult.Result.IsDefaultWallet}");
                    Console.WriteLine($"   Has Private Key: {!string.IsNullOrEmpty(walletResult.Result.PrivateKey)}");
                    Console.WriteLine($"   Has Public Key: {!string.IsNullOrEmpty(walletResult.Result.PublicKey)}");
                }
                Console.WriteLine();

                Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                    Test Summary                           ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
                Console.WriteLine("✅ Base wallet creation is working!");
                Console.WriteLine("\nNext Steps:");
                Console.WriteLine("1. Ensure BaseOASIS provider is configured in OASIS_DNA.json");
                Console.WriteLine("2. Test wallet creation for an actual avatar using:");
                Console.WriteLine("   WalletManager.Instance.CreateWalletForAvatarByIdAsync(avatarId, name, description, ProviderType.BaseOASIS)");
                Console.WriteLine("3. Verify wallet can be used for SERV token transfers");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ UNEXPECTED ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static async Task Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║      🔵 Base OASIS Provider - TEST HARNESS v1.0 🔵       ║");
            Console.WriteLine("║           Powered by Coinbase - Built on Optimism        ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Network: Base Sepolia Testnet");
            Console.WriteLine($"Chain ID: 84532");
            Console.WriteLine($"RPC URL: {_chainUrl}");
            Console.WriteLine("Explorer: https://sepolia.basescan.org");
            Console.WriteLine();
            Console.WriteLine("Available Tests:");
            Console.WriteLine("1. Avatar CRUD");
            Console.WriteLine("2. Avatar Detail CRUD");
            Console.WriteLine("3. Holon CRUD");
            Console.WriteLine("4. Mint NFT");
            Console.WriteLine("5. Send NFT");
            Console.WriteLine("6. Wallet Creation Test (NEW)");
            Console.WriteLine();

            // Run wallet creation test
            await ExecuteWalletCreationTest();
            
            // TODO: Uncomment one of the example methods to start testing
            //await ExecuteAvatarProviderExample(_contractAddress);
            //await ExecuteAvatarDetailProviderExample(_contractAddress);
            //await ExecuteHolonProviderExample(_contractAddress);
            //await ExecuteMintNftExample();
            //await ExecuteSendNFTExample();

            Console.WriteLine();
            Console.WriteLine("✨ Test completed! Press any key to exit...");
            Console.ReadKey();
        }
    }
}



