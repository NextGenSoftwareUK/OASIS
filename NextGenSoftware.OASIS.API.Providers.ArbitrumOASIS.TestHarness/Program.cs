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

namespace NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.TestHarness
{
    internal static class Program
    {
        private static readonly BigInteger _chainId = 421614;
        private const string _chainUrl = "https://sepolia-rollup.arbitrum.io/rpc";
        private const string _chainPrivateKey = "d3c80ec102d5fe42beadcb7346f74df529a0a10a1906f6ecc5fe3770eb65fb1a";
        private const string _contractAddress = "0x299440d24Cd0e8Ca6425ae92cE66032E58B8c436";
        private const string _accountAddress = "0x604b88BECeD9d6a02113fE1A0129f67fbD565D38";

        /// <summary>
        /// Execute that example to see, how does Avatar CRUD works via arbitrum provider
        /// </summary>
        private static async Task ExecuteAvatarProviderExample(string contractAddress)
        {
            ArbitrumOASIS arbitrumOASIS = new(_chainUrl, _chainPrivateKey, _chainId, contractAddress);

            #region Create Avatar

            IAvatar avatar = new Avatar()
            {
                Username = "@avatar",
                FirstName = "Avatar",
                LastName = "Bob",
                AvatarId = Guid.NewGuid(),
                Id = Guid.NewGuid()
            };

            Console.WriteLine("Requesting avatar saving...");
            var saveAvatarResult = await arbitrumOASIS.SaveAvatarAsync(avatar);

            if (saveAvatarResult.IsError && !saveAvatarResult.IsSaved)
            {
                Console.WriteLine($"Saving avatar failed! Error message: {saveAvatarResult.Message}");
                return;
            }
            else
            {
                Console.WriteLine("Avatar saving completed successfully!");
            }

            #endregion

            #region Query Avatar

            Console.WriteLine("Querying avatar...");
            var queriedAvatarResult = await arbitrumOASIS.LoadAvatarAsync(avatar.Id);

            if (queriedAvatarResult.IsError && !queriedAvatarResult.IsLoaded)
            {
                Console.WriteLine($"Avatar querying failed, {queriedAvatarResult.Message}!");
                return;
            }
            else
            {
                Console.WriteLine($"Avatar querying completed successfully! Avatar Id {queriedAvatarResult.Result.Id}, {avatar.FullName}");
            }

            #endregion

            #region Delete Avatar

            Console.WriteLine("Requesting avatar deleting...");
            var deletedAvatarResult = await arbitrumOASIS.DeleteAvatarAsync(avatar.Id);
            if (deletedAvatarResult.IsError)
            {
                Console.WriteLine($"Avatar deleting failed, {deletedAvatarResult.Message}!");
                return;
            }
            else
            {
                Console.WriteLine("Avatar deleting completed successfully...");
            }

            #endregion
        }

        /// <summary>
        /// Execute that example to see, how does Avatar Detail CRUD works via arbitrum provider
        /// </summary>
        private static async Task ExecuteAvatarDetailProviderExample(string contractAddress)
        {
            ArbitrumOASIS arbitrumOASIS = new(_chainUrl, _chainPrivateKey, _chainId, contractAddress);

            #region Create Avatar Detail

            IAvatarDetail avatar = new AvatarDetail()
            {
                Username = "@avatar",
                Address = "Where you are!",
                Id = Guid.NewGuid()
            };

            Console.WriteLine("Requesting avatar detail saving...");
            var saveAvatarDetailResult = await arbitrumOASIS.SaveAvatarDetailAsync(avatar);

            if (saveAvatarDetailResult.IsError && !saveAvatarDetailResult.IsSaved)
            {
                Console.WriteLine($"Saving avatar detailed failed! Error message: {saveAvatarDetailResult.Message}");
                return;
            }
            else
            {
                Console.WriteLine("Avatar detail saving completed successfully!");
            }

            #endregion

            #region Query Avatar Detail

            Console.WriteLine("Querying avatar...");
            var queriedAvatarDetailResult = await arbitrumOASIS.LoadAvatarDetailAsync(avatar.Id);

            if (queriedAvatarDetailResult.IsError && !queriedAvatarDetailResult.IsLoaded)
            {
                Console.WriteLine($"Avatar detail querying failed, {queriedAvatarDetailResult.Message}!");
                return;
            }
            else
            {
                Console.WriteLine($"Avatar detail querying completed successfully! Avatar Id {queriedAvatarDetailResult.Result.Id}!");
            }

            #endregion
        }

