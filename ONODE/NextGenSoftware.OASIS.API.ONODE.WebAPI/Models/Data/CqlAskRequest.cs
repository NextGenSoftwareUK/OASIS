namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

/// <summary>A plain-language question to turn into CQL. Set Execute to false to only see the statement.</summary>
public sealed class CqlAskRequest
{
    public string Question { get; set; }
    public bool Execute { get; set; } = true;
    /// <summary>Limits drafting to one provider; used with VirtualMappingId or PublicExample.</summary>
    public string Provider { get; set; }
    /// <summary>A saved virtual mapping the statement must use (MongoDB, SQLite or Neo4j).</summary>
    public string VirtualMappingId { get; set; }
    /// <summary>The question asks for a reviewed public example of Provider.</summary>
    public bool PublicExample { get; set; }
}
