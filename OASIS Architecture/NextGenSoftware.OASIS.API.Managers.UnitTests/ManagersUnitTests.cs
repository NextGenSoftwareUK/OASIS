using System.Reflection;
using NextGenSoftware.OASIS.API.Contracts.Enums;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Managers.UnitTests;

/// <summary>
/// Records every call made to IOASISMapProvider and answers from a table of
/// canned results keyed by member name, so MapManager can be tested without
/// a real map engine.
/// </summary>
public class FakeMapProvider : DispatchProxy
{
    public Dictionary<string, object?> Results { get; } = new();
    public List<string> Calls { get; } = new();

    public static (IOASISMapProvider Provider, FakeMapProvider Fake) Create()
    {
        var provider = Create<IOASISMapProvider, FakeMapProvider>();
        return (provider, (FakeMapProvider)(object)provider);
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var name = targetMethod!.Name;
        Calls.Add(name);
        if (Results.TryGetValue(name, out var result))
            return result;
        var type = targetMethod.ReturnType;
        if (type == typeof(Task)) return Task.CompletedTask;
        return type.IsValueType && type != typeof(void) ? Activator.CreateInstance(type) : null;
    }
}

public class MapManagerTests
{
    [Fact]
    public void WithoutProvider_OperationsReturnSafeDefaults()
    {
        var manager = new MapManager();

        Assert.False(manager.IsInitialized());
        Assert.Equal("None", manager.GetCurrentMapProviderName());
        Assert.False(manager.PanMapUp(1));
        Assert.False(manager.ZoomMapIn(1));
        Assert.False(manager.DropPin(new Geolocation(1, 2), new object()));
        Assert.Null(manager.GetCurrentLocation());
        Assert.Null(manager.GetRandomLocationNearCurrent(100));
        Assert.Equal(0f, manager.CalculateDistance(0, 0, 1, 1));
        Assert.True(manager.WaitForOriginSet().IsCompleted);
    }

    [Fact]
    public void WithoutProvider_IsWithinRadiusIsFalse()
    {
        var manager = new MapManager();

        Assert.False(manager.IsWithinRadius(new Geolocation(0, 0), new Geolocation(50, 50), 10));
    }

    [Fact]
    public void SetCurrentMapProvider_TakesTypeFromProvider()
    {
        var (provider, fake) = FakeMapProvider.Create();
        fake.Results["get_MapProviderType"] = MapProviderType.MapLibre;
        fake.Results["get_MapProviderName"] = "Fake";
        fake.Results["get_IsInitialized"] = true;

        var manager = new MapManager(provider);

        Assert.Same(provider, manager.CurrentMapProvider);
        Assert.Equal(MapProviderType.MapLibre, manager.CurrentMapProviderType);
        Assert.Equal("Fake", manager.GetCurrentMapProviderName());
        Assert.True(manager.IsInitialized());
    }

    [Fact]
    public void SetCurrentMapProvider_ByTypeOnlyRecordsType()
    {
        var manager = new MapManager();

        manager.SetCurrentMapProvider(MapProviderType.GoogleMaps);

        Assert.Equal(MapProviderType.GoogleMaps, manager.CurrentMapProviderType);
        Assert.Null(manager.CurrentMapProvider);
    }

    [Fact]
    public void Operations_AreForwardedToProvider()
    {
        var (provider, fake) = FakeMapProvider.Create();
        fake.Results["PanMapUp"] = true;
        fake.Results["DropPin"] = true;
        var manager = new MapManager(provider);

        Assert.True(manager.PanMapUp(2));
        Assert.True(manager.DropPin(new Geolocation(1, 2), new object()));
        Assert.Contains("PanMapUp", fake.Calls);
        Assert.Contains("DropPin", fake.Calls);
    }

    [Theory]
    [InlineData(50f, 100f, true)]
    [InlineData(100f, 100f, true)]
    [InlineData(150f, 100f, false)]
    public void IsWithinRadius_ComparesProviderDistance(float distance, float radius, bool expected)
    {
        var (provider, fake) = FakeMapProvider.Create();
        fake.Results["CalculateDistance"] = distance;
        var manager = new MapManager(provider);

        Assert.Equal(expected, manager.IsWithinRadius(new Geolocation(0, 0), new Geolocation(0, 1), radius));
    }

    [Fact]
    public void GetRandomLocationNearCurrent_StaysWithinRadius()
    {
        var (provider, fake) = FakeMapProvider.Create();
        var current = new Geolocation(51.5, -0.12);
        fake.Results["GetCurrentLocation"] = current;
        var manager = new MapManager(provider);

        for (var i = 0; i < 50; i++)
        {
            var location = manager.GetRandomLocationNearCurrent(1000)!;
            var offsetDegrees = Math.Sqrt(Math.Pow(location.Latitude - current.Latitude, 2) + Math.Pow(location.Longitude - current.Longitude, 2));
            Assert.InRange(offsetDegrees, 0, 1000 / 111111.0 + 1e-9);
        }
    }

    [Fact]
    public void ZoomToCoOrdsOnMap_IsNotImplementedAndReturnsFalse()
    {
        var (provider, _) = FakeMapProvider.Create();

        Assert.False(new MapManager(provider).ZoomToCoOrdsOnMap(1, 2));
    }
}
