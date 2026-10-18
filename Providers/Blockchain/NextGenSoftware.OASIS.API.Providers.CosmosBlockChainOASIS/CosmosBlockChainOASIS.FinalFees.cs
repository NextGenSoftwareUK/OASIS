using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

public partial class CosmosBlockChainOASIS
{
    private readonly ReviewedDeployment _reviewedDeployment;

    private sealed class ReviewedDeployment(CosmosFeeModelReview review, Uri endpoint,
        Func<Uri,CancellationToken,Task<CosmosDeploymentObservation>> inspector)
    {
        internal CosmosFeeModelReview Review {get;}=review;
        private readonly string endpointHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(endpoint.AbsoluteUri)));
        internal async Task<CosmosDeploymentObservation> ValidateAsync(long? originalHeight,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();Review.ValidateCurrentReview();
            var observed=await inspector(endpoint,token);
            token.ThrowIfCancellationRequested();
            if(observed==null||observed.RpcEndpointSha256!=endpointHash)
                throw new InvalidDataException("Trusted deployment inspection must bind the exact configured Cosmos RPC endpoint.");
            Review.ValidateObservation(observed,originalHeight??observed.CommittedHeight);
            return observed;
        }
        internal NativeFeeAuditSnapshot CaptureAudit(CosmosDeploymentObservation observation,string original,long receiptHeight)
            =>NativeFeeAuditSnapshot.Capture("cosmos.wasmd0703.fee-review.v1","CosmosBlockChainOASIS",Review.ChainId,original,
                JsonSerializer.Serialize(new{Review,Deployment=observation,ReceiptHeight=receiptHeight}));
    }

    /// <summary>Server composition only. The inspector must actually inspect this endpoint's deployed binary/genesis;
    /// RPC version strings or client DTOs are not code attestation. The inspector must be read-only and must not
    /// activate providers or mutate native state. Construction itself neither activates nor spends.</summary>
    public static async Task<CosmosBlockChainOASIS> CreateWithReviewedFinalFeesAsync(CosmosNativeFeeLimits limits,
        CosmosFeeModelReview review,Func<Uri,CancellationToken,Task<CosmosDeploymentObservation>> inspector,
        string rpcEndpoint,string chainId,string privateKey="",string contractAddress="",string addressPrefix="wasm",
        string gasPrice="0.025stake",string nativeDenom="stake",int nativeDecimals=0,
        Func<byte[]> storageEncryptionKeyResolver=null,CosmosTokenEscrowConfiguration tokenEscrow=null,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(limits);ArgumentNullException.ThrowIfNull(review);ArgumentNullException.ThrowIfNull(inspector);
        if(review.ChainId!=chainId||limits.ReviewedStandardSdkChainId!=chainId
            ||!Uri.TryCreate(rpcEndpoint,UriKind.Absolute,out var endpoint)||endpoint.Scheme is not ("http" or "https"))
            throw new ArgumentException("Exact reviewed chain, signed standard-SDK maxima and HTTP(S) RPC endpoint required.");
        var deployment=new ReviewedDeployment(review,endpoint,inspector);
        var observed=await deployment.ValidateAsync(null,token);
        // Validate the bounded audit encoding before this provider can sign anything. Cosmos hashes are64hex
        // characters; the maximal height width avoids a later audit overflow at a height-digit boundary.
        deployment.CaptureAudit(observed with{CommittedHeight=long.MaxValue,ObservedAtUtc=DateTimeOffset.MaxValue},new string('0',64),long.MaxValue);
        var sdk=new CosmosSdkBackend(rpcEndpoint,chainId,privateKey,contractAddress,addressPrefix,gasPrice,null,null,limits,
            cancellation=>deployment.ValidateAsync(null,cancellation),review.LastHeight);
        try{return new CosmosBlockChainOASIS(sdk,privateKey,contractAddress,nativeDenom,nativeDecimals,storageEncryptionKeyResolver,tokenEscrow,deployment);}
        catch{sdk.Dispose();throw;}
    }

    private CosmosBlockChainOASIS(CosmosSdkBackend sdk,string privateKey,string contractAddress,string nativeDenom,int nativeDecimals,
        Func<byte[]> keyResolver,CosmosTokenEscrowConfiguration tokenEscrow,ReviewedDeployment deployment)
        :this(sdk,privateKey,contractAddress,nativeDenom,nativeDecimals,keyResolver)
    {
        tokenEscrow?.Validate(contractAddress);_tokenEscrow=tokenEscrow;_reviewedDeployment=deployment;
    }

    private NativeFeeCoverage CurrentFeeCoverage
    {
        get{
            if(_reviewedDeployment==null)return NativeFeeCoverage.AnteDeductionOnly;
            _reviewedDeployment.Review.ValidateCurrentReview();
            return NativeFeeCoverage.NetFinal;
        }
    }
}
