using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Directions;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.UnitTests;

[TestClass]
public sealed class OSRMNativeRouteTests
{
    [TestMethod]
    public async Task ThreeFixedPublicBerlinRoutesUseActualClient()
    {
        Assert.AreEqual("fixed-public-berlin",Environment.GetEnvironmentVariable("COSMIC_GOMAP_NATIVE_ROUTE"),
            "Native routing requires explicit cloud qualification; never run as part of the controlled suite.");
        Assert.AreEqual("true",Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var points=new[]{new Geolocation(52.517037,13.388860),new Geolocation(52.529407,13.397634),new Geolocation(52.523219,13.428555)};
        var directory=Path.GetFullPath(Environment.GetEnvironmentVariable("COSMIC_GOMAP_NATIVE_EVIDENCE_DIRECTORY")!);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE")!,"OASIS","TestResults","GOMapRouteClientCurrent","native")),directory);
        Assert.IsFalse(Directory.Exists(directory));Directory.CreateDirectory(directory);
        var client=new OSRMDirectionsProvider();
        var observed=new List<object>();
        using var cancellation=new CancellationTokenSource(TimeSpan.FromSeconds(100));
        for(var i=0;i<3;i++)
        {
            var route=await client.GetRouteAsync(points[i],points[(i+1)%3],RoutingType.Driving,cancellation.Token);
            Assert.IsFalse(route.IsError,route.Message);Assert.IsNotNull(route.Result);
            Assert.IsTrue(route.Result.Geometry.Count>=2);Assert.IsTrue(route.Result.DistanceMetres>0);
            Assert.AreEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(route.Result.RawJson))).ToLowerInvariant(),route.Result.ResponseSha256);
            await File.WriteAllTextAsync(Path.Combine(directory,"route-"+i+".json"),route.Result.RawJson,cancellation.Token);
            observed.Add(new{index=i,route.Result.ResponseSha256,route.Result.DistanceMetres,route.Result.DurationSeconds,
                coordinateCount=route.Result.Geometry.Count,route.Result.ObservedUtc});
        }
        await File.WriteAllTextAsync(Path.Combine(directory,"acknowledged.json"),JsonSerializer.Serialize(new{
            source=Environment.GetEnvironmentVariable("GITHUB_SHA"),endpoint=OSRMDirectionsProvider.DefaultBaseUrl,
            fixedPublicCoordinates=true,requests=3,retries=0,observations=observed,
            attribution="© OpenStreetMap contributors",attributionUrl="https://www.openstreetmap.org/copyright",
            fixMapUrl="https://www.openstreetmap.org/fixthemap",developerQualificationOnly=true,
            cqlQualified=false,liveDeployment=false}));
    }
}
