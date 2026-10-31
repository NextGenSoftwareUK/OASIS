using System;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
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
        Proposal=Project(source);
        _expected=StoredRecordJson.CaptureRuntimeToken(Proposal);
    }
    public static StoredRecordPublicationSnapshot<T> Capture(IHolonBase source)=>new(source);
    public void ValidateUnchanged()
    {
        var current=Project(_source);
        if(!StoredRecordPayloadEquality.Equivalent(_expected,StoredRecordJson.CaptureRuntimeToken(current)))
            throw new InvalidOperationException("Caller canonical record changed after its pre-write snapshot; do not publish over newer caller state.");
    }
    // Project requires a concrete DTO; interface-facing provider APIs use the same canonical record classes.
    private static T Project(IHolonBase source)
    {
        if(typeof(T)==typeof(IAvatar))return (T)(object)StoredRecordJson.Project<Avatar>(source);
        if(typeof(T)==typeof(IAvatarDetail))return (T)(object)StoredRecordJson.Project<AvatarDetail>(source);
        if(typeof(T)==typeof(IHolon))return (T)(object)StoredRecordJson.Project<Holon>(source);
        return StoredRecordJson.Project<T>(source);
    }
}
