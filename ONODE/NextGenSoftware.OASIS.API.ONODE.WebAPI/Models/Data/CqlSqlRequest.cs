using System.Text.Json;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

/// <summary>A SQL-like CQL statement and the values of its @named parameters.</summary>
public sealed class CqlSqlRequest
{
    public string Query { get; set; }
    public JsonElement? Parameters { get; set; }
}
