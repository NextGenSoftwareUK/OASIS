using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Assembles results from an owned, already acknowledged conditional graph.
/// Native commit/receipt validation remains the provider's responsibility. This helper
/// performs no I/O and must not be used to turn an uncertain outcome into success.</summary>
public static class ConditionalHolonAcknowledgement
{
    public static OASISResult<IEnumerable<IHolon>> Create(IEnumerable<IHolon> acknowledgedRoots,ConditionalHolonOperation operation)
    {
        ArgumentNullException.ThrowIfNull(acknowledgedRoots);
        if(!Enum.IsDefined(typeof(ConditionalHolonOperation),operation))throw new ArgumentOutOfRangeException(nameof(operation));
        var roots=acknowledgedRoots.ToList();
        if(roots.Count==0)throw new ArgumentException("A conditional acknowledgement requires a nonempty selected graph.",nameof(acknowledgedRoots));
        var graph=HolonGraphWritePlan.Create(roots,true,true,0,0,node=>{
            if(node.Id==Guid.Empty)throw new ArgumentException("Acknowledged entities must already have nonempty identities.",nameof(acknowledgedRoots));
        });
        // Validate the complete graph before publishing persisted-state flags.
        foreach(var node in graph.Nodes)node.Source.IsNewHolon=false;
        return new(){Result=roots,IsSaved=operation==ConditionalHolonOperation.Save,IsDeleted=operation!=ConditionalHolonOperation.Save,
            SavedCount=operation==ConditionalHolonOperation.Save?graph.Nodes.Count:0,
            DeletedCount=operation==ConditionalHolonOperation.Save?0:graph.Nodes.Count,HasAnyHolonsChanged=true};
    }
}
