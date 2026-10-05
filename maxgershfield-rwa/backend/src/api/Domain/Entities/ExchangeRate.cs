namespace Domain.Entities;

/// <summary>
/// Represents the exchange rate between two network tokens in the system.
/// This entity stores the rate at which one token can be exchanged for another, 
/// along with the source URL for reference and related orders.
/// </summary>
public sealed class ExchangeRate : BaseEntity
{
    /// <summary>
    /// The unique identifier of the token being exchanged from.
    /// This field establishes a relationship with the <see cref="NetworkToken"/> entity.
    /// </summary>
    public Guid FromTokenId { get; set; }

    /// <summary>
    /// The token being exchanged from.
    /// This property creates a navigation property to the <see cref="NetworkToken"/> entity.
    /// </summary>
    public NetworkToken FromToken { get; set; } = default!;

    /// <summary>
    /// The unique identifier of the token being exchanged to.
    /// This field establishes a relationship with the <see cref="NetworkToken"/> entity.
    /// </summary>
    public Guid ToTokenId { get; set; }

    /// <summary>
    /// The token being exchanged to.
    /// This property creates a navigation property to the <see cref="NetworkToken"/> entity.
    /// </summary>
    public NetworkToken ToToken { get; set; } = default!;

    /// <summary>
    /// The exchange rate between the two tokens.
    /// This represents how much of the "ToToken" can be obtained for one unit of the "FromToken".
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    /// The source URL where the exchange rate was obtained from.
    /// This could be a link to an API or a platform providing the exchange data.
    /// </summary>
    public string SourceUrl { get; set; } = default!;

    /// <summary>
    /// The collection of orders that are associated with this exchange rate.
    /// This represents all orders that are affected by this specific exchange rate.
    /// </summary>
    public ICollection<Order> Orders { get; } = new List<Order>();
}