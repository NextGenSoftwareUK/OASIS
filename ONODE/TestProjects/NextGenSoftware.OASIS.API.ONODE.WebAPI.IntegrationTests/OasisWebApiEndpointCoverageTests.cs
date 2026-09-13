using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests;

[TestClass]
public class OasisWebApiEndpointCoverageTests
{
    [TestMethod]
    public async Task SwaggerEndpoints_ReturnNonServerError()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("OASIS_WEBAPI_BASE_URL") ?? "http://localhost:5003").TrimEnd('/');
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        var token = await TryAuthenticateAsync(client, baseUrl);
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var swaggerDoc = JsonDocument.Parse(await client.GetStringAsync($"{baseUrl}/swagger/v1/swagger.json"));
        var endpoints = ReadEndpoints(swaggerDoc.RootElement).ToList();
        Assert.IsTrue(endpoints.Count > 0);

        var failures = new List<string>();
        foreach (var endpoint in endpoints)
        {
            var request = new HttpRequestMessage(new HttpMethod(endpoint.Method), $"{baseUrl}{ResolvePath(endpoint.Path)}");
            if (endpoint.Method is "POST" or "PUT" or "PATCH")
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                failures.Add($"{endpoint.Method} {endpoint.Path} => exception: {ex.Message}");
                continue;
            }

            if ((int)response.StatusCode >= 500)
            {
                var body = await response.Content.ReadAsStringAsync();
                failures.Add($"{endpoint.Method} {endpoint.Path} => {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
            }
        }

        Assert.IsTrue(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static IEnumerable<(string Method, string Path)> ReadEndpoints(JsonElement root)
    {
        if (!root.TryGetProperty("paths", out var paths) || paths.ValueKind != JsonValueKind.Object)
            yield break;

        foreach (var pathProp in paths.EnumerateObject())
        {
            if (pathProp.Value.ValueKind != JsonValueKind.Object)
                continue;

            foreach (var methodProp in pathProp.Value.EnumerateObject())
            {
                var method = methodProp.Name.ToUpperInvariant();
                if (method is "GET" or "POST" or "PUT" or "PATCH" or "DELETE")
                    yield return (method, pathProp.Name);
            }
        }
    }

    private static string ResolvePath(string path)
    {
        return System.Text.RegularExpressions.Regex.Replace(path, "\\{([^}]+)\\}", m =>
        {
            var key = m.Groups[1].Value.ToLowerInvariant();
            if (key.Contains("id"))
                return Guid.NewGuid().ToString();
            if (key.Contains("version"))
                return "1";
            return "test";
        });
    }

    private static async Task<string?> TryAuthenticateAsync(HttpClient client, string baseUrl)
    {
        var username = Environment.GetEnvironmentVariable("OASIS_WEBAPI_USERNAME") ?? "dellams";
        var password = Environment.GetEnvironmentVariable("OASIS_WEBAPI_PASSWORD") ?? "test!";
        var payload = JsonSerializer.Serialize(new { username, password });
        var response = await client.PostAsync($"{baseUrl}/api/avatar/authenticate", new StringContent(payload, Encoding.UTF8, "application/json"));
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return null;

        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
            return null;

        using var doc = JsonDocument.Parse(body);
        return FindStringRecursive(doc.RootElement, "jwtToken");
    }

    private static string? FindStringRecursive(JsonElement element, string propertyName)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
                        property.Value.ValueKind == JsonValueKind.String)
                    {
                        return property.Value.GetString();
                    }

                    var nested = FindStringRecursive(property.Value, propertyName);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var nested = FindStringRecursive(item, propertyName);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }
                break;
        }

        return null;
    }
}


