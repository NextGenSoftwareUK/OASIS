namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;

/// <summary>A plain-language question to turn into CQL. Set Execute to false to only see the statement.</summary>
public sealed class CqlAskRequest
{
    public string Question { get; set; }
    public bool Execute { get; set; } = true;
}
