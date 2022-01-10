using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nethereum.Web3;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using Org.BouncyCastle.Ocsp;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public class EthereumOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar
    {
        private readonly string _abi;
        private readonly string _abiByteCode;
        private readonly string _hostUri;
        private readonly string _password;
        private readonly string _projectId;
        private readonly string _senderAddress;

        public EthereumOASIS(string hostUri, string projectId, string abi, string abiByteCode, string password,
            string senderAddress)
        {
            this.ProviderName = "EthereumOASIS";
            this.ProviderDescription = "Ethereum Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EthereumOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);

            _hostUri = hostUri;
            _projectId = projectId;
            _abi = abi;
            _abiByteCode = abiByteCode;
            _password = password;
            _senderAddress = senderAddress;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var web3 = new Web3();
                await web3.Personal.UnlockAccount.SendRequestAsync(_senderAddress, _password, 120);

                var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(_abi, _abiByteCode, _senderAddress);

                var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                while (receipt == null)
                {
                    await Task.Delay(5000);
                    receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                }

                var contractAddress = receipt.ContractAddress;
                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var mFunc = contract.GetFunction("CreateAvatar");
                
                
                // CreateAvatar(uint entityId, string avatarId, string info)

                var entityId = new Random().Next(-90000, 90000);
                var avatarId = avatar.AvatarId.ToString();
                var avatarInfo = JsonConvert.SerializeObject(avatar); 

                object[] avatarObjects =
                {
                    entityId,
                    avatarId,
                    avatarInfo
                };
                await mFunc.CallAsync<object>(avatarObjects);

                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var web3 = new Web3();
                web3.Personal.UnlockAccount.SendRequestAsync(_senderAddress, _password, 120)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                var transactionHash = web3.Eth.DeployContract.SendRequestAsync(_abi, _abiByteCode, _senderAddress)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                var receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                while (receipt == null)
                {
                    Task.Delay(5000);
                    receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash)
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                }

                var contractAddress = receipt.ContractAddress;
                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var mFunc = contract.GetFunction("CreateAvatarDetail");

                // CreateAvatarDetail(uint entityId, string avatarId, string info)

                var entityId = new Random().Next(-90000, 90000);
                var avatarId = avatar.Id.ToString();
                var avatarInfo = JsonConvert.SerializeObject(avatar); 
                
                object[] avatarObjects =
                {
                    entityId,
                    avatarId,
                    avatarInfo
                };
                mFunc.CallAsync<int>(avatarObjects)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var web3 = new Web3();
                await web3.Personal.UnlockAccount.SendRequestAsync(_senderAddress, _password, 120);

                var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(_abi, _abiByteCode, _senderAddress);

                var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                while (receipt == null)
                {
                    await Task.Delay(5000);
                    receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                }

                var contractAddress = receipt.ContractAddress;
                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var mFunc = contract.GetFunction("CreateAvatarDetail");

                // CreateAvatarDetail(uint entityId, string avatarId, string info)

                var entityId = new Random().Next(-90000, 90000);
                var avatarId = avatar.Id.ToString();
                var avatarInfo = JsonConvert.SerializeObject(avatar); 
                
                object[] avatarObjects =
                {
                    entityId,
                    avatarId,
                    avatarInfo
                };
                
                await mFunc.CallAsync<int>(avatarObjects);

                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
        
            try
            {
                var web3 = new Web3();
                await web3.Personal.UnlockAccount.SendRequestAsync(_senderAddress, _password, 120);

                var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(_abi, _abiByteCode, _senderAddress);

                var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                while (receipt == null)
                {
                    await Task.Delay(5000);
                    receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                }

                var contractAddress = receipt.ContractAddress;
                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var mFunc = contract.GetFunction("CreateAvatarDetail");
                
                foreach (var holon in holons)
                {
                    // CreateHolon(uint entityId, string holonId, string info)

                    var entityId = new Random().Next(-90000, 90000);
                    var holonId = holon.Id.ToString();
                    var holonInfo = JsonConvert.SerializeObject(holon); 
                    
                    object[] holonObjects =
                    {
                        entityId,
                        holonId,
                        holonInfo
                    };
                    await mFunc.CallAsync<int>(holonObjects);
                }
                
                result.Result = holons;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var web3 = new Web3();
                web3.Personal.UnlockAccount.SendRequestAsync(_senderAddress, _password, 120)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                var transactionHash = web3.Eth.DeployContract.SendRequestAsync(_abi, _abiByteCode, _senderAddress)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                var receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                while (receipt == null)
                    receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                var contractAddress = receipt.ContractAddress;
                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var mFunc = contract.GetFunction("CreateAvatarDetail");

                // CreateHolon(uint entityId, string holonId, string info)
            
                var entityId = new Random().Next(-90000, 90000);
                var holonId = holon.Id.ToString();
                var holonInfo = JsonConvert.SerializeObject(holon); 

                object[] holonObjects =
                {
                    entityId,
                    holonId,
                    holonInfo
                };
                mFunc.CallAsync<int>(holonObjects)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                result.Result = holon;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            return result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var web3 = new Web3();
                await web3.Personal.UnlockAccount.SendRequestAsync(_senderAddress, _password, 120);

                var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(_abi, _abiByteCode, _senderAddress);

                var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                while (receipt == null)
                {
                    await Task.Delay(5000);
                    receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                }

                var contractAddress = receipt.ContractAddress;
                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var mFunc = contract.GetFunction("CreateAvatarDetail");
                
                // CreateHolon(uint entityId, string holonId, string info)
                
                var entityId = new Random().Next(-90000, 90000);
                var holonId = holon.Id.ToString();
                var holonInfo = JsonConvert.SerializeObject(holon); 
                
                object[] holonObjects =
                {
                    entityId,
                    holonId,
                    holonInfo
                };
                
                await mFunc.CallAsync<int>(holonObjects);

                result.Result = holon;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                var web3 = new Web3();
                web3.Personal.UnlockAccount.SendRequestAsync(_senderAddress, _password, 120)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                var transactionHash = web3.Eth.DeployContract.SendRequestAsync(_abi, _abiByteCode, _senderAddress)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                var receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                while (receipt == null)
                    receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                var contractAddress = receipt.ContractAddress;
                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var mFunc = contract.GetFunction("CreateAvatarDetail");
                foreach (var holon in holons)
                {
                    // CreateHolon(uint entityId, string holonId, string info)

                    var entityId = new Random().Next(-90000, 90000);
                    var holonId = holon.Id.ToString();
                    var holonInfo = JsonConvert.SerializeObject(holon);

                    object[] holonObjects =
                    {
                        entityId,
                        holonId,
                        holonInfo
                    };
                    mFunc.CallAsync<int>(holonObjects)
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                }

                result.Result = holons;
                result.IsError = false;
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }
            
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var web3 = new Web3();
                web3.Personal.UnlockAccount.SendRequestAsync(_senderAddress, _password, 120)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                var transactionHash = web3.Eth.DeployContract.SendRequestAsync(_abi, _abiByteCode, _senderAddress)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                var receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                while (receipt == null)
                    receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                var contractAddress = receipt.ContractAddress;
                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var mFunc = contract.GetFunction("CreateAvatar");
                
                // CreateAvatar(uint entityId, string avatarId, string info)

                var entityId = new Random().Next(-90000, 90000);
                var avatarId = avatar.AvatarId.ToString();
                var avatarInfo = JsonConvert.SerializeObject(avatar); 
                
                object[] avatarObjects =
                {
                    entityId,
                    avatarId,
                    avatarInfo
                };
                mFunc.CallAsync<int>(avatarObjects)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                result.Result = avatar;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.Message = ex.Message;
                result.IsError = true;
                result.IsSaved = false;
                result.Result = null;
                
                ErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public bool IsVersionControlEnabled { get; set; }
        public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }
    }
}