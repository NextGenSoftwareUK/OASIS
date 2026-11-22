#nullable enable
using System;
using System.Text.Json.Serialization;
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
/// <summary>Two independently bounded native entity windows under one server-selected read policy.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlSetV1Request
{
    public string? Provider { get; set; }
    public string Kind { get; set; } = "Union";
    public CqlSetV1Source Left { get; set; } = new();
    public CqlSetV1Source Right { get; set; } = new();
    public int MaximumLeftRows { get; set; } = 100;
    public int MaximumRightRows { get; set; } = 100;
    public int MaximumResults { get; set; } = 25;
    public int Skip { get; set; }
    public int? Take { get; set; }
    public bool Reverse { get; set; }
    public bool IncludeMetadata { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlV1JoinOrder
{
    public string? Alias { get; set; }
    public bool Descending { get; set; }
}
