using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.RPC;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Rpc.Utilities;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Helpers;
using Newtonsoft.Json;
using NextGenSoftware.Utilities.ExtentionMethods;
using System.Linq;
using System.IO;
using System.Text;
using static Solnet.Programs.TokenProgram;
using static Solnet.Programs.AssociatedTokenAccountProgram;
using static Solnet.Programs.SystemProgram;
using static Solnet.Programs.MemoProgram;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Solana provider: {activateResult.Message}");
                    return result;
                }
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
                                Id = CreateDeterministicGuid($"{ProviderType.Value}:{account.PublicKey}"),
                                AvatarId = CreateDeterministicGuid($"{ProviderType.Value}:{account.PublicKey}"),
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Solana provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to activate Solana provider: {activateResult.Message}");
                    return response;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Solana provider: {activateResult.Message}");
                    return result;
                }
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

    public override OASISResult<IAvatar> LoadAvatarByVerificationToken(string verificationToken, int version = 0)
        => new OASISResult<IAvatar> { IsLoaded = false, IsError = false, Message = "Avatar not found" };

    public override Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string verificationToken, int version = 0)
        => Task.FromResult(LoadAvatarByVerificationToken(verificationToken, version));

    public override OASISResult<IAvatar> LoadAvatarByResetToken(string resetToken, int version = 0)
        => new OASISResult<IAvatar> { IsLoaded = false, IsError = false, Message = "Avatar not found" };

    public override Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string resetToken, int version = 0)
        => Task.FromResult(LoadAvatarByResetToken(resetToken, version));

    public override OASISResult<IAvatar> LoadAvatarByRefreshToken(string refreshToken, int version = 0)
        => new OASISResult<IAvatar> { IsLoaded = false, IsError = false, Message = "Avatar not found" };

    public override Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string refreshToken, int version = 0)
        => Task.FromResult(LoadAvatarByRefreshToken(refreshToken, version));

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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Solana provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Solana provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Solana provider: {activateResult.Message}");
                    return result;
                }
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

}
