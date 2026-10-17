using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

/// <summary>Explicit per-transaction signed fee+tip ceilings, not aggregate reservation or sponsorship approval.</summary>
public sealed class CosmosNativeFeeLimits
{
    public IReadOnlyDictionary<string,string> MaximumFeeUnitsByDenomination {get;}
    /// <summary>Trusted deployment review, not an RPC/code attestation or user spending permission.
    /// The chain must use standard signed SDK transaction fees with no additional network fee debits.</summary>
    public string ReviewedStandardSdkChainId {get;}
    public CosmosNativeFeeLimits(IReadOnlyDictionary<string,string> limits)
        :this(limits,null) { }

    /// <summary>Opt in only after reviewing this exact chain's fee checker and post-handler.
    /// Contract funds/transferred principal are not transaction fees. No production default is inferred.</summary>
    public static CosmosNativeFeeLimits ForReviewedStandardSdkChain(IReadOnlyDictionary<string,string> limits,string chainId)
    {
        if(string.IsNullOrWhiteSpace(chainId)||chainId.Length>128||chainId.Any(c=>char.IsWhiteSpace(c)||char.IsControl(c)))
            throw new ArgumentException("Exact reviewed Cosmos chain ID required.",nameof(chainId));
        return new(limits,chainId);
    }

    private CosmosNativeFeeLimits(IReadOnlyDictionary<string,string> limits,string reviewedChainId)
    {
        ArgumentNullException.ThrowIfNull(limits);
        var captured=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var pair in limits.Take(33)){
            if(captured.Count==32||string.IsNullOrWhiteSpace(pair.Key)||pair.Key.Length>128||pair.Key.Any(c=>char.IsWhiteSpace(c)||char.IsControl(c))
                ||string.IsNullOrEmpty(pair.Value)||pair.Value.Length>128||pair.Value[0]=='0'||pair.Value.Any(c=>c<'0'||c>'9')||!captured.TryAdd(pair.Key,pair.Value))
                throw new ArgumentException("Canonical bounded positive Cosmos denomination fee ceilings required.",nameof(limits));
        }
        if(captured.Count==0)throw new ArgumentException("At least one explicit Cosmos fee denomination ceiling required.",nameof(limits));
        MaximumFeeUnitsByDenomination=new ReadOnlyDictionary<string,string>(captured);
        ReviewedStandardSdkChainId=reviewedChainId;
    }
}
