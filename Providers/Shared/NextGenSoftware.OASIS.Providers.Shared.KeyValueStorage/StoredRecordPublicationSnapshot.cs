using System;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Captures the canonical caller contract before I/O. Providers must classify a
/// failed post-ack validation as acknowledged-write reconciliation, not a safe retry.
/// This detects observed edits; it is not an atomic lock against unsynchronized setters.</summary>
public sealed class StoredRecordPublicationSnapshot<T> where T:class,IHolonBase
{
    private readonly IHolonBase _source;
    private readonly JToken _expected;
    public T Proposal{get;}
    private StoredRecordPublicationSnapshot(IHolonBase source)
    {
        ArgumentNullException.ThrowIfNull(source);_source=source;
        Proposal=StoredRecordJson.Project<T>(source);
        _expected=JToken.Parse(StoredRecordJson.Serialize(Proposal));
    }
    public static StoredRecordPublicationSnapshot<T> Capture(IHolonBase source)=>new(source);
    public void ValidateUnchanged()
    {
        var current=StoredRecordJson.Project<T>(_source);
        if(!StoredRecordPayloadEquality.Equivalent(_expected,JToken.Parse(StoredRecordJson.Serialize(current))))
            throw new InvalidOperationException("Caller canonical record changed after its pre-write snapshot; do not publish over newer caller state.");
    }
}
