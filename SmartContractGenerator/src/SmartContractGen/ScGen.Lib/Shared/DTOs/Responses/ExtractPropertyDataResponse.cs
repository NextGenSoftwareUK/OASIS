namespace ScGen.Lib.Shared.DTOs.Responses;

public sealed record ExtractPropertyDataResponse
{
    public required PropertyData Data { get; init; }
    public required Dictionary<string, double> Confidence { get; init; }
    public required List<string> Warnings { get; init; }
    public required ExtractionSource Source { get; init; }
}

public sealed record PropertyData
{
    // Location
    public string? PropertyAddress { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? County { get; init; }
    
    // Valuation
    public long? PropertyValue { get; init; }
    
    // Size
    public int? TotalSquareFootage { get; init; }
    public double? LotSize { get; init; }
    public string? LotSizeUnit { get; init; }
    
    // Core Characteristics
    public int? Bedrooms { get; init; }
    public int? BathroomsFull { get; init; }
    public int? BathroomsPartial { get; init; }
    public int? YearBuilt { get; init; }
    
    // Property Classification
    public string? PropertyType { get; init; }
    public string? ArchitecturalStyle { get; init; }
    
    // Premium Features
    public List<string>? PremiumAmenities { get; init; }
    
    // Building Systems
    public int? ParkingSpaces { get; init; }
    public string? ParkingType { get; init; }
    public string? HvacType { get; init; }
    public int? Fireplaces { get; init; }
    public List<string>? FireplaceTypes { get; init; }
    
    // Condition
    public int? YearRenovated { get; init; }
    public string? OverallCondition { get; init; }
    public List<string>? RecentUpgrades { get; init; }
    
    // Additional Info
    public string? SchoolDistrict { get; init; }
    public string? MlsNumber { get; init; }
    public string? Description { get; init; }
    public List<string>? PropertyImages { get; init; }
    public string? VirtualTourUrl { get; init; }
}

public sealed record ExtractionSource
{
    public required string Url { get; init; }
    public required string Platform { get; init; }
    public required DateTime ExtractedAt { get; init; }
}

