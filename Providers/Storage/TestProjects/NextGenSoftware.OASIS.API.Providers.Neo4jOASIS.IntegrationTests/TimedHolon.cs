using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

/// <summary>Entity for the Q30 journey: date/time domains stored through the standard COSMIC conversions.</summary>
public sealed class TimedHolon : Holon
{
    public TimeSpan Duration { get; set; }
    public DateOnly Day { get; set; }
    public TimeOnly At { get; set; }
    public DateTimeOffset Seen { get; set; }
}
