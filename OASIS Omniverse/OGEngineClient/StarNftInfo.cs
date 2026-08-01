using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STARAPI.Client;

public sealed class StarNftInfo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public Dictionary<string, string> MetaData { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
