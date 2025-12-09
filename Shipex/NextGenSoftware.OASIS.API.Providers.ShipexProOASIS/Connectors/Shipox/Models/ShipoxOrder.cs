namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.Shipox.Models;

/// <summary>
/// Shipox Order model for order management operations
/// </summary>
public class ShipoxOrder
{
    public string? OrderId { get; set; }
    public string? ExternalOrderId { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ShipoxOrderDetails? Details { get; set; }
    public ShipoxCustomer? Customer { get; set; }
    public List<ShipoxCarrier>? Carriers { get; set; }
}

/// <summary>
/// Shipox Order Details
/// </summary>
public class ShipoxOrderDetails
{
    public decimal? Weight { get; set; }
    public ShipoxDimensions? Dimensions { get; set; }
    public ShipoxAddress? Origin { get; set; }
    public ShipoxAddress? Destination { get; set; }
    public string? ServiceLevel { get; set; }
}

/// <summary>
/// Shipox Dimensions
/// </summary>
public class ShipoxDimensions
{
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public string Unit { get; set; } = "inches";
}

/// <summary>
/// Shipox Address
/// </summary>
public class ShipoxAddress
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}

/// <summary>
/// Shipox Customer
/// </summary>
public class ShipoxCustomer
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

/// <summary>
/// Shipox Carrier
/// </summary>
public class ShipoxCarrier
{
    public string? CarrierId { get; set; }
    public string? Name { get; set; }
    public string? Service { get; set; }
    public decimal? Rate { get; set; }
}




