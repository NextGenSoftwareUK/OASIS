using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.State;

/// <summary>
/// Request for querying entity state details via Gateway API
/// </summary>
public class StateEntityDetailsRequest
{
    [JsonPropertyName("addresses")]
    public List<string> Addresses { get; set; } = new();

    [JsonPropertyName("at_ledger_state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LedgerState? AtLedgerState { get; set; }
}

/// <summary>
/// Optional ledger state specification for historical queries
/// </summary>
public class LedgerState
{
    [JsonPropertyName("state_version")]
    public ulong StateVersion { get; set; }
}

