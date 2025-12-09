namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

/// <summary>
/// Response model for shipping orders
/// </summary>
public class OrderResponse
{
    public Guid OrderId { get; set; }
    public Guid QuoteId { get; set; }
    public Guid? ShipmentId { get; set; }
    public string SelectedCarrier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public CustomerInfo? CustomerInfo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}




