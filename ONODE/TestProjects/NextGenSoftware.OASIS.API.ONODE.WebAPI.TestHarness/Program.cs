using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var baseUrl = (Environment.GetEnvironmentVariable("OASIS_WEBAPI_BASE_URL") ?? "http://localhost:5003").TrimEnd('/');
var username = Environment.GetEnvironmentVariable("OASIS_WEBAPI_USERNAME") ?? "dellams";
var password = Environment.GetEnvironmentVariable("OASIS_WEBAPI_PASSWORD") ?? "test!";

using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
var (token, avatarId) = await TryAuthenticateAsync(client, baseUrl, username, password);
if (!string.IsNullOrWhiteSpace(token))
{
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    if (avatarId.HasValue)
        client.DefaultRequestHeaders.Add("X-Avatar-Id", avatarId.Value.ToString());
}

using var swaggerDoc = JsonDocument.Parse(await client.GetStringAsync($"{baseUrl}/swagger/v1/swagger.json"));
var endpoints = ReadEndpoints(swaggerDoc.RootElement).ToList();
var failures = new List<string>();
var orderedEndpoints = endpoints
    .OrderBy(e => IsDestructiveOnetEndpoint(e.Path) ? 1 : 0)
    .ThenBy(e => e.Path, StringComparer.OrdinalIgnoreCase)
    .ThenBy(e => e.Method, StringComparer.OrdinalIgnoreCase)
    .ToList();

Console.WriteLine($"OASIS WEB4 endpoint count: {orderedEndpoints.Count}");

foreach (var endpoint in orderedEndpoints)
{
    if (!await IsApiHealthyAsync(client, baseUrl))
    {
        failures.Add($"{endpoint.Method} {endpoint.Path} => skipped: API became unhealthy before request dispatch.");
        break;
    }

    var url = $"{baseUrl}{ResolvePath(endpoint.Path)}";
    var request = new HttpRequestMessage(new HttpMethod(endpoint.Method), url);
    if (endpoint.Method is "POST" or "PUT" or "PATCH")
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

    try
    {
        using var response = await client.SendAsync(request);
        var code = (int)response.StatusCode;
        Console.WriteLine($"{endpoint.Method} {endpoint.Path} => {code}");
        if (code >= 500)
        {
            var body = await response.Content.ReadAsStringAsync();
            failures.Add($"{endpoint.Method} {endpoint.Path} => {code}. Body: {body}");
        }
    }
    catch (Exception ex)
    {
        failures.Add($"{endpoint.Method} {endpoint.Path} => exception: {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Console.WriteLine("Failures:");
    foreach (var failure in failures)
        Console.WriteLine(failure);
    Environment.ExitCode = 1;
}
else
{
    Console.WriteLine("All OASIS WEB4 endpoints returned non-5xx responses.");
}

static IEnumerable<(string Method, string Path)> ReadEndpoints(JsonElement root)
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

static string ResolvePath(string path)
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

static async Task<(string? Token, Guid? AvatarId)> TryAuthenticateAsync(HttpClient client, string baseUrl, string username, string password)
{
    var payload = JsonSerializer.Serialize(new { username, password });
    using var response = await client.PostAsync($"{baseUrl}/api/avatar/authenticate", new StringContent(payload, Encoding.UTF8, "application/json"));
    var body = await response.Content.ReadAsStringAsync();
    
    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Authentication failed with status {response.StatusCode}: {body.Substring(0, Math.Min(body.Length, 500))}");
        return (null, null);
    }
    
    if (string.IsNullOrWhiteSpace(body))
        return (null, null);

    try
    {
        using var doc = JsonDocument.Parse(body);
        var token = FindStringRecursive(doc.RootElement, "jwtToken") ?? FindStringRecursive(doc.RootElement, "token");
        
        // Try to find avatar ID in various locations - WEB4 returns IAvatar in Result.Result
        var avatarIdStr = FindStringRecursive(doc.RootElement, "avatarId") 
            ?? FindStringRecursive(doc.RootElement, "id");
        
        Guid? avatarId = null;
        if (!string.IsNullOrWhiteSpace(avatarIdStr) && Guid.TryParse(avatarIdStr, out var parsedId))
            avatarId = parsedId;
        
        // If we have a token but no avatar ID, try to extract from JWT
        if (!string.IsNullOrWhiteSpace(token) && !avatarId.HasValue)
        {
            avatarId = ExtractAvatarIdFromJwt(token);
        }
        
        return (token, avatarId);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error parsing authentication response: {ex.Message}");
        return (null, null);
    }
}

static Guid? ExtractAvatarIdFromJwt(string token)
{
    try
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
            return null;

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var bytes = Convert.FromBase64String(payload);
        using var doc = JsonDocument.Parse(bytes);
        
        // Try common JWT claim names for avatar ID
        if (doc.RootElement.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
        {
            if (Guid.TryParse(idProp.GetString(), out var id))
                return id;
        }
        
        if (doc.RootElement.TryGetProperty("avatarId", out var avatarIdProp) && avatarIdProp.ValueKind == JsonValueKind.String)
        {
            if (Guid.TryParse(avatarIdProp.GetString(), out var id))
                return id;
        }
        
        if (doc.RootElement.TryGetProperty("sub", out var subProp) && subProp.ValueKind == JsonValueKind.String)
        {
            if (Guid.TryParse(subProp.GetString(), out var id))
                return id;
        }
    }
    catch
    {
        // Ignore JWT parsing errors
    }
    
    return null;
}

static string? FindStringRecursive(JsonElement element, string propertyName)
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

static bool IsDestructiveOnetEndpoint(string path)
{
    return path.Contains("/api/v1/onet/network/start", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/api/v1/onet/network/stop", StringComparison.OrdinalIgnoreCase);
}

static async Task<bool> IsApiHealthyAsync(HttpClient client, string baseUrl)
{
    try
    {
        using var response = await client.GetAsync($"{baseUrl}/swagger/v1/swagger.json");
        return response.IsSuccessStatusCode;
    }
    catch
    {
        return false;
    }
}



