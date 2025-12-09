namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.Shipox.Models;

/// <summary>
/// Webhook payload from Shipox platform
/// </summary>
public class ShipoxWebhookPayload
{
    public string? EventType { get; set; }
    public string? OrderId { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Status { get; set; }
    public DateTime? Timestamp { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}




