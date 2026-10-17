using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

/// <summary>Explicit per-transaction signed fee+tip ceilings, not aggregate reservation or sponsorship approval.</summary>
public sealed class CosmosNativeFeeLimits
{
    public IReadOnlyDictionary<string,string> MaximumFeeUnitsByDenomination {get;}
    public CosmosNativeFeeLimits(IReadOnlyDictionary<string,string> limits)
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
    }
}
