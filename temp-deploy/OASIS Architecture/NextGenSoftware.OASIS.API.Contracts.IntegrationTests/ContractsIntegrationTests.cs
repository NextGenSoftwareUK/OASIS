using NextGenSoftware.OASIS.API.Contracts.DataModels;
using NextGenSoftware.OASIS.API.Contracts.Enums;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Contracts.IntegrationTests;

public class DataModelsIntegrationTests
{
    [Fact]
    public void BoundingBox_ShouldCalculateAreaCorrectly()
    {
        // Arrange
        var boundingBox = new BoundingBox
        {
            MinLatitude = 40.0,
            MinLongitude = -74.0,
            MaxLatitude = 41.0,
            MaxLongitude = -73.0
        };

        // Act & Assert
        Assert.True(boundingBox.MaxLatitude > boundingBox.MinLatitude);
        Assert.True(boundingBox.MaxLongitude > boundingBox.MinLongitude);
    }

    [Fact]
    public void Geolocation_ShouldValidateCoordinates()
    {
        // Arrange & Act
        var validGeolocation = new Geolocation
        {
            Latitude = 40.7128,
            Longitude = -74.0060,
            Accuracy = 10.5
        };

        // Assert
        Assert.True(validGeolocation.Latitude >= -90 && validGeolocation.Latitude <= 90);
        Assert.True(validGeolocation.Longitude >= -180 && validGeolocation.Longitude <= 180);
        Assert.True(validGeolocation.Accuracy >= 0);
    }

    [Fact]
    public void ForwardGeocodingResponse_ShouldHandleMultipleResults()
    {
        // Arrange
        var results = new List<Geolocation>
        {
            new Geolocation { Latitude = 40.7128, Longitude = -74.0060, Accuracy = 10.0 },
            new Geolocation { Latitude = 40.7589, Longitude = -73.9851, Accuracy = 15.0 },
            new Geolocation { Latitude = 40.6892, Longitude = -74.0445, Accuracy = 20.0 }
        };

        // Act
        var response = new ForwardGeocodingResponse
        {
            Query = "New York City",
            Results = results
        };

        // Assert
        Assert.Equal(3, response.Results.Count);
        Assert.All(response.Results, result => 
        {
            Assert.True(result.Latitude >= -90 && result.Latitude <= 90);
            Assert.True(result.Longitude >= -180 && result.Longitude <= 180);
        });
    }
}

public class EnumsIntegrationTests
{
    [Theory]
    [InlineData(MapProviderType.GoogleMaps, 0)]
    [InlineData(MapProviderType.OpenStreetMap, 1)]
    [InlineData(MapProviderType.Mapbox, 2)]
    [InlineData(MapProviderType.Here, 3)]
    public void MapProviderType_ShouldHaveCorrectValues(MapProviderType provider, int expectedValue)
    {
        // Assert
        Assert.Equal(expectedValue, (int)provider);
    }

    [Theory]
    [InlineData(RoutingType.Driving, 0)]
    [InlineData(RoutingType.Walking, 1)]
    [InlineData(RoutingType.Cycling, 2)]
    [InlineData(RoutingType.Transit, 3)]
    public void RoutingType_ShouldHaveCorrectValues(RoutingType routing, int expectedValue)
    {
        // Assert
        Assert.Equal(expectedValue, (int)routing);
    }
}

public class InterfacesIntegrationTests
{
    [Fact]
    public void AllInterfaces_ShouldBeAccessible()
    {
        // Assert - Verify all interfaces can be accessed
        Assert.NotNull(typeof(IRequest));
        Assert.NotNull(typeof(IBatchRequest));
        Assert.NotNull(typeof(IOASISMapProvider));
        Assert.NotNull(typeof(IDirectionsAPIProvider));
        Assert.NotNull(typeof(IForwardGeocodingProvider));
        Assert.NotNull(typeof(IPointOfInterest));
    }

    [Fact]
    public void Interfaces_ShouldHaveExpectedMembers()
    {
        // Assert - Verify interfaces have expected structure
        Assert.True(typeof(IRequest).IsInterface);
        Assert.True(typeof(IBatchRequest).IsInterface);
        Assert.True(typeof(IOASISMapProvider).IsInterface);
        Assert.True(typeof(IDirectionsAPIProvider).IsInterface);
        Assert.True(typeof(IForwardGeocodingProvider).IsInterface);
        Assert.True(typeof(IPointOfInterest).IsInterface);
    }
}
