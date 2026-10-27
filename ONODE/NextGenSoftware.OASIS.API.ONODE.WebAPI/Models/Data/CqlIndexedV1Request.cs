using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

/// <summary>Alias and bounded query intent only; endpoints, keys, grants, checkpoint claims and prices are server-owned.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlIndexedV1Request
{
    public string Alias { get; set; }
    public CqlV1Request Query { get; set; } = new();
    public long? MaximumLagSlots { get; set; }
    public int? MaximumHeadAgeSeconds { get; set; }
}
