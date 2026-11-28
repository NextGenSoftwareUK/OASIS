using System;
using System.Collections.Generic;
using System.IO;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
public partial class Neo4jOASIS
{
    // One guard owns the ordered entity-source envelope before any selected header,
    // including selected-set inputs and an empty final selected sequence.
    private sealed class NeoCqlEntitySelectedProof
    {
        private readonly NeoCqlEntityInputProof source;
        private long observed;private long? expected;private bool selectedStarted;
        internal NeoCqlEntitySelectedProof(CosmicEntityCompositionOptions options){source=new(options);}
        internal bool Consume(IReadOnlyDictionary<string,object> record)
        {
            if(record["phase"].As<long>()!=-3){selectedStarted=true;return false;}
            if(selectedStarted||record.Count!=6||record["position"].As<long>()!=observed
                ||record["sourcePosition"].As<long>()!=-1||record["stageWork"].As<long>()!=0)
                throw new InvalidDataException("Entity source proof phase/order is invalid.");
            var count=record["nativeCount"].As<long>();
            if(count<=observed||expected.HasValue&&expected.Value!=count)
                throw new InvalidDataException("Entity source proof count changed.");
            expected=count;source.Observe(record["proof"].As<IReadOnlyDictionary<string,object>>());observed++;
            return true;
        }
        internal void Complete()
        {
            if(expected.HasValue&&observed!=expected.Value)
                throw new InvalidDataException("Entity source proof is incomplete.");
        }
    }
}
