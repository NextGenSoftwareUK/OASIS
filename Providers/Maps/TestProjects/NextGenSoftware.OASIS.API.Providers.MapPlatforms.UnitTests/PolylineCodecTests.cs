using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Geo;

namespace NextGenSoftware.OASIS.API.Providers.MapPlatforms.UnitTests;

[TestClass]
public sealed class PolylineCodecTests
{
    // Google's published worked example for the Encoded Polyline Algorithm Format.
    internal const string GoogleVector = "_p~iF~ps|U_ulLnnqC_mqNvxq`@";

    // HERE's published flexible-polyline test vector (precision 5, no third dimension).
    internal const string HereVector = "BFoz5xJ67i1B1B7PzIhaxL7Y";

    [TestMethod]
    public void GoogleDecodesPublishedExample()
    {
        List<Geolocation> points = GooglePolylineCodec.Decode(GoogleVector);

        Assert.AreEqual(3, points.Count);
        AssertPoint(points[0], 38.5, -120.2);
        AssertPoint(points[1], 40.7, -120.95);
        AssertPoint(points[2], 43.252, -126.453);
    }

    [TestMethod]
    public void GoogleEncodesPublishedExample()
    {
        var points = new[] { new Geolocation(38.5, -120.2), new Geolocation(40.7, -120.95), new Geolocation(43.252, -126.453) };
        Assert.AreEqual(GoogleVector, GooglePolylineCodec.Encode(points));
    }

    [TestMethod]
    public void GoogleRejectsTruncatedInput()
    {
        Assert.ThrowsException<InvalidDataException>(() => GooglePolylineCodec.Decode("_p~iF"));
    }

    [TestMethod]
    public void HereDecodesPublishedVector()
    {
        List<Geolocation> points = FlexiblePolylineCodec.Decode(HereVector);

        Assert.AreEqual(4, points.Count);
        AssertPoint(points[0], 50.10228, 8.69821);
        AssertPoint(points[1], 50.10201, 8.69567);
        AssertPoint(points[2], 50.10063, 8.69150);
        AssertPoint(points[3], 50.09878, 8.68752);
    }

    [TestMethod]
    public void HereRejectsUnknownVersion()
    {
        Assert.ThrowsException<InvalidDataException>(() => FlexiblePolylineCodec.Decode("CFoz5xJ67i1B"));
    }

    [TestMethod]
    public void HereRejectsCharactersOutsideAlphabet()
    {
        Assert.ThrowsException<InvalidDataException>(() => FlexiblePolylineCodec.Decode("BF*z5xJ67i1B"));
    }

    internal static void AssertPoint(Geolocation point, double latitude, double longitude)
    {
        Assert.AreEqual(latitude, point.Latitude, 1e-9);
        Assert.AreEqual(longitude, point.Longitude, 1e-9);
    }
}
