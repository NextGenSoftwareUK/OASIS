namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

/// <summary>
/// Order entity model
/// </summary>
public class Order
{
    public Guid OrderId { get; set; }
    public Guid MerchantId { get; set; }
    public Guid QuoteId { get; set; }
    public Guid? ShipmentId { get; set; }
    public string SelectedCarrier { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Created, Shipped, Delivered, Cancelled
    public string? TrackingNumber { get; set; }
    public CustomerInfo? CustomerInfo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

