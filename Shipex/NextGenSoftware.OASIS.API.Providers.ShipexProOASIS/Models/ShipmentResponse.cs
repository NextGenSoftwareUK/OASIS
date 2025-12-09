namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

/// <summary>
/// Response model for shipment creation
/// </summary>
public class ShipmentResponse
{
    public Guid ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public ShipmentLabel? Label { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Shipment label information
/// </summary>
public class ShipmentLabel
{
    public string? PdfUrl { get; set; }
    public string? SignedUrl { get; set; }
    public string? PdfBase64 { get; set; }
}

/// <summary>
/// Response model for tracking information
/// </summary>
public class TrackingResponse
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CurrentLocation { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
    public List<TrackingHistoryItem> History { get; set; } = new();
}

/// <summary>
/// Tracking history item
/// </summary>
public class TrackingHistoryItem
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
}




