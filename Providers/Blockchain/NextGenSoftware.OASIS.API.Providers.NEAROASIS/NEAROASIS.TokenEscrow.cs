using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)=>LockTokenAsync(request).GetAwaiter().GetResult();
    public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        =>SetTokenLockAsync(true,request?.TokenAddress,request?.LockId??Guid.Empty,request?.Amount??0,
            request?.EscrowContractAddress,request?.LockedByAvatarId??Guid.Empty,request?.FromWalletAddress,request?.FromWalletPrivateKey);
    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)=>UnlockTokenAsync(request).GetAwaiter().GetResult();
    public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        =>SetTokenLockAsync(false,request?.TokenAddress,request?.LockId??Guid.Empty,0,request?.EscrowContractAddress,request?.UnlockedByAvatarId??Guid.Empty,null,null);

    protected virtual Task<OASISResult<IProviderWallet>> LoadTokenSigningWalletAsync(Guid actor)
        =>WalletManager.GetAvatarDefaultWalletByIdAsync(actor,Core.Enums.ProviderType.NEAROASIS,showPrivateKeys:true);

    private async Task<(string Account,string Key)> ResolveTokenLockSignerAsync(Guid actor,string requestedFrom,string suppliedKey)
    {
        if(!string.IsNullOrWhiteSpace(suppliedKey)){
            if(string.IsNullOrWhiteSpace(requestedFrom))throw new ArgumentException("An explicit private key requires its actual NEAR account address.");
            var known=await ResolveNearExplicitKeyAsync(suppliedKey,null);
            return await ResolveNearSignerAsync(requestedFrom,suppliedKey,null,known);
        }
        if(actor!=Guid.Empty){
            var loaded=await LoadTokenSigningWalletAsync(actor);
            if(loaded==null||loaded.IsError||loaded.IsWarning||loaded.Result==null||loaded.Result.AvatarId!=actor
                ||loaded.Result.ProviderType!=Core.Enums.ProviderType.NEAROASIS||string.IsNullOrWhiteSpace(loaded.Result.PrivateKey)
                ||string.IsNullOrWhiteSpace(loaded.Result.WalletAddress))
                throw new InvalidOperationException("The exact requesting avatar's NEAR signing wallet is required.",loaded?.Exception);
            if(!string.IsNullOrWhiteSpace(requestedFrom)&&requestedFrom!=loaded.Result.WalletAddress)
                throw new ArgumentException("Requested source account differs from the avatar's signing wallet.");
            return await ResolveNearSignerAsync(loaded.Result.WalletAddress,loaded.Result.PrivateKey,loaded.Result.PublicKey);
        }
        if(!string.IsNullOrWhiteSpace(requestedFrom)&&requestedFrom!=_accountId)
            throw new ArgumentException("An independent NEAR account requires its own signing wallet/key.");
        return await ResolveNearSignerAsync(_accountId,null,null);
    }

    private static BigInteger ExactTokenUnits(string value,bool positive=false)
    {
        if(string.IsNullOrEmpty(value)||value.Length>78||!BigInteger.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out var amount)
            ||amount.Sign<0||amount.ToString(CultureInfo.InvariantCulture)!=value||amount>=(BigInteger.One<<256)||positive&&amount.IsZero)
            throw new InvalidDataException("Canonical exact unsigned token units required.");
        return amount;
    }
    private static BigInteger VerifyTokenDeposit(JsonElement record,Guid id,string symbol,string owner,bool released,string amount=null)
    {
        if(record.ValueKind!=JsonValueKind.Object||record.EnumerateObject().Count()!=6||record.GetProperty("protocol").GetInt32()!=1
            ||record.GetProperty("lock_id").GetString()!=id.ToString("N")||record.GetProperty("symbol").GetString()!=symbol
            ||record.GetProperty("owner_id").GetString()!=owner||record.GetProperty("released").GetBoolean()!=released)
            throw new InvalidDataException("Native deposit differs from the original requested ID/token/owner/state.");
        var exact=record.GetProperty("amount").GetString();var quantity=ExactTokenUnits(exact,true);
        if(amount!=null&&exact!=amount)throw new InvalidDataException("Native deposit amount differs from the exact requested units.");
        return quantity;
    }

    private async Task<OASISResult<ITransactionResponse>> SetTokenLockAsync(bool locking,string symbol,Guid id,decimal amount,
        string escrow,Guid actor,string from,string key)
    {
        var result=new OASISResult<ITransactionResponse>();string acknowledged=null;
        try{
            if(string.IsNullOrWhiteSpace(symbol))throw new ArgumentException("Token address is required.");
            if(_chainId!=_networkId||string.IsNullOrWhiteSpace(_contractAddress))throw new InvalidOperationException("Consistent explicit NEAR network/contract required.");
            var custody=id!=Guid.Empty||amount!=0||escrow!=null;
            string units=null;
            if(custody){
                if(id==Guid.Empty||locking&&amount<=0||escrow!=null&&(string.IsNullOrWhiteSpace(escrow)||escrow!=_contractAddress))
                    throw new ArgumentException("Explicit custody requires an original deposit ID, positive lock amount and matching configured escrow contract.");
                if(locking)units=ToTokenUnits(amount);
            }
            var signer=await ResolveTokenLockSignerAsync(actor,from,key);
            if(!await EnsureSdkActiveAsync(result))return result;
            if(!custody){
                var token=await _sdk.ViewAsync("token_get",new{symbol});
                if(token.ValueKind!=JsonValueKind.Object||token.GetProperty("symbol").GetString()!=symbol||token.GetProperty("issuer_id").GetString()!=signer.Account)
                    throw new ArgumentException("Only this token's actual issuer may change its global trading freeze.");
                acknowledged=await _sdk.CallAsync("token_set_locked",new{symbol,locked=locking},signerAccountId:signer.Account,signerPrivateKey:signer.Key);
                var observed=await _sdk.ViewAsync("token_get",new{symbol});
                if(observed.ValueKind!=JsonValueKind.Object||observed.GetProperty("symbol").GetString()!=symbol
                    ||observed.GetProperty("issuer_id").GetString()!=signer.Account||observed.GetProperty("locked").GetBoolean()!=locking)
                    throw new InvalidDataException("Native global token freeze state differs after acknowledgement.");
                result.Result=new TransactionResponse{TransactionResult=acknowledged};result.Message=locking?"Token trading frozen.":"Token trading unfrozen.";return result;
            }
            var protocol=await _sdk.ViewAsync("token_escrow_protocol",new{});
            if(protocol.ValueKind!=JsonValueKind.Number||!protocol.TryGetInt32(out var version)||version!=1)
                throw new InvalidOperationException("The selected contract does not expose token escrow protocol1; no transaction submitted.");
            var original=await _sdk.ViewAsync("token_lock_get",new{lock_id=id.ToString("N")});
            var quantity=locking?ExactTokenUnits(units,true):VerifyTokenDeposit(original,id,symbol,signer.Account,false);
            if(locking&&original.ValueKind!=JsonValueKind.Null)throw new ArgumentException("This deposit ID already exists; never replace/replay it.");
            var before=await ReadCustomTokenUnitsAsync(symbol,signer.Account);
            if(locking&&before<quantity)throw new ArgumentException("Insufficient unlocked token balance for this exact deposit.");
            acknowledged=locking?await _sdk.CallAsync("token_lock",new{symbol,lock_id=id.ToString("N"),amount=units},signerAccountId:signer.Account,signerPrivateKey:signer.Key)
                :await _sdk.CallAsync("token_release",new{symbol,lock_id=id.ToString("N")},signerAccountId:signer.Account,signerPrivateKey:signer.Key);
            var after=await _sdk.ViewAsync("token_lock_get",new{lock_id=id.ToString("N")});
            VerifyTokenDeposit(after,id,symbol,signer.Account,!locking,quantity.ToString(CultureInfo.InvariantCulture));
            var balance=await ReadCustomTokenUnitsAsync(symbol,signer.Account);
            if(balance!=(locking?before-quantity:before+quantity))throw new InvalidDataException("Native unlocked balance differs after the acknowledged custody operation.");
            result.Result=new TransactionResponse{TransactionResult=acknowledged};result.Message=locking?"Token deposit locked.":"Original token deposit released.";
        }catch(Exception failure){
            if(!string.IsNullOrWhiteSpace(acknowledged))failure=new NativeTransactionFollowupException(Core.Enums.ProviderType.NEAROASIS,acknowledged,failure);
            OASISErrorHandling.HandleError(ref result,failure.Message,failure);
        }
        return result;
    }
}
