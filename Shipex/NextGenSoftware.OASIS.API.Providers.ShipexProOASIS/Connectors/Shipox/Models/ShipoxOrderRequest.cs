namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.Shipox.Models;

/// <summary>
/// Request model for creating a Shipox order
/// </summary>
public class ShipoxOrderRequest
{
    public string? ExternalOrderId { get; set; }
    public ShipoxOrderDetails? Details { get; set; }
    public ShipoxCustomer? Customer { get; set; }
    public string? SelectedCarrier { get; set; }
}

/// <summary>
/// Request model for updating a Shipox order
/// </summary>
public class ShipoxOrderUpdate
{
    public string? Status { get; set; }
    public ShipoxOrderDetails? Details { get; set; }
    public ShipoxCustomer? Customer { get; set; }
}

/// <summary>
/// Response model from Shipox API
/// </summary>
public class ShipoxApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

