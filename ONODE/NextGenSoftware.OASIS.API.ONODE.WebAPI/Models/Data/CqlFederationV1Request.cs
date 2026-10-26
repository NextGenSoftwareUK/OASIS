#nullable enable
using System;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

/// <summary>Explicit federation intent; grants, tariffs and adapter capabilities are server-owned.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CqlFederationV1Request
{
    public string Scope { get; set; } = "CurrentDefault";
    public string[] Providers { get; set; } = Array.Empty<string>();
    public CqlV1Request Source { get; set; } = new();
    public string IdentityMode { get; set; } = "PreserveCopies";
    public string[] ProviderPriority { get; set; } = Array.Empty<string>();
    public int Skip { get; set; }
    public int Take { get; set; } = 25;
    public int MaximumInputRows { get; set; } = 300;
    public int MaximumRowsPerProvider { get; set; } = 100;
    public int MaximumInputBytes { get; set; } = 4 * 1024 * 1024;
    public int MaximumOutputBytes { get; set; } = 1024 * 1024;
    public int MaximumWork { get; set; } = 100000;
    public int MaximumProviderAttempts { get; set; } = 3;
    public int MaximumDurationMilliseconds { get; set; } = 5000;
    public bool EnableContinuation { get; set; }
    public string? Cursor { get; set; }
}
