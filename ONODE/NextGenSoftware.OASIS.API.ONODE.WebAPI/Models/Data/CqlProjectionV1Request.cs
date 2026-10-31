#nullable enable
using System;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

/// <summary>Bounded native selected-column intent. Actor and provider eligibility belong to the host.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlProjectionV1Request
{
    public string? Provider { get; set; }
    public CqlV1Filter[] Filters { get; set; } = Array.Empty<CqlV1Filter>();
    public CqlV1Filter[] AnyOf { get; set; } = Array.Empty<CqlV1Filter>();
    public CqlV1ChildExists[] AnyChildren { get; set; } = Array.Empty<CqlV1ChildExists>();
    public CqlV1DescendantExists[] AnyDescendants { get; set; } = Array.Empty<CqlV1DescendantExists>();
    public string OrderBy { get; set; } = "Name";
    public bool Descending { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 25;
    public CqlV1Graph? Graph { get; set; }
    public CqlProjectionV1Column[] Columns { get; set; } = Array.Empty<CqlProjectionV1Column>();
}

/// <summary>Exactly one canonical field or typed metadata path; aliases are data, never native identifiers.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlProjectionV1Column
{
    public string? Alias { get; set; }
    public string? Field { get; set; }
    public string[]? MetadataPath { get; set; }
    public string? ValueType { get; set; }
    public bool? Nullable { get; set; }
}
