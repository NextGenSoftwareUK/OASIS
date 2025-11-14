using System;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;

/// <summary>
/// Response after creating a bridge order
/// </summary>
public class CreateBridgeOrderResponse
{
    /// <summary>
    /// The order ID
    /// </summary>
    public Guid OrderId { get; set; }
    
    /// <summary>
    /// Success/status message
    /// </summary>
    public string Message { get; set; }

    public CreateBridgeOrderResponse(Guid orderId, string message)
    {
        OrderId = orderId;
        Message = message;
    }
}

