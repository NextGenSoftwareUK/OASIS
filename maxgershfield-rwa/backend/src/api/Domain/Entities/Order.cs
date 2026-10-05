namespace Domain.Entities;

/// <summary>
/// Represents an order placed by a user for exchanging tokens across networks.
/// This entity stores information about the order's exchange rate, source and destination networks,
/// token details, amount, status, and transaction information.
/// </summary>
public sealed class Order : BaseEntity
{
    /// <summary>
    /// The unique identifier of the user who placed the order.
    /// This field establishes a relationship with the <see cref="User"/> entity.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user who placed the order.
    /// This property creates a navigation property to the <see cref="User"/> entity.
    /// </summary>
    public User User { get; set; } = default!;

    /// <summary>
    /// The unique identifier of the exchange rate used for the order.
    /// This field establishes a relationship with the <see cref="ExchangeRate"/> entity.
    /// </summary>
    public Guid ExchangeRateId { get; set; }

    /// <summary>
    /// The exchange rate used for this order.
    /// This property creates a navigation property to the <see cref="ExchangeRate"/> entity.
    /// </summary>
    public ExchangeRate ExchangeRate { get; set; } = default!;

    /// <summary>
    /// The source network for the exchange (e.g., "Solana", "Ethereum").
    /// </summary>
    public string FromNetwork { get; set; } = string.Empty;

    /// <summary>
    /// The source token for the exchange (e.g., "SOL", "ETH").
    /// </summary>
    public string FromToken { get; set; } = string.Empty;

    /// <summary>
    /// The destination network for the exchange (e.g., "Solana", "Ethereum").
    /// </summary>
    public string ToNetwork { get; set; } = string.Empty;

    /// <summary>
    /// The destination token for the exchange (e.g., "SOL", "ETH").
    /// </summary>
    public string ToToken { get; set; } = string.Empty;

    /// <summary>
    /// The destination address for the exchanged tokens.
    /// This is where the tokens will be sent after the exchange is completed.
    /// </summary>
    public string DestinationAddress { get; set; } = string.Empty;

    /// <summary>
    /// The amount of tokens being exchanged.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The transaction hash for the completed exchange, if available.
    /// This can be used to track the transaction on the blockchain.
    /// </summary>
    public string? TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// The current status of the order (e.g., "Pending", "Completed").
    /// </summary>
    public OrderStatus OrderStatus { get; set; }

    /// <summary>
    /// The date and time when the order was completed, if applicable.
    /// This is set once the transaction is confirmed on the blockchain.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
