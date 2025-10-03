namespace NextGenSoftware.OASIS.API.Contracts.Interfaces
{
    /// <summary>
    /// Point of Interest interface
    /// </summary>
    public interface IPointOfInterest
    {
        string Name { get; set; }
        string Description { get; set; }
        Geolocation Location { get; set; }
        string Category { get; set; }
        float Distance { get; set; }
    }
}

