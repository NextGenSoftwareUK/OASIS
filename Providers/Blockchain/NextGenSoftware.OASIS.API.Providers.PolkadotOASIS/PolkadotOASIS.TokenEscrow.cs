using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    private Task<OASISResult<ITransactionResponse>> ChangeOwnedTokenEscrowAsync(ILockWeb3TokenRequest locking,IUnlockWeb3TokenRequest unlocking)
        =>ExecuteOwnedMutationAsync(async owner=>{
            if((locking==null)==(unlocking==null))throw new ArgumentException("Exactly one token lock or release request is required.");
            var config=_tokenEscrowConfiguration??throw new InvalidOperationException("Configure the reviewed PSP22 per-deposit escrow before token custody.");
            // Freeze every caller-owned field before wallet or SDK awaits.
            var isLock=locking!=null;
            var actor=isLock?locking.LockedByAvatarId:unlocking.UnlockedByAvatarId;
            var lockId=isLock?locking.LockId:unlocking.LockId;
            var web3Id=isLock?locking.Web3TokenId:unlocking.Web3TokenId;
            var amount=isLock?locking.Amount:0;
            var contractAlias=isLock?locking.EscrowContractAddress:unlocking.EscrowContractAddress;
            var tokenAddress=NativeAccount(isLock?locking.TokenAddress:unlocking.TokenAddress);
            var senderAlias=locking?.FromWalletAddress;
            var suppliedKey=locking?.FromWalletPrivateKey;
            if(actor==Guid.Empty||lockId==Guid.Empty)throw new ArgumentException("Authenticated actor and stable original LockId are required.");
            if(web3Id!=Guid.Empty)throw new ArgumentException("Resolve the application token record to its explicit PSP22 address before native custody; a GUID is not a native token locator.");
            if(isLock&&amount<=0)throw new ArgumentException("Lock requires a positive exact typed Amount; no lock-all or fixed amount is substituted.");
            if(!string.IsNullOrWhiteSpace(senderAlias))senderAlias=NativeAccount(senderAlias);
            if(!string.IsNullOrWhiteSpace(contractAlias))contractAlias=NativeAccount(contractAlias);
            var loaded=await ResolveAssetMintWalletAsync(actor).ConfigureAwait(false);
            owner.Token.ThrowIfCancellationRequested();
            if(loaded.IsError||loaded.Result==null)throw loaded.Exception??new InvalidOperationException(loaded.Message);
            var wallet=loaded.Result;
            if(wallet.AvatarId!=actor||wallet.ProviderType!=Core.Enums.ProviderType.PolkadotOASIS)
                throw new InvalidOperationException("Escrow signing wallet does not belong to the requesting avatar/provider.");
            var publicKey=wallet.PublicKey;
            var privateKey=string.IsNullOrWhiteSpace(suppliedKey)?wallet.PrivateKey:suppliedKey;
            var addresses=new[]{tokenAddress,NativeAccount(wallet.WalletAddress),senderAlias,contractAlias};
            var canonical=await NormalizeNativeAccountsAsync(addresses,owner.Token).ConfigureAwait(false);
            tokenAddress=canonical[0];var sender=canonical[1];senderAlias=canonical[2];contractAlias=canonical[3];
            if(senderAlias!=null&&senderAlias!=sender)throw new ArgumentException("Requested token source differs from the actor's default signing wallet.");
            if(contractAlias!=null&&contractAlias!=config.ContractAccountId)throw new ArgumentException("Requested escrow differs from the explicitly configured contract.");
            var depositId="0x"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(lockId.ToString("N")))).ToLowerInvariant();
            var request=new Dictionary<string,object> {
                ["operation"]="prepareTokenEscrowMutation",["endpoint"]=_rpcEndpoint,["genesisHash"]=ReportedGenesisHash,
                ["action"]=isLock?"lock":"unlock",["depositId"]=depositId,["tokenAddress"]=tokenAddress,
                ["contractAddress"]=config.ContractAccountId,["contractCodeHash"]=config.CodeHash,
                ["sender"]=sender,["recipient"]=config.ContractAccountId,["publicKey"]=publicKey,["privateKey"]=privateKey };
            if(isLock)request["amount"]=amount.ToString(CultureInfo.InvariantCulture);
            var prepared=await InvokePolkadotSdkAsync(request,owner.Token).ConfigureAwait(false);
            if(prepared.GetProperty("action").GetString()!=(isLock?"lock":"unlock")
                ||prepared.GetProperty("depositId").GetString()!=depositId||prepared.GetProperty("tokenAddress").GetString()!=tokenAddress
                ||prepared.GetProperty("sender").GetString()!=sender||prepared.GetProperty("contractAddress").GetString()!=config.ContractAccountId
                ||prepared.GetProperty("contractCodeHash").GetString()!=config.CodeHash)
                throw new InvalidDataException("SDK preparation differs from the frozen escrow actor/token/deposit/contract.");
            return new OASISResult<JsonElement>{Result=prepared};
        },NativeMutationKind.TokenEscrow);

    private static void ValidateFinalizedTokenEscrowEffect(JsonElement original,JsonElement receipt)
    {
        if(!receipt.GetProperty("tokenEscrowEffectMatchesRequest").GetBoolean()
            ||receipt.GetProperty("action").GetString()!=original.GetProperty("action").GetString()
            ||receipt.GetProperty("contractAddress").GetString()!=original.GetProperty("contractAddress").GetString()
            ||receipt.GetProperty("depositId").GetString()!=original.GetProperty("depositId").GetString()
            ||receipt.GetProperty("tokenAddress").GetString()!=original.GetProperty("tokenAddress").GetString()
            ||receipt.GetProperty("amountUnits").GetString()!=original.GetProperty("expectedAmountUnits").GetString())
            throw new InvalidDataException("Finalized token custody differs from the original action/deposit/token/amount; reconcile, never replay.");
    }

    public async Task<OASISResult<JsonElement>> RecoverTokenEscrowPreparationAsync(UsageProviderAttempt attempt,CancellationToken token=default)
    {
        var result=new OASISResult<JsonElement>();
        try {
            var config=_tokenEscrowConfiguration??throw new InvalidOperationException("Configure the reviewed token escrow before recovery.");
            if(!_isActivated)throw new InvalidOperationException("Activate the verified network before escrow recovery.");
            var snapshot=NativePreparedAttemptSnapshot.Capture(attempt,"PolkadotOASIS",ReportedGenesisHash);
            var original=new { chainId=snapshot.ChainId,sender=snapshot.Sender,transactionHash=snapshot.ProviderRequestId,
                signedTransactionBase64=snapshot.SignedTransactionBase64,signDocBase64=snapshot.SignDocBase64,signature=snapshot.Signature };
            var recovered=await InvokePolkadotSdkAsync(new { operation="recoverTokenEscrowPreparation",endpoint=_rpcEndpoint,
                genesisHash=ReportedGenesisHash,contractCodeHash=config.CodeHash,preparation=original },token).ConfigureAwait(false);
            if(recovered.GetProperty("transactionHash").GetString()!=snapshot.ProviderRequestId
                ||recovered.GetProperty("sender").GetString()!=snapshot.Sender||recovered.GetProperty("chainId").GetString()!=snapshot.ChainId
                ||recovered.GetProperty("contractAddress").GetString()!=config.ContractAccountId
                ||recovered.GetProperty("contractCodeHash").GetString()!=config.CodeHash
                ||!recovered.GetProperty("recoveredFromSignedOriginal").GetBoolean())
                throw new InvalidDataException("Recovered token custody does not bind the retained original and configured escrow.");
            result.Result=recovered;result.Message="Original escrow reconstructed read-only; reconcile exact native effects, never replay.";
        } catch(Exception failure){OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        return result;
    }
}
