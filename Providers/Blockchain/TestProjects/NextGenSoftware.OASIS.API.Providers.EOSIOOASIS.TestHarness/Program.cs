using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EosSharp;
using EosSharp.Core;
using EosSharp.Core.Api.v1;
using EosSharp.Core.Providers;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetTableRows;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
using Action = EosSharp.Core.Api.v1.Action;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.TestHarness
{
    internal static class Program
    {
        private static readonly string _chainUrl = "http://localhost:8888";
        private static readonly string _chainId = "8a34ec7df1b8cd06ff4a8abbaa7cc50300823350cadc59ab296cb00d104d2b8f";
        private static readonly string _accountPk = "5KUm4te3kPbR6Fmrmr5WwCw3pfkymVxdh44x1b9QFYhm66uwYsP";

        private static readonly string _oasisEosAccount = "oasis";
        private static readonly string _oasisEosAccount2 = "oasis22";
        private static readonly string _avatarTable = "avatar";
        private static readonly string _holonTable = "holon";
        private static readonly string _avatarDetailTable = "avatardetail";

        public static async Task Main()
        {
            // Transferring
            await Run_SendTransactionAsync();
            await Run_SendNftAsync();
            
            // Account Examples
            await Run_GetEOSIOAccountAsync();
            await Run_GetBalanceForEOSIOAccount();
            
            // Avatar Examples
            await Run_GetAvatarTableRows();
            
            // Avatar Detail Examples
            await Run_GetAvatarDetailsTableRows();
            
            // Holon Examples
            await Run_GetAllHolon();
            await Run_SaveAndGetHolonById();
            await Run_SoftAndHardDeleteHolonById();
            await Run_EosSharp_HolonPushTransaction();
            await Run_GetHolonTableRows();
        }

        #region Eos Account Examples

        public static async Task Run_GetEOSIOAccountAsync()
        {
            var eosioOasis = new EOSIOOASIS(_chainUrl, _oasisEosAccount, _chainId, _accountPk);

            Console.WriteLine("Run_GetEOSIOAccountAsync-->ActivateProvider()");
            eosioOasis.ActivateProvider();

            Console.WriteLine("Requesting account...");
            var eosAccount = await eosioOasis.GetEOSIOAccountAsync(_oasisEosAccount);
            if (eosAccount == null)
            {
                Console.WriteLine("Account requesting error...");
                eosioOasis.DeActivateProvider();
                
                return;
            }
            
            Console.WriteLine("Account Name: " + eosAccount.AccountName);
            Console.WriteLine("Account Parent Permission: " + eosAccount.Permissions[0].Parent);
            
            Console.WriteLine("Run_GetEOSIOAccountAsync-->ActivateProvider()");
            eosioOasis.DeActivateProvider();
        }

        public static async Task Run_GetBalanceForEOSIOAccount()
        {
            var eosioOasis = new EOSIOOASIS(_chainUrl, _oasisEosAccount, _chainId, _accountPk);

            Console.WriteLine("Run_GetBalanceForEOSIOAccount-->ActivateProvider()");
            eosioOasis.ActivateProvider();

            var accountBalance = eosioOasis.GetBalanceForEOSIOAccount(_oasisEosAccount, _oasisEosAccount, _oasisEosAccount);
            if (string.IsNullOrEmpty(accountBalance))
            {
                Console.WriteLine("Balance requesting error...");
                eosioOasis.DeActivateProvider();
                
                return;
            }
            
            Console.WriteLine("Account Balance: " + accountBalance);

            Console.WriteLine("Run_GetBalanceForEOSIOAccount-->ActivateProvider()");
            eosioOasis.DeActivateProvider();
        }

        #endregion
        
        #region Avatar Examples

        private static async Task Run_GetAvatarTableRows()
        {
            IEosClient eosClient = new EosClient(new Uri(_chainUrl));

            Console.WriteLine("Requesting avatar table rows...");

            var avatarsTableDto = await eosClient.GetTableRows<AvatarDto>(new GetTableRowsRequestDto
            {
                Code = _oasisEosAccount,
                Table = _avatarTable,
                Scope = _oasisEosAccount
            });

            foreach (var avatarItem in avatarsTableDto.Rows)
            {
                Console.WriteLine(new string('-', 10));
                Console.WriteLine("Info: " + avatarItem.Info);
                Console.WriteLine("AvatarId: " + avatarItem.AvatarId);
                Console.WriteLine("EntityId: " + avatarItem.EntityId);
                Console.WriteLine("IsDeleted: " + avatarItem.IsDeleted);
                Console.WriteLine(new string('-', 10));
            }

            Console.WriteLine("Requesting avatar table rows completed successfully...");

            eosClient.Dispose();
        }

        #endregion

        #region Avatar Detail

        private static async Task Run_GetAvatarDetailsTableRows()
        {
            IEosClient eosClient = new EosClient(new Uri(_chainUrl));

            Console.WriteLine("Requesting avatar detail table rows...");

            var avatarDetailsTableDto = await eosClient.GetTableRows<AvatarDetailDto>(new GetTableRowsRequestDto
            {
                Code = _oasisEosAccount,
                Table = _avatarDetailTable,
                Scope = _oasisEosAccount
            });

            foreach (var avatarDetailItem in avatarDetailsTableDto.Rows)
            {
                Console.WriteLine(new string('-', 10));
                Console.WriteLine("Info: " + avatarDetailItem.Info);
                Console.WriteLine("AvatarId: " + avatarDetailItem.AvatarId);
                Console.WriteLine("EntityId: " + avatarDetailItem.EntityId);
                Console.WriteLine(new string('-', 10));
            }

            Console.WriteLine("Requesting avatar detail table rows completed successfully...");

            eosClient.Dispose();
        }

        #endregion

        #region Holon Example

        /// <summary>
        ///     Runtime Error :(
        /// </summary>
        private static async Task Run_EOSNewYork_HolonPushTransaction()
        {
            // var chainApi = new ChainAPI(_chainUrl);
            // var pushTransactionResult = await chainApi.PushTransactionAsync(
            //     new Action[]
            //     {
            //         new Action()
            //         {
            //             account = new AccountName()
            //             {
            //                 value = _oasisEosAccount
            //             },
            //             authorization = new []
            //             { 
            //                 new Authorization()
            //                 {
            //                     actor = new AccountName()
            //                     {
            //                         value = _oasisEosAccount
            //                     },
            //                     permission = new PermissionName()
            //                     {
            //                         value = "active"
            //                     }
            //                 }
            //             },
            //             name = new ActionName()
            //             {
            //                 value = "addholon"
            //             },
            //             data = new BinaryString()
            //             {
            //                 value = "[123, \"test_holon_from_testharness\", \"test_holon_from_testharness2\"]"
            //             }
            //         }
            //     }, 
            //     new List<string>()
            //     {
            //         "5KUm4te3kPbR6Fmrmr5WwCw3pfkymVxdh44x1b9QFYhm66uwYsP"
            //     });
        }

        private static async Task Run_EosSharp_HolonPushTransaction()
        {
            var eos = new Eos(new EosConfigurator
            {
                HttpEndpoint = _chainUrl,
                ChainId = _chainId,
                ExpireSeconds = 60,
                SignProvider = new DefaultSignProvider(_accountPk)
            });

            Console.WriteLine("Requesting chain info...");

            var chainInfo = await eos.GetInfo();
            Console.WriteLine("EOS local-environment chain id: " + chainInfo.chain_id);
            Console.WriteLine("EOS local-environment server version: " + chainInfo.server_version);

            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = "OASIS",
                Version = 1,
                IsActive = true,
                Description = "EOS transaction creating."
            };

            var entityId = HashUtility.GetNumericHash(holon.Id);
            var holonInfo = JsonConvert.SerializeObject(holon);

            Console.WriteLine("Requesting transaction creating...");
            var pushTransactionResult = await eos.CreateTransaction(new Transaction
            {
                actions = new List<Action>
                {
                    new()
                    {
                        account = _oasisEosAccount,
                        authorization = new List<PermissionLevel>
                        {
                            new()
                            {
                                actor = _oasisEosAccount,
                                permission = "active"
                            }
                        },
                        name = "addholon",
                        data = new
                        {
                            entityId,
                            holonId = holon.Id.ToString(),
                            info = holonInfo
                        }
                    }
                }
            });

            Console.WriteLine("Transaction completed...");
            Console.WriteLine($"Transaction hash: {pushTransactionResult}");
        }

        private static async Task Run_GetHolonTableRows()
        {
            IEosClient eosClient = new EosClient(new Uri(_chainUrl));

            Console.WriteLine("Requesting holon table rows...");

            var holonTableRows = await eosClient.GetTableRows<HolonDto>(new GetTableRowsRequestDto
            {
                Code = _oasisEosAccount,
                Table = _holonTable,
                Scope = _oasisEosAccount
            });

            foreach (var holonDto in holonTableRows.Rows)
            {
                Console.WriteLine(new string('-', 10));
                Console.WriteLine("Info: " + holonDto.Info);
                Console.WriteLine("HolonId: " + holonDto.HolonId);
                Console.WriteLine("EntityId: " + holonDto.EntityId);
                Console.WriteLine("IsDeleted: " + holonDto.IsDeleted);
                Console.WriteLine(new string('-', 10));
            }

            Console.WriteLine("Requesting holon table rows completed successfully...");

            eosClient.Dispose();
        }

        private static async Task Run_GetAllHolon()
        {
            var eosioOasis = new EOSIOOASIS(_chainUrl, _oasisEosAccount, _chainId, _accountPk);

            Console.WriteLine("Run_GetAllHolon-->ActivateProvider()");
            eosioOasis.ActivateProvider();

            Console.WriteLine("Run_GetAllHolon-->LoadAllHolonsAsync()");
            var allHolonsResult = await eosioOasis.LoadAllHolonsAsync();
            if (!allHolonsResult.IsLoaded)
            {
                Console.WriteLine(allHolonsResult.Message);
                return;
            }

            foreach (var holon in allHolonsResult.Result)
            {
                Console.WriteLine(new string('-', 10));
                Console.WriteLine("Holon Id: " + holon.Id);
                Console.WriteLine("Name: " + holon.Name);
                Console.WriteLine(new string('-', 10));
            }

            Console.WriteLine("Run_GetAllHolon-->DeActivateProvider()");
            eosioOasis.DeActivateProvider();
        }

        private static async Task Run_SaveAndGetHolonById()
        {
            var eosioOasis = new EOSIOOASIS(_chainUrl, _oasisEosAccount, _chainId, _accountPk);

            Console.WriteLine("Run_SaveAndGetHolonById-->ActivateProvider()");
            eosioOasis.ActivateProvider();

            var entityId = Guid.NewGuid();
            var entity = new Holon
            {
                Name = "Bob",
                Id = entityId
            };
            Console.WriteLine("Run_SaveAndGetHolonById-->SaveHolonAsync()");
            var saveResult = await eosioOasis.SaveHolonAsync(entity);
            if (!saveResult.IsSaved)
            {
                Console.WriteLine(saveResult.Message);
                return;
            }

            Console.WriteLine("Run_SaveAndGetHolonById-->LoadHolonAsync()");
            var loadedEntityResult = await eosioOasis.LoadHolonAsync(entityId);
            if (!loadedEntityResult.IsLoaded)
            {
                Console.WriteLine(saveResult.Message);
                return;
            }

            Console.WriteLine("Holon Id: " + loadedEntityResult.Result.Id);
            Console.WriteLine("Holon Name: " + loadedEntityResult.Result.Name);

            Console.WriteLine("Run_SaveAndGetHolonById-->DeActivateProvider()");
            eosioOasis.DeActivateProvider();
        }

        private static async Task Run_SoftAndHardDeleteHolonById()
        {
            var eosioOasis = new EOSIOOASIS(_chainUrl, _oasisEosAccount, _chainId, _accountPk);

            Console.WriteLine("Run_SoftAndHardDeleteHolonById-->ActivateProvider()");
            eosioOasis.ActivateProvider();

            var entityId = Guid.NewGuid();
            var entity = new Holon
            {
                Name = "Bob",
                Id = entityId
            };
            Console.WriteLine("Run_SoftAndHardDeleteHolonById-->SaveHolonAsync()");
            var saveResult = await eosioOasis.SaveHolonAsync(entity);
            if (!saveResult.IsSaved)
            {
                Console.WriteLine(saveResult.Message);
                return;
            }

            Console.WriteLine("Run_SoftAndHardDeleteHolonById-->DeleteHolonAsync()");
            var softDeleteResult = await eosioOasis.DeleteHolonAsync(entityId);
            if (!softDeleteResult.IsSaved)
            {
                Console.WriteLine(softDeleteResult.Message);
                return;
            }

            var entityId2 = Guid.NewGuid();
            var entity2 = new Holon
            {
                Name = "Bob",
                Id = entityId2
            };
            Console.WriteLine("Run_SoftAndHardDeleteHolonById-->SaveHolonAsync()");
            var saveResult2 = await eosioOasis.SaveHolonAsync(entity2);
            if (!saveResult2.IsSaved)
            {
                Console.WriteLine(saveResult.Message);
                return;
            }

            Console.WriteLine("Run_SoftAndHardDeleteHolonById-->DeleteHolonAsync()");
            var hardDeleteResult = await eosioOasis.DeleteHolonAsync(entityId2, false);
            if (!hardDeleteResult.IsSaved)
            {
                Console.WriteLine(hardDeleteResult.Message);
                return;
            }

            Console.WriteLine("Run_SoftAndHardDeleteHolonById-->DeActivateProvider()");
            eosioOasis.DeActivateProvider();
        }

        #endregion

        #region Transfer Example

        private static async Task Run_SendNftAsync()
        {
            var eosioOasis = new EOSIOOASIS(_chainUrl, _oasisEosAccount, _chainId, _accountPk);

            Console.WriteLine("Run_SendNftAsync-->ActivateProvider()");
            eosioOasis.ActivateProvider();

            var sendNftRequest = new NFTWalletTransactionRequest()
            {
                Amount = 0.001m,
                //Date = DateTime.Now,
                FromProviderType = ProviderType.EOSIOOASIS,
                FromWalletAddress = _oasisEosAccount,
                ToWalletAddress = _oasisEosAccount2
            };
            Console.WriteLine("Run_SendNftAsync-->SendNFTAsync()-->Sending...");
            var sendNftResult = await eosioOasis.SendNFTAsync(sendNftRequest);
            if (sendNftResult.IsError)
            {
                Console.WriteLine("Run_SendNftAsync-->SendNFTAsync()-->Failed...");
                Console.WriteLine(sendNftResult.Message);
                return;
            }
            Console.WriteLine("Run_SendNftAsync-->SendNFTAsync()-->Completed...");

            Console.WriteLine("Run_SendNftAsync-->ActivateProvider()");
            eosioOasis.DeActivateProvider();
        }

        private static async Task Run_SendTransactionAsync()
        {
            var eosioOasis = new EOSIOOASIS(_chainUrl, _oasisEosAccount, _chainId, _accountPk);

            Console.WriteLine("Run_SendTransactionAsync-->ActivateProvider()");
            eosioOasis.ActivateProvider();

            var walletTransaction = new WalletTransactionRequest()
            {
                Amount = 0.001m,
               // Date = DateTime.Now,
                FromProviderType = ProviderType.EOSIOOASIS,
                FromWalletAddress = _oasisEosAccount,
                ToWalletAddress = _oasisEosAccount2
            };
            Console.WriteLine("Run_SendTransactionAsync-->SendTransactionAsync()-->Sending...");
            var sendTransactionResult = await eosioOasis.SendTransactionAsync(walletTransaction);
            if (sendTransactionResult.IsError)
            {
                Console.WriteLine("Run_SendTransactionAsync-->SendTransactionAsync()-->Failed...");
                Console.WriteLine(sendTransactionResult.Message);
                return;
            }
            Console.WriteLine("Run_SendTransactionAsync-->SendTransactionAsync()-->Completed...");
            
            Console.WriteLine("Run_SendTransactionAsync-->ActivateProvider()");
            eosioOasis.DeActivateProvider();
        }

        #endregion
    }
}