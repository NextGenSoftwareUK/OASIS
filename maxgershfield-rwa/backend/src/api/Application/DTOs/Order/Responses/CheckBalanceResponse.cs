namespace Application.DTOs.Order.Responses;

/// <summary>
/// Represents the response containing balance check information for a specific order.
/// </summary>
/// <param name="OrderId">The unique identifier of the order being checked.</param>
/// <param name="Network">The network on which the token balance is checked.</param>
/// <param name="Token">The symbol or name of the token involved in the order.</param>
/// <param name="Balance">The current available balance of the token on the specified network.</param>
/// <param name="RequiredBalance">The required amount of tokens needed to process the order.</param>
/// <param name="Status">The status of the balance check (e.g., Sufficient, Insufficient).</param>
/// <param name="Message">A message providing context or details about the status.</param>
/// <param name="TransactionId">The ID of the transaction related to the balance check, if available.</param>
public record CheckBalanceResponse(
    Guid OrderId,
    string Network,
    string Token,
    decimal Balance,
    decimal RequiredBalance,
    string Status,
    string Message,
    string? TransactionId
);