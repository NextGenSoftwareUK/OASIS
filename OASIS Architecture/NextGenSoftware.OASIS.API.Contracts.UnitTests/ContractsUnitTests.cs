using NextGenSoftware.OASIS.API.Contracts.DataModels;
using NextGenSoftware.OASIS.API.Contracts.Enums;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Contracts.UnitTests;

public class DataModelsTests
{
    [Fact]
    public void BoundingBox_ShouldInitializeCorrectly()
    {
        // Arrange
        var minLat = 40.0;
        var minLng = -74.0;
        var maxLat = 41.0;
        var maxLng = -73.0;

        // Act
        var boundingBox = new BoundingBox
        {
            MinLatitude = minLat,
            MinLongitude = minLng,
            MaxLatitude = maxLat,
            MaxLongitude = maxLng
        };

        // Assert
        Assert.Equal(minLat, boundingBox.MinLatitude);
        Assert.Equal(minLng, boundingBox.MinLongitude);
        Assert.Equal(maxLat, boundingBox.MaxLatitude);
        Assert.Equal(maxLng, boundingBox.MaxLongitude);
    }

    [Fact]
    public void Geolocation_ShouldInitializeCorrectly()
    {
        // Arrange
        var latitude = 40.7128;
        var longitude = -74.0060;
        var accuracy = 10.5;

        // Act
        var geolocation = new Geolocation
        {
            Latitude = latitude,
            Longitude = longitude,
            Accuracy = accuracy
        };

        // Assert
        Assert.Equal(latitude, geolocation.Latitude);
        Assert.Equal(longitude, geolocation.Longitude);
        Assert.Equal(accuracy, geolocation.Accuracy);
    }

    [Fact]
    public void ForwardGeocodingResponse_ShouldInitializeCorrectly()
    {
        // Arrange
        var query = "New York, NY";
        var results = new List<Geolocation>
        {
            new Geolocation { Latitude = 40.7128, Longitude = -74.0060 }
        };

        // Act
        var response = new ForwardGeocodingResponse
        {
            Query = query,
            Results = results
        };

        // Assert
        Assert.Equal(query, response.Query);
        Assert.Equal(results, response.Results);
        Assert.Single(response.Results);
    }
}

public class EnumsTests
{
    [Fact]
    public void MapProviderType_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)MapProviderType.GoogleMaps);
        Assert.Equal(1, (int)MapProviderType.OpenStreetMap);
        Assert.Equal(2, (int)MapProviderType.Mapbox);
        Assert.Equal(3, (int)MapProviderType.Here);
    }

    [Fact]
    public void RoutingType_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)RoutingType.Driving);
        Assert.Equal(1, (int)RoutingType.Walking);
        Assert.Equal(2, (int)RoutingType.Cycling);
        Assert.Equal(3, (int)RoutingType.Transit);
    }
}

public class InterfacesTests
{
    [Fact]
    public void IRequest_ShouldBeInterface()
    {
        // Assert
        Assert.True(typeof(IRequest).IsInterface);
    }

    [Fact]
    public void IBatchRequest_ShouldBeInterface()
    {
        // Assert
        Assert.True(typeof(IBatchRequest).IsInterface);
    }

    [Fact]
    public void IOASISMapProvider_ShouldBeInterface()
    {
        // Assert
        Assert.True(typeof(IOASISMapProvider).IsInterface);
    }

    [Fact]
    public void IDirectionsAPIProvider_ShouldBeInterface()
    {
        // Assert
        Assert.True(typeof(IDirectionsAPIProvider).IsInterface);
    }

    [Fact]
    public void IForwardGeocodingProvider_ShouldBeInterface()
    {
        // Assert
        Assert.True(typeof(IForwardGeocodingProvider).IsInterface);
    }

    [Fact]
    public void IPointOfInterest_ShouldBeInterface()
    {
        // Assert
        Assert.True(typeof(IPointOfInterest).IsInterface);
    }
}
