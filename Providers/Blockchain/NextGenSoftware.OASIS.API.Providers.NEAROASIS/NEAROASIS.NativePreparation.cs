using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

/// <summary>Immutable native intent. Credentials belong to the configured signing owner, never this DTO.</summary>
public sealed class NearNativeAction
{
    public string Kind{get;}public string Recipient{get;}public string DepositUnits{get;}
    public string MethodName{get;}public string GasUnits{get;}public string ArgumentsBase64{get;}
    public NearNativeAction(string kind,string recipient,string depositUnits,string methodName=null,string gasUnits=null,string argumentsBase64=null)
    {Kind=kind;Recipient=recipient;DepositUnits=depositUnits;MethodName=methodName;GasUnits=gasUnits;ArgumentsBase64=argumentsBase64;}
}

public partial class NEAROASIS : IPreparedNativeStorageProvider
{
    /// <summary>Executes one native action under an EXISTING admission; never creates a reservation or authorizes spending.</summary>
    public async Task<OASISResult<JsonElement>> ExecutePreparedNativeActionAsync(NearNativeAction request,UsageExecutionHandle execution,
        string clientRequestId,string expectedSender,CancellationToken cancellationToken=default)
    {
        var result=new OASISResult<JsonElement>();string originalId=null;var submissionStarted=false;
        try {
            ArgumentNullException.ThrowIfNull(execution);
            if(_sdk.HasPreparationOwner)throw new InvalidOperationException("Explicit native execution cannot replace an existing NEAR operation owner.");
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(clientRequestId);
            if(string.IsNullOrWhiteSpace(expectedSender)||expectedSender!=_accountId)
                throw new ArgumentException("Expected NEAR signer must equal this configured execution wallet.");
            if(execution.RequiresVerifiedNativeFeeMaximums)throw new UsageProtocolException("NEAR_VERIFIED_FEE_MAXIMUM_REQUIRED",
                "Full verified native fee maxima are required before sponsored NEAR execution.",400);
            if(!_isActivated||_chainId!=_networkId)throw new InvalidOperationException("Activate a consistently configured NEAR network before prepared execution.");
            cancellationToken.ThrowIfCancellationRequested();
            var intent=new Dictionary<string,object>{{"kind",request.Kind},{"recipient",request.Recipient},{"depositUnits",request.DepositUnits}};
            if(request.Kind=="functionCall"){
                intent["methodName"]=request.MethodName;intent["gasUnits"]=request.GasUnits;intent["argsBase64"]=request.ArgumentsBase64;
            } else if(request.Kind!="transfer"||request.MethodName!=null||request.GasUnits!=null||request.ArgumentsBase64!=null)
                throw new ArgumentException("Explicit transfer or function-call intent required; no ignored fields.");
            var expectedIntent=JsonSerializer.SerializeToElement(intent);
            var nativeRequest=new Dictionary<string,object>(intent){{"sender",_accountId},{"chainId",_networkId}};
            var preparation=await _sdk.PrepareNativeActionAsync(nativeRequest,cancellationToken).ConfigureAwait(false);
            originalId=preparation.GetProperty("transactionHash").GetString();
            Task Verify(JsonElement evidence,CancellationToken token)=>VerifyPreparedIntentAsync(evidence,expectedIntent,expectedSender,originalId,token);
            await Verify(preparation,cancellationToken).ConfigureAwait(false);
            using var server=new PreparedSubmissionServer(execution.NativePreparationPersistence("NEAROASIS",null,clientRequestId,
                _networkId,expectedSender,Verify),originalId);
            submissionStarted=true;
            var submitted=await _sdk.SubmitNativePreparationAsync(preparation,server,cancellationToken).ConfigureAwait(false);
            if(submitted.GetProperty("transactionHash").GetString()!=originalId)throw new InvalidDataException("Submission returned a different original identity.");
            var outcome=submitted.GetProperty("outcome");
            var status=NearSdkService.ValidatePreparedOutcome(preparation,outcome);
            if(status=="Failed")throw new NearMutationRejectedException(originalId,outcome.GetProperty("status").GetRawText());
            if(status!="Completed")throw new InvalidDataException("Original NEAR execution is not finalized.");
            result.Result=submitted.Clone();result.IsSaved=true;
            result.Message="Original native execution finalized; application effects and billing settlement remain separate.";
        } catch(Exception failure){
            if(submissionStarted&&failure is not INativeMutationReconciliationRequired&&failure is not NearMutationRejectedException)
                failure=new NearMutationUncertainException(originalId,failure);
            OASISErrorHandling.HandleError(ref result,failure.Message,failure);
        }
        return result;
    }

    /// <summary>Uses the existing async-flow owner for every physical mutation in an admitted ordinary provider operation.</summary>
    public Task<T> ExecutePreparedStorageAsync<T>(Func<Task<T>> operation,UsageExecutionHandle execution,
        string clientRequestId,string expectedSender,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(operation);ArgumentNullException.ThrowIfNull(execution);
        token.ThrowIfCancellationRequested();
        if(!_isActivated||_chainId!=_networkId)throw new InvalidOperationException("Activate a consistently configured NEAR network before prepared storage.");
        if(string.IsNullOrWhiteSpace(expectedSender)||expectedSender!=_accountId)throw new ArgumentException("Expected NEAR signer must equal this configured execution wallet.");
        if(execution.RequiresVerifiedNativeFeeMaximums)throw new UsageProtocolException("NEAR_VERIFIED_FEE_MAXIMUM_REQUIRED",
            "Full verified native fee maxima are required before sponsored NEAR execution.",400);
        var correlation=new NativeStorageCorrelation(clientRequestId);
        return _sdk.WithPreparationAsync(operation,(_,context)=>{
            using var parsed=JsonDocument.Parse(context);
            if(parsed.RootElement.GetProperty("sender").GetString()!=expectedSender)
                throw new InvalidOperationException("Ordinary native operation cannot replace its admitted signer.");
            var intent=parsed.RootElement.GetProperty("intent").Clone();
            return new PreparedSubmissionServer(execution.NativePreparationPersistence("NEAROASIS",null,correlation.Next(),_networkId,expectedSender,
                (evidence,ct)=>VerifyPreparedIntentAsync(evidence,intent,expectedSender,null,ct)));
        },token);
    }

    private async Task VerifyPreparedIntentAsync(JsonElement evidence,JsonElement expectedIntent,string expectedSender,string originalId,CancellationToken token)
    {
        var verified=await _sdk.VerifyNativePreparationAsync(evidence,token).ConfigureAwait(false);
        if(!verified.GetProperty("verified").GetBoolean()||verified.GetProperty("chainId").GetString()!=_networkId
            ||verified.GetProperty("sender").GetString()!=expectedSender
            ||verified.GetProperty("transactionHash").GetString()!=evidence.GetProperty("transactionHash").GetString()
            ||(originalId!=null&&verified.GetProperty("transactionHash").GetString()!=originalId)
            ||!JsonElement.DeepEquals(verified.GetProperty("intent"),expectedIntent))
            throw new InvalidDataException("Original signed NEAR identity/action differs from the admitted immutable request.");
    }

    /// <summary>Reconstructs persisted signed bytes read-only. Does not sign, replay, settle fees or declare completion.</summary>
    public async Task<OASISResult<JsonElement>> RecoverNativePreparationAsync(UsageProviderAttempt attempt,CancellationToken cancellationToken=default)
    {
        var result=new OASISResult<JsonElement>();
        try {
            if(!_isActivated||_chainId!=_networkId)throw new InvalidOperationException("Activate the configured NEAR network before recovery.");
            var snapshot=NativePreparedAttemptSnapshot.Capture(attempt,"NEAROASIS",_networkId);
            var recovered=await _sdk.RecoverNativePreparationAsync(new{chainId=snapshot.ChainId,sender=snapshot.Sender,
                transactionHash=snapshot.ProviderRequestId,signedTransactionBase64=snapshot.SignedTransactionBase64,
                signDocBase64=snapshot.SignDocBase64,signature=snapshot.Signature},cancellationToken).ConfigureAwait(false);
            if(recovered.GetProperty("transactionHash").GetString()!=snapshot.ProviderRequestId||recovered.GetProperty("sender").GetString()!=snapshot.Sender
                ||recovered.GetProperty("chainId").GetString()!=_networkId||!recovered.GetProperty("recoveredFromSignedOriginal").GetBoolean())
                throw new InvalidDataException("Recovered NEAR original differs from the persisted identity.");
            result.Result=recovered.Clone();result.Message="Original recovered read-only; reconcile native outcome, never replay.";
        } catch(Exception failure){OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        return result;
    }

    /// <summary>Read-only exact-original observation; a native Failure is a known outcome, never permission to replay.</summary>
    public async Task<OASISResult<JsonElement>> ReadPreparedNativeOutcomeAsync(JsonElement preparation,CancellationToken cancellationToken=default)
    {
        var result=new OASISResult<JsonElement>();
        try {
            if(!_isActivated||_chainId!=_networkId)throw new InvalidOperationException("Activate the configured NEAR network before observing original outcomes.");
            var original=preparation.Clone();
            var outcome=await _sdk.ReadPreparedNativeOutcomeAsync(original,cancellationToken).ConfigureAwait(false);
            var status=NearSdkService.ValidatePreparedOutcome(original,outcome);
            result.Result=outcome.Clone();result.IsLoaded=true;
            result.Message="Observed original native status "+status+"; no submission or billing settlement performed.";
        } catch(Exception failure){OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        return result;
    }
}
