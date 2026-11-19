using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.FirebaseOASIS;

internal sealed class FirebaseMutationReconciliationException(Guid proposedId, Exception cause)
    : Exception("Firebase write outcome requires reconciliation before replay.", cause), INativeMutationReconciliationRequired
{
    public Guid ProposedId { get; } = proposedId;
    public IReadOnlyList<Guid> ProposedIds { get; init; } = new[] { proposedId };
    public string OriginalTransactionId => string.Empty;
}
internal sealed class FirebaseRepresentationException(string message) : Exception(message);

public partial class FirebaseOASIS
{
    // ParentHolonId is the durable relationship. Embedded navigation objects
    // from older rows are ignored when reading the canonical record.
    private static JsonSerializerOptions HolonRowJsonOptions => HolonRowOptions.Value;

    private static readonly Lazy<JsonSerializerOptions> HolonRowOptions = new(() => new JsonSerializerOptions(_jsonOpts)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                typeInfo =>
                {
                    if (!typeof(IHolon).IsAssignableFrom(typeInfo.Type)) return;
                    for (var index = typeInfo.Properties.Count - 1; index >= 0; index--)
                    {
                        if (typeInfo.Properties[index].Name is "children" or "allChildren" or
                            "parentHolon" or "parentHolon2" or "parentHolon3")
                            typeInfo.Properties.RemoveAt(index);
                    }
                }
            }
        }
    });

    // RTDB removes null/empty nodes and stores native JSON numbers. Normalize
    // the declared wire representation before dispatch, not by repairing an
    // acknowledgement afterward. Refuse numbers whose JSON value cannot survive
    // the backend's double representation rather than claiming an exact save.
    private static JsonNode? NativeProjection(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(pair => pair.Key).ToArray())
            {
                var child = obj[key];
                if (child is JsonObject or JsonArray) NativeProjection(child);
                else ValidateNumber(child);
                if (child == null || child is JsonObject empty && empty.Count == 0 || child is JsonArray array && array.All(value => value == null))
                    obj.Remove(key);
            }
            return obj.Count == 0 ? null : obj;
        }
        if (node is JsonArray items)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var child = items[i];
                if (child is JsonObject or JsonArray)
                {
                    if (NativeProjection(child) == null) items[i] = null;
                }
                else ValidateNumber(child);
            }
            return items.All(value => value == null) ? null : items;
        }
        ValidateNumber(node);
        return node;
    }

    private static void ValidateNumber(JsonNode? node)
    {
        if (node == null) return;
        using var json = JsonDocument.Parse(node.ToJsonString());
        if (json.RootElement.ValueKind != JsonValueKind.Number) return;
        var number = json.RootElement.GetDouble();
        if (!double.IsFinite(number) || !json.RootElement.TryGetDecimal(out var original)
            || !decimal.TryParse(JsonSerializer.Serialize(number), NumberStyles.Float, CultureInfo.InvariantCulture, out var stored)
            || original != stored)
            throw new FirebaseRepresentationException("Firebase native JSON cannot preserve this numeric value; no write dispatched.");
    }

    internal static string ReadStrongRevisionTag(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("ETag", out var values))
        {
            var tags = values.Take(2).ToArray();
            if (tags.Length == 1)
            {
                var tag = tags[0];
                if (System.Net.Http.Headers.EntityTagHeaderValue.TryParse(tag, out var parsed) &&
                    !parsed.IsWeak && parsed.Tag != "*")
                    return tag;

                // The RTDB emulator also emits an unquoted base64 revision.
                Span<byte> revision = stackalloc byte[128];
                if (tag.Length is > 0 and <= 172 && !tag.Any(char.IsWhiteSpace) &&
                    Convert.TryFromBase64String(tag, revision, out var length) && length > 0)
                    return tag;
            }
        }
        throw new InvalidDataException("Firebase did not return one strong revision tag.");
    }

    private static async Task<string> ReadFirebaseAcknowledgement(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var buffer = new MemoryStream();
        var block = new byte[16384];
        for (var read = await stream.ReadAsync(block); read != 0; read = await stream.ReadAsync(block))
        {
            if (buffer.Length + read > 4 * 1024 * 1024)
                throw new InvalidDataException("Firebase acknowledgement exceeds4MiB; reconcile the dispatched write.");
            buffer.Write(block, 0, read);
        }
        return new UTF8Encoding(false, true).GetString(buffer.ToArray());
    }

    private static Holon? ParseFirebaseHolon(Guid expected, string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Null) return null;
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Firebase canonical holon must be an object or null.");
        var holon = JsonSerializer.Deserialize<Holon>(json, HolonRowJsonOptions);
        if (holon == null || expected == Guid.Empty || holon.Id != expected)
            throw new InvalidDataException("Firebase native key and canonical holon identity disagree.");
        return holon;
    }
}
