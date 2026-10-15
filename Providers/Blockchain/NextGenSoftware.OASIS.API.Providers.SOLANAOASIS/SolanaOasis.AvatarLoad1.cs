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
            var health = await _rpcClient.GetHealthAsync().WaitAsync(TimeSpan.FromSeconds(10));
            if (!health.WasSuccessful || !string.Equals(health.Result, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Solana RPC health check failed: {health.Reason ?? health.RawRpcResponse}. ");

            _solanaRepository = new SolanaRepository(_oasisSolanaAccount, _rpcClient);
            _solanaService = new SolanaService(_oasisSolanaAccount, _rpcClient);

            result.Result = true;
            IsProviderActivated = true;
        }
        catch (Exception e)
        {
            _solanaRepository = null;
            _solanaService = null;
            IsProviderActivated = false;
            OASISErrorHandling.HandleError(ref result,
                $"Unknown Error Occured In SolanaOASIS Provider in ActivateProviderAsync. Reason: {e}");
        }

        return result;
    }

    public override OASISResult<bool> ActivateProvider()
    {
        return ActivateProviderAsync().GetAwaiter().GetResult();
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
        var result=new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if(version != 0) throw new NotSupportedException("Solana collection reads currently require latest version (0).");
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) throw new InvalidOperationException(activation.Message,activation.Exception);
            }
            var records=await _solanaRepository.GetAllAsync<SolanaAvatarDto>();
            var mapped=new List<IAvatar>();
            foreach(var entry in records)
            {
                var record=entry.Record.GetAvatar();
                record.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS]=entry.Signature;
                mapped.Add(record);
            }
            result.Result=mapped;
            result.IsLoaded=true;
        }
        catch(Exception cause) { OASISErrorHandling.HandleError(ref result,"Solana finalized collection read failed: "+cause.Message,cause); }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
    {
        return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
    }

    public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        => FindStoredIdentityAsync(!string.IsNullOrWhiteSpace(avatarUsername),()=>LoadAllAvatarsAsync(version),record=>string.Equals(record.Username,avatarUsername,StringComparison.OrdinalIgnoreCase));

    public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        => FindStoredIdentityAsync(Id != Guid.Empty,()=>LoadAllAvatarsAsync(version),record=>record.Id==Id);

    public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        => FindStoredIdentityAsync(!string.IsNullOrWhiteSpace(avatarEmail),()=>LoadAllAvatarsAsync(version),record=>string.Equals(record.Email,avatarEmail,StringComparison.OrdinalIgnoreCase));

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

    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        => FindStoredIdentityAsync(id != Guid.Empty,()=>LoadAllAvatarDetailsAsync(version),record=>record.Id==id);

    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        => FindStoredIdentityAsync(!string.IsNullOrWhiteSpace(avatarUsername),()=>LoadAllAvatarDetailsAsync(version),record=>string.Equals(record.Username,avatarUsername,StringComparison.OrdinalIgnoreCase));

    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        => FindStoredIdentityAsync(!string.IsNullOrWhiteSpace(avatarEmail),()=>LoadAllAvatarDetailsAsync(version),record=>string.Equals(record.Email,avatarEmail,StringComparison.OrdinalIgnoreCase));

}
