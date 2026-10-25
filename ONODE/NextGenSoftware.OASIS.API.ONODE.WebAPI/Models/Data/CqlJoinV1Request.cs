#nullable enable
using System;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

/// <summary>Two bounded source windows in one host-selected provider. No client actor, plan or executable expression.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlJoinV1Request
{
    public string? Provider { get; set; }
    public string Kind { get; set; } = "Inner";
    public CqlJoinV1Source Left { get; set; } = new();
    public CqlJoinV1Source Right { get; set; } = new();
    public CqlJoinV1Key[] Keys { get; set; } = Array.Empty<CqlJoinV1Key>();
    public CqlJoinV1Column[] Columns { get; set; } = Array.Empty<CqlJoinV1Column>();
    public int MaximumLeftRows { get; set; } = 100;
    public int MaximumRightRows { get; set; } = 100;
    public long MaximumPairs { get; set; } = 1000;
    public int MaximumResults { get; set; } = 25;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlJoinV1Source
{
    public CqlV1Filter[] Filters { get; set; } = Array.Empty<CqlV1Filter>();
    public CqlV1Filter[] AnyOf { get; set; } = Array.Empty<CqlV1Filter>();
    public CqlV1ChildExists[] AnyChildren { get; set; } = Array.Empty<CqlV1ChildExists>();
    public CqlV1DescendantExists[] AnyDescendants { get; set; } = Array.Empty<CqlV1DescendantExists>();
    public string OrderBy { get; set; } = "Name";
    public bool Descending { get; set; }
    public int Skip { get; set; }
    public int? Take { get; set; }
    public CqlV1Graph? Graph { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlJoinV1Key
{
    public string? Alias { get; set; }
    public CqlScalarV1Selection? Left { get; set; }
    public CqlScalarV1Selection? Right { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlJoinV1Column
{
    public string? Alias { get; set; }
    public string Source { get; set; } = "Left";
    public CqlScalarV1Selection? Selection { get; set; }
    public bool? AllowMissing { get; set; }
    public bool? WhenMissing { get; set; }
}
