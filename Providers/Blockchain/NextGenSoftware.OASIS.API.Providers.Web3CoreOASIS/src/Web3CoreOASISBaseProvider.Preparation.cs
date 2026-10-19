using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using Nethereum.RPC.Eth.DTOs;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public partial class Web3CoreOASISBaseProvider : NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeStorageProvider
{
    /// <summary>Verify retained original evidence against trusted mutation context, then inspect only its receipt. Never submits or settles.</summary>
    public async Task<OASISResult<TransactionReceipt>> GetPreparedNativeReceiptAsync(UsageProviderAttempt attempt,string expectedCalldata,CancellationToken token=default)
    {
        var result=new OASISResult<TransactionReceipt>();
        try
        {
            ArgumentNullException.ThrowIfNull(attempt);token.ThrowIfCancellationRequested();
            if(_web3CoreOASIS==null || !_expectedChainId.HasValue || _expectedChainId.Value<=0) throw new InvalidOperationException("Activated EVM provider with explicit chain identity required.");
            var preparation=attempt.NativePreparation;
            if(attempt.Provider!=ProviderType.Value.ToString() || preparation==null || preparation.ChainId!=_expectedChainId.Value.ToString(CultureInfo.InvariantCulture)
                || !string.Equals(preparation.Sender,_web3CoreOASIS.SigningAddress,StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Retained EVM attempt differs from configured provider, chain or signer.");
            if(string.IsNullOrWhiteSpace(expectedCalldata) || expectedCalldata.Length>2097154) throw new ArgumentException("Bounded trusted expected calldata required.",nameof(expectedCalldata));
            preparation.ValidateAndMeasure();
            Web3SignedPreparation.Verify(Convert.FromBase64String(preparation.SignedTransactionBase64),attempt.ProviderRequestId,_expectedChainId.Value,
                _web3CoreOASIS.SigningAddress,_contractAddress,expectedCalldata.HexToByteArray());
            if(await _web3CoreOASIS.ReadChainIdAsync().WaitAsync(token)!=_expectedChainId.Value) throw new InvalidDataException("Recovery RPC chain differs from retained signed attempt.");
            var receipt=await _web3CoreOASIS.ReadOriginalReceiptAsync(attempt.ProviderRequestId).WaitAsync(token);
            if(receipt==null)
            {result.IsError=true;result.ErrorCode="OASIS_NATIVE_OUTCOME_UNRESOLVED";result.Message="Original receipt unavailable; absence is not proof of non-submission. Do not replay.";return result;}
            if(!string.Equals(receipt.TransactionHash,attempt.ProviderRequestId,StringComparison.OrdinalIgnoreCase)
                || !string.Equals(receipt.From,preparation.Sender,StringComparison.OrdinalIgnoreCase)
                || !string.Equals(receipt.To,_contractAddress,StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(receipt.BlockHash) || receipt.BlockNumber==null || receipt.Status==null
                || (receipt.Status.Value!=0 && receipt.Status.Value!=1)) throw new InvalidDataException("Original EVM receipt identity or execution evidence is invalid.");
            result.Result=receipt;result.IsLoaded=true;result.Message=receipt.Status.Value==1 ? "Original transaction executed; no submission or settlement performed.":"Original transaction reverted; no submission or settlement performed.";
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    /// <summary>Bind every physical contract mutation in this explicit scope to an admitted durable usage owner.</summary>
    public Task<T> ExecutePreparedStorageAsync<T>(Func<Task<T>> operation,UsageExecutionHandle execution,
        string clientRequestId,string expectedSender,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(operation);ArgumentNullException.ThrowIfNull(execution);token.ThrowIfCancellationRequested();
        if(string.IsNullOrWhiteSpace(clientRequestId) || clientRequestId.Length>110) throw new ArgumentException("Bounded storage correlation required.",nameof(clientRequestId));
        if(_web3CoreOASIS==null || !_expectedChainId.HasValue || _expectedChainId.Value<=0) throw new InvalidOperationException("Activated EVM provider with explicit chain identity required.");
        if(!string.Equals(expectedSender,_web3CoreOASIS.SigningAddress,StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Admitted sender differs from configured EVM signing account.");
        var sdk=_web3CoreOASIS;
        var persist=EvmStoragePreparation.Bind(execution,ProviderType.Value.ToString(),clientRequestId,_expectedChainId.Value,sdk.SigningAddress,_contractAddress);
        return sdk.WithCancellableNativePreparationAsync(operation,evidence=>persist(evidence,token),token);
    }
}
