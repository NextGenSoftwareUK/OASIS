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
    public CqlV1Ordering[]? Ordering { get; set; }
    public bool Reverse { get; set; }
    public bool IncludeMetadata { get; set; }
    /// <summary>Ordered entity operations after the combined window and before OutputColumns.</summary>
    public CqlSetV1EntityStage[]? EntityStages {get;set;}
    /// <summary>Selection after entity membership and output entity windows.
    /// Cannot be combined with selected columns/stages on either operand.</summary>
    public CqlProjectionV1Column[]? OutputColumns {get;set;}
    /// <summary>Native selected-value stages after OutputColumns. Distinct compares
    /// the currently visible selected tuple, after entity identity membership.</summary>
    public CqlCompositionV1Stage[]? SelectedStages {get;set;}
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlSetV1Source
{
    public CqlWhereV1Predicate? Where { get; set; }
    public CqlV1Filter[] Filters { get; set; } = Array.Empty<CqlV1Filter>();
    public CqlV1Filter[] AnyOf { get; set; } = Array.Empty<CqlV1Filter>();
    public CqlV1ChildExists[] AnyChildren { get; set; } = Array.Empty<CqlV1ChildExists>();
    public CqlV1DescendantExists[] AnyDescendants { get; set; } = Array.Empty<CqlV1DescendantExists>();
    public string OrderBy { get; set; } = "Name";
    public bool Descending { get; set; }
    public int Skip { get; set; }
    public int? Take { get; set; }
    public CqlProjectionV1Column[]? Columns {get;set;}
    /// <summary>Native selected stages after this input window, before set equality.</summary>
    public CqlCompositionV1Stage[]? Stages {get;set;}
    public CqlV1Graph? Graph { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlSetV1EntityStage
{
    public string? Operation {get;set;}
    public CqlWhereV1Predicate? Where {get;set;}
    public CqlV1Ordering[]? Ordering {get;set;}
    public int? Count {get;set;}
}
