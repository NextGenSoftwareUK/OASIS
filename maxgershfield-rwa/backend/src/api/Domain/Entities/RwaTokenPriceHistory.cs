namespace Domain.Entities;

public sealed class RwaTokenPriceHistory : BaseEntity
{
    public Guid RwaTokenId { get; set; }
    public RwaToken RwaToken { get; set; } = default!;

    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid OwnerId { get; set; }
    public VirtualAccount Owner { get; set; } = default!;
}