using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

public partial class AzureCosmosDBOASIS
{
    // Consume only the existing native owner's typed evidence. Callers bind
    // these identities to the current dispatch, never a future planned write.
    private static IEnumerable<Guid> KnownCosmosAcknowledgements(Exception cause)
    {
        if(cause==null)yield break;
        var pending=new Stack<Exception>();var visited=new HashSet<Exception>(ReferenceEqualityComparer.Instance);pending.Push(cause);
        while(pending.Count>0)
        {
            var current=pending.Pop();if(!visited.Add(current))continue;
            if(current is CosmosMutationReconciliationException native && native.Acknowledged
                && Guid.TryParse(native.DocumentId,out var id) && id!=Guid.Empty)yield return id;
            if(current is CosmosPartialMutationException partial)
                foreach(var acknowledged in partial.AcknowledgedRecordIds)if(acknowledged!=Guid.Empty)yield return acknowledged;
            if(current is AggregateException aggregate)foreach(var inner in aggregate.InnerExceptions)pending.Push(inner);
            if(current.InnerException!=null)pending.Push(current.InnerException);
        }
    }
}
