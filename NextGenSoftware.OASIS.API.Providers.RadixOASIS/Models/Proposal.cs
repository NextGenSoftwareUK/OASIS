using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

public sealed record class Proposal
{
    [JsonPropertyName("id")]
    public required ulong Id { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("votes_for")]
    public required decimal VotesFor { get; init; }

    [JsonPropertyName("votes_against")]
    public required decimal VotesAgainst { get; init; }

    [JsonPropertyName("end_time")]
    public required ulong EndTime { get; init; }

    [JsonPropertyName("executed")]
    public required bool Executed { get; init; }
}
