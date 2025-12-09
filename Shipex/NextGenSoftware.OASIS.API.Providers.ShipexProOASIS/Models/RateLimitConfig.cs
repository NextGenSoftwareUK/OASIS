namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

/// <summary>
/// Rate limit configuration per tier
/// </summary>
public class RateLimitConfig
{
    public string Tier { get; set; } = string.Empty;
    public int RequestsPerHour { get; set; }
    public int RequestsPerDay { get; set; }
}

/// <summary>
/// Rate limit status for a merchant
/// </summary>
public class RateLimitStatus
{
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public DateTime ResetAt { get; set; }
}




