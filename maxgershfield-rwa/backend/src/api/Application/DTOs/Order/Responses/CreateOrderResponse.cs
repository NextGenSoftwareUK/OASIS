namespace Application.DTOs.Order.Responses;

/// <summary>
/// Represents the response returned after successfully creating a token transfer order.
/// </summary>
/// <param name="OrderId">The unique identifier of the created order.</param>
/// <param name="Message">An optional message providing additional information about the order creation.</param>
public record CreateOrderResponse(
    Guid OrderId,
    string? Message
);