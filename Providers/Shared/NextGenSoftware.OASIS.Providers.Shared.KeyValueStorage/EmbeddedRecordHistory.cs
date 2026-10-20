using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Canonical same-item history format. Does not perform I/O or publish caller state.
/// The SDK owner must persist the whole document with create-if-absent or an observed native revision condition.
/// No trimming: native item-size rejection must preserve the previous committed document.</summary>
public sealed class EmbeddedRecordHistory
{
    public const string PropertyName = "_oasis_record_history";
    private readonly string[] _nativeFields;
    public EmbeddedRecordHistory(params string[] nativeFields)
    {
        ArgumentNullException.ThrowIfNull(nativeFields);
        if(nativeFields.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Native field names must not be blank.",nameof(nativeFields));
        _nativeFields=nativeFields.Concat(new[]{"id",PropertyName}).Distinct(StringComparer.Ordinal).ToArray();
    }

    public JObject CanonicalPayload(JObject document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var copy = (JObject)document.DeepClone();
        foreach (var field in _nativeFields) copy.Remove(field);
        return copy;
    }

    private static T Decode<T>(JObject payload, Guid identity, Func<string,T> decode) where T : class,IHolonBase
    {
        var record = decode(payload.ToString(Newtonsoft.Json.Formatting.None));
        if (record == null || record.Id != identity || record.Version < 0)
            throw new InvalidDataException("Embedded history record identity/version conflicts with its item.");
        return record;
    }

    private static JObject EncodedPayload(string json)
    {
        using var document=System.Text.Json.JsonDocument.Parse(json);
        if(document.RootElement.ValueKind!=System.Text.Json.JsonValueKind.Object)
            throw new InvalidDataException("Embedded history requires a canonical record object.");
        // Reuse Core's structural JSON-value converter; a default JObject text reader narrows numbers to double.
        return (JObject)StoredRecordJson.CaptureRuntimeToken(document.RootElement);
    }

    private SortedDictionary<int,JObject> ReadArchive<T>(JObject document,T current,Func<string,T> decode) where T : class,IHolonBase
    {
        var result = new SortedDictionary<int,JObject>();
        if (document[PropertyName] == null) return result;
        if (document[PropertyName] is not JObject envelope || envelope.Count != 2
            || envelope["schema"]?.Type != JTokenType.Integer || envelope["schema"].Value<int>() != 1
            || envelope["records"] is not JArray entries)
            throw new InvalidDataException("Unsupported or malformed Embedded history envelope.");
        foreach (var token in entries)
        {
            if (token is not JObject item || _nativeFields.Any(field => item[field] != null))
                throw new InvalidDataException("Embedded history contains a noncanonical archived record.");
            var archived = Decode(item,current.Id,decode);
            if (archived.Version >= current.Version || !result.TryAdd(archived.Version,(JObject)item.DeepClone()))
                throw new InvalidDataException("Embedded archived versions must be unique and precede the current version.");
        }
        return result;
    }

    public Task<JObject> StageAsync<T>(T proposed,JObject previousDocument,Func<string,T> decode) where T : class,IHolonBase
        => StageAsync(proposed,previousDocument,decode,record=>OasisJson.Serialize(record));

    public async Task<JObject> StageAsync<T>(T proposed,JObject previousDocument,Func<string,T> decode,Func<T,string> encode) where T : class,IHolonBase
    {
        ArgumentNullException.ThrowIfNull(proposed); ArgumentNullException.ThrowIfNull(decode); ArgumentNullException.ThrowIfNull(encode);
        if (proposed.Id == Guid.Empty) throw new ArgumentException("An explicit staged identity is required.",nameof(proposed));
        // Decode a detached canonical copy; no caller Version/keys are published before native acknowledgement.
        var next = Decode(EncodedPayload(encode(proposed)),proposed.Id,decode);
        T previous = null;
        var archive = new SortedDictionary<int,JObject>();
        if (previousDocument != null)
        {
            if (previousDocument["id"]?.Type != JTokenType.String || previousDocument["id"].Value<string>() != proposed.Id.ToString())
                throw new InvalidDataException("Embedded history item key conflicts with proposed identity.");
            previous = Decode(CanonicalPayload(previousDocument),proposed.Id,decode);
            archive = ReadArchive(previousDocument,previous,decode);
        }
        await StoredRecordVersions.StampAsync(proposed.Id,next,previous,stored => {
            if (!archive.TryAdd(stored.Version,EncodedPayload(encode((T)stored))))
                throw new InvalidDataException("Embedded history archive would overwrite an existing version.");
            return Task.CompletedTask;
        });
        var document = EncodedPayload(encode(next));
        document["id"] = next.Id.ToString();
        document[PropertyName] = new JObject { ["schema"] = 1, ["records"] = new JArray(archive.Values) };
        return document;
    }

    public async Task<T> ReadAsync<T>(Guid identity,JObject document,int version,Func<string,T> decode) where T : class,IHolonBase
    {
        if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
        ArgumentNullException.ThrowIfNull(document); ArgumentNullException.ThrowIfNull(decode);
        if (identity == Guid.Empty || document["id"]?.Type != JTokenType.String || document["id"].Value<string>() != identity.ToString())
            throw new InvalidDataException("Embedded history item identity is invalid.");
        var current = Decode(CanonicalPayload(document),identity,decode);
        var archive = ReadArchive(document,current,decode);
        return await StoredRecordVersions.ReadAsync(identity,current,version,requested => Task.FromResult(
            archive.TryGetValue(requested,out var stored) ? Decode(stored,identity,decode) : null));
    }
}
