using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Avatar = NextGenSoftware.OASIS.API.Core.Holons.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail;
using Holon = NextGenSoftware.OASIS.API.Core.Holons.Holon;

namespace NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.TestHarness
{
    internal static class Program
    {
        // Avalanche Fuji Testnet Configuration
        private static readonly BigInteger _chainId = 43113;
        private const string _chainUrl = "https://api.avax-test.network/ext/bc/C/rpc";
        private const string _chainPrivateKey = "YOUR_PRIVATE_KEY_HERE"; // Replace with your test private key
        private const string _contractAddress = "YOUR_CONTRACT_ADDRESS_HERE"; // Replace with your deployed contract
        private const string _accountAddress = "YOUR_ACCOUNT_ADDRESS_HERE"; // Replace with your account address

        /// <summary>
        /// Execute this example to see how Avatar CRUD works via Avalanche provider
        /// </summary>
        private static async Task ExecuteAvatarProviderExample(string contractAddress)
        {
            AvalancheOASIS avalancheOASIS = new(_chainUrl, _chainPrivateKey, _chainId, contractAddress);

            #region Create Avatar

            IAvatar avatar = new Avatar()
            {
                Username = "@avalanche_avatar",
                FirstName = "Avalanche",
                LastName = "User",
                AvatarId = Guid.NewGuid(),
                Id = Guid.NewGuid()
            };

            Console.WriteLine("🔄 Requesting avatar saving on Avalanche...");
            var saveAvatarResult = await avalancheOASIS.SaveAvatarAsync(avatar);

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

            Console.WriteLine("🔍 Querying avatar from Avalanche...");
            var queriedAvatarResult = await avalancheOASIS.LoadAvatarAsync(avatar.Id);

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
            var deletedAvatarResult = await avalancheOASIS.DeleteAvatarAsync(avatar.Id);
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
        /// Execute this example to see how Avatar Detail CRUD works via Avalanche provider
        /// </summary>
        private static async Task ExecuteAvatarDetailProviderExample(string contractAddress)
        {
            AvalancheOASIS avalancheOASIS = new(_chainUrl, _chainPrivateKey, _chainId, contractAddress);

            #region Create Avatar Detail

            IAvatarDetail avatar = new AvatarDetail()
            {
                Username = "@avalanche_detail",
                Address = "Avalanche Network",
                Id = Guid.NewGuid()
            };

            Console.WriteLine("🔄 Saving avatar detail on Avalanche...");
            var saveAvatarDetailResult = await avalancheOASIS.SaveAvatarDetailAsync(avatar);

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
            var queriedAvatarDetailResult = await avalancheOASIS.LoadAvatarDetailAsync(avatar.Id);

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
        /// Execute this example to see how Holon CRUD works via Avalanche provider
        /// </summary>
        private static async Task ExecuteHolonProviderExample(string contractAddress)
        {
            AvalancheOASIS avalancheOASIS = new(_chainUrl, _chainPrivateKey, _chainId, contractAddress);
            avalancheOASIS.ActivateProvider();

            #region Create Holon

            IHolon holon = new Holon
            {
                HolonType = HolonType.All,
                Description = "Avalanche Test Holon",
                Name = "AvalancheHolon",
                Id = Guid.NewGuid()
            };

            Console.WriteLine("🔄 Saving holon on Avalanche...");
            var saveHolonResult = await avalancheOASIS.SaveHolonAsync(holon);

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
            var loadHolonAsyncResult = await avalancheOASIS.LoadHolonAsync(holon.Id);

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
            var deleteHolonAsyncResult = await avalancheOASIS.DeleteHolonAsync(holon.Id);
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
            AvalancheOASIS avalancheOASIS = new(_chainUrl, _chainPrivateKey, _chainId, _contractAddress);
            avalancheOASIS.ActivateProvider();

            OASISResult<INFTTransactionRespone> result = await avalancheOASIS.SendNFTAsync(new NFTWalletTransactionRequest()
            {
                FromWalletAddress = _contractAddress,
                ToWalletAddress = _contractAddress,
                FromProvider = new EnumValue<ProviderType>(ProviderType.IPFSOASIS),
                ToProvider = new EnumValue<ProviderType>(ProviderType.AvalancheOASIS),
                Amount = 1m,
                MemoText = "Sending NFT on Avalanche",
                TokenId = 1
            });

            Console.WriteLine($"NFT Sent: {result.IsSaved}, Message: {result.Message}");
            Console.WriteLine($"Transaction Hash: {result.Result?.TransactionResult}");
        }

        private static async Task ExecuteMintNftExample()
        {
            AvalancheOASIS avalancheOASIS = new(_chainUrl, _chainPrivateKey, _chainId, _contractAddress);
            avalancheOASIS.ActivateProvider();

            OASISResult<INFTTransactionRespone> result = await avalancheOASIS.MintNFTAsync(
                new MintNFTTransactionRequest()
                {
                    MintedByAvatarId = Guid.NewGuid(),
                    Title = "Avalanche OASIS NFT",
                    Description = "An NFT minted on Avalanche C-Chain via OASIS",
                    Image = [0x01, 0x02, 0x03, 0x04],
                    ImageUrl = "https://example.com/images/avalanche-nft.jpg",
                    Thumbnail = [0x05, 0x06, 0x07, 0x08],
                    ThumbnailUrl = "https://example.com/thumbnails/avalanche-nft-thumb.jpg",
                    Price = 1m,
                    Discount = 0.1m,
                    MemoText = "Minted on Avalanche!",
                    NumberToMint = 1,
                    StoreNFTMetaDataOnChain = true,
                    MetaData = new Dictionary<string, object>
                    {
                        { "Network", "Avalanche" },
                        { "Chain", "C-Chain" },
                        { "Attributes", new Dictionary<string, string>
                            {
                                { "BackgroundColor", "Red" },
                                { "Rarity", "Epic" }
                            }
                        }
                    },
                    OffChainProvider = new EnumValue<ProviderType>(ProviderType.IPFSOASIS),
                    OnChainProvider = new EnumValue<ProviderType>(ProviderType.AvalancheOASIS),
                    JSONMetaDataURL = "https://example.com/metadata/avalanche-nft.json"
                }
            );

            Console.WriteLine($"NFT Minted: {result.IsSaved}, Message: {result.Message}");
            Console.WriteLine($"Transaction Hash: {result.Result?.TransactionResult}");
        }

        private static async Task Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   🏔️  Avalanche OASIS Provider - TEST HARNESS v1.0 🏔️    ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Network: Avalanche Fuji Testnet (C-Chain)");
            Console.WriteLine($"Chain ID: {_chainId}");
            Console.WriteLine($"RPC URL: {_chainUrl}");
            Console.WriteLine();
            Console.WriteLine("Available Tests:");
            Console.WriteLine("1. Avatar CRUD");
            Console.WriteLine("2. Avatar Detail CRUD");
            Console.WriteLine("3. Holon CRUD");
            Console.WriteLine("4. Mint NFT");
            Console.WriteLine("5. Send NFT");
            Console.WriteLine();

            // TODO: Uncomment one of the example methods to start testing
            //await ExecuteAvatarProviderExample(_contractAddress);
            //await ExecuteAvatarDetailProviderExample(_contractAddress);
            //await ExecuteHolonProviderExample(_contractAddress);
            await ExecuteMintNftExample();
            //await ExecuteSendNFTExample();

            Console.WriteLine();
            Console.WriteLine("✨ Test completed! Press any key to exit...");
            Console.ReadKey();
        }
    }
}



