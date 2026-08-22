namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;

public class ChainEventData
{
    public string EventType { get; set; } = string.Empty;
    public string ChainName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object? EventData { get; set; }
}









