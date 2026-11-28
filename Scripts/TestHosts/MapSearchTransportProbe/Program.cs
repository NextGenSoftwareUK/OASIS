using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Grpc.Core;
using Grpc.Net.Client;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;

try
{
    if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true")
        throw new InvalidOperationException("This owned probe is cloud-only.");
    var input = JsonSerializer.Deserialize<ProbeInput>(await Console.In.ReadToEndAsync(),
        new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new InvalidOperationException("Probe input is required.");
    var address = new Uri(input.Address);
    if (address.Scheme != "http" || address.Port != 5002 || address.AbsolutePath != "/" ||
        !IPAddress.TryParse(address.Host, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        throw new InvalidOperationException("Only the inspected owned IPv4 gRPC endpoint is permitted.");
    var bytes = ip.GetAddressBytes();
    if (!(bytes[0] == 10 || bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31 ||
          bytes[0] == 192 && bytes[1] == 168))
        throw new InvalidOperationException("The gRPC endpoint must be private.");
    using var handler = new SocketsHttpHandler { UseProxy = false, AllowAutoRedirect = false, ConnectTimeout = TimeSpan.FromSeconds(5) };
    using var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions { HttpHandler = handler });
    var client = new MapService.MapServiceClient(channel);
    var counts = new List<int>();
    foreach (var query in new[] { input.Query, input.NoMatchQuery })
    {
        var operation = Guid.NewGuid().ToString("D");
        var headers = new Metadata {
            { "authorization", "Bearer " + input.Bearer },
            { "idempotency-key", operation },
            { "x-cosmic-operation-id", operation },
            { "x-forwarded-proto", "https" }
        };
        var result = await client.SearchMapLocationsAsync(new MapSearchRequest {
            Query = query, LocationType = "Custom", Latitude = 46.98, Longitude = 8.25,
            RadiusKm = 5, HasLocation = true
        }, headers, deadline: DateTime.UtcNow.AddSeconds(20));
        if (result.IsError)
        {
            Console.Error.WriteLine("MAP_SEARCH_GRPC_FAILURE:" + Safe(result.Message));
            return 1;
        }
        using var data = JsonDocument.Parse(result.Json);
        var rows = data.RootElement.EnumerateArray().ToArray();
        int expected = counts.Count == 0 ? 1 : 0;
        if (rows.Length != expected || expected == 1 && rows[0].GetProperty("Id").GetGuid() != input.RecordId)
            throw new InvalidOperationException("The real gRPC search acknowledgement did not match.");
        counts.Add(rows.Length);
    }
    Console.WriteLine("MAP_SEARCH_GRPC_PROOF:" + JsonSerializer.Serialize(new {
        positiveCount = counts[0], noMatchCount = counts[1], identityMatched = true,
        actualClient = "Grpc.Net.Client", actualSourceProto = true
    }));
    return 0;
}
catch (RpcException ex)
{
    Console.Error.WriteLine("MAP_SEARCH_GRPC_FAILURE:" + ex.StatusCode + ":" + Safe(ex.Status.Detail));
    return 1;
}
catch (Exception)
{
    Console.Error.WriteLine("MAP_SEARCH_GRPC_FAILURE:INVALID_PROBE_OR_ACKNOWLEDGEMENT");
    return 1;
}

static string Safe(string value)
{
    var text = Regex.Replace(value ?? "", @"(?:https?|mongodb(?:\+srv)?):\/\/[^\s]+", "[URI]", RegexOptions.IgnoreCase);
    text = Regex.Replace(text, @"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", "[JWT]");
    if (Regex.IsMatch(text, "password|secret|credential|connectionstring|refresh.?token", RegexOptions.IgnoreCase))
        return "Sensitive detail withheld";
    text = text.Split('\n')[0];
    return text.Length <= 240 ? text : text[..240];
}

sealed class ProbeInput
{
    public required string Address { get; init; }
    public required string Bearer { get; init; }
    public required string Query { get; init; }
    public required string NoMatchQuery { get; init; }
    public required Guid RecordId { get; init; }
}
