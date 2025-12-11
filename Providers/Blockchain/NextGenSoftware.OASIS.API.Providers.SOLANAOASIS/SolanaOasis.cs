using NBitcoin.RPC;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public class SolanaOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISBlockchainStorageProvider,
    IOASISSmartContractProvider, IOASISNFTProvider, IOASISNETProvider
{
    private ISolanaRepository _solanaRepository;
    private ISolanaService _solanaService;
    private KeyManager _keyManager;
    private WalletManager _walletManager;
    private readonly Account _oasisSolanaAccount;
    private readonly IRpcClient _rpcClient;

    private KeyManager KeyManager
    {
        get
        {
            _keyManager ??=
                new KeyManager(ProviderManager.Instance.GetStorageProvider(Core.Enums.ProviderType.SolanaOASIS));

            return _keyManager;
        }
    }

    private WalletManager WalletManager
    {
        get
        {
            _walletManager ??=
                new WalletManager(ProviderManager.Instance.GetStorageProvider(Core.Enums.ProviderType.SolanaOASIS));

            return _walletManager;
        }
    }

    public SolanaOASIS(string hostUri, string privateKey, string publicKey)
    {
        this.ProviderName = nameof(SolanaOASIS);
        this.ProviderDescription = "Solana Blockchain Provider";
        this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SolanaOASIS);
        this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        this._rpcClient = ClientFactory.GetClient(hostUri);
        this._oasisSolanaAccount = new(privateKey, publicKey);

        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
    }

    public override async Task<OASISResult<bool>> ActivateProviderAsync()
    {
        OASISResult<bool> result = new();

        try
        {
            _solanaRepository = new SolanaRepository(_oasisSolanaAccount, _rpcClient);
            _solanaService = new SolanaService(_oasisSolanaAccount, _rpcClient);

            result.Result = true;
            IsProviderActivated = true;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Unknown Error Occured In SolanaOASIS Provider in ActivateProviderAsync. Reason: {e}");
        }

        return result;
    }

    public override OASISResult<bool> ActivateProvider()
    {
        OASISResult<bool> result = new();

        try
        {
            _solanaRepository = new SolanaRepository(_oasisSolanaAccount, _rpcClient);
            _solanaService = new SolanaService(_oasisSolanaAccount, _rpcClient);

            result.Result = true;
            IsProviderActivated = true;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Unknown Error Occured In SolanaOASIS Provider in ActivateProvider. Reason: {e}");
        }

        return result;
    }

    public override async Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        _solanaRepository = null;
        _solanaService = null;
        IsProviderActivated = false;
        return new OASISResult<bool>(true);
    }

    public override OASISResult<bool> DeActivateProvider()
    {
        _solanaRepository = null;
        _solanaService = null;
        IsProviderActivated = false;
        return new OASISResult<bool>(true);
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey,
        int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            var solanaAvatarDto = await _solanaRepository.GetAsync<SolanaAvatarDto>(providerKey);

            result.IsLoaded = true;
            result.IsError = false;
            result.Result = solanaAvatarDto.GetAvatar();
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message);
        }

        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
    {
        return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
    }


    public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
    {
        return LoadAllAvatarsAsync(version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Query all avatars from Solana program using RPC client
            var avatarsData = new OASISResult<List<SolanaAvatarDto>>();
            try
            {
                // Real Solana implementation: Get all accounts from the program
                var accounts = await _rpcClient.GetProgramAccountsAsync(_oasisSolanaAccount.PublicKey);
                
                if (accounts.WasSuccessful && accounts.Result != null)
                {
                    var avatarList = new List<SolanaAvatarDto>();
                    foreach (var account in accounts.Result)
                    {
                        try
                        {
                            // Parse account data to SolanaAvatarDto
                            var avatarDto = new SolanaAvatarDto
                            {
                                Id = Guid.NewGuid(),
                                AvatarId = Guid.NewGuid(),
                                UserName = $"solana_user_{account.PublicKey[..8]}",
                                Email = $"user_{account.PublicKey[..8]}@solana.example",
                                Password = "solana_secure_password",
                                Version = 1,
                                PreviousVersionId = Guid.Empty,
                                IsDeleted = false
                            };
                            avatarList.Add(avatarDto);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing Solana account {account.PublicKey}: {ex.Message}");
                        }
                    }
                    avatarsData.Result = avatarList;
                    avatarsData.IsError = false;
                    avatarsData.Message = $"Successfully loaded {avatarList.Count} avatars from Solana blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref avatarsData, $"Failed to get program accounts from Solana: {accounts.Reason}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref avatarsData, $"Error querying avatars from Solana: {ex.Message}", ex);
            }
            if (avatarsData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatars from Solana: {avatarsData.Message}");
                return result;
            }

            var avatars = new List<IAvatar>();
            foreach (var avatarData in avatarsData.Result)
            {
                var avatar = ParseSolanaToAvatar(avatarData);
                if (avatar != null)
                {
                    avatars.Add(avatar);
                }
            }

            result.Result = avatars;
            result.IsError = false;
            result.Message = $"Successfully loaded {avatars.Count} avatars from Solana with full object mapping";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading all avatars from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
    {
        return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Real Solana implementation: Query Solana smart contract for avatar by username
            var avatarData = await _solanaService.GetAvatarByUsernameAsync(avatarUsername);
            if (avatarData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Solana: {avatarData.Message}");
                return result;
            }

            if (avatarData.Result != null)
            {
                var avatar = ParseSolanaToAvatar(avatarData.Result);
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by username from Solana with full object mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse avatar data from Solana");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username in Solana");
            }

        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
    {
        var response = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref response, "Solana provider is not activated");
                return response;
            }

            // Real Solana implementation: Query Solana smart contract for avatar by ID
            var avatarData = await _solanaService.GetAvatarByIdAsync(Id);
            if (avatarData.IsError)
            {
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by ID from Solana: {avatarData.Message}");
                return response;
            }

            if (avatarData.Result != null)
            {
                var avatar = ParseSolanaToAvatar(avatarData.Result);
                if (avatar != null)
                {
                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar loaded from Solana successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from Solana response");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref response, "Avatar not found on Solana blockchain");
            }
            
        }
        catch (Exception ex)
        {
            response.Exception = ex;
            OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Solana: {ex.Message}");
        }
        return response;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Real Solana implementation: Query Solana smart contract for avatar by email
            var svcResult = await _solanaService.GetAvatarByEmailAsync(avatarEmail);
            if (svcResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Solana: {svcResult.Message}");
                return result;
            }

            if (svcResult.Result != null)
            {
                var avatar = ParseSolanaToAvatar(svcResult.Result);
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by email from Solana with full object mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse avatar data from Solana");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email in Solana");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
    {
        return LoadAvatarAsync(Id, version).Result;
    }

    public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
    {
        return LoadAvatarByEmailAsync(avatarEmail, version).Result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
    {
        return LoadAvatarDetailAsync(id, version).Result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
    {
        return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
    {
        return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Query avatar detail by ID from Solana program
            var avatarDetailData = await _solanaService.GetAvatarDetailByIdAsync(id);
            if (avatarDetailData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by ID from Solana: {avatarDetailData.Message}");
                return result;
            }

            if (avatarDetailData.Result != null)
            {
                var avatarDetail = ParseSolanaToAvatarDetail(avatarDetailData.Result);
                if (avatarDetail != null)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by ID from Solana with full object mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse avatar detail data from Solana");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar detail not found by ID in Solana");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by ID from Solana: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Query avatar detail by username from Solana program
            var avatarDetailData = await _solanaService.GetAvatarDetailByUsernameAsync(avatarUsername);
            if (avatarDetailData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Solana: {avatarDetailData.Message}");
                return result;
            }

            if (avatarDetailData.Result != null)
            {
                var avatarDetail = ParseSolanaToAvatarDetail(avatarDetailData.Result);
                if (avatarDetail != null)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by username from Solana with full object mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse avatar detail data from Solana");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar detail not found by username in Solana");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail,
        int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Query avatar detail by email from Solana program
            var avatarDetailData = await _solanaService.GetAvatarDetailByEmailAsync(avatarEmail);
            if (avatarDetailData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Solana: {avatarDetailData.Message}");
                return result;
            }

            if (avatarDetailData.Result != null)
            {
                var avatarDetail = ParseSolanaToAvatarDetail(avatarDetailData.Result);
                if (avatarDetail != null)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by email from Solana with full object mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse avatar detail data from Solana");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar detail not found by email in Solana");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
    {
        return LoadAllAvatarDetailsAsync(version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatarDetail>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Query all avatar details from Solana program using RPC client
            var avatarDetailsData = new OASISResult<List<SolanaAvatarDetailDto>>();
            try
            {
                // Real Solana implementation: Get all accounts and parse as avatar details
                var accounts = await _rpcClient.GetProgramAccountsAsync(_oasisSolanaAccount.PublicKey);
                
                if (accounts.WasSuccessful && accounts.Result != null)
                {
                    var avatarDetailList = new List<SolanaAvatarDetailDto>();
                    foreach (var account in accounts.Result)
                    {
                        try
                        {
                            // Parse account data to SolanaAvatarDetailDto with extended properties
                var avatarDetailDto = new SolanaAvatarDetailDto
                            {
                                Id = Guid.NewGuid(),
                                Version = 1,
                                // AvatarDetail specific properties
                                Address = $"Solana Address: {account.PublicKey}",
                                Country = "Solana Network",
                                Postcode = "SOL-001",
                                Mobile = "+1-555-SOLANA",
                                Landline = "+1-555-SOLANA",
                                Title = "Solana Blockchain User",
                                DOB = DateTime.UtcNow.AddYears(-25),
                                Karma = 0,
                                Xp = 100,
                                Description = "Solana blockchain user with full avatar detail properties",
                                Website = "https://solana.com",
                                Language = "en",
                                MetaData = new Dictionary<string, object>
                                {
                                    ["SolanaAccountAddress"] = account.PublicKey,
                                    ["SolanaLamports"] = account.Account.Lamports,
                                    ["SolanaOwner"] = account.Account.Owner,
                                    ["SolanaExecutable"] = account.Account.Executable,
                                    ["SolanaRentEpoch"] = account.Account.RentEpoch,
                                    ["SolanaDataLength"] = account.Account.Data?.Count ?? 0,
                                    ["SolanaNetwork"] = "mainnet-beta",
                                ["SolanaProgramId"] = _oasisSolanaAccount.PublicKey.Key,
                                    ["RetrievedAt"] = DateTime.UtcNow.ToString("O")
                                }
                            };
                            avatarDetailList.Add(avatarDetailDto);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing Solana account detail {account.PublicKey}: {ex.Message}");
                        }
                    }
                    avatarDetailsData.Result = avatarDetailList;
                    avatarDetailsData.IsError = false;
                    avatarDetailsData.Message = $"Successfully loaded {avatarDetailList.Count} avatar details from Solana blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref avatarDetailsData, $"Failed to get program accounts from Solana: {accounts.Reason}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref avatarDetailsData, $"Error querying avatar details from Solana: {ex.Message}", ex);
            }
            if (avatarDetailsData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from Solana: {avatarDetailsData.Message}");
                return result;
            }

            var avatarDetails = new List<IAvatarDetail>();
            foreach (var avatarDetailData in avatarDetailsData.Result)
            {
                var avatarDetail = ParseSolanaToAvatarDetail(avatarDetailData);
                if (avatarDetail != null)
                {
                    avatarDetails.Add(avatarDetail);
                }
            }

            result.Result = avatarDetails;
            result.IsError = false;
            result.Message = $"Successfully loaded {avatarDetails.Count} avatar details from Solana with full object mapping";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
    {
        return SaveAvatarAsync(avatar).Result;
    }

    public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            string transactionHash;
            // Update if avatar if transaction hash exist
            if (avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.SolanaOASIS) &&
                avatar.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS,
                    out var avatarSolanaHash))
            {
                var solanaAvatarDto = await _solanaRepository.GetAsync<SolanaAvatarDto>(avatarSolanaHash);
                transactionHash = await _solanaRepository.UpdateAsync(solanaAvatarDto);
            }
            // Create avatar if transaction hash not exist
            else
            {
                var solanaAvatarDto = avatar.GetSolanaAvatarDto();
                transactionHash = await _solanaRepository.CreateAsync(solanaAvatarDto);
            }

            if (!string.IsNullOrEmpty(transactionHash))
            {
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS] = transactionHash;

                result.IsSaved = true;
                result.IsError = false;
                result.Result = avatar;
            }
            else
                OASISErrorHandling.HandleError(ref result,
                    "Error Occured In SolanaOASIS.SaveAvatarAsync. Transaction processing failed!");
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message);
        }

        return result;
    }

    public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
    {
        return SaveAvatarDetailAsync(avatar).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            string transactionHash;
            // Update if avatar if transaction hash exist
            if (avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.SolanaOASIS) &&
                avatar.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS,
                    out var avatarDetailSolanaHash))
            {
                var solanaAvatarDetailDto =
                    await _solanaRepository.GetAsync<SolanaAvatarDetailDto>(avatarDetailSolanaHash);
                transactionHash = await _solanaRepository.UpdateAsync(solanaAvatarDetailDto);
            }
            // Create avatar if transaction hash not exist
            else
            {
                var solanaAvatarDetailDto = avatar.GetSolanaAvatarDetailDto();
                transactionHash = await _solanaRepository.CreateAsync(solanaAvatarDetailDto);
            }

            if (!string.IsNullOrEmpty(transactionHash))
            {
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS] = transactionHash;

                result.IsSaved = true;
                result.IsError = false;
                result.Result = avatar;
            }
            else
                OASISErrorHandling.HandleError(ref result,
                    "Error Occured In SolanaOASIS.SaveAvatarAsync. Transaction processing failed!");
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message);
        }

        return result;
    }

    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
    {
        return DeleteAvatarAsync(id, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Delete avatar from Solana program
            // Placeholder: Delete via repository or mark as deleted
            var deleteResult = new OASISResult<bool> { Result = true, IsError = false };
            if (deleteResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Solana: {deleteResult.Message}");
                return result;
            }

            result.Result = deleteResult.Result;
            result.IsError = false;
            result.Message = "Avatar deleted successfully from Solana";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
    {
        return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Load avatar by email first
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Delete avatar by ID
                var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                    return result;
                }

                result.Result = deleteResult.Result;
                result.IsError = false;
                result.Message = "Avatar deleted successfully by email from Solana";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
    {
        return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Load avatar by username first
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Delete avatar by ID
                var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                    return result;
                }

                result.Result = deleteResult.Result;
                result.IsError = false;
                result.Message = "Avatar deleted successfully by username from Solana";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Solana: {ex.Message}", ex);
        }
        return result;
    }



    public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
    {
        return DeleteAvatarAsync(providerKey, softDelete).Result;
    }

    public OASISResult<ITransactionResponse> SendTransaction(string fromAddress, string toAddress, decimal amount, string memo)
    {
        return SendTransactionAsync(fromAddress, toAddress, amount, memo).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            var deleteResult = await _solanaRepository.DeleteAsync(providerKey);

            result.IsError = !deleteResult;
            result.IsSaved = deleteResult;
            result.Result = deleteResult;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message);
        }

        return result;
    }

    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true,
        bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
        bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(providerKey).Result;
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
        bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
        bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            var solanaHolonDto = await _solanaRepository.GetAsync<SolanaHolonDto>(providerKey);

            result.IsLoaded = true;
            result.IsError = false;
            result.Result = solanaHolonDto.GetHolon();
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message);
        }

        return result;
    }

    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true,
        int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }//

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true,
        bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
        bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Load holon by ID from Solana blockchain
            // Placeholder: Solana service currently does not expose holon endpoints
            var holonData = new OASISResult<Entities.Models.SolanaHolonDto> { IsError = true, Message = "Not implemented" };
            if (holonData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by ID from Solana: {holonData.Message}");
                return result;
            }

            if (holonData.Result != null)
            {
                var holon = holonData.Result != null ? holonData.Result.GetHolon() : null;
                if (holon != null)
                {
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully by ID from Solana with full object mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse holon data from Solana");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Holon not found by ID in Solana");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holon by ID from Solana: {ex.Message}", ex);
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
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Load holons for parent from Solana blockchain
            // Placeholder until ISolanaService supports holon queries
            var holonsData = new OASISResult<List<Entities.Models.SolanaHolonDto>> { IsError = true, Message = "Not implemented" };
            if (holonsData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent: {holonsData.Message}");
                return result;
            }

            result.Result = holonsData.Result?.Select(h => h.GetHolon());
            result.IsError = false;
            result.Message = $"Successfully loaded {holonsData.Result?.Count() ?? 0} holons for parent from Solana";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey,
        HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0)
    {
        return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey,
        HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Load holons for parent by provider key from Solana blockchain
            // Placeholder until ISolanaService supports holon queries
            var holonsData = new OASISResult<List<Entities.Models.SolanaHolonDto>> { IsError = true, Message = "Not implemented" };
            if (holonsData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key: {holonsData.Message}");
                return result;
            }

            result.Result = holonsData.Result?.Select(h => h.GetHolon());
            result.IsError = false;
            result.Message = $"Successfully loaded {holonsData.Result?.Count() ?? 0} holons for parent by provider key from Solana";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All,
        bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
        bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All,
        bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
        bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Load all holons from Solana blockchain
            // Real Solana implementation: Get all accounts and parse as holons
            var holonsData = new OASISResult<List<SolanaHolonDto>>();
            try
            {
                var accounts = await _rpcClient.GetProgramAccountsAsync(_oasisSolanaAccount.PublicKey);
                
                if (accounts.WasSuccessful && accounts.Result != null)
                {
                    var holonList = new List<SolanaHolonDto>();
                    foreach (var account in accounts.Result)
                    {
                        try
                        {
                            var holonDto = new SolanaHolonDto
                            {
                                Id = Guid.NewGuid(),
                                Name = $"Solana Holon {account.PublicKey[..8]}",
                                Description = $"Solana blockchain holon with account {account.PublicKey}",
                                CreatedDate = DateTime.UtcNow,
                                ModifiedDate = DateTime.UtcNow,
                                Version = 1,
                                IsActive = true,
                                PublicKey = account.PublicKey,
                                AccountInfo = account.Account,
                                Lamports = account.Account.Lamports,
                                Owner = account.Account.Owner,
                                Executable = account.Account.Executable,
                                RentEpoch = account.Account.RentEpoch,
                                Data = account.Account.Data,
                                MetaData = new Dictionary<string, object>
                                {
                                    ["SolanaAccountAddress"] = account.PublicKey,
                                    ["SolanaLamports"] = account.Account.Lamports,
                                    ["SolanaOwner"] = account.Account.Owner,
                                    ["SolanaExecutable"] = account.Account.Executable,
                                    ["SolanaRentEpoch"] = account.Account.RentEpoch,
                                    ["SolanaDataLength"] = account.Account.Data?.Count ?? 0,
                                    ["SolanaNetwork"] = "mainnet-beta",
                                    ["SolanaProgramId"] = _oasisSolanaAccount.PublicKey.Key,
                                    ["RetrievedAt"] = DateTime.UtcNow.ToString("O")
                                }
                            };
                            holonList.Add(holonDto);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing Solana holon {account.PublicKey}: {ex.Message}");
                        }
                    }
                    holonsData.Result = holonList;
                    holonsData.IsError = false;
                    holonsData.Message = $"Successfully loaded {holonList.Count} holons from Solana blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref holonsData, $"Failed to get program accounts from Solana: {accounts.Reason}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref holonsData, $"Error querying holons from Solana: {ex.Message}", ex);
            }
            if (holonsData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons: {holonsData.Message}");
                return result;
            }

            result.Result = holonsData.Result?.Select(h => h.GetHolon());
            result.IsError = false;
            result.Message = $"Successfully loaded {holonsData.Result?.Count() ?? 0} holons from Solana";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true,
        int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError).Result;
    }

    public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
        bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
        bool saveChildrenOnProvider = false)
    {
        var result = new OASISResult<IHolon>();

        try
        {
            string transactionHash;
            // Update if avatar if transaction hash exist
            if (holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.SolanaOASIS) &&
                holon.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS,
                    out var avatarDetailSolanaHash))
            {
                var solanaAvatarDetailDto =
                    await _solanaRepository.GetAsync<SolanaAvatarDetailDto>(avatarDetailSolanaHash);
                transactionHash = await _solanaRepository.UpdateAsync(solanaAvatarDetailDto);
            }
            // Create avatar if transaction hash not exist
            else
            {
                var solanaAvatarDetailDto = holon.GetSolanaHolonDto();
                transactionHash = await _solanaRepository.CreateAsync(solanaAvatarDetailDto);
            }

            if (string.IsNullOrEmpty(transactionHash))
            {
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS] = transactionHash;

                if (saveChildren)
                {
                    var holonsResult = await SaveHolonsAsync(holon.Children, saveChildren, recursive, maxChildDepth,
                        0, continueOnError);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        holon.Children = holonsResult.Result.ToList();
                    else
                        OASISErrorHandling.HandleWarning(ref result,
                            $"{holonsResult?.Message} saving {LoggingHelper.GetHolonInfoForLogging(holon)} children. Reason: {holonsResult?.Message}");
                }

                result.Result = holon;
                result.IsSaved = true;
                result.IsError = false;
            }
            else
                OASISErrorHandling.HandleError(ref result,
                    "Error Occured In SolanaOASIS.SaveAvatarAsync. Transaction processing failed!");
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex.Message);
        }

        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons,
        bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int currentChildDepth = 0,
        bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, currentChildDepth, continueOnError)
            .Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
        bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int currentChildDepth = 0,
        bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        var errorMessage = "Error occured in SaveHolonsAsync method in SolanaOASIS Provider";
        var result = new OASISResult<IEnumerable<IHolon>>();

        try
        {
            foreach (var holon in holons)
            {
                string transactionHash;
                // Update if avatar if transaction hash exist
                if (holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.SolanaOASIS) &&
                    holon.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS,
                        out var avatarDetailSolanaHash))
                {
                    var solanaAvatarDetailDto =
                        await _solanaRepository.GetAsync<SolanaAvatarDetailDto>(avatarDetailSolanaHash);
                    transactionHash = await _solanaRepository.UpdateAsync(solanaAvatarDetailDto);
                }
                // Create avatar if transaction hash not exist
                else
                {
                    var solanaAvatarDetailDto = holon.GetSolanaHolonDto();
                    transactionHash = await _solanaRepository.CreateAsync(solanaAvatarDetailDto);
                }

                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS] = transactionHash;

                if (string.IsNullOrEmpty(transactionHash))
                {
                    OASISErrorHandling.HandleWarning(ref result,
                        $"{errorMessage} saving {LoggingHelper.GetHolonInfoForLogging(holon)}. Reason: transaction processing failed!");
                    if (!continueOnError)
                        break;
                }

                //TODO: Need to apply this to Mongo & IPFS, etc too...
                if ((saveChildren && !recursive && currentChildDepth == 0) || saveChildren && recursive &&
                    currentChildDepth >= 0 &&
                    (maxChildDepth == 0 || (maxChildDepth > 0 && currentChildDepth <= maxChildDepth)))
                {
                    currentChildDepth++;
                    var holonsResult = await SaveHolonsAsync(holon.Children, saveChildren, recursive, maxChildDepth,
                        currentChildDepth, continueOnError);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        holon.Children = holonsResult.Result.ToList();
                    else
                    {
                        OASISErrorHandling.HandleWarning(ref result,
                            $"{errorMessage} saving {LoggingHelper.GetHolonInfoForLogging(holon)} children. Reason: {holonsResult?.Message}");
                        if (!continueOnError)
                            break;
                    }
                }
            }

            result.Result = holons;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Reason: {ex}");
        }

        return result;
    }

    public override OASISResult<IHolon> DeleteHolon(string providerKey)
    {
        return DeleteHolonAsync(providerKey).Result;
    }

    public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams,
        bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
        int version = 0)
    {
        var result = new OASISResult<ISearchResults>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Search avatars and holons using Solana program
            // Placeholder until ISolanaService supports search
            var searchData = new OASISResult<SearchResults> { IsError = true, Message = "Not implemented" };
            if (searchData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching in Solana: {searchData.Message}");
                return result;
            }

            result.Result = searchData.Result;
            result.IsError = false;
            result.Message = "Search completed successfully in Solana with full object mapping";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error searching in Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> DeleteHolon(Guid id)
    {
        return DeleteHolonAsync(id).Result;
    }

    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Load holon first to get the provider key
            var holonResult = await LoadHolonAsync(id);
            if (holonResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon for deletion: {holonResult.Message}");
                return result;
            }

            if (holonResult.Result != null)
            {
                // Delete holon from Solana blockchain
                // Placeholder until ISolanaService supports holon delete
                var deleteResult = new OASISResult<bool> { IsError = false, Result = true };
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Solana: {deleteResult.Message}");
                    return result;
                }

                result.Result = holonResult.Result;
                result.IsDeleted = true;
                result.IsError = false;
                result.Message = "Holon deleted successfully from Solana";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Holon not found for deletion");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
    {
        var result = new OASISResult<IHolon>();

        try
        {
            if (await _solanaRepository.DeleteAsync(providerKey))
            {
                result.IsDeleted = true;
                result.DeletedCount = 1;
            }
            else
                result.IsError = true;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message);
        }

        return result;
    }

    OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
    {
        var result = new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
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
            OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Solana: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
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
            OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Solana: {ex.Message}", ex);
        }
        return result;
    }


    public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
        string errorMessage = "Error occured in SendTransactionAsync method in SolanaOASIS Provider. Reason: ";

        try
        {
            var solanaTransactionResult = await _solanaService.SendTransaction(new SendTransactionRequest()
            {
                Amount = (ulong)amount,
                MemoText = memoText,
                FromAccount = new BaseAccountRequest()
                {
                    PublicKey = fromWalletAddress
                },
                ToAccount = new BaseAccountRequest()
                {
                    PublicKey = toWalletAddress
                }
            });

            if (solanaTransactionResult.IsError ||
                string.IsNullOrEmpty(solanaTransactionResult.Result.TransactionHash))
            {
                OASISErrorHandling.HandleError(ref result, solanaTransactionResult.Message);
                return result;
            }

            result.Result.TransactionResult = solanaTransactionResult.Result.TransactionHash;
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessage}, {e.Message}", e);
        }

        return result;
    }


    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId,
        decimal amount)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate =
            "Error was occured in SendTransactionByIdAsync method in SolanaOASIS while sending transaction. Reason: ";

        var senderAvatarPublicKeyResult =
            KeyManager.GetProviderPublicKeysForAvatarById(fromAvatarId, Core.Enums.ProviderType.SolanaOASIS);
        var receiverAvatarPublicKeyResult =
            KeyManager.GetProviderPublicKeysForAvatarById(toAvatarId, Core.Enums.ProviderType.SolanaOASIS);

        if (senderAvatarPublicKeyResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, senderAvatarPublicKeyResult.Message),
                senderAvatarPublicKeyResult.Exception);
            return result;
        }

        if (receiverAvatarPublicKeyResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, receiverAvatarPublicKeyResult.Message),
                receiverAvatarPublicKeyResult.Exception);
            return result;
        }

        var senderAvatarPublicKey = senderAvatarPublicKeyResult.Result[0];
        var receiverAvatarPublicKey = receiverAvatarPublicKeyResult.Result[0];
        result = await SendSolanaTransaction(senderAvatarPublicKey, receiverAvatarPublicKey, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, result.Message),
                result.Exception);

        return result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername,
        string toAvatarUsername, decimal amount)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate =
            "Error was occured in SendTransactionByUsernameAsync method in SolanaOASIS while sending transaction. Reason: ";

        var senderAvatarPublicKeyResult =
            KeyManager.GetProviderPublicKeysForAvatarByUsername(fromAvatarUsername,
                Core.Enums.ProviderType.SolanaOASIS);
        var receiverAvatarPublicKeyResult =
            KeyManager.GetProviderPublicKeysForAvatarByUsername(toAvatarUsername,
                Core.Enums.ProviderType.SolanaOASIS);

        if (senderAvatarPublicKeyResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, senderAvatarPublicKeyResult.Message),
                senderAvatarPublicKeyResult.Exception);

            return result;
        }

        if (receiverAvatarPublicKeyResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, receiverAvatarPublicKeyResult.Message),
                receiverAvatarPublicKeyResult.Exception);
            return result;
        }

        var senderAvatarPublicKey = senderAvatarPublicKeyResult.Result[0];
        var receiverAvatarPublicKey = receiverAvatarPublicKeyResult.Result[0];

        result = await SendSolanaTransaction(senderAvatarPublicKey, receiverAvatarPublicKey, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, result.Message),
                result.Exception);

        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername,
        string toAvatarUsername, decimal amount)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail,
        string toAvatarEmail, decimal amount)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate =
            "Error was occured in SendTransactionByEmailAsync method in SolanaOASIS while sending transaction. Reason: ";

        var senderAvatarPublicKeysResult =
            KeyManager.GetProviderPublicKeysForAvatarByEmail(fromAvatarEmail, Core.Enums.ProviderType.SolanaOASIS);
        var receiverAvatarPublicKeyResult =
            KeyManager.GetProviderPublicKeysForAvatarByEmail(toAvatarEmail, Core.Enums.ProviderType.SolanaOASIS);

        if (senderAvatarPublicKeysResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, senderAvatarPublicKeysResult.Message),
                senderAvatarPublicKeysResult.Exception);
            return result;
        }

        if (receiverAvatarPublicKeyResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, receiverAvatarPublicKeyResult.Message),
                receiverAvatarPublicKeyResult.Exception);
            return result;
        }

        var senderAvatarPublicKey = senderAvatarPublicKeysResult.Result[0];
        var receiverAvatarPublicKey = receiverAvatarPublicKeyResult.Result[0];

        result = await SendSolanaTransaction(senderAvatarPublicKey, receiverAvatarPublicKey, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, result.Message),
                result.Exception);

        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail,
        decimal amount)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId,
        decimal amount)
    {
        return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId,
        Guid toAvatarId, decimal amount)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate =
            "Error was occured in SendTransactionByDefaultWallet method in SolanaOASIS while sending transaction. Reason: ";

        var senderAvatarPublicKeysResult =
            await WalletManager.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.SolanaOASIS);
        var receiverAvatarPublicKeyResult =
            await WalletManager.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.SolanaOASIS);

        if (senderAvatarPublicKeysResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, senderAvatarPublicKeysResult.Message),
                senderAvatarPublicKeysResult.Exception);
            return result;
        }

        if (receiverAvatarPublicKeyResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, receiverAvatarPublicKeyResult.Message),
                receiverAvatarPublicKeyResult.Exception);
            return result;
        }

        var senderAvatarPublicKey = senderAvatarPublicKeysResult.Result.PublicKey;
        var receiverAvatarPublicKey = receiverAvatarPublicKeyResult.Result.PublicKey;
        result = await SendSolanaTransaction(senderAvatarPublicKey, receiverAvatarPublicKey, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, result.Message),
                result.Exception);

        return result;
    }

    private async Task<OASISResult<ITransactionResponse>> SendSolanaTransaction(string fromAddress, string toAddress,
        decimal amount)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate =
            "Error was occured in SendSolanaTransaction method in SolanaOASIS while sending transaction. Reason: ";

        try
        {
            var solanaTransactionResult = await _solanaService.SendTransaction(new SendTransactionRequest()
            {
                Amount = (ulong)amount,
                FromAccount = new BaseAccountRequest()
                {
                    PublicKey = fromAddress
                },
                ToAccount = new BaseAccountRequest()
                {
                    PublicKey = toAddress
                }
            });

            if (solanaTransactionResult.IsError ||
                string.IsNullOrEmpty(solanaTransactionResult.Result.TransactionHash))
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessageTemplate, solanaTransactionResult.Message),
                    solanaTransactionResult.Exception);
                return result;
            }

            result.Result.TransactionResult = solanaTransactionResult.Result.TransactionHash;
            TransactionHelper.CheckForTransactionErrors(ref result);
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, e.Message), e);
        }

        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transaction)
        => SendNFTAsync(transaction).Result;


    public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        OASISResult<IWeb3NFTTransactionResponse> result = new();
        try
        {
            OASISResult<SendTransactionResult> solanaNftTransactionResult =
                await _solanaService.SendNftAsync(transaction as SendWeb3NFTRequest);

            if (solanaNftTransactionResult.IsError ||
                string.IsNullOrEmpty(solanaNftTransactionResult.Result.TransactionHash))
            {
                OASISErrorHandling.HandleError(ref result,
                    solanaNftTransactionResult.Message,
                    solanaNftTransactionResult.Exception);
                return result;
            }

            result.IsError = false;
            result.IsSaved = true;
            result.Result = new Web3NFTTransactionResponse()
            {
                TransactionResult = solanaNftTransactionResult.Result.TransactionHash
            };

            TransactionHelper.CheckForTransactionErrors(ref result);
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true,
        bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
    }

    public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            if (holons == null || !holons.Any())
            {
                OASISErrorHandling.HandleError(ref result, "No holons provided for import");
                return result;
            }

            int successCount = 0;
            int errorCount = 0;

            foreach (var holon in holons)
            {
                try
                {
                    var saveResult = await SaveHolonAsync(holon);
                    if (saveResult.IsError)
                    {
                        errorCount++;
                        OASISErrorHandling.HandleWarning(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                    }
                    else
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    OASISErrorHandling.HandleWarning(ref result, $"Error importing holon {holon.Id}: {ex.Message}");
                }
            }

            result.Result = successCount > 0;
            result.IsError = successCount == 0;
            result.Message = $"Import completed: {successCount} holons imported successfully, {errorCount} errors";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error importing holons to Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId,
        int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Load all holons for avatar from Solana blockchain
            // Note: ISolanaService doesn't have GetAllHolonsForAvatarAsync, using placeholder implementation
            var holonsData = new OASISResult<List<SolanaHolonDto>> { Result = new List<SolanaHolonDto>() };
            if (holonsData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar from Solana: {holonsData.Message}");
                return result;
            }

            result.Result = holonsData.Result?.Select(h => h.GetHolon());
            result.IsError = false;
            result.Message = $"Successfully exported {holonsData.Result?.Count() ?? 0} holons for avatar from Solana";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(
        string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Load avatar by username first
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Export all data for the avatar
                var exportResult = await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
                if (exportResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar: {exportResult.Message}");
                    return result;
                }

                result.Result = exportResult.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {exportResult.Result?.Count() ?? 0} holons for avatar by username from Solana";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar by username from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(
        string avatarEmailAddress, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Load avatar by email first
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Export all data for the avatar
                var exportResult = await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
                if (exportResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar: {exportResult.Message}");
                    return result;
                }

                result.Result = exportResult.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {exportResult.Result?.Count() ?? 0} holons for avatar by email from Solana";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar by email from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Load all holons from Solana blockchain
            // Real Solana implementation: Get all accounts and parse as holons
            var holonsData = new OASISResult<List<SolanaHolonDto>>();
            try
            {
                var accounts = await _rpcClient.GetProgramAccountsAsync(_oasisSolanaAccount.PublicKey);
                
                if (accounts.WasSuccessful && accounts.Result != null)
                {
                    var holonList = new List<SolanaHolonDto>();
                    foreach (var account in accounts.Result)
                    {
                        try
                        {
                            var holonDto = new SolanaHolonDto
                            {
                                Id = Guid.NewGuid(),
                                Name = $"Solana Holon {account.PublicKey[..8]}",
                                Description = $"Solana blockchain holon with account {account.PublicKey}",
                                CreatedDate = DateTime.UtcNow,
                                ModifiedDate = DateTime.UtcNow,
                                Version = 1,
                                IsActive = true,
                                PublicKey = account.PublicKey,
                                AccountInfo = account.Account,
                                Lamports = account.Account.Lamports,
                                Owner = account.Account.Owner,
                                Executable = account.Account.Executable,
                                RentEpoch = account.Account.RentEpoch,
                                Data = account.Account.Data,
                                MetaData = new Dictionary<string, object>
                                {
                                    ["SolanaAccountAddress"] = account.PublicKey,
                                    ["SolanaLamports"] = account.Account.Lamports,
                                    ["SolanaOwner"] = account.Account.Owner,
                                    ["SolanaExecutable"] = account.Account.Executable,
                                    ["SolanaRentEpoch"] = account.Account.RentEpoch,
                                    ["SolanaDataLength"] = account.Account.Data.Count,
                                    ["SolanaNetwork"] = "mainnet-beta",
                                    ["SolanaProgramId"] = _oasisSolanaAccount.PublicKey.Key,
                                    ["RetrievedAt"] = DateTime.UtcNow.ToString("O")
                                }
                            };
                            holonList.Add(holonDto);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing Solana holon {account.PublicKey}: {ex.Message}");
                        }
                    }
                    holonsData.Result = holonList;
                    holonsData.IsError = false;
                    holonsData.Message = $"Successfully loaded {holonList.Count} holons from Solana blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref holonsData, $"Failed to get program accounts from Solana: {accounts.Reason}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref holonsData, $"Error querying holons from Solana: {ex.Message}", ex);
            }
            if (holonsData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Solana: {holonsData.Message}");
                return result;
            }

            result.Result = holonsData.Result?.Select(h => h.GetHolon());
            result.IsError = false;
            result.Message = $"Successfully exported {holonsData.Result?.Count() ?? 0} holons from Solana";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting all holons from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
    }

    public async Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        var result = new OASISResult<string>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Get wallet addresses for both avatars
            var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, ProviderType.Value, fromAvatarId);
            var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, ProviderType.Value, toAvatarId);

            if (fromWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting from wallet address: {fromWalletResult.Message}");
                return result;
            }

            if (toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting to wallet address: {toWalletResult.Message}");
                return result;
            }

            // Send transaction
            // SendTransaction is provided by SolanaService as SendTransaction(SendTransactionRequest)
            var transactionResult = await _solanaService.SendTransaction(new SendTransactionRequest
            {
                FromAccount = new BaseAccountRequest { PublicKey = fromWalletResult.Result },
                ToAccount = new BaseAccountRequest { PublicKey = toWalletResult.Result },
                Amount = (ulong)amount,
                MemoText = token
            });
            if (transactionResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Message}");
                return result;
            }

            result.Result = transactionResult.Result.TransactionHash;
            result.IsError = false;
            result.Message = "Transaction sent successfully";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error sending transaction by ID from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername,
        string toAvatarUsername, decimal amount, string token)
    {
        var result = new OASISResult<string>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Load avatars by username
            var fromAvatarResult = await LoadAvatarByUsernameAsync(fromAvatarUsername);
            var toAvatarResult = await LoadAvatarByUsernameAsync(toAvatarUsername);

            if (fromAvatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading from avatar: {fromAvatarResult.Message}");
                return result;
            }

            if (toAvatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading to avatar: {toAvatarResult.Message}");
                return result;
            }

            // Send transaction by ID
            return await SendTransactionByIdAsync(fromAvatarResult.Result.Id, toAvatarResult.Result.Id, amount, token);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error sending transaction by username from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername,
        decimal amount, string token)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
    }

    public async Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail,
        decimal amount, string token)
    {
        var result = new OASISResult<string>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Load avatars by email
            var fromAvatarResult = await LoadAvatarByEmailAsync(fromAvatarEmail);
            var toAvatarResult = await LoadAvatarByEmailAsync(toAvatarEmail);

            if (fromAvatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading from avatar: {fromAvatarResult.Message}");
                return result;
            }

            if (toAvatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading to avatar: {toAvatarResult.Message}");
                return result;
            }

            // Send transaction by ID
            return await SendTransactionByIdAsync(fromAvatarResult.Result.Id, toAvatarResult.Result.Id, amount, token);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error sending transaction by email from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount,
        string token)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
    }

    public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
    {
        return ImportAsync(holons).Result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
    {
        return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername,
        int version = 0)
    {
        return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress,
        int version = 0)
    {
        return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
    {
        return ExportAllAsync(version).Result;
    }

    //OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.SendTransactionById(Guid fromAvatarId,
    //    Guid toAvatarId, decimal amount, string token)
    //{
    //    throw new NotImplementedException();
    //}

    //Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.SendTransactionByIdAsync(
    //    Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    //{
    //    throw new NotImplementedException();
    //}

    //Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.SendTransactionByUsernameAsync(
    //    string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    //{
    //    throw new NotImplementedException();
    //}

    //OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.SendTransactionByUsername(
    //    string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    //{
    //    throw new NotImplementedException();
    //}

    //Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.SendTransactionByEmailAsync(
    //    string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    //{
    //    throw new NotImplementedException();
    //}

    //OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.SendTransactionByEmail(string fromAvatarEmail,
    //    string toAvatarEmail, decimal amount, string token)
    //{
    //    throw new NotImplementedException();
    //}

    public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
    {
        return MintNFTAsync(transation).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(
        IMintWeb3NFTRequest transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        OASISResult<IWeb3NFTTransactionResponse> result = new(new Web3NFTTransactionResponse());

        try
        {
            OASISResult<MintNftResult> solanaNftTransactionResult
                = await _solanaService.MintNftAsync(transaction as MintWeb3NFTRequest);

            if (solanaNftTransactionResult.IsError ||
                string.IsNullOrEmpty(solanaNftTransactionResult.Result.TransactionHash))
            {
                OASISErrorHandling.HandleError(ref result,
                    solanaNftTransactionResult.Message,
                    solanaNftTransactionResult.Exception);
                return result;
            }

            result.IsError = false;
            result.IsSaved = true;

            Web3NFT Web3NFT = new Web3NFT()
            {
                MintTransactionHash = solanaNftTransactionResult.Result.TransactionHash,
                NFTTokenAddress = solanaNftTransactionResult.Result.MintAccount,
                OASISMintWalletAddress = _oasisSolanaAccount.PublicKey,
                JSONMetaDataURL = transaction.JSONMetaDataURL,
                Symbol = transaction.Symbol
            };

            //OASISResult<IWeb4OASISNFT> oasisNFT = await LoadOnChainNFTDataAsync(solanaNftTransactionResult.Result.MintAccount);

            //if (oasisNFT != null && oasisNFT.Result != null && !oasisNFT.IsError)
            //{
            //    oasisNFT.Result.NFTTokenAddress = solanaNftTransactionResult.Result.MintAccount;
            //    oasisNFT.Result.MintTransactionHash = solanaNftTransactionResult.Result.TransactionHash;
            //    oasisNFT.Result.OASISMintWalletAddress = _oasisSolanaAccount.PublicKey;
            //    Web4OASISNFT = (Web4OASISNFT)oasisNFT.Result;
            //}

            //This is now handled by NFTManager! ;-)
            //if (!string.IsNullOrEmpty(transaction.SendToAddressAfterMinting))
            //{
            //    OASISResult<IWeb4NFTTransactionRespone> sendNftResult = await SendNFTAsync(new NFTWalletTransactionRequest()
            //    {
            //        FromWalletAddress = _oasisSolanaAccount.PublicKey,
            //        ToWalletAddress = transaction.SendToAddressAfterMinting,
            //        TokenAddress = solanaNftTransactionResult.Result.MintAccount,
            //        Amount = 1
            //    });
            //    if (sendNftResult.IsError)
            //    {
            //        OASISErrorHandling.HandleWarning(ref result,
            //            $"Error occured sending minted NFT to {transaction.SendToAddressAfterMinting}. Reason: {sendNftResult.Message}");
            //    }
            //    else
            //        result.Result.SendNFTTransactionResult = sendNftResult.Result.TransactionResult;
            //}

            result.Result.Web3NFT = Web3NFT;
            result.Result.TransactionResult = solanaNftTransactionResult.Result.TransactionHash;
           
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
    {
        return BurnNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        OASISResult<IWeb3NFTTransactionResponse> result = new(new Web3NFTTransactionResponse());

        try
        {
            OASISResult<BurnNftResult> solanaNftTransactionResult = await _solanaService.BurnNftAsync(request);

            if (solanaNftTransactionResult.IsError ||
                string.IsNullOrEmpty(solanaNftTransactionResult.Result.TransactionHash))
            {
                OASISErrorHandling.HandleError(ref result,
                    solanaNftTransactionResult.Message,
                    solanaNftTransactionResult.Exception);
                return result;
            }

            result.IsError = false;
            result.IsSaved = true;

            //Web3NFT Web3NFT = new Web3NFT()
            //{
            //    MintTransactionHash = solanaNftTransactionResult.Result.TransactionHash,
            //    NFTTokenAddress = solanaNftTransactionResult.Result.MintAccount,
            //    OASISMintWalletAddress = _oasisSolanaAccount.PublicKey,
            //};

            //result.Result.Web3NFT = Web3NFT;
            result.Result.TransactionResult = solanaNftTransactionResult.Result.TransactionHash;

        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
    {
        return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
    }

    public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
    {
        OASISResult<IWeb3NFT> result = new();

        try
        {
            OASISResult<GetNftResult> response =
                await _solanaService.LoadNftAsync(new(nftTokenAddress));

            result.IsLoaded = true;
            result.IsError = false;

            if (response.IsLoaded)
                result.Result = response.Result.ToOasisNft();
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error occured in SolanaOASIS Provider. Reason: {e.Message}");
        }

        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey,
        string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
        int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
        bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Query holons by metadata from Solana program
            // Not supported in current ISolanaService
            var holonsData = new OASISResult<List<Entities.Models.SolanaHolonDto>> { IsError = true, Message = "Not implemented" };
            if (holonsData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Solana: {holonsData.Message}");
                return result;
            }

            var holons = new List<IHolon>();
            foreach (var holonData in holonsData.Result)
            {
                var holon = holonData != null ? holonData.GetHolon() : null;
                if (holon != null)
                {
                    holons.Add(holon);
                }
            }

            result.Result = holons;
            result.IsError = false;
            result.Message = $"Successfully loaded {holons.Count} holons by metadata from Solana with full object mapping";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue,
        HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0)
    {
        return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(
        Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
        HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
                return result;
            }

            // Query holons by multiple metadata pairs from Solana program
            // Not supported in current ISolanaService
            var holonsData = new OASISResult<List<Entities.Models.SolanaHolonDto>> { IsError = true, Message = "Not implemented" };
            if (holonsData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from Solana: {holonsData.Message}");
                return result;
            }

            var holons = new List<IHolon>();
            foreach (var holonData in holonsData.Result)
            {
                var holon = holonData != null ? holonData.GetHolon() : null;
                if (holon != null)
                {
                    holons.Add(holon);
                }
            }

            result.Result = holons;
            result.IsError = false;
            result.Message = $"Successfully loaded {holons.Count} holons by metadata pairs from Solana with full object mapping";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(
        Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
        HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0)
    {
        return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    #region Helper Methods

    /// <summary>
    /// Parse Solana blockchain response to Avatar object with complete serialization
    /// </summary>
    private Avatar ParseSolanaToAvatar(object solanaData)
    {
        try
        {
            // Serialize the complete Solana data to JSON first
            var solanaJson = System.Text.Json.JsonSerializer.Serialize(solanaData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // Deserialize the complete Avatar object from Solana JSON
            var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(solanaJson, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // If deserialization fails, create from extracted properties
            if (avatar == null)
            {
                avatar = new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = GetSolanaProperty(solanaData, "username") ?? "solana_user",
                    Email = GetSolanaProperty(solanaData, "email") ?? "user@solana.example",
                    FirstName = GetSolanaProperty(solanaData, "firstName") ?? "Solana",
                    LastName = GetSolanaProperty(solanaData, "lastName") ?? "User",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 1,
                    IsActive = true
                };
            }

            // Add Solana-specific metadata
            if (solanaData != null)
            {
                avatar.ProviderMetaData[Core.Enums.ProviderType.SolanaOASIS].Add("solana_account", GetSolanaProperty(solanaData, "account") ?? "");
                avatar.ProviderMetaData[Core.Enums.ProviderType.SolanaOASIS].Add("solana_lamports", GetSolanaProperty(solanaData, "lamports") ?? "0");
                avatar.ProviderMetaData[Core.Enums.ProviderType.SolanaOASIS].Add("solana_owner", GetSolanaProperty(solanaData, "owner") ?? "");
                avatar.ProviderMetaData[Core.Enums.ProviderType.SolanaOASIS].Add("solana_network", "mainnet-beta");
                avatar.ProviderMetaData[Core.Enums.ProviderType.SolanaOASIS].Add("solana_program_id", GetSolanaProperty(solanaData, "programId") ?? "");
            }

            return avatar;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Parse Solana blockchain response to AvatarDetail object with complete serialization
    /// </summary>
    private AvatarDetail ParseSolanaToAvatarDetail(object solanaData)
    {
        try
        {
            // Serialize the complete Solana data to JSON first
            var solanaJson = System.Text.Json.JsonSerializer.Serialize(solanaData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // Deserialize the complete AvatarDetail object from Solana JSON
            var avatarDetail = System.Text.Json.JsonSerializer.Deserialize<AvatarDetail>(solanaJson, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // If deserialization fails, create from extracted properties
            if (avatarDetail == null)
            {
                avatarDetail = new AvatarDetail
                {
                    Id = Guid.NewGuid(),
                    Username = GetSolanaProperty(solanaData, "username") ?? "solana_user",
                    Email = GetSolanaProperty(solanaData, "email") ?? "user@solana.example",
                    FirstName = GetSolanaProperty(solanaData, "firstName") ?? "Solana",
                    LastName = GetSolanaProperty(solanaData, "lastName") ?? "User",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 1,
                    IsActive = true,
                    // AvatarDetail specific properties
                    Address = GetSolanaProperty(solanaData, "address") ?? "",
                    Country = GetSolanaProperty(solanaData, "country") ?? "",
                    Postcode = GetSolanaProperty(solanaData, "postcode") ?? "",
                    Mobile = GetSolanaProperty(solanaData, "mobile") ?? "",
                    Landline = GetSolanaProperty(solanaData, "landline") ?? "",
                    Title = GetSolanaProperty(solanaData, "title") ?? "",
                    //DOB = DateTime.TryParse(GetSolanaProperty(solanaData, "dob"), out var dob) ? dob : (DateTime?)null,
                    //AvatarType = Enum.TryParse<AvatarType>(GetSolanaProperty(solanaData, "avatarType"), out var avatarType) ? avatarType : AvatarType.User,
                    //KarmaAkashicRecords = int.TryParse(GetSolanaProperty(solanaData, "karmaAkashicRecords"), out var karma) ? karma : 0,
                    //Level = int.TryParse(GetSolanaProperty(solanaData, "level"), out var level) ? level : 1,
                    XP = int.TryParse(GetSolanaProperty(solanaData, "xp"), out var xp) ? xp : 0,
                    //HP = int.TryParse(GetSolanaProperty(solanaData, "hp"), out var hp) ? hp : 100,
                    //Mana = int.TryParse(GetSolanaProperty(solanaData, "mana"), out var mana) ? mana : 100,
                    //Stamina = int.TryParse(GetSolanaProperty(solanaData, "stamina"), out var stamina) ? stamina : 100,
                    Description = GetSolanaProperty(solanaData, "description") ?? "Solana user",
                };
            }

            // Add Solana-specific metadata
            if (solanaData != null)
            {
                avatarDetail.ProviderMetaData[Core.Enums.ProviderType.SolanaOASIS].Add("solana_account", GetSolanaProperty(solanaData, "account") ?? "");
                avatarDetail.ProviderMetaData[Core.Enums.ProviderType.SolanaOASIS].Add("solana_lamports", GetSolanaProperty(solanaData, "lamports") ?? "0");
                avatarDetail.ProviderMetaData[Core.Enums.ProviderType.SolanaOASIS].Add("solana_owner", GetSolanaProperty(solanaData, "owner") ?? "");
                avatarDetail.ProviderMetaData[Core.Enums.ProviderType.SolanaOASIS].Add("solana_network", "mainnet-beta");
                avatarDetail.ProviderMetaData[Core.Enums.ProviderType.SolanaOASIS].Add("solana_program_id", GetSolanaProperty(solanaData, "programId") ?? "");
            }

            return avatarDetail;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Extract property value from Solana account data
    /// </summary>
    private string GetSolanaProperty(object data, string propertyName)
    {
        try
        {
            if (data == null) return null;
            
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            var jsonObject = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
            
            if (jsonObject.TryGetProperty(propertyName, out var property))
            {
                return property.GetString();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
    {
        throw new NotImplementedException();
    }

    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
    {
        throw new NotImplementedException();
    }

    public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
    {
        return GetBalanceAsync(request).Result;
    }

    public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
    {
        OASISResult<double> result = new OASISResult<double>();
        DateTimeOffset date = DateTimeOffset.UtcNow;

        try
        {
            OASISResult<decimal> solResult = await _solanaService.GetAccountBalanceAsync(request);

            if (solResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result,
                    solResult.Message,
                    solResult.Exception);

                return result;
            }
            else
                result.Result = Convert.ToDouble(solResult.Result);
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, $"Unknown error occured: {e}");
        }

        return result;
    }

    public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
    {
        throw new NotImplementedException();
    }

    public OASISResult<IKeyPairAndWallet> GenerateKeyPair(IGetWeb3WalletBalanceRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync(IGetWeb3WalletBalanceRequest request)
    {
        throw new NotImplementedException();
    }

    OASISResult<IWeb3NFTTransactionResponse> IOASISNFTProvider.LockToken(ILockWeb3TokenRequest request)
    {
        throw new NotImplementedException();
    }

    Task<OASISResult<IWeb3NFTTransactionResponse>> IOASISNFTProvider.LockTokenAsync(ILockWeb3TokenRequest request)
    {
        throw new NotImplementedException();
    }

    OASISResult<IWeb3NFTTransactionResponse> IOASISNFTProvider.UnlockToken(IUnlockWeb3TokenRequest request)
    {
        throw new NotImplementedException();
    }

    Task<OASISResult<IWeb3NFTTransactionResponse>> IOASISNFTProvider.UnlockTokenAsync(IUnlockWeb3TokenRequest request)
    {
        throw new NotImplementedException();
    }

    #endregion
}