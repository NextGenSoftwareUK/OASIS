#nullable enable
using System.Text.Json.Serialization;
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

/// <summary>Bounded business predicates only. Actor, authority and visibility remain host-owned.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlWhereV1Predicate
{
    public string Kind { get; set; } = "";
    public CqlV1Filter? Filter { get; set; }
    public CqlWhereV1Predicate[]? Terms { get; set; }
    public CqlWhereV1Predicate? Match { get; set; }
    public int? MaximumDepth { get; set; }
    public int? MaximumNodes { get; set; }
}
