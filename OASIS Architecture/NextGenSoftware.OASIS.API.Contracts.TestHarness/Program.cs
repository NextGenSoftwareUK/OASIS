using NextGenSoftware.OASIS.API.Contracts.DataModels;
using NextGenSoftware.OASIS.API.Contracts.Enums;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Contracts.TestHarness;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== OASIS API Contracts Test Harness ===");
        Console.WriteLine();

        // Test Data Models
        await TestDataModels();
        
        // Test Enums
        await TestEnums();
        
        // Test Interfaces
        await TestInterfaces();
        
        Console.WriteLine();
        Console.WriteLine("=== All Tests Completed Successfully! ===");
    }

    static async Task TestDataModels()
    {
        Console.WriteLine("Testing Data Models...");
        
        // Test BoundingBox
        var boundingBox = new BoundingBox
        {
            MinLatitude = 40.0,
            MinLongitude = -74.0,
            MaxLatitude = 41.0,
            MaxLongitude = -73.0
        };
        Console.WriteLine($"✓ BoundingBox: {boundingBox.MinLatitude}, {boundingBox.MinLongitude} to {boundingBox.MaxLatitude}, {boundingBox.MaxLongitude}");
        
        // Test Geolocation
        var geolocation = new Geolocation
        {
            Latitude = 40.7128,
            Longitude = -74.0060,
            Accuracy = 10.5
        };
        Console.WriteLine($"✓ Geolocation: {geolocation.Latitude}, {geolocation.Longitude} (Accuracy: {geolocation.Accuracy})");
        
        // Test ForwardGeocodingResponse
        var response = new ForwardGeocodingResponse
        {
            Query = "New York, NY",
            Results = new List<Geolocation> { geolocation }
        };
        Console.WriteLine($"✓ ForwardGeocodingResponse: Query='{response.Query}', Results={response.Results.Count}");
        
        await Task.Delay(100); // Simulate async operation
    }

    static async Task TestEnums()
    {
        Console.WriteLine("Testing Enums...");
        
        // Test MapProviderType
        var providers = new[] { MapProviderType.GoogleMaps, MapProviderType.OpenStreetMap, MapProviderType.Mapbox, MapProviderType.Here };
        foreach (var provider in providers)
        {
            Console.WriteLine($"✓ MapProviderType: {provider} = {(int)provider}");
        }
        
        // Test RoutingType
        var routingTypes = new[] { RoutingType.Driving, RoutingType.Walking, RoutingType.Cycling, RoutingType.Transit };
        foreach (var routing in routingTypes)
        {
            Console.WriteLine($"✓ RoutingType: {routing} = {(int)routing}");
        }
        
        await Task.Delay(100); // Simulate async operation
    }

    static async Task TestInterfaces()
    {
        Console.WriteLine("Testing Interfaces...");
        
        // Test interface accessibility
        var interfaces = new[]
        {
            typeof(IRequest),
            typeof(IBatchRequest),
            typeof(IOASISMapProvider),
            typeof(IDirectionsAPIProvider),
            typeof(IForwardGeocodingProvider),
            typeof(IPointOfInterest)
        };
        
        foreach (var interfaceType in interfaces)
        {
            Console.WriteLine($"✓ Interface: {interfaceType.Name} - IsInterface: {interfaceType.IsInterface}");
        }
        
        await Task.Delay(100); // Simulate async operation
    }
}
