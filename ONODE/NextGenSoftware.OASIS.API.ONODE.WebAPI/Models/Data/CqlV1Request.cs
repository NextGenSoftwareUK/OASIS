#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlDemoIdentityRequest
{
    public string? Provider { get; set; }
    public string[] Keys { get; set; } = Array.Empty<string>();
}

// This is a bounded query description, never executable C#/SQL/Cypher or a caller-supplied plan.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlV1Request
{
    public string? Provider { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CqlWhereV1Predicate? Where { get; set; }
    public CqlV1Filter[] Filters { get; set; } = Array.Empty<CqlV1Filter>();
    public CqlV1Filter[] AnyOf { get; set; } = Array.Empty<CqlV1Filter>();
    public CqlV1ChildExists[] AnyChildren { get; set; } = Array.Empty<CqlV1ChildExists>();
    public CqlV1DescendantExists[] AnyDescendants { get; set; } = Array.Empty<CqlV1DescendantExists>();
    // None is explicit and restricted to the unsorted ActivityPub, IPFS and SOLID point profiles.
    public string OrderBy { get; set; } = "Name";
    public bool Descending { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CqlV1Ordering[]? Ordering { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 25;
    // Explicit execution budget for the leaf endpoint; federation has its own global budget.
    public int MaximumDurationMilliseconds { get; set; } = 5000;
    public bool IncludeMetadata { get; set; }
    public CqlV1Graph? Graph { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlV1Ordering
{
    public string? Field { get; set; }
    public bool Descending { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlV1ChildExists
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Negated { get; set; }
    // Each group requires one visible direct child satisfying its local filters.
    // Separate groups are ANDed and may match different children.
    public CqlV1Filter[] Filters { get; set; } = Array.Empty<CqlV1Filter>();
    public CqlV1Filter[] AnyOf { get; set; } = Array.Empty<CqlV1Filter>();
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlV1DescendantExists
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Negated { get; set; }
    // Filtering depth/node bounds are independent of response Graph shape.
    public int MaximumDepth { get; set; } = 8;
    public int MaximumNodes { get; set; } = 1000;
    public CqlV1Filter[] Filters { get; set; } = Array.Empty<CqlV1Filter>();
    public CqlV1Filter[] AnyOf { get; set; } = Array.Empty<CqlV1Filter>();
}

[JsonConverter(typeof(JsonStringEnumConverter<CqlV1Scalar>))]
public enum CqlV1Scalar { String, Integer, Boolean, Guid, DateTime, UInt64, Decimal, Double, Single, Int32 }

[JsonConverter(typeof(JsonStringEnumConverter<CqlV1Operator>))]
public enum CqlV1Operator { Equal, NotEqual, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual, Contains, StartsWith, EndsWith, Exists, IsNull, In, IsMissing, IsNotNull, NotContains, NotStartsWith, NotEndsWith }

[JsonConverter(typeof(JsonStringEnumConverter<CqlV1Function>))]
public enum CqlV1Function { Utf16Length, TextCoalesce, BooleanTextCase, Int32Coalesce, UtcYear, UtcMonth, UtcDay, UtcDayOfYear, UtcHour, UtcMinute, UtcSecond }

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlV1Filter
{
    // Either one whitelisted canonical Field, or a typed MetadataPath (maximum four segments).
    public string? Field { get; set; }
    public string[]? MetadataPath { get; set; }
    public CqlV1Scalar Scalar { get; set; }
    public CqlV1Operator Operator { get; set; }
    // Utf16Length compares a string's UTF-16 code-unit count with an exact Int64 bound.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CqlV1Function? Function { get; set; }
    // In accepts an array of1–100 exact literals; other value operators accept one literal.
    public JsonElement Value { get; set; }
}


[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlV1Graph
{
    public bool Recursive { get; set; } = true;
    public int MaxChildDepth { get; set; } = 2;
    public string ChildType { get; set; } = "All";
    public int MaximumNodes { get; set; } = 256;
    public int MaximumPayloadBytes { get; set; } = 1024 * 1024;
    public string Consistency { get; set; } = "ProviderDefault";
}
