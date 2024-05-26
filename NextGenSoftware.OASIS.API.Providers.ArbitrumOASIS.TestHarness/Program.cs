using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Avatar = NextGenSoftware.OASIS.API.Core.Holons.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail;
using Holon = NextGenSoftware.OASIS.API.Core.Holons.Holon;

namespace NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.TestHarness
{
    internal static class Program
    {
        private static readonly string _chainUrl = "https://sepolia-rollup.arbitrum.io/rpc";
        private static readonly string _chainPrivateKey = "d3c80ec102d5fe42beadcb7346f74df529a0a10a1906f6ecc5fe3770eb65fb1a";
        private static readonly BigInteger _chainId = 421614;
        private static string _contractAddress = "0x06a2dabf7fec27d27f9283cb2de1cd328685510c";
        private static string _abi = "";

        /// <summary>
        /// Execute that example to see, how does Avatar CRUD works via arbitrum provider
        /// </summary>
        private static async Task ExecuteAvatarProviderExample(string contractAddress)
        {
            ArbitrumOASIS arbitrumOASIS = new(_chainUrl, _chainPrivateKey, _chainId, contractAddress, _abi);

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
            ArbitrumOASIS arbitrumOASIS = new(_chainUrl, _chainPrivateKey, _chainId, contractAddress, _abi);

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
            ArbitrumOASIS arbitrumOASIS = new(_chainUrl, _chainPrivateKey, _chainId, contractAddress, _abi);

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
                Console.WriteLine($"Saving holon failed! Error message: {saveHolonResult}");
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

        private static async Task Main(string[] args)
        {
            Console.WriteLine("NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS - TEST HARNESS");

            // TODO: Uncomment one of example method to start testing ethereum provider CRUD
            // await ExecuteAvatarProviderExample(_contractAddress);
            // await ExecuteAvatarProviderExample(_contractAddress);
            await ExecuteHolonProviderExample(_contractAddress);
        }
    }
}