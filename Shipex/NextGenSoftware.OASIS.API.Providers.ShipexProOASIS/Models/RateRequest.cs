namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

/// <summary>
/// Request model for shipping rate requests
/// </summary>
public class RateRequest
{
    public Guid MerchantId { get; set; }
    public Dimensions Dimensions { get; set; } = new();
    public decimal Weight { get; set; }
    public Address Origin { get; set; } = new();
    public Address Destination { get; set; } = new();
    public string ServiceLevel { get; set; } = "standard"; // "standard", "express", "overnight"
}

/// <summary>
/// Package dimensions
/// </summary>
public class Dimensions
{
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public string Unit { get; set; } = "inches"; // "inches" or "cm"
}

/// <summary>
/// Address information
/// </summary>
public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

