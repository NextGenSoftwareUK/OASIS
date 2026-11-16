using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.HEREMapsOASIS;

public partial class HEREMapsOASIS : IOASISRoutingProvider
{
    private const string FlexibleAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
    private static readonly HttpClient RoutingClient = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public async Task<RoutingRoute> ComputeRouteAsync(RoutingCoordinate origin, RoutingCoordinate destination,
        RoutingType travelMode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var mode = travelMode switch
        {
            RoutingType.Driving => "car",
            RoutingType.Walking => "pedestrian",
            RoutingType.Cycling => "bicycle",
            _ => throw new ArgumentOutOfRangeException(nameof(travelMode))
        };
        var key = Environment.GetEnvironmentVariable("OASIS_HERE_ROUTING_API_KEY");
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Configure OASIS_HERE_ROUTING_API_KEY for HERE Routing.");

        var coordinates = FormattableString.Invariant(
            $"origin={origin.Latitude:R},{origin.Longitude:R}&destination={destination.Latitude:R},{destination.Longitude:R}");
        var url = $"https://router.hereapi.com/v8/routes?{coordinates}&transportMode={mode}&return=polyline,summary&apiKey={Uri.EscapeDataString(key)}";
        try
        {
            using var response = await RoutingClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"HERE Routing returned HTTP {(int)response.StatusCode}.");

            using var body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken).ConfigureAwait(false);
            var routes = document.RootElement.GetProperty("routes");
            if (routes.GetArrayLength() == 0)
                throw new InvalidOperationException("HERE Routing returned no route.");

            var points = new List<RoutingCoordinate>();
            double distance = 0;
            foreach (var section in routes[0].GetProperty("sections").EnumerateArray())
            {
                var length = section.GetProperty("summary").GetProperty("length").GetDouble();
                if (!double.IsFinite(length) || length < 0)
                    throw new JsonException("HERE Routing returned an invalid section distance.");

                distance += length;
                DecodeSection(section.GetProperty("polyline").GetString(), points);
            }

            return new RoutingRoute(distance, points);
        }
        catch (HttpRequestException)
        {
            throw new InvalidOperationException("HERE Routing transport failed.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("HERE Routing request timed out.");
        }
    }

    private static void DecodeSection(string encoded, List<RoutingCoordinate> points)
    {
        if (string.IsNullOrEmpty(encoded))
            throw new JsonException("HERE Routing returned no section geometry.");

        int index = 0;
        if (ReadUnsigned(encoded, ref index) != 1)
            throw new JsonException("Unsupported HERE flexible polyline version.");
        long header = ReadUnsigned(encoded, ref index);
        if ((header & ~15L) != 0)
            throw new JsonException("Expected a two-dimensional HERE polyline.");

        double scale = Math.Pow(10, header);
        long latitude = 0;
        long longitude = 0;
        int start = index;
        while (index < encoded.Length)
        {
            latitude = checked(latitude + DecodeDelta(ReadUnsigned(encoded, ref index)));
            longitude = checked(longitude + DecodeDelta(ReadUnsigned(encoded, ref index)));
            var point = new RoutingCoordinate(latitude / scale, longitude / scale);
            if (points.Count == 0 || points[^1] != point)
                points.Add(point);
        }

        if (index == start)
            throw new JsonException("HERE Routing returned empty section geometry.");
    }

    private static long DecodeDelta(long value) => (value & 1) == 0 ? value >> 1 : ~(value >> 1);

    private static long ReadUnsigned(string encoded, ref int index)
    {
        long value = 0;
        for (int shift = 0; index < encoded.Length && shift <= 55; shift += 5)
        {
            int group = FlexibleAlphabet.IndexOf(encoded[index++]);
            if (group < 0)
                break;

            value |= (long)(group & 31) << shift;
            if ((group & 32) == 0)
                return value;
        }

        throw new JsonException("HERE Routing returned malformed flexible polyline geometry.");
    }
}