        /// <summary>
        /// Execute that example to see, how does Holon CRUD works via arbitrum provider
        /// </summary>
        private static async Task ExecuteHolonProviderExample(string contractAddress)
        {
            ArbitrumOASIS arbitrumOASIS = new(_chainUrl, _chainPrivateKey, _chainId, contractAddress);
            arbitrumOASIS.ActivateProvider();

            #region Create Holon

            IHolon holon = new Holon
            {
                HolonType = HolonType.All,
                Description = "Simple Dimple Description",
                Name = "Testable Holon",
                Id = Guid.NewGuid()
            };

            Console.WriteLine("Requesting holon saving...");
            var saveHolonResult = await arbitrumOASIS.SaveHolonAsync(holon);

            if (saveHolonResult.IsError && !saveHolonResult.IsSaved)
            {
                Console.WriteLine($"Saving holon failed! Error message: {saveHolonResult.Message}");
                return;
            }
            else
            {
                Console.WriteLine("Holon saving completed successfully!");
            }

            #endregion

            #region Query Holon

            Console.WriteLine("Querying holon...");
            var loadHolonAsyncResult = await arbitrumOASIS.LoadHolonAsync(holon.Id);

            if (loadHolonAsyncResult.IsError && !loadHolonAsyncResult.IsLoaded)
            {
                Console.WriteLine($"Holon querying failed, {loadHolonAsyncResult.Message}!");
                return;
            }
            else
            {
                Console.WriteLine($"Holon querying completed successfully! Avatar Id {loadHolonAsyncResult.Result.Id}, {holon.Name}");
            }

            #endregion

            #region Delete Holon

            Console.WriteLine("Requesting holon deleting...");
            var deleteHolonAsyncResult = await arbitrumOASIS.DeleteHolonAsync(holon.Id);
            if (deleteHolonAsyncResult.IsError)
            {
                Console.WriteLine($"Holon deleting failed, {deleteHolonAsyncResult.Message}!");
                return;
            }
            else
            {
                Console.WriteLine("Holon deleting completed successfully...");
            }

            #endregion
        }

        private static async Task ExecuteSendNFTExample()
        {
            ArbitrumOASIS arbitrumOASIS = new(_chainUrl, _chainPrivateKey, _chainId, _contractAddress);
            arbitrumOASIS.ActivateProvider();

            OASISResult<INFTTransactionRespone> result = await arbitrumOASIS.SendNFTAsync(new NFTWalletTransactionRequest()
            {
                MintWalletAddress = _contractAddress,
                FromWalletAddress = _contractAddress,
                ToWalletAddress = _contractAddress,
                FromProviderType = ProviderType.IPFSOASIS, // Example provider type
                ToProviderType = ProviderType.EthereumOASIS, // Example provider type
                Amount = 1m, // Example amount
                MemoText = "Sending NFT to a new owner.",
                TokenId = 10
            });

            Console.WriteLine($"Is NFT Sent: {result.IsSaved}, {result.Message}");
            Console.WriteLine($"NFT Sending Transaction Hash: {result.Result?.TransactionResult}");
        }

        private static async Task ExecuteMintNftExample()
        {
            ArbitrumOASIS arbitrumOASIS = new(_chainUrl, _chainPrivateKey, _chainId, _contractAddress);
            arbitrumOASIS.ActivateProvider();

            OASISResult<INFTTransactionRespone> result = await arbitrumOASIS.MintNFTAsync(
                new MintNFTTransactionRequestForProvider()
                {
                    MintWalletAddress = _accountAddress,
                    MintedByAvatarId = Guid.NewGuid(),
                    Title = "Sample NFT Title",
                    Description = "This is a description of the sample NFT. It includes all the unique attributes and features.",
                    Image = [0x01, 0x02, 0x03, 0x04], // Mock byte array for the image
                    ImageUrl = "https://example.com/images/sample-nft.jpg",
                    Thumbnail = [0x05, 0x06, 0x07, 0x08], // Mock byte array for the thumbnail
                    ThumbnailUrl = "https://example.com/thumbnails/sample-nft-thumb.jpg",
                    Price = 1m, // Price in whatever currency the system uses, e.g., Ether
                    Discount = 1m, // 5% discount
                    MemoText = "Thank you for purchasing this NFT!",
                    NumberToMint = 100,
                    StoreNFTMetaDataOnChain = true,
                    MetaData = new Dictionary<string, object>
                    {
                        { "Creator", "John Doe" },
                        { "Attributes", new Dictionary<string, string>
                            {
                                { "BackgroundColor", "Blue" },
                                { "Rarity", "Rare" }
                            }
                        },
                        { "Edition", "First Edition" }
                    },
                    OffChainProvider = new EnumValue<ProviderType>(ProviderType.IPFSOASIS),
                    OnChainProvider = new EnumValue<ProviderType>(ProviderType.ArbitrumOASIS),
                    JSONMetaDataUrl = "https://example.com/metadata/sample-nft.json"
                }
            );

            Console.WriteLine($"Is NFT Minted: {result.IsSaved}, {result.Message}");
            Console.WriteLine($"NFT Minting Transaction Hash: {result.Result?.TransactionResult}");
        }

        private static async Task Main(string[] args)
        {
            Console.WriteLine("NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS - TEST HARNESS v1.2");

            // TODO: Uncomment one of example method to start testing ethereum provider CRUD
            //await ExecuteAvatarProviderExample(_contractAddress);
            // await ExecuteAvatarProviderExample(_contractAddress);
            await ExecuteMintNftExample();
            //await ExecuteSendNFTExample();
        }
    }
}