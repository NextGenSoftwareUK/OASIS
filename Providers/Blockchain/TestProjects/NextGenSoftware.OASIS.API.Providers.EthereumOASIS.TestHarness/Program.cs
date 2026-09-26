using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition;
using Avatar = NextGenSoftware.OASIS.API.Core.Holons.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail;
using Holon = NextGenSoftware.OASIS.API.Core.Holons.Holon;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.TestHarness
{
    internal static class Program
    {
        private static readonly string _chainUrl = "http://testchain.nethereum.com:8545";
        private static readonly string _chainPrivateKey = "0x7580e7fb49df1c861f0050fae31c2224c6aba908e116b8da44ee8cd927b990b0";
        private static readonly BigInteger _chainId = 444444444500;
        private static string _contractAddress = "0x06a2dabf7fec27d27f9283cb2de1cd328685510c";

        /// <summary>
        /// Execute that example to see, how does Avatar Entity CRUD works via ethereum smart contract
        /// </summary>
        /// <exception cref="JsonRpc.Client.RpcResponseException">Throws that exception while avatar querying was null</exception>>
        private static async Task ExecuteAvatarRawExample(Web3 web3, string contractAddress)
        {
            var nextGenSoftwareOasisService = new NextGenSoftwareOASISService(web3, contractAddress);

            #region Create Avatar

            // Creating avatar instances for creating
            IAvatar bobAvatar = new Avatar
            {
                Id = Guid.NewGuid(),
                AvatarId = Guid.NewGuid(),
                Username = "@Bob",
                CreatedDate = DateTime.Now
            };

            var avatarInfo = JsonConvert.SerializeObject(bobAvatar);
            var avatarEntityId = HashUtility.GetNumericHash(bobAvatar.Id.ToString());
            var avatarId = bobAvatar.AvatarId.ToString();

            Console.WriteLine("Creating avatar...");
            var creatingTransaction = await nextGenSoftwareOasisService
                .CreateAvatarRequestAndWaitForReceiptAsync(avatarEntityId, avatarId, avatarInfo);

            if (string.IsNullOrEmpty(creatingTransaction.TransactionHash))
            {
                Console.WriteLine("Avatar creating failed, please try again later...");
                return;
            }
            Console.WriteLine("Creating avatar completed successfully!");

            #endregion

            #region Query Avatar

            Console.WriteLine("Querying avatar...");
            var avatarByIdOutputDto = await nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(avatarEntityId);

            if (avatarByIdOutputDto == null)
            {
                Console.WriteLine("Avatar not found, avatar creating failed...");
                return;
            }
            Console.WriteLine($"{avatarByIdOutputDto.ReturnValue1.AvatarId} - avatar queried successfully!");

            #endregion

            #region Update Avatar

            Console.WriteLine($"Updating {avatarByIdOutputDto.ReturnValue1.AvatarId} avatar");
            bobAvatar.Password = "B@b1k";
            bobAvatar.Email = "bob@email.net";
            bobAvatar.FirstName = "Bob";
            bobAvatar.LastName = "Break";
            bobAvatar.Title = "Bomber";
            var updatedAvatarInfo = JsonConvert.SerializeObject(bobAvatar);

            var updateAvatarTransaction = await nextGenSoftwareOasisService
                .UpdateAvatarRequestAndWaitForReceiptAsync(avatarEntityId, updatedAvatarInfo);

            if (string.IsNullOrEmpty(updateAvatarTransaction.TransactionHash))
            {
                Console.WriteLine("Avatar updating failed, please try again later...");
                return;
            }
            
            Console.WriteLine("Avatar updating completed...");
            var updatedAvatarByIdOutputDto = await nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(avatarEntityId);

            if (updatedAvatarByIdOutputDto == null)
            {
                Console.WriteLine("Avatar not found, avatar querying failed...");
                return;
            }

            Console.WriteLine($"{avatarByIdOutputDto.ReturnValue1.AvatarId} - avatar queried successfully! " +
                              $"Updated avatar data: {updatedAvatarByIdOutputDto.ReturnValue1.Info}");

            #endregion

            #region Deleting Avatar

            Console.WriteLine("Requesting avatar deleting...");
            var deleteAvatarTransaction = await nextGenSoftwareOasisService
                .DeleteAvatarRequestAndWaitForReceiptAsync(avatarEntityId);

            if (string.IsNullOrEmpty(deleteAvatarTransaction.TransactionHash))
            {
                Console.WriteLine("Avatar deleting failed, please try again later...");
                return;
            }   
            Console.WriteLine("Requesting of avatar deleting completed...");

            #endregion
        }

        /// <summary>
        /// Execute that example to see, how does Avatar Detail Entity CRUD works via ethereum smart contract
        /// </summary>
        /// <exception cref="JsonRpc.Client.RpcResponseException">Throws that exception while avatar detail querying was null</exception>>
        private static async Task ExecuteAvatarDetailRawExample(Web3 web3, string contractAddress)
        {
            var nextGenSoftwareOasisService = new NextGenSoftwareOASISService(web3, contractAddress);
            
            #region Create Avatar Detail
            
            // Creating avatar detail instances for creating request
            IAvatarDetail bobAvatarDetail = new AvatarDetail()
            {
                Id = Guid.NewGuid(),
                Username = "@BobDetil",
                CreatedDate = DateTime.Now
            };

            var avatarDetailInfo = JsonConvert.SerializeObject(bobAvatarDetail);
            var avatarDetailEntityId = HashUtility.GetNumericHash(bobAvatarDetail.Id.ToString());
            var avatarDetailId = bobAvatarDetail.Id.ToString();

            Console.WriteLine("Creating avatar detail...");
            var creatingTransaction = await nextGenSoftwareOasisService
                .CreateAvatarDetailRequestAndWaitForReceiptAsync(avatarDetailEntityId, avatarDetailId, avatarDetailInfo);

            if (string.IsNullOrEmpty(creatingTransaction.TransactionHash))
            {
                Console.WriteLine("Avatar detail creating failed, please try again later...");
                return;
            }
            Console.WriteLine("Creating avatar detail completed successfully!");

            #endregion
            
            #region Query Avatar Detail
            
            Console.WriteLine("Querying avatar detail...");
            var avatarByIdOutputDto = await nextGenSoftwareOasisService.GetAvatarDetailByIdQueryAsync(avatarDetailEntityId);
            if (avatarByIdOutputDto == null)
            {
                Console.WriteLine("Avatar detail not found, avatar creating failed...");
                return;
            }
            Console.WriteLine($"{avatarByIdOutputDto.ReturnValue1.AvatarId} - avatar queried successfully!");

            #endregion
            
            #region Update Avatar Detail
            
            Console.WriteLine($"Updating {avatarByIdOutputDto.ReturnValue1.AvatarId} avatar");
            bobAvatarDetail.Email = "bob@email.net";
            bobAvatarDetail.Mobile = "iBomber 7 MAX";
            bobAvatarDetail.IsActive = true;
            bobAvatarDetail.XP = 111;
            var updatedAvatarDetailInfo = JsonConvert.SerializeObject(bobAvatarDetail);

            var updateAvatarDetailTransaction = await nextGenSoftwareOasisService
                .UpdateAvatarDetailRequestAndWaitForReceiptAsync(avatarDetailEntityId, updatedAvatarDetailInfo);

            if (string.IsNullOrEmpty(updateAvatarDetailTransaction.TransactionHash))
            {
                Console.WriteLine("Avatar detail updating failed, please try again later...");
                return;
            }
            
            Console.WriteLine("Avatar detail updating completed...");
            var updatedAvatarDetailByIdOutputDto = await nextGenSoftwareOasisService.GetAvatarDetailByIdQueryAsync(avatarDetailEntityId);

            if (updatedAvatarDetailByIdOutputDto == null)
            {
                Console.WriteLine("Avatar detail not found, avatar querying failed...");
                return;
            }

            Console.WriteLine($"{updatedAvatarDetailByIdOutputDto.ReturnValue1.AvatarId} - avatar queried successfully! " +
                              $"Updated avatar detail data: {updatedAvatarDetailByIdOutputDto.ReturnValue1.Info}");

            #endregion
            
            #region Deleting Avatar Detail

            Console.WriteLine("Request avatar detail deleting...");
            var deleteAvatarTransaction = await nextGenSoftwareOasisService
                .DeleteAvatarDetailRequestAndWaitForReceiptAsync(avatarDetailEntityId);
            
            if (string.IsNullOrEmpty(deleteAvatarTransaction.TransactionHash))
            {
                Console.WriteLine("Avatar detail deleting failed, please try again later...");
                return;
            }
            Console.WriteLine("Avatar Detail deleting requested!");

            #endregion
        }

        /// <summary>
        /// Execute that example to see, how does Holon Entity CRUD works via ethereum smart contract
        /// </summary>
        /// <exception cref="JsonRpc.Client.RpcResponseException">Throws that exception while holon querying was null</exception>>
        private static async Task ExecuteHolonRawExample(Web3 web3, string contractAddress)
        {
            var nextGenSoftwareOasisService = new NextGenSoftwareOASISService(web3, contractAddress);

            #region Creating Holon Entity
            
            // Creating avatar detail instances for creating request
            IHolon holonEntity = new Holon()
            {
                Id = Guid.NewGuid(),
                Description = "Basic Holon description...",
                Name = "Holon",
                Version = 15,
                IsActive = true,
                HolonType = HolonType.Comet,
                CreatedDate = DateTime.Now
            };

            var holonEntityInfo = JsonConvert.SerializeObject(holonEntity);
            var holonEntityId = HashUtility.GetNumericHash(holonEntity.Id.ToString());
            var holonId = holonEntity.Id.ToString();

            Console.WriteLine("Creating Holon...");
            var creatingTransaction = await nextGenSoftwareOasisService
                .CreateHolonRequestAndWaitForReceiptAsync(holonEntityId, holonId, holonEntityInfo);
            if (string.IsNullOrEmpty(creatingTransaction.TransactionHash))
            {
                Console.WriteLine("Holon creating failed, please try again later...");
                return;
            }
            Console.WriteLine("Creating holon completed successfully!");

            #endregion

            #region Querying Holon Entity

            Console.WriteLine("Querying Holon Entity...");
            var holonByIdOutputDto = await nextGenSoftwareOasisService.GetHolonByIdQueryAsync(holonEntityId);
            if (holonByIdOutputDto == null)
            {
                Console.WriteLine("Holon not found, holon creating failed...");
                return;
            }
            Console.WriteLine($"{holonByIdOutputDto.ReturnValue1.HolonId} - holon queried successfully!");

            #endregion

            #region Updating Holon Entity
            
            Console.WriteLine("Updating Holon Entity...");

            holonEntity.Name = "Holon (updated)";
            holonEntity.HolonType = HolonType.Moon;
            var updatedHolonEntityInfo = JsonConvert.SerializeObject(holonEntity);

            var updateHolonRequest = await nextGenSoftwareOasisService
                .UpdateHolonRequestAndWaitForReceiptAsync(holonEntityId, updatedHolonEntityInfo);

            if (string.IsNullOrEmpty(updateHolonRequest.TransactionHash))
            {
                Console.WriteLine("Holon updating failed, please try again later...");
                return;
            }
            Console.WriteLine("Holon updating completed...");
            
            var updatedHolonEntityDto = await nextGenSoftwareOasisService.GetHolonByIdQueryAsync(holonEntityId);
            if (updatedHolonEntityDto == null)
            {
                Console.WriteLine("Holon not found, holon querying failed...");
                return;
            }
            Console.WriteLine($"{updatedHolonEntityDto.ReturnValue1.HolonId} - holon queried successfully! " +
                              $"Updated holon data: {updatedHolonEntityDto.ReturnValue1.Info}");

            #endregion

            #region Deleting Holon Entity

            Console.WriteLine("Requesting holon entity deleting...");
            var deleteHolonRequest = await nextGenSoftwareOasisService
                .DeleteHolonRequestAndWaitForReceiptAsync(holonEntityId);
            
            if (string.IsNullOrEmpty(deleteHolonRequest.TransactionHash))
            {
                Console.WriteLine("Holon deleting failed, please try again later...");
                return;
            }
            Console.WriteLine("Holon deleting completed!");

            #endregion
        }
        
        /// <summary>
        /// Execute that example to see, how does Avatar CRUD works via ethereum provider
        /// </summary>
        private static async Task ExecuteAvatarProviderExample(string contractAddress)
        {
            var ethereumOasis = new EthereumOASIS(_chainUrl, _chainPrivateKey, _chainId, contractAddress);

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
            var saveAvatarResult = await ethereumOasis.SaveAvatarAsync(avatar);

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
            var queriedAvatarResult = await ethereumOasis.LoadAvatarAsync(avatar.Id);

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
            var deletedAvatarResult = await ethereumOasis.DeleteAvatarAsync(avatar.Id);
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
        /// Execute that example to see, how does Avatar Detail CRUD works via ethereum provider
        /// </summary>
        private static async Task ExecuteAvatarDetailProviderExample(string contractAddress)
        {
            var ethereumOasis = new EthereumOASIS(_chainUrl, _chainPrivateKey, _chainId, contractAddress);

            #region Create Avatar Detail

            IAvatarDetail avatar = new AvatarDetail()
            {
                Username = "@avatar",
                Address = "Where you are!",
                Id = Guid.NewGuid()
            };
            
            Console.WriteLine("Requesting avatar detail saving...");
            var saveAvatarDetailResult = await ethereumOasis.SaveAvatarDetailAsync(avatar);

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
            var queriedAvatarDetailResult = await ethereumOasis.LoadAvatarDetailAsync(avatar.Id);

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
        /// Execute that example to see, how does Holon CRUD works via ethereum provider
        /// </summary>
        private static async Task ExecuteHolonProviderExample(string contractAddress)
        {
            var ethereumOasis = new EthereumOASIS(_chainUrl, _chainPrivateKey, _chainId, contractAddress);

            #region Create Holon

            IHolon holon = new Holon
            {
                HolonType = HolonType.All,
                Description = "Simple Dimple Description",
                Name = "Testable Holon",
                Id = Guid.NewGuid()
            };
            
            Console.WriteLine("Requesting holon saving...");
            var saveHolonResult = await ethereumOasis.SaveHolonAsync(holon);

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
            var loadHolonAsyncResult = await ethereumOasis.LoadHolonAsync(holon.Id);

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
            var deleteHolonAsyncResult = await ethereumOasis.DeleteHolonAsync(holon.Id);
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

        private static async Task ExecuteSendTransaction(string contractAddress)
        {
            var ethereumOasis = new EthereumOASIS(_chainUrl, _chainPrivateKey, _chainId, contractAddress);
            const string toAddress = "0x13f022d72158410433cbd66f5dd8bf6d2d129924";
            const decimal amount = 0.01m;
            var account = new Nethereum.Web3.Accounts.Account(_chainPrivateKey, _chainId);
            var fromAddress = account.Address;
            Console.WriteLine("Sending transaction started...");
            var sendTransactionResult = await ethereumOasis.SendTransactionAsync(fromAddress, toAddress, amount, "EthereumOASIS TestHarness");
            if (sendTransactionResult.IsError)
            {
                Console.WriteLine($"Transaction performing failed! Reason: {sendTransactionResult.Message}");
                return;
            }
            Console.WriteLine($"Transaction completed successfully! Transaction hash: {sendTransactionResult.Result?.TransactionResult}");
        }
        
        private static async Task Main(string[] args)
        {
            Console.WriteLine("NextGenSoftware.OASIS.API.Providers.EthereumOASIS - TEST HARNESS");

            // Setting up ethereum account and web3 protocol
            var account = new Account(_chainPrivateKey, _chainId);
            var web3 = new Web3(account, _chainUrl);

            if (string.IsNullOrEmpty(_contractAddress))
            {
                // Deploying contract
                Console.WriteLine("Deploying ethereum provider contract...");
                var contractDeployment = new NextGenSoftwareOASISDeployment();
                var contractDeploymentReceipt = await NextGenSoftwareOASISService.DeployContractAndWaitForReceiptAsync(web3, contractDeployment);
                Console.WriteLine($"Ethereum provider contract deployed, address: {contractDeploymentReceipt.ContractAddress}");

                _contractAddress = contractDeploymentReceipt.ContractAddress;
            }

            // TODO: Uncomment one of example method to start testing ethereum provider CRUD
            // await ExecuteAvatarRawExample(web3, _contractAddress);
            // await ExecuteAvatarDetailRawExample(web3, _contractAddress);
            // await ExecuteHolonRawExample(web3, _contractAddress);
            // await ExecuteAvatarProviderExample(_contractAddress);
            // await ExecuteHolonProviderExample(_contractAddress);
            await ExecuteSendTransaction(_contractAddress);
        }
    }
}