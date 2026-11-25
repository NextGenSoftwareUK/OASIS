using NextGenSoftware.OASIS.API.Contracts.Enums;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Contracts.UnitTests;

public class DataModelsTests
{
    [Fact]
    public void Geolocation_TwoArgumentConstructor_IsLatitudeThenLongitude()
    {
        var location = new Geolocation(51.5, -0.12);

        Assert.Equal(51.5, location.Latitude);
        Assert.Equal(-0.12, location.Longitude);
    }

    [Fact]
    public void Geolocation_LongitudeFirstConstructor_SwapsArguments()
    {
        var location = new Geolocation(-0.12, 51.5, isLongitudeFirst: true);

        Assert.Equal(51.5, location.Latitude);
        Assert.Equal(-0.12, location.Longitude);
    }

    [Fact]
    public void Geolocation_ToString_IsLongitudeCommaLatitudeInInvariantCulture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            Assert.Equal("-0.12,51.5", new Geolocation(51.5, -0.12).ToString());
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Geolocation_GetBoundingBox_SurroundsThePoint()
    {
        var centre = new Geolocation(40.0, -74.0);

        var box = centre.GetBoundingBox(10);

        Assert.True(box.MinPoint.Latitude < centre.Latitude && centre.Latitude < box.MaxPoint.Latitude);
        Assert.True(box.MinPoint.Longitude < centre.Longitude && centre.Longitude < box.MaxPoint.Longitude);
        // 10 km is roughly 0.09 degrees of latitude.
        Assert.InRange(box.MaxPoint.Latitude - centre.Latitude, 0.08, 0.10);
        Assert.Equal(centre.Latitude - box.MinPoint.Latitude, box.MaxPoint.Latitude - centre.Latitude, 6);
    }

    [Fact]
    public void BoundingBox_ToString_JoinsMinAndMaxPoints()
    {
        var box = new BoundingBox { MinPoint = new Geolocation(1, 2), MaxPoint = new Geolocation(3, 4) };

        Assert.Equal("2,1,4,3", box.ToString());
    }

    [Fact]
    public void ForwardGeocodingResponse_ConstructorSetsSuccessAndData()
    {
        var data = new List<string> { "a" };

        var response = new ForwardGeocodingResponse<string>(true, data);

        Assert.True(response.Success);
        Assert.Same(data, response.Data);
    }

    [Fact]
    public void RoutingCoordinate_RejectsOutOfRangeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RoutingCoordinate(91, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RoutingCoordinate(0, -181));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RoutingCoordinate(double.NaN, 0));
    }

    [Fact]
    public void RoutingRoute_RequiresAtLeastTwoPointsAndNonNegativeDistance()
    {
        var a = new RoutingCoordinate(0, 0);
        var b = new RoutingCoordinate(1, 1);

        Assert.Throws<ArgumentException>(() => new RoutingRoute(10, new[] { a }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RoutingRoute(-1, new[] { a, b }));

        var route = new RoutingRoute(10, new[] { a, b });
        Assert.Equal(10, route.DistanceMeters);
        Assert.Equal(2, route.Points.Count);
    }
}

public class EnumsTests
{
    [Fact]
    public void MapProviderType_HasExpectedProviders()
    {
        Assert.True(Enum.IsDefined(MapProviderType.MapBox));
        Assert.True(Enum.IsDefined(MapProviderType.GoogleMaps));
        Assert.True(Enum.IsDefined(MapProviderType.HEREMaps));
        Assert.True(Enum.IsDefined(MapProviderType.MapLibre));
    }

    [Fact]
    public void RoutingType_HasWalkingDrivingCycling()
    {
        Assert.Equal(new[] { RoutingType.Walking, RoutingType.Driving, RoutingType.Cycling }, Enum.GetValues<RoutingType>());
    }
}
