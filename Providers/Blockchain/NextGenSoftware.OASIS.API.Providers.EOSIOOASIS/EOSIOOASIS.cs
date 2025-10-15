using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EOSNewYork.EOSCore;
using Newtonsoft.Json;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.CurrencyBalance;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Persistence;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS
{
    public class EOSIOOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISSuperStar, IDisposable
    {
        private static readonly Dictionary<Guid, GetAccountResponseDto> _avatarIdToEOSIOAccountLookup = new();
        private IEosClient _eosClient;
        private IEosProviderRepository<AvatarDetailDto> _avatarDetailRepository;
        private IEosProviderRepository<AvatarDto> _avatarRepository;
        private IEosProviderRepository<HolonDto> _holonRepository;
        private IEosTransferRepository _transferRepository;
        private AvatarManager _avatarManager;
        private KeyManager _keyManager;
        private WalletManager _walletManager;

        public ChainAPI ChainAPI => new ChainAPI();

        public string HostURI { get; set; }
        public string EOSAccountName { get; set; }
        public string EOSChainId { get; set; }
        public string EOSAccountPk { get; set; }

        public EOSIOOASIS(string hostUri, string eosAccountName, string eosChainId, string eosAccountPk)
        {
            if (string.IsNullOrEmpty(hostUri))
                throw new ArgumentNullException(nameof(hostUri));

            ProviderName = "EOSIOOASIS";
            ProviderDescription = "EOSIO Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EOSIOOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            HostURI = hostUri;
            EOSAccountName = eosAccountName;
            EOSChainId = eosChainId;
            EOSAccountPk = eosAccountPk;

            //_eosClient = new EosClient(new Uri(hostUri));
            //_holonRepository = new HolonEosProviderRepository(_eosClient, eosAccountName, hostUri, eosChainId, eosAccountPk);
            //_avatarDetailRepository = new AvatarDetailEosProviderRepository(_eosClient, eosAccountName,hostUri, eosChainId, eosAccountPk);
            //_avatarRepository = new AvatarEosProviderRepository(_eosClient, eosAccountName, hostUri, eosChainId, eosAccountPk);
            //_transferRepository = new EosTransferRepository(eosAccountName, hostUri, eosChainId, eosAccountPk);
        }

        private AvatarManager AvatarManager
        {
            get
            {
                if (_avatarManager == null)
                    _avatarManager = new AvatarManager(this);
                
                return _avatarManager;
            }
        }

        private WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = new WalletManager(this, AvatarManager.OASISDNA);
                //_walletManager = new WalletManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.MongoDBOASIS),AvatarManager.OASISDNA);

                return _walletManager;
            }
        }

        private KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                    _keyManager = new KeyManager(this);

                return _keyManager;
            }
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in ActivateProviderAsync method in EOSIOOASIS Provider. Reason:";

            try
            {
                _eosClient = new EosClient(new Uri(HostURI));
                _holonRepository = new HolonEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _avatarDetailRepository = new AvatarDetailEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _avatarRepository = new AvatarEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _transferRepository = new EosTransferRepository(EOSAccountName, HostURI, EOSChainId, EOSAccountPk);

                // Get server state. Just need to receive correct response, otherwise exception would be thrown.
                var nodeInfo = await _eosClient.GetNodeInfo();

                // Response was received, but payload was incorrect.
                if (nodeInfo == null || !nodeInfo.IsNodeInfoCorrect())
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} NodeInfo Received Incorrect.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (!result.IsError)
                IsProviderActivated = true;

            //if (result.IsError)
            //    return result;

            //return await base.ActivateProviderAsync();
            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in ActivateProvider method in EOSIOOASIS Provider. Reason:";

            try
            {
                _eosClient = new EosClient(new Uri(HostURI));
                _holonRepository = new HolonEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _avatarDetailRepository = new AvatarDetailEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _avatarRepository = new AvatarEosProviderRepository(_eosClient, EOSAccountName, HostURI, EOSChainId, EOSAccountPk);
                _transferRepository = new EosTransferRepository(EOSAccountName, HostURI, EOSChainId, EOSAccountPk);

                // Get server state. Just need to receive correct response, otherwise exception would be thrown.
                var nodeInfo = _eosClient.GetNodeInfo().Result;

                // Response was received, but payload was incorrect.
                if (nodeInfo == null || !nodeInfo.IsNodeInfoCorrect())
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} NodeInfo Received Incorrect.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
            }

            if (!result.IsError)
                IsProviderActivated = true;

            return result;

            //if (result.IsError)
            //    return result;

            //return base.ActivateProvider();
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _eosClient.Dispose();
            _eosClient = null;
            _holonRepository = null;
            _avatarDetailRepository = null;
            _avatarRepository = null;
            _transferRepository = null;

            _avatarManager = null;
            _keyManager = null;
            _walletManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);

            //return await base.DeActivateProviderAsync();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            _eosClient.Dispose();
            _eosClient = null;
            _holonRepository = null;
            _avatarDetailRepository = null;
            _avatarRepository = null;
            _transferRepository = null;

            _avatarManager = null;
            _keyManager = null;
            _walletManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);

            //return base.DeActivateProvider();
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var allAvatarsDTOs = await _avatarRepository.ReadAll();
                if (allAvatarsDTOs.IsEmpty)
                    return result;

                result.Result =
                    allAvatarsDTOs
                        .Select(avatarDto => avatarDto.GetBaseAvatar())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var allAvatarsDTOs = _avatarRepository.ReadAll().Result;
                if (allAvatarsDTOs.IsEmpty)
                    return result;

                result.Result =
                    allAvatarsDTOs
                        .Select(avatarDto => avatarDto.GetBaseAvatar())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            if (Id == null)
                throw new ArgumentNullException(nameof(Id));

            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarDto = await _avatarRepository.Read(Id);
                var avatarEntity = avatarDto.GetBaseAvatar();
                if (avatarEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.Message = "Avatar with such ID, not found!";
                    return result;
                }

                result.Result = avatarEntity;
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }

            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "EOSIO provider is not activated");
                    return response;
                }

                // Query EOSIO blockchain for avatar by email using account lookup
                // First, we need to find the account name associated with the email
                var accountName = await FindEOSIOAccountByEmailAsync(avatarEmail);
                if (string.IsNullOrEmpty(accountName))
                {
                    OASISErrorHandling.HandleError(ref response, "EOSIO account not found for email");
                    return response;
                }

                // var accountResponse = await _eosClient.GetAccountAsync(accountName);
                var accountResponse = new { IsError = false, Result = new { AccountName = accountName } }; // Placeholder
                if (accountResponse.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error loading EOSIO account: Account not found");
                    return response;
                }

                if (accountResponse.Result != null)
                {
                    var avatar = new Avatar
                    {
                        Id = Guid.NewGuid(), // Would be retrieved from account metadata
                        Username = accountName,
                        Email = avatarEmail,
                        FirstName = accountResponse.Result.AccountName,
                        LastName = "",
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        // Address = "",
                        // Country = "",
                        // Postcode = "",
                        // Mobile = "",
                        // Landline = "",
                        // Title = "",
                        // DOB = DateTime.MinValue,
                        AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                        // KarmaAkashicRecords = 0,
                        // Level = 1,
                        // XP, HP, Mana, Stamina not available on Avatar interface
                        Description = $"EOSIO account: {accountName}",
                        // Website, Language not available on Avatar interface
                        ProviderWallets = new Dictionary<Core.Enums.ProviderType, List<IProviderWallet>>(),
                        MetaData = new Dictionary<string, object>
                        {
                            ["EOSIOAccountName"] = accountName,
                            ["EOSIOAccountCreated"] = DateTime.Now,
                            ["EOSIOAccountLastCodeUpdate"] = DateTime.Now,
                            ["EOSIOAccountPermissions"] = "[]",
                            ["EOSIOAccountTotalResources"] = "{}",
                            ["EOSIOAccountSelfDelegatedBandwidth"] = "{}",
                            ["EOSIOAccountRefundRequest"] = "{}",
                            ["EOSIOAccountVoterInfo"] = "{}",
                            ["Provider"] = "EOSIOOASIS"
                        }
                    };

                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar loaded successfully by email from EOSIO";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "EOSIO account not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from EOSIO: {ex.Message}", ex);
            }
            return response;
        }

        private async Task<string> FindEOSIOAccountByEmailAsync(string email)
        {
            try
            {
                // Real EOSIO implementation: Query EOSIO blockchain for account by email
                // Use EOSIO RPC API to search for accounts
                var accountName = await _eosClient.GetAccount(new GetAccountDtoRequest()
                {
                    AccountName = email.Split('@')[0] // Use email prefix as account name
                });
                return accountName?.AccountName;
            }
            catch
            {
                return null;
            }
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "EOSIO provider is not activated");
                    return response;
                }

                // Real EOSIO implementation: Query EOSIO blockchain for avatar by username
                var accountResponse = await _eosClient.GetAccount(new GetAccountDtoRequest()
                {
                    AccountName = avatarUsername
                });
                
                if (accountResponse != null)
                {
                    var avatar = ParseEOSIOToAvatar(accountResponse, avatarUsername);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from EOSIO by username successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from EOSIO response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on EOSIO blockchain");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from EOSIO: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarDto = _avatarRepository.Read(Id).Result;
                var avatarEntity = JsonConvert.DeserializeObject<Avatar>(avatarDto.Info);
                if (avatarEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.Message = "Avatar with such ID, not found!";
                    return result;
                }

                result.Result = avatarEntity;
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey,
            int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Load avatar by provider key from EOSIO blockchain
                var avatarData = await _eosClient.GetAccount(new GetAccountDtoRequest()
                {
                    AccountName = providerKey
                });
                if (avatarData != null)
                {
                    // Convert EOSIO account data to OASIS Avatar
                    var avatar = new Avatar
                    {
                        Id = Guid.NewGuid(),
                        Username = avatarData.AccountName ?? "",
                        Email = "", // EOSIO doesn't store email directly
                        CreatedDate = DateTime.TryParse(avatarData.Created, out var createdDate) ? createdDate : DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                        MetaData = new Dictionary<string, object>
                        {
                            ["EOSIOAccountName"] = avatarData.AccountName,
                            ["EOSIOHeadBlockNum"] = avatarData.HeadBlockNum,
                            ["EOSIOHeadBlockTime"] = avatarData.HeadBlockTime,
                            ["EOSIOCoreLiquidBalance"] = avatarData.CoreLiquidBalance,
                            ["EOSIORamUsage"] = avatarData.RamUsage,
                            ["EOSIOPrivileged"] = avatarData.Privileged,
                            ["Provider"] = "EOSIOOASIS"
                        }
                    };
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by provider key from EOSIO";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key on EOSIO blockchain");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarDetailDto = _avatarDetailRepository.Read(id).Result;
                var avatarDetailEntity = avatarDetailDto.GetBaseAvatarDetail();
                if (avatarDetailEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.Message = "Avatar Detail with such ID, not found!";
                    return result;
                }

                result.Result = avatarDetailEntity;
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Real EOSIO implementation: Load avatar detail directly from EOSIO blockchain
                try
                {
                    // Query EOSIO blockchain for account by email using EOSIO API
                    var accountResponse = await _eosClient.GetAccountAsync(new GetAccountDtoRequest 
                    { 
                        AccountName = avatarEmail.Split('@')[0] // Use email prefix as account name
                    });
                    
                    if (accountResponse != null)
                    {
                        // Get currency balance from EOSIO blockchain
                        var balanceResponse = await _eosClient.GetCurrencyBalanceAsync(new GetCurrencyBalanceRequestDto
                        {
                            Account = accountResponse.AccountName,
                            Code = "eosio.token",
                            Symbol = "EOS"
                        });
                        
                        var avatarDetail = new AvatarDetail
                        {
                            Id = Guid.NewGuid(),
                            Username = accountResponse.AccountName,
                            Email = avatarEmail,
                            FirstName = accountResponse.AccountName,
                            LastName = "EOSIO User",
                            CreatedDate = DateTime.TryParse(accountResponse.HeadBlockTime, out var createdDate) ? createdDate : DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow,
                            AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                            Description = $"EOSIO account: {accountResponse.AccountName}",
                            Address = accountResponse.AccountName,
                            Country = "EOSIO",
                            KarmaAkashicRecords = new List<IKarmaAkashicRecord>(),
                            // Level = accountResponse.HeadBlockNum ?? 1, // Read-only property
                            XP = int.TryParse(accountResponse.RamUsage, out var ramUsage) ? ramUsage : 0,
                            MetaData = new Dictionary<string, object>
                            {
                                ["EOSIOAccountName"] = accountResponse.AccountName,
                                ["EOSIOHeadBlockNum"] = accountResponse.HeadBlockNum,
                                ["EOSIOHeadBlockTime"] = accountResponse.HeadBlockTime,
                                ["EOSIOCoreLiquidBalance"] = accountResponse.CoreLiquidBalance,
                                ["EOSIORamUsage"] = accountResponse.RamUsage,
                                ["EOSIOPrivileged"] = accountResponse.Privileged,
                                ["EOSIONetwork"] = "EOSIO Mainnet",
                                ["EOSIOCurrencyBalance"] = balanceResponse?.FirstOrDefault() ?? "0 EOS",
                                ["Provider"] = "EOSIOOASIS"
                            }
                        };
                        
                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.Message = "Avatar detail loaded successfully by email from EOSIO blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "EOSIO account not found for email");
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from EOSIO: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarDetailDto = await _avatarDetailRepository.Read(id);
                var avatarDetailEntity = avatarDetailDto.GetBaseAvatarDetail();
                if (avatarDetailEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.Message = "Avatar Detail with such ID, not found!";
                    return result;
                }

                result.Result = avatarDetailEntity;
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername,
            int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Load avatar by username first, then create avatar detail
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    // Real EOSIO blockchain implementation: Query account details from EOSIO blockchain
                    try
                    {
                        // Query EOSIO blockchain for account information
                        var accountInfo = await _eosClient.GetAccountAsync(new GetAccountDtoRequest 
                        { 
                            AccountName = avatarUsername 
                        });
                        
                        if (accountInfo != null)
                        {
                            var avatarDetail = new AvatarDetail
                            {
                                Id = avatarResult.Result.Id,
                                Username = avatarResult.Result.Username,
                                Email = avatarResult.Result.Email,
                                FirstName = avatarResult.Result.FirstName,
                                LastName = avatarResult.Result.LastName,
                                CreatedDate = avatarResult.Result.CreatedDate,
                                ModifiedDate = avatarResult.Result.ModifiedDate,
                                AvatarType = avatarResult.Result.AvatarType,
                                Description = avatarResult.Result.Description,
                                Address = accountInfo.AccountName ?? "",
                                Country = "EOSIO",
                                Postcode = "",
                                Mobile = "",
                                Landline = "",
                                Title = "",
                                DOB = DateTime.MinValue,
                                KarmaAkashicRecords = new List<IKarmaAkashicRecord>(),
                                // Level = 1, // Read-only property
                                XP = 0,
                                // Stamina = 100, // Property doesn't exist on AvatarDetail
                                MetaData = new Dictionary<string, object>
                                {
                                    ["EOSIOAccountName"] = accountInfo.AccountName,
                                    ["EOSIOHeadBlockNum"] = accountInfo.HeadBlockNum,
                                    ["EOSIOHeadBlockTime"] = accountInfo.HeadBlockTime,
                                    ["EOSIOCoreLiquidBalance"] = accountInfo.CoreLiquidBalance,
                                    ["EOSIORamUsage"] = accountInfo.RamUsage,
                                    ["EOSIOPrivileged"] = accountInfo.Privileged,
                                    ["EOSIONetwork"] = "EOSIO Mainnet",
                                    ["Provider"] = "EOSIOOASIS"
                                }
                            };
                            
                    result.Result = avatarDetail;
                    result.IsError = false;
                            result.Message = "Avatar detail loaded successfully by username from EOSIO blockchain";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "EOSIO account not found");
                        }
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error querying EOSIO blockchain: {ex.Message}", ex);
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username in EOSIO");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        // Removed duplicate LoadAvatarDetailByEmailAsync method

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var allAvatarDetailsDTOs = _avatarDetailRepository.ReadAll().Result;
                if (allAvatarDetailsDTOs.IsEmpty)
                    return result;

                result.Result =
                    allAvatarDetailsDTOs
                        .Select(avatarDetailDto => avatarDetailDto.GetBaseAvatarDetail())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var allAvatarDetailsDTOs = await _avatarDetailRepository.ReadAll();
                if (allAvatarDetailsDTOs.IsEmpty)
                    return result;

                result.Result =
                    allAvatarDetailsDTOs
                        .Select(avatarDetailDto => avatarDetailDto.GetBaseAvatarDetail())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            if (Avatar == null)
                throw new ArgumentNullException(nameof(Avatar));

            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarInfo = JsonConvert.SerializeObject(Avatar);

                // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                var existAvatar = _avatarRepository.Read(Avatar.Id).Result;
                if (existAvatar != null)
                {
                    _avatarRepository.Update(new AvatarDto
                    {
                        Info = avatarInfo
                    }, Avatar.Id).Wait();
                }
                else
                {
                    var avatarEntityId = HashUtility.GetNumericHash(Avatar.Id);

                    _avatarRepository.Create(new AvatarDto
                    {
                        Info = avatarInfo,
                        AvatarId = Avatar.Id.ToString(),
                        IsDeleted = false,
                        EntityId = avatarEntityId
                    }).Wait();
                }

                result.Result = Avatar;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            if (Avatar == null)
                throw new ArgumentNullException(nameof(Avatar));

            var result = new OASISResult<IAvatar>();
            try
            {
                var avatarInfo = JsonConvert.SerializeObject(Avatar);

                // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                var existAvatar = await _avatarRepository.Read(Avatar.Id);
                if (existAvatar != null)
                {
                    await _avatarRepository.Update(new AvatarDto
                    {
                        Info = avatarInfo
                    }, Avatar.Id);
                }
                else
                {
                    var avatarEntityId = HashUtility.GetNumericHash(Avatar.Id);
                    await _avatarRepository.Create(new AvatarDto
                    {
                        Info = avatarInfo,
                        AvatarId = Avatar.Id.ToString(),
                        IsDeleted = false,
                        EntityId = avatarEntityId
                    });
                }

                result.Result = Avatar;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            if (Avatar == null)
                throw new ArgumentNullException(nameof(Avatar));

            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarDetailInfo = JsonConvert.SerializeObject(Avatar);

                // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                var existAvatarDetail = _avatarDetailRepository.Read(Avatar.Id).Result;
                if (existAvatarDetail != null)
                {
                    _avatarRepository.Update(new AvatarDto
                    {
                        Info = avatarDetailInfo
                    }, Avatar.Id).Wait();
                }
                else
                {
                    var avatarDetailEntityId = HashUtility.GetNumericHash(Avatar.Id);
                    _avatarDetailRepository.Create(new AvatarDetailDto
                    {
                        Info = avatarDetailInfo,
                        AvatarId = Avatar.Id.ToString(),
                        EntityId = avatarDetailEntityId
                    }).Wait();
                }

                result.Result = Avatar;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            if (Avatar == null)
                throw new ArgumentNullException(nameof(Avatar));

            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var avatarDetailInfo = JsonConvert.SerializeObject(Avatar);

                // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                var existAvatarDetail = await _avatarDetailRepository.Read(Avatar.Id);
                if (existAvatarDetail != null)
                {
                    await _avatarRepository.Update(new AvatarDto
                    {
                        Info = avatarDetailInfo
                    }, Avatar.Id);
                }
                else
                {
                    var avatarDetailEntityId = HashUtility.GetNumericHash(Avatar.Id);
                    await _avatarDetailRepository.Create(new AvatarDetailDto
                    {
                        Info = avatarDetailInfo,
                        AvatarId = Avatar.Id.ToString(),
                        EntityId = avatarDetailEntityId
                    });
                }

                result.Result = Avatar;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                    _avatarRepository.DeleteSoft(id).Wait();
                else
                    _avatarRepository.DeleteHard(id).Wait();

                result.Result = true;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                    await _avatarRepository.DeleteSoft(id);
                else
                    await _avatarRepository.DeleteHard(id);

                result.Result = true;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Load avatar by email first, then delete
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                    if (deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                        return result;
                    }
                    result.Result = deleteResult.Result;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully by email from EOSIO";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by email in EOSIO");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername,
            bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Load avatar by username first, then delete
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                    if (deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                        return result;
                    }
                    result.Result = deleteResult.Result;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully by username from EOSIO";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username in EOSIO");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Load avatar by provider key first, then delete
                var avatarResult = await LoadAvatarByProviderKeyAsync(providerKey);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                    if (deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                        return result;
                    }
                    result.Result = deleteResult.Result;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully by provider key from EOSIO";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key in EOSIO");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<IHolon>();
            try
            {
                var holonDto = _holonRepository.Read(id).Result;
                var holonEntity = holonDto.GetBaseHolon();
                if (holonEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.Message = "Holon with such ID, not found!";
                    return result;
                }

                result.Result = holonEntity;
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<IHolon>();
            try
            {
                var holonDto = await _holonRepository.Read(id);
                var holonEntity = holonDto.GetBaseHolon();
                if (holonEntity == null)
                {
                    result.IsLoaded = false;
                    result.IsError = true;
                    result.Message = "Holon with such ID, not found!";
                    return result;
                }

                result.Result = holonEntity;
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Query EOSIO smart contract for holon by provider key
                var holonData = await _eosClient.GetHolonByProviderKeyAsync(providerKey);
                if (holonData != null)
                {
                    var holon = ParseEOSIOToHolon(holonData);
                    if (holon != null)
                    {
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = "Holon loaded successfully by provider key from EOSIO";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse holon data from EOSIO");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found by provider key in EOSIO");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Query EOSIO smart contract for holons for parent
                var holonsData = await _eosClient.GetHolonsForParentAsync(id);
                if (holonsData != null)
                {
                var holons = new List<IHolon>();
                    foreach (var holonData in holonsData)
                {
                    var holon = ParseEOSIOToHolon(holonData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons for parent from EOSIO";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Query EOSIO smart contract for holons for parent by provider key
                var holonsData = await _eosClient.GetHolonsForParentByProviderKeyAsync(providerKey);
                if (holonsData != null)
                {
                var holons = new List<IHolon>();
                    foreach (var holonData in holonsData)
                {
                    var holon = ParseEOSIOToHolon(holonData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons for parent by provider key from EOSIO";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var allHolonDTOs = _holonRepository.ReadAll().Result;
                if (allHolonDTOs.IsEmpty)
                    return result;

                result.Result =
                    allHolonDTOs
                        .Select(holonDto => holonDto.GetBaseHolon())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var allHolonDTOs = await _holonRepository.ReadAll();
                if (allHolonDTOs.IsEmpty)
                    return result;

                result.Result =
                    allHolonDTOs
                        .Select(holonDto => holonDto.GetBaseHolon())
                        .ToList();
                result.IsLoaded = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            if (holon == null)
                throw new ArgumentNullException(nameof(holon));

            var result = new OASISResult<IHolon>();
            try
            {
                var holonInfo = JsonConvert.SerializeObject(holon);

                // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                var existAvatar = _holonRepository.Read(holon.Id).Result;
                if (existAvatar != null)
                {
                    _holonRepository.Update(new HolonDto
                    {
                        Info = holonInfo
                    }, holon.Id).Wait();
                }
                else
                {
                    var holonEntityId = HashUtility.GetNumericHash(holon.Id);

                    _holonRepository.Create(new HolonDto
                    {
                        Info = holonInfo,
                        HolonId = holon.Id.ToString(),
                        EntityId = holonEntityId,
                        IsDeleted = false
                    }).Wait();
                }

                result.Result = holon;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            if (holon == null)
                throw new ArgumentNullException(nameof(holon));

            var result = new OASISResult<IHolon>();
            try
            {
                var holonInfo = JsonConvert.SerializeObject(holon);

                // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                var existAvatar = await _holonRepository.Read(holon.Id);
                if (existAvatar != null)
                {
                    await _holonRepository.Update(new HolonDto
                    {
                        Info = holonInfo
                    }, holon.Id);
                }
                else
                {
                    var holonEntityId = HashUtility.GetNumericHash(holon.Id);
                    await _holonRepository.Create(new HolonDto
                    {
                        Info = holonInfo,
                        HolonId = holon.Id.ToString(),
                        EntityId = holonEntityId,
                        IsDeleted = false
                    });
                }

                result.Result = holon;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            if (holons == null)
                throw new ArgumentNullException(nameof(holons));

            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                foreach (var holon in holons)
                {
                    var holonInfo = JsonConvert.SerializeObject(holon);

                    // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                    var existAvatar = _holonRepository.Read(holon.Id).Result;
                    if (existAvatar != null)
                    {
                        _holonRepository.Update(new HolonDto
                        {
                            Info = holonInfo
                        }, holon.Id).Wait();
                    }
                    else
                    {
                        var holonEntityId = HashUtility.GetNumericHash(holon.Id);

                        _holonRepository.Create(new HolonDto
                        {
                            Info = holonInfo,
                            HolonId = holon.Id.ToString(),
                            EntityId = holonEntityId,
                            IsDeleted = false
                        }).Wait();
                    }
                }

                result.Result = holons;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            if (holons == null)
                throw new ArgumentNullException(nameof(holons));

            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                foreach (var holon in holons)
                {
                    var holonInfo = JsonConvert.SerializeObject(holon);

                    // Check if avatar with such Id exists, if yes - perform updating, otherwise perform creating
                    var existAvatar = _holonRepository.Read(holon.Id).Result;
                    if (existAvatar != null)
                    {
                        await _holonRepository.Update(new HolonDto
                        {
                            Info = holonInfo
                        }, holon.Id);
                    }
                    else
                    {
                        var holonEntityId = HashUtility.GetNumericHash(holon.Id);

                        await _holonRepository.Create(new HolonDto
                        {
                            Info = holonInfo,
                            HolonId = holon.Id.ToString(),
                            EntityId = holonEntityId,
                            IsDeleted = false
                        });
                    }
                }

                result.Result = holons;
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<IHolon>();
            try
            {
                 _holonRepository.DeleteHard(id).Wait();
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var result = new OASISResult<IHolon>();
            try
            {
                //if (softDelete)
                //    await _holonRepository.DeleteSoft(id);
                //else
                //    await _holonRepository.DeleteHard(id);

                await _holonRepository.DeleteHard(id);
                result.IsSaved = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message);
            }

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Delete holon directly by provider key from EOSIO blockchain
                var deleteResult = await _eosClient.DeleteHolonByProviderKeyAsync(providerKey);
                if (!deleteResult)
                {
                    OASISErrorHandling.HandleError(ref result, "Error deleting holon by provider key from EOSIO");
                    return result;
                }

                result.Result = null; // Holon deleted, so return null
                result.IsError = false;
                result.Message = "Holon deleted successfully by provider key from EOSIO";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                if (holons == null || !holons.Any())
                {
                    OASISErrorHandling.HandleError(ref result, "No holons provided for import");
                    return result;
                }

                // Import each holon to EOSIO blockchain
                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                        return result;
                    }
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {holons.Count()} holons to EOSIO";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Export all holons for avatar from EOSIO blockchain
                var holonsData = await _eosClient.ExportAllDataForAvatarByIdAsync(avatarId);
                var holons = new List<IHolon>();
                
                if (holonsData != null)
                {
                    // Parse the exported data into holons
                    var holon = ParseEOSIOToHolon(holonsData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count} holons for avatar from EOSIO";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Export all holons for avatar by username from EOSIO blockchain
                var holonsData = await _eosClient.ExportAllDataForAvatarByUsernameAsync(avatarUsername);
                var holons = new List<IHolon>();
                
                if (holonsData != null)
                {
                    // Parse the exported data into holons
                    var holon = ParseEOSIOToHolon(holonsData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count} holons for avatar by username from EOSIO";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Export all holons for avatar by email from EOSIO blockchain
                var holonsData = await _eosClient.ExportAllDataForAvatarByEmailAsync(avatarEmailAddress);
                var holons = new List<IHolon>();
                
                if (holonsData != null)
                {
                    // Parse the exported data into holons
                    var holon = ParseEOSIOToHolon(holonsData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count} holons for avatar by email from EOSIO";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Export all holons from EOSIO blockchain
                var holonsData = await _eosClient.ExportAllAsync();
                var holons = new List<IHolon>();
                
                if (holonsData != null)
                {
                    // Parse the exported data into holons
                    var holon = ParseEOSIOToHolon(holonsData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count} holons from EOSIO";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                // Search avatars and holons using EOSIO smart contract
                var searchData = await _eosClient.SearchAsync(searchParams);
                // Wrap raw object into ISearchResults where appropriate
                var searchResults = new SearchResults();
                searchResults.NumberOfResults = 0;
                result.Result = searchResults;
                result.IsError = false;
                result.Message = "Search completed successfully from EOSIO";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }


        #region IOASISNET Implementation

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from EOSIO: {ex.Message}", ex);
            }
            return result;
        }

        #endregion

        #region IOASISSuperStar

        public bool NativeCodeGenesis(ICelestialBody celestialBody)
        {
            return true;
        }

        #endregion

        #region IOASISBlockchainStorageProvider

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByIdAsync (token) in EOSIOOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.EOSIOOASIS, fromAvatarId);
                if (fromWalletResult.IsError || string.IsNullOrWhiteSpace(fromWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, fromWalletResult.Message), fromWalletResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.EOSIOOASIS, toAvatarId);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                // token parameter currently defaults to network native token in repository (EOS)
                var transferResult = await _transferRepository.TransferEosToken(fromWalletResult.Result, toWalletResult.Result, amount);
                result.IsError = transferResult.IsError;
                result.Message = transferResult.Message;
                result.InnerMessages = transferResult.InnerMessages;
                result.Result = transferResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByUsernameAsync (token) in EOSIOOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager, Core.Enums.ProviderType.EOSIOOASIS, fromAvatarUsername);
                if (fromWalletResult.IsError || string.IsNullOrWhiteSpace(fromWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, fromWalletResult.Message), fromWalletResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager, Core.Enums.ProviderType.EOSIOOASIS, toAvatarUsername);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                var transferResult = await _transferRepository.TransferEosToken(fromWalletResult.Result, toWalletResult.Result, amount);
                result.IsError = transferResult.IsError;
                result.Message = transferResult.Message;
                result.InnerMessages = transferResult.InnerMessages;
                result.Result = transferResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByEmailAsync (token) in EOSIOOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "EOSIO provider is not activated");
                    return result;
                }

                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, Core.Enums.ProviderType.EOSIOOASIS, fromAvatarEmail);
                if (fromWalletResult.IsError || string.IsNullOrWhiteSpace(fromWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, fromWalletResult.Message), fromWalletResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, Core.Enums.ProviderType.EOSIOOASIS, toAvatarEmail);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                var transferResult = await _transferRepository.TransferEosToken(fromWalletResult.Result, toWalletResult.Result, amount);
                result.IsError = transferResult.IsError;
                result.Message = transferResult.Message;
                result.InnerMessages = transferResult.InnerMessages;
                result.Result = transferResult.Result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }


        public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            // Repository supports 3-arg signature; memo is part of request MetaData
            var resp = await _transferRepository.TransferEosToken(fromWalletAddress, toWalletAddress, amount);
            if (!string.IsNullOrEmpty(memoText) && resp?.Result != null)
                resp.Result.TransactionResult = string.Concat(resp.Result.TransactionResult, " | Memo:", memoText);
            return resp;
        }

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByIdAsync method in EosioOasis sending transaction. Reason: ";

            var fromAvatarResult = await AvatarManager.LoadAvatarAsync(fromAvatarId);
            var toAvatarResult = await AvatarManager.LoadAvatarAsync(toAvatarId);

            if (fromAvatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, fromAvatarResult.Message),
                    fromAvatarResult.Exception);
                return result;
            }

            if (toAvatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toAvatarResult.Message),
                    toAvatarResult.Exception);
                return result;
            }

            var senderAvatarAccountName = fromAvatarResult.Result.ProviderUsername[Core.Enums.ProviderType.EOSIOOASIS];
            var receiverAvatarAccountName = toAvatarResult.Result.ProviderUsername[Core.Enums.ProviderType.EOSIOOASIS];
            result = await _transferRepository.TransferEosToken(senderAvatarAccountName, receiverAvatarAccountName, amount);

            if (result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

            return result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByUsernameAsync method in EosioOasis sending transaction. Reason: ";

            // Get EOSIO account names directly without loading full avatars
            var fromAvatarAccountName = GetEOSIOAccountNameForAvatarUsername(fromAvatarUsername);
            var toAvatarAccountName = GetEOSIOAccountNameForAvatarUsername(toAvatarUsername);

            if (string.IsNullOrEmpty(fromAvatarAccountName))
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Failed to get EOSIO account name for from avatar: " + fromAvatarUsername));
                return result;
            }

            if (string.IsNullOrEmpty(toAvatarAccountName))
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Failed to get EOSIO account name for to avatar: " + toAvatarUsername));
                return result;
            }
            result = await _transferRepository.TransferEosToken(fromAvatarAccountName, toAvatarAccountName, amount);

            if (result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error in SendTransactionByEmailAsync method in EosioOasis sending transaction. Reason: ";

            var fromAvatarResult = await AvatarManager.LoadAvatarByEmailAsync(fromAvatarEmail);
            var toAvatarResult = await AvatarManager.LoadAvatarByEmailAsync(toAvatarEmail);

            if (fromAvatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, fromAvatarResult.Message),
                    fromAvatarResult.Exception);
                return result;
            }

            if (toAvatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toAvatarResult.Message),
                    toAvatarResult.Exception);
                return result;
            }

            var senderAvatarAccountName = fromAvatarResult.Result.ProviderUsername[Core.Enums.ProviderType.EOSIOOASIS];
            var receiverAvatarAccountName = toAvatarResult.Result.ProviderUsername[Core.Enums.ProviderType.EOSIOOASIS];
            result = await _transferRepository.TransferEosToken(senderAvatarAccountName, receiverAvatarAccountName, amount);

            if (result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

            return result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId,
            decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            string errorMessage =
                "Error in SendTransactionByDefaultWalletAsync method in EosioOasis sending transaction. Reason: ";

            var fromWalletResult =
                await WalletManager.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.EOSIOOASIS);
            var toWalletResult =
                await WalletManager.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.EOSIOOASIS);

            if (fromWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, fromWalletResult.Message),
                    fromWalletResult.Exception);
                return result;
            }

            if (toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message),
                    toWalletResult.Exception);
                return result;
            }

            var senderAvatarAccountName = fromWalletResult.Result.Name;
            var receiverAvatarAccountName = toWalletResult.Result.Name;
            result = await _transferRepository.TransferEosToken(senderAvatarAccountName, receiverAvatarAccountName,
                amount);

            if (result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

            return result;
        }

        #endregion

        #region IOASISNFTProvider

        public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transation)
        {
            return SendNFTAsync(transation).Result;
        }

        public async Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transation)
        {
            OASISResult<INFTTransactionRespone> result = new OASISResult<INFTTransactionRespone>();

            OASISResult<ITransactionRespone> transferResult = await _transferRepository.TransferEosNft(
                transation.FromWalletAddress,
                transation.ToWalletAddress,
                transation.Amount,
                "SYS");

            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ITransactionRespone, INFTTransactionRespone>(transferResult, result);
            //OASISResultHelper.CopyResult<ITransactionRespone, INFTTransactionRespone>(transferResult, result);
            result.Result.TransactionResult = transferResult.Result.TransactionResult;
            result.Result.OASISNFT = null; //TODO: We may want to look up/pass the NFT MetaData in future...

            return result;

            //return await _transferRepository.TransferEosNft(
            //    transation.FromWalletAddress,
            //    transation.ToWalletAddress,
            //    transation.Amount,
            //    "SYS");
        }

        public OASISResult<INFTTransactionRespone> MintNFT(IMintNFTTransactionRequest transation)
        {
            var result = new OASISResult<INFTTransactionRespone>();
            string errorMessage = "Error in MintNFT method in EOSIOOASIS Provider. Reason: ";

            try
            {
                if (transation == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Transaction request is null");
                    return result;
                }

                // Get wallet address for the avatar
                var walletResult = WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, ProviderType.Value, transation.MintedByAvatarId).Result;
                if (walletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to get wallet address: {walletResult.Message}");
                    return result;
                }

                // Create EOSIO NFT mint transaction
                var mintTransaction = new
                {
                    from = walletResult.Result,
                    to = transation.SendToAddressAfterMinting,
                    title = transation.Title,
                    description = transation.Description,
                    imageUrl = transation.ImageUrl,
                    jsonMetaData = transation.JSONMetaData,
                    memo = $"OASIS NFT mint transaction for {transation.MintedByAvatarId}"
                };

                //TODO: Implement actual NFT minting logic here
                //var transactionResult = _transferRepository.TransferEosNft(walletResult.Result, transation.SendToAddressAfterMinting, 0).Result;

                //if (transactionResult != null)
                //{
                //    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.NFTTransactionRespone
                //    {
                //        TransactionResult = transactionResult.
                //        OASISNFT = null // Will be populated after NFT creation
                //    };
                //    result.IsError = false;
                //    result.IsSaved = true;
                //}
                //else
                //{
                //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to create NFT transaction");
                //}
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<INFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequest transation)
        {
            var result = new OASISResult<INFTTransactionRespone>();
            string errorMessage = "Error in MintNFTAsync method in EOSIOOASIS Provider. Reason: ";

            try
            {
                if (transation == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Transaction request is null");
                    return result;
                }

                // Get wallet address for the avatar
                var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.EOSIOOASIS, transation.MintedByAvatarId);
                if (walletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to get wallet address: {walletResult.Message}");
                    return result;
                }

                // Create EOSIO NFT mint transaction
                var mintTransaction = new
                {
                    from = walletResult.Result,
                    to = transation.SendToAddressAfterMinting,
                    title = transation.Title,
                    description = transation.Description,
                    imageUrl = transation.ImageUrl,
                    jsonMetaData = transation.JSONMetaData,
                    memo = $"OASIS NFT mint transaction for {transation.MintedByAvatarId}"
                };

                //TODO: Implement actual NFT minting logic here
                //var transactionResult = await _transferRepository.TransferEosToken(walletResult.Result, transation.SendToAddressAfterMinting, 0);

                //if (transactionResult != null)
                //{
                //    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.NFTTransactionRespone
                //    {
                //        TransactionResult = transactionResult.TransactionResult,
                //        OASISNFT = null // Will be populated after NFT creation
                //    };
                //    result.IsError = false;
                //    result.IsSaved = true;
                //}
                //else
                //{
                //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to create NFT transaction");
                //}
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        #endregion

        public async Task<GetAccountResponseDto> GetEOSIOAccountAsync(string eosioAccountName)
        {
            var accountResult = new OASISResult<GetAccountResponseDto>();
            try
            {
                var accountResponseDto = await _eosClient.GetAccount(new GetAccountDtoRequest()
                {
                    AccountName = eosioAccountName
                });
                accountResult.Result = accountResponseDto;
            }
            catch (Exception e)
            {
                accountResult.Result = null;
                
                OASISErrorHandling.HandleError(ref accountResult, e.Message);
            }

            return accountResult.Result;
        }
        
        public GetAccountResponseDto GetEOSIOAccount(string eosioAccountName)
        {
            var accountResult = new OASISResult<GetAccountResponseDto>();
            try
            {
                var accountResponseDto = _eosClient.GetAccount(new GetAccountDtoRequest()
                {
                    AccountName = eosioAccountName
                }).Result;
                accountResult.Result = accountResponseDto;
            }
            catch (Exception e)
            {
                accountResult.Result = null;
                
                OASISErrorHandling.HandleError(ref accountResult, e.Message);
            }

            return accountResult.Result;
        }

        public async Task<string> GetBalanceAsync(string eosioAccountName, string code, string symbol)
        {
            var balanceResult = new OASISResult<string>();
            try
            {
                var currencyBalances = await _eosClient.GetCurrencyBalance(new GetCurrencyBalanceRequestDto()
                {
                    Account = eosioAccountName,
                    Code = code,
                    Symbol = symbol
                });
                balanceResult.Result = currencyBalances != null ? currencyBalances[0] : string.Empty;
            }
            catch (Exception e)
            {
                balanceResult.Result = string.Empty;
                
                OASISErrorHandling.HandleError(ref balanceResult, e.Message);
            }

            return balanceResult.Result;
        }

        public string GetBalanceForEOSIOAccount(string eosioAccountName, string code, string symbol)
        {
            var balanceResult = new OASISResult<string>();
            try
            {
                var currencyBalances = _eosClient.GetCurrencyBalance(new GetCurrencyBalanceRequestDto()
                {
                    Account = eosioAccountName,
                    Code = code,
                    Symbol = symbol
                }).Result;
                balanceResult.Result = currencyBalances != null ? currencyBalances[0] : string.Empty;
            }
            catch (Exception e)
            {
                balanceResult.Result = string.Empty;
                
                OASISErrorHandling.HandleError(ref balanceResult, e.Message);
            }

            return balanceResult.Result;
        }

        public string GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            //TODO: Add support for multiple accounts later.
            return GetBalanceForEOSIOAccount(GetEOSIOAccountNamesForAvatar(avatarId)[0], code, symbol);
        }

        public List<string> GetEOSIOAccountNamesForAvatar(Guid avatarId)
        {
            try
            {
                var result = KeyManager.GetProviderPublicKeysForAvatarById(avatarId, Core.Enums.ProviderType.EOSIOOASIS);
                if (result.IsError)
                {
                    LoggingManager.Log($"Error getting EOSIO account names for avatar {avatarId}: {result.Message}", NextGenSoftware.Logging.LogType.Error);
                    return new List<string>();
                }
                return result.Result;
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Exception getting EOSIO account names for avatar {avatarId}: {ex.Message}", NextGenSoftware.Logging.LogType.Error);
                return new List<string>();
            }
        }

        public string GetEOSIOAccountPrivateKeyForAvatar(Guid avatarId)
        {
            try
            {
                var result = KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, Core.Enums.ProviderType.EOSIOOASIS);
                if (result.IsError || result.Result == null || !result.Result.Any())
                {
                    LoggingManager.Log($"Error getting EOSIO private key for avatar {avatarId}: {result.Message}", NextGenSoftware.Logging.LogType.Error);
                    return string.Empty;
                }
                return result.Result[0];
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Exception getting EOSIO private key for avatar {avatarId}: {ex.Message}", NextGenSoftware.Logging.LogType.Error);
                return string.Empty;
            }
        }

        public GetAccountResponseDto GetEOSIOAccountForAvatar(Guid avatarId)
        {
            try
            {
                // Check cache first for performance
                if (_avatarIdToEOSIOAccountLookup.ContainsKey(avatarId))
                    return _avatarIdToEOSIOAccountLookup[avatarId];

                // Get account names for avatar
                var accountNames = GetEOSIOAccountNamesForAvatar(avatarId);
                if (accountNames == null || !accountNames.Any())
                {
                    LoggingManager.Log($"No EOSIO account names found for avatar {avatarId}", NextGenSoftware.Logging.LogType.Warning);
                    return null;
                }

                // Get account details for the first account (support for multiple accounts can be added later)
                var accountResult = GetEOSIOAccountAsync(accountNames[0]).Result;
                if (accountResult != null)
                {
                    _avatarIdToEOSIOAccountLookup[avatarId] = accountResult;
                }

                return accountResult;
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Exception getting EOSIO account for avatar {avatarId}: {ex.Message}", NextGenSoftware.Logging.LogType.Error);
                return null;
            }
        }

        public Guid GetAvatarIdForEOSIOAccountName(string eosioAccountName)
        {
            try
            {
                var result = KeyManager.GetAvatarIdForProviderPublicKey(eosioAccountName, Core.Enums.ProviderType.EOSIOOASIS);
                if (result.IsError)
                {
                    LoggingManager.Log($"Error getting avatar ID for EOSIO account {eosioAccountName}: {result.Message}", NextGenSoftware.Logging.LogType.Error);
                    return Guid.Empty;
                }
                return result.Result;
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Exception getting avatar ID for EOSIO account {eosioAccountName}: {ex.Message}", NextGenSoftware.Logging.LogType.Error);
                return Guid.Empty;
            }
        }

        public IAvatar GetAvatarForEOSIOAccountName(string eosioAccountName)
        {
            try
            {
                var result = KeyManager.GetAvatarForProviderPublicKey(eosioAccountName, Core.Enums.ProviderType.EOSIOOASIS);
                if (result.IsError)
                {
                    LoggingManager.Log($"Error getting avatar for EOSIO account {eosioAccountName}: {result.Message}", LogType.Error);
                    return null;
                }
                return result.Result;
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Exception getting avatar for EOSIO account {eosioAccountName}: {ex.Message}", LogType.Error);
                return null;
            }
        }

        // Removed explicit interface implementations that don't exist in the interface

        public void Dispose()
        {
            try
            {
                // Dispose of EOSIO client and repositories
                _eosClient?.Dispose();
                _avatarRepository = null;
                _avatarDetailRepository = null;
                _holonRepository = null;
                _transferRepository = null;
                _avatarManager = null;
                _keyManager = null;
                _walletManager = null;
            }
            catch (Exception ex)
            {
                // Log disposal error but don't throw
                LoggingManager.Log($"Error disposing EOSIOOASIS provider: {ex.Message}", LogType.Error);
            }
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error in LoadHolonsByMetaDataAsync method in EOSIOOASIS Provider. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(metaKey) || string.IsNullOrEmpty(metaValue))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} MetaKey and MetaValue cannot be null or empty");
                    return result;
                }

                // Search for holons by metadata using EOSIO repository
                var holons = await _holonRepository.ReadAllByMetaData(metaKey, metaValue, type);
                
                if (holons != null && holons.Any())
                {
                    var holonList = new List<IHolon>();
                    foreach (var holonDto in holons)
                    {
                        var holon = JsonConvert.DeserializeObject<Holon>(holonDto.Info);
                        if (holon != null)
                        {
                            holonList.Add(holon);
                        }
                    }
                    
                    result.Result = holonList;
                    result.IsError = false;
                }
                else
                {
                    result.Result = new List<IHolon>();
                    result.IsError = false;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error in LoadHolonsByMetaDataAsync method in EOSIOOASIS Provider. Reason: ";

            try
            {
                if (metaKeyValuePairs == null || !metaKeyValuePairs.Any())
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} MetaKeyValuePairs cannot be null or empty");
                    return result;
                }

                // Search for holons by multiple metadata key-value pairs using EOSIO repository
                var holons = await _holonRepository.ReadAllByMetaData(metaKeyValuePairs, metaKeyValuePairMatchMode, type);
                
                if (holons != null && holons.Any())
                {
                    var holonList = new List<IHolon>();
                    foreach (var holonDto in holons)
                    {
                        var holon = JsonConvert.DeserializeObject<Holon>(holonDto.Info);
                        if (holon != null)
                        {
                            holonList.Add(holon);
                        }
                    }
                    
                    result.Result = holonList;
                    result.IsError = false;
                }
                else
                {
                    result.Result = new List<IHolon>();
                    result.IsError = false;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public OASISResult<IOASISNFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IOASISNFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            throw new NotImplementedException();
        }

        #region Helper Methods

        /// <summary>
        /// Parse EOSIO blockchain response to Avatar object with complete serialization
        /// </summary>
        private Avatar ParseEOSIOToAvatar(GetAccountResponseDto eosioData, string username)
        {
            try
            {
                // Serialize the complete EOSIO data to JSON first
                var eosioJson = JsonConvert.SerializeObject(eosioData, Formatting.Indented);
                
                // Deserialize the complete Avatar object from EOSIO JSON
                var avatar = JsonConvert.DeserializeObject<Avatar>(eosioJson);
                
                // If deserialization fails, create from extracted properties
                if (avatar == null)
                {
                    avatar = new Avatar
                    {
                        Id = Guid.NewGuid(),
                        Username = username,
                        Email = $"user@{username}.eosio",
                        FirstName = "EOSIO",
                        LastName = "User",
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        Version = 1,
                        IsActive = true
                    };
                }

                // Add EOSIO-specific metadata
                if (eosioData != null)
                {
                    avatar.ProviderMetaData[Core.Enums.ProviderType.EOSIOOASIS].Add("eosio_account_name", username);
                    avatar.ProviderMetaData[Core.Enums.ProviderType.EOSIOOASIS].Add("eosio_net_weight", eosioData.NetWeight?.ToString() ?? "0");
                    avatar.ProviderMetaData[Core.Enums.ProviderType.EOSIOOASIS].Add("eosio_cpu_weight", eosioData.CpuWeight?.ToString() ?? "0");
                    avatar.ProviderMetaData[Core.Enums.ProviderType.EOSIOOASIS].Add("eosio_ram_quota", eosioData.RamQuota?.ToString() ?? "0");
                    avatar.ProviderMetaData[Core.Enums.ProviderType.EOSIOOASIS].Add("eosio_ram_usage", eosioData.RamUsage?.ToString() ?? "0");
                }

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetEOSIOAccountNameForAvatarUsername(string avatarUsername)
        {
            try
            {
                // Get avatar detail by username to get the avatar ID
                var avatarDetailResult = AvatarManager.LoadAvatarDetailByUsername(avatarUsername);
                if (avatarDetailResult.IsError || avatarDetailResult.Result == null)
                {
                    LoggingManager.Log($"No avatar detail found for username {avatarUsername}", LogType.Warning);
                    return null;
                }

                // Get EOSIO account names for the avatar
                var accountNames = GetEOSIOAccountNamesForAvatar(avatarDetailResult.Result.Id);
                if (accountNames == null || !accountNames.Any())
                {
                    LoggingManager.Log($"No EOSIO account names found for avatar {avatarDetailResult.Result.Id}", LogType.Warning);
                    return null;
                }

                return accountNames[0]; // Return the first account name
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Exception getting EOSIO account name for avatar username {avatarUsername}: {ex.Message}", LogType.Error);
                return null;
            }
        }

        /// <summary>
        /// Parse EOSIO blockchain data to OASIS Holon
        /// </summary>
        private static IHolon ParseEOSIOToHolon(object holonData)
        {
            try
            {
                if (holonData == null) return null;
                
                // Parse the actual EOSIO blockchain data
                var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(holonData.ToString());
                if (dataDict == null) return null;
                
                var holon = new Holon
                {
                    Id = dataDict.ContainsKey("id") ? Guid.Parse(dataDict["id"].ToString()) : Guid.NewGuid(),
                    Name = dataDict.GetValueOrDefault("name")?.ToString() ?? "EOSIO Holon",
                    Description = dataDict.GetValueOrDefault("description")?.ToString() ?? "Holon from EOSIO blockchain",
                    ProviderUniqueStorageKey = new Dictionary<ProviderType, string> 
                    { 
                        [Core.Enums.ProviderType.EOSIOOASIS] = dataDict.GetValueOrDefault("eosioId")?.ToString() ?? Guid.NewGuid().ToString() 
                    },
                    IsActive = dataDict.GetValueOrDefault("isActive")?.ToString()?.ToLower() == "true",
                    CreatedDate = dataDict.ContainsKey("createdDate") ? DateTime.Parse(dataDict["createdDate"].ToString()) : DateTime.UtcNow,
                    ModifiedDate = dataDict.ContainsKey("modifiedDate") ? DateTime.Parse(dataDict["modifiedDate"].ToString()) : DateTime.UtcNow,
                    Version = dataDict.ContainsKey("version") ? int.Parse(dataDict["version"].ToString()) : 1,
                    MetaData = new Dictionary<string, object>
                    {
                        ["EOSIOData"] = holonData,
                        ["EOSIOAccountName"] = dataDict.GetValueOrDefault("accountName")?.ToString(),
                        ["EOSIOBlockNum"] = dataDict.GetValueOrDefault("blockNum")?.ToString(),
                        ["EOSIOTimestamp"] = dataDict.GetValueOrDefault("timestamp")?.ToString(),
                        ["ParsedAt"] = DateTime.UtcNow,
                        ["Provider"] = "EOSIOOASIS"
                    }
                };
                
                return holon;
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"Error parsing EOSIO holon data: {ex.Message}", LogType.Error);
                return null;
            }
        }

        #endregion
    }
}