namespace Application.DTOs.Order.Requests;

/// <summary>
/// Represents a request to create a new order for token transfer.
/// </summary>
/// <param name="FromNetwork">The network from which tokens will be transferred.</param>
/// <param name="FromToken">The token type being transferred from the source network.</param>
/// <param name="ToNetwork">The network to which tokens will be transferred.</param>
/// <param name="ToToken">The token type to be received on the destination network.</param>
/// <param name="Amount">The amount of tokens to be transferred.</param>
/// <param name="DestinationAddress">The destination address where the tokens will be sent.</param>
public record CreateOrderRequest(
    string FromNetwork,
    string FromToken,
    string ToNetwork,
    string ToToken,
    decimal Amount,
    string DestinationAddress
);