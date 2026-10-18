using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

public partial class TRONOASIS : IPreparedNativeStorageProvider,IPreparedNativeFeeEvidenceReader
{
    string IPreparedNativeFeeEvidenceReader.NativeFeeProvider=>"TRONOASIS";
    string IPreparedNativeFeeEvidenceReader.NativeFeeChainId=>_nativeTransfers!=null?_nativeTransfers.ExpectedGenesisBlockId:_genesisBlockId;
    NativeFeeCoverage IPreparedNativeFeeEvidenceReader.DeclaredFeeCoverage=>NativeFeeCoverage.NetFinal;

    public async Task<NextGenSoftware.OASIS.Common.OASISResult<PreparedNativeFeeEvidence>> ReadPreparedNativeFeeEvidenceAsync(
        UsageProviderAttempt original,CancellationToken token=default)
    {
        var result=new NextGenSoftware.OASIS.Common.OASISResult<PreparedNativeFeeEvidence>();
        try{
            var fee=await GetPreparedNativeFeeReceiptAsync(original,token);
            if(fee.IsError)return new(){IsError=true,Message=fee.Message,Exception=fee.Exception,ErrorCode=fee.ErrorCode};
            result.Result=new("TRONOASIS",fee.Result.GetProperty("chainGenesisId").GetString(),fee.Result.GetProperty("transactionHash").GetString(),
                fee.Result.GetProperty("payer").GetString(),fee.Result.GetProperty("success").GetBoolean(),NativeFeeCoverage.NetFinal,
                new[]{new NativeFeeComponent("SUN",fee.Result.GetProperty("netUnits").GetString())},"verified-tron-solidified-total");
        }catch(Exception failure){NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        return result;
    }
    /// <summary>Read-only recovery using the original pre-submission permission snapshot.</summary>
    public async Task<NextGenSoftware.OASIS.Common.OASISResult<System.Text.Json.JsonElement>> GetPreparedNativeFeeReceiptAsync(
        UsageProviderAttempt attempt,CancellationToken token=default)
    {
        var result=new NextGenSoftware.OASIS.Common.OASISResult<System.Text.Json.JsonElement>();
        await _runtimeLifecycle.WaitAsync(token);
        try{
            EnsureNativeRuntime();
            result.Result=await _nativeTransfers.GetPreparedNativeFeeReceiptAsync(attempt,token);
        }catch(Exception failure){NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        finally{_runtimeLifecycle.Release();}
        return result;
    }
    /// <summary>Read-only recovery using independently verified permission authority for the original
    /// transaction. Does not fetch current permissions as a substitute for historical authority.</summary>
    public async Task<NextGenSoftware.OASIS.Common.OASISResult<System.Text.Json.JsonElement>> GetPreparedNativeFeeReceiptAsync(
        UsageProviderAttempt attempt,TronNet.Protocol.Account verifiedOriginalAccount,CancellationToken token=default)
    {
        var result=new NextGenSoftware.OASIS.Common.OASISResult<System.Text.Json.JsonElement>();
        await _runtimeLifecycle.WaitAsync(token);
        try{
            EnsureNativeRuntime();
            result.Result=await _nativeTransfers.GetPreparedNativeFeeReceiptAsync(attempt,verifiedOriginalAccount,token);
        }catch(Exception failure){NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        finally{_runtimeLifecycle.Release();}
        return result;
    }

    /// <summary>Own every physical encrypted-storage mutation before broadcast, without replacing shared SDK state.</summary>
    public Task<T> ExecutePreparedStorageAsync<T>(Func<Task<T>> operation,UsageExecutionHandle execution,
        string clientRequestId,string expectedSender,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();ArgumentNullException.ThrowIfNull(operation);ArgumentNullException.ThrowIfNull(execution);
        var correlation=new NativeStorageCorrelation(clientRequestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSender);
        return ((TronProviderStorageBackend)Backend).WithPreparedStorageAsync(operation,
            client=>client.WithAccountPreparation(execution,
                correlation.Next,expectedSender,token),token);
    }
}
