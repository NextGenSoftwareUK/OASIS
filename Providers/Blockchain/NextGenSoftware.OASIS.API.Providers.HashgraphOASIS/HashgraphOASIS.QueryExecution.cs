using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

public partial class HashgraphOASIS
{
    /// <summary>Explicit host-owned native/read invocation with separate durable transaction and paid-query streams.
    /// Sponsorship/reservation/price policy must be authorized by the host before creating this execution.</summary>
    public async Task<JsonElement> ExecutePreparedWithQueriesAsync(string action,object request,UsageExecutionHandle execution,string clientRequestId,
        string expectedSender,string expectedPublicKey,string[] expectedNodes,long maxQueryPaymentTinybars,long maxQueryTransactionFeeTinybars,
        int persistenceTimeoutMs=10000,int maximumRequests=64,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        if(persistenceTimeoutMs<1||persistenceTimeoutMs>30000)throw new ArgumentOutOfRangeException(nameof(persistenceTimeoutMs));
        var mutation=HederaSdkClient.IsNativeMutationAction(action);
        if(!mutation&&action is not ("storageGet" or "storageList" or "storageProbe" or "fileRead"))
            throw new ArgumentException("An explicit supported native operation or paid storage/file read is required.",nameof(action));
        using var document=JsonDocument.Parse(JsonSerializer.Serialize(request));
        var original=document.RootElement;
        if(original.ValueKind!=JsonValueKind.Object||original.GetProperty("network").GetString()!=_hederaNetwork
            ||original.GetProperty("sender").GetString()!=expectedSender)
            throw new InvalidDataException("SDK invocation must match the admitted provider network and payer.");
        var values=JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(original.GetRawText());
        values["queryMaxPaymentTinybars"]=JsonSerializer.SerializeToElement(maxQueryPaymentTinybars.ToString(CultureInfo.InvariantCulture));
        values["queryMaxTransactionFeeTinybars"]=JsonSerializer.SerializeToElement(maxQueryTransactionFeeTinybars.ToString(CultureInfo.InvariantCulture));
        values["queryMaximumRequests"]=JsonSerializer.SerializeToElement(maximumRequests);
        values["queryPersistenceTimeoutMs"]=JsonSerializer.SerializeToElement(persistenceTimeoutMs);
        using var queries=CreatePaidQueryPreparationServer(execution,clientRequestId,expectedSender,expectedPublicKey,expectedNodes,
            maxQueryPaymentTinybars,maxQueryTransactionFeeTinybars,maximumRequests);
        if(!mutation)return await HederaSdkClient.InvokePreparedQueriesAsync(action,values,queries,token);
        var id=original.GetProperty("transactionId").GetString();ArgumentException.ThrowIfNullOrWhiteSpace(id);
        using var native=action is "nftEscrowLock" or "tokenEscrowLock"
            ?CreateEscrowPreparationServer(execution,clientRequestId,_hederaNetwork,expectedSender,expectedPublicKey,
                original.GetProperty("approvalTransactionId").GetString(),id)
            :CreateNativePreparationServer(execution,clientRequestId,_hederaNetwork,expectedSender,expectedPublicKey,id);
        return await HederaSdkClient.InvokePreparedWithQueriesAsync(action,values,native,queries,token);
    }
}
