using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nethereum.Web3;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public class EthereumOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        private readonly string _hostUri;
        private readonly string _projectId;
        private readonly string _abi;
        private readonly string _abiByteCode;
        private readonly string _password;
        private readonly string _senderAddress;

        public EthereumOASIS(string hostUri, string projectId, string abi, string abiByteCode, string password, string senderAddress)
        {
            this.ProviderName = "EthereumOASIS";
            this.ProviderDescription = "Ethereum Provider";
            this.ProviderType = new Core.Helpers.EnumValue<ProviderType>(Core.Enums.ProviderType.EthereumOASIS);
            this.ProviderCategory = new Core.Helpers.EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _hostUri = hostUri;
            _projectId = projectId;
            _abi = abi;
            _abiByteCode = abiByteCode;
            _password = password;
            _senderAddress = senderAddress;
        }
        
        public IEnumerable<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }
        
        public override Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail LoadAvatarDetail(Guid id)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IAvatarDetail> LoadAllAvatarDetails()
        {
            throw new NotImplementedException();
        }

        public override async Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync()
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatarByEmail(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatarByUsername(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarAsync(Guid Id)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatar> LoadAvatarByEmailAsync(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatar> LoadAvatarByUsernameAsync(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarAsync(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatar> SaveAvatarAsync(IAvatar avatar)
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
            object[] avatarObjects = {
                avatar.AvatarId,
                avatar.Title,
                avatar.FirstName,
                avatar.LastName,
                avatar.FullName,
                avatar.Username,
                avatar.Email,
                avatar.Password,
                avatar.AvatarType,
                avatar.AcceptTerms,
                avatar.IsVerified,
                avatar.JwtToken,
                avatar.PasswordReset,
                avatar.RefreshToken,
                avatar.ResetToken,
                avatar.ResetTokenExpires,
                avatar.VerificationToken,
                avatar.Verified,
                avatar.LastBeamedIn,
                avatar.LastBeamedOut,
                avatar.IsBeamedIn,
                avatar.Image2D,
                avatar.Karma,
                avatar.Level,
                avatar.XP
            };
            await mFunc.CallAsync<int>(avatarObjects);
            return avatar;
        }

        public override IAvatarDetail SaveAvatarDetail(IAvatarDetail avatar)
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
            object[] avatarObjects = {
                avatar.Id,
                avatar.Title,
                avatar.FirstName,
                avatar.LastName,
                avatar.FullName,
                avatar.Username,
                avatar.Email,
                avatar.Address,
                avatar.Country,
                avatar.County,
                avatar.DOB,
                avatar.Image2D,
                avatar.Karma,
                avatar.Landline,
                avatar.Level,
                avatar.Mobile,
                avatar.Model3D,
                avatar.Postcode,
                avatar.Town,
                avatar.UmaJson,
                avatar.XP
            };
            mFunc.CallAsync<int>(avatarObjects)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            return avatar;
        }

        public override async Task<IAvatarDetail> SaveAvatarDetailAsync(IAvatarDetail avatar)
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
            object[] avatarObjects = {
                avatar.Id,
                avatar.Title,
                avatar.FirstName,
                avatar.LastName,
                avatar.FullName,
                avatar.Username,
                avatar.Email,
                avatar.Address,
                avatar.Country,
                avatar.County,
                avatar.DOB,
                avatar.Image2D,
                avatar.Karma,
                avatar.Landline,
                avatar.Level,
                avatar.Mobile,
                avatar.Model3D,
                avatar.Postcode,
                avatar.Town,
                avatar.UmaJson,
                avatar.XP
            };
            await mFunc.CallAsync<int>(avatarObjects);
            return avatar;
        }

        public override Task<ISearchResults> SearchAsync(ISearchParams searchTerm)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IAvatar> LoadAllAvatars()
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(Guid Id)
        {
            throw new NotImplementedException();
        }

        public override IAvatar SaveAvatar(IAvatar avatar)
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
                receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }

            var contractAddress = receipt.ContractAddress;
            var contract = web3.Eth.GetContract(_abi, contractAddress);
            var mFunc = contract.GetFunction("CreateAvatar");
            object[] avatarObjects = {
                avatar.AvatarId,
                avatar.Title,
                avatar.FirstName,
                avatar.LastName,
                avatar.FullName,
                avatar.Username,
                avatar.Email,
                avatar.Password,
                avatar.AvatarType,
                avatar.AcceptTerms,
                avatar.IsVerified,
                avatar.JwtToken,
                avatar.PasswordReset,
                avatar.RefreshToken,
                avatar.ResetToken,
                avatar.ResetTokenExpires,
                avatar.VerificationToken,
                avatar.Verified,
                avatar.LastBeamedIn,
                avatar.LastBeamedOut,
                avatar.IsBeamedIn,
                avatar.Image2D,
                avatar.Karma,
                avatar.Level,
                avatar.XP
            };
            mFunc.CallAsync<int>(avatarObjects)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            return avatar;
        }

        public override IAvatar LoadAvatar(string username)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatar(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override async Task<bool> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override IHolon LoadHolon(Guid id)
        {
            throw new NotImplementedException();
        }

        public override IHolon LoadHolon(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(string providerKey)
        {
            throw new NotImplementedException();
        }


        public override Task<IAvatar> LoadAvatarForProviderKeyAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatarForProviderKey(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatar(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteHolon(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteHolon(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.Holon)
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail LoadAvatarDetailByEmail(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail LoadAvatarDetailByUsername(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatarDetail> LoadAvatarDetailByUsernameAsync(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override async Task<IAvatarDetail> LoadAvatarDetailByEmailAsync(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildrenRecursive = true)
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
                receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }

            var contractAddress = receipt.ContractAddress;
            var contract = web3.Eth.GetContract(_abi, contractAddress);
            var mFunc = contract.GetFunction("CreateAvatarDetail");
            object[] holonObjects = {
                holon.Id,
                holon.ParentOmiverseId,
                holon.ParentMultiverseId,
                holon.ParentUniverseId,
                holon.ParentDimensionId,
                holon.ParentGalaxyClusterId,
                holon.ParentGalaxyId,
                holon.ParentSolarSystemId,
                holon.ParentGreatGrandSuperStarId,
                holon.ParentGrandSuperStarId,
                holon.ParentSuperStarId,
                holon.ParentStarId,
                holon.ParentPlanetId,
                holon.ParentMoonId,
                holon.ParentZomeId,
                holon.ParentHolonId
            };
            mFunc.CallAsync<int>(holonObjects)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            return new OASISResult<IHolon>(holon);
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildrenRecursive = true)
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
            object[] holonObjects = {
                holon.Id,
                holon.ParentOmiverseId,
                holon.ParentMultiverseId,
                holon.ParentUniverseId,
                holon.ParentDimensionId,
                holon.ParentGalaxyClusterId,
                holon.ParentGalaxyId,
                holon.ParentSolarSystemId,
                holon.ParentGreatGrandSuperStarId,
                holon.ParentGrandSuperStarId,
                holon.ParentSuperStarId,
                holon.ParentStarId,
                holon.ParentPlanetId,
                holon.ParentMoonId,
                holon.ParentZomeId,
                holon.ParentHolonId
            };
            await mFunc.CallAsync<int>(holonObjects);
            return new OASISResult<IHolon>(holon);
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true)
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
                receipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }

            var contractAddress = receipt.ContractAddress;
            var contract = web3.Eth.GetContract(_abi, contractAddress);
            var mFunc = contract.GetFunction("CreateAvatarDetail");
            foreach (var holon in holons)
            {
                object[] holonObjects = {
                    holon.Id,
                    holon.ParentOmiverseId,
                    holon.ParentMultiverseId,
                    holon.ParentUniverseId,
                    holon.ParentDimensionId,
                    holon.ParentGalaxyClusterId,
                    holon.ParentGalaxyId,
                    holon.ParentSolarSystemId,
                    holon.ParentGreatGrandSuperStarId,
                    holon.ParentGrandSuperStarId,
                    holon.ParentSuperStarId,
                    holon.ParentStarId,
                    holon.ParentPlanetId,
                    holon.ParentMoonId,
                    holon.ParentZomeId,
                    holon.ParentHolonId
                };
                mFunc.CallAsync<int>(holonObjects)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
            return new OASISResult<IEnumerable<IHolon>>(holons);
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true)
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
                object[] holonObjects = {
                    holon.Id,
                    holon.ParentOmiverseId,
                    holon.ParentMultiverseId,
                    holon.ParentUniverseId,
                    holon.ParentDimensionId,
                    holon.ParentGalaxyClusterId,
                    holon.ParentGalaxyId,
                    holon.ParentSolarSystemId,
                    holon.ParentGreatGrandSuperStarId,
                    holon.ParentGrandSuperStarId,
                    holon.ParentSuperStarId,
                    holon.ParentStarId,
                    holon.ParentPlanetId,
                    holon.ParentMoonId,
                    holon.ParentZomeId,
                    holon.ParentHolonId
                };
                await mFunc.CallAsync<int>(holonObjects);
            }
            return new OASISResult<IEnumerable<IHolon>>(holons);
        }
    }
}
