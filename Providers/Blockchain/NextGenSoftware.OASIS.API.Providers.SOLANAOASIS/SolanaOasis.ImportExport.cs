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
    public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
    {
        var result=new OASISResult<bool>();
        try
        {
            ArgumentNullException.ThrowIfNull(holons);
            var planned=holons.ToList();
            if(planned.Count==0) throw new ArgumentException("At least one holon is required for import.");
            var saved=await SaveHolonsAsync(planned,saveChildren:true,recursive:true,continueOnError:false,saveChildrenOnProvider:true);
            result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,bool>(saved);
            result.Result=!saved.IsError && saved.IsSaved;
        }
        catch(Exception cause)
        {
            OASISErrorHandling.HandleError(ref result,"Solana import failed: "+cause.Message,cause);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId,
        int version = 0)
    {
        return await ExportStoredHolonsForAvatarAsync(avatarId,version);
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(
        string avatarUsername, int version = 0)
    {
        return await ExportStoredHolonsForSelectorAsync(avatarUsername,false,version);
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(
        string avatarEmailAddress, int version = 0)
    {
        return await ExportStoredHolonsForSelectorAsync(avatarEmailAddress,true,version);
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
    {
        return await ReadHolonCollectionAsync(row=>true,true,true,0,0,true,version);
    }

    public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
    {
        return ImportAsync(holons).GetAwaiter().GetResult();
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
    {
        return ExportAllDataForAvatarByIdAsync(avatarId, version).GetAwaiter().GetResult();
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername,
        int version = 0)
    {
        return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress,
        int version = 0)
    {
        return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).GetAwaiter().GetResult();
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
    {
        return ExportAllAsync(version).GetAwaiter().GetResult();
    }

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
    }

    public Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        => SendAvatarTransactionAsync(AvatarTransferSelector.Id, fromAvatarId.ToString(), toAvatarId.ToString(), amount, token);

    public Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        => SendAvatarTransactionAsync(AvatarTransferSelector.Username, fromAvatarUsername, toAvatarUsername, amount, token);

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
    }
}
