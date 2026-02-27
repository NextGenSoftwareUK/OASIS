using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;

var baseUrl = (Environment.GetEnvironmentVariable("OASIS_WEBAPI_BASE_URL") ?? "http://localhost:5555").TrimEnd('/');
// Single place for test user: set env OASIS_WEBAPI_USERNAME / OASIS_WEBAPI_PASSWORD or change defaults below
HarnessConfig.Username = Environment.GetEnvironmentVariable("OASIS_WEBAPI_USERNAME") ?? "dellams";
HarnessConfig.Password = Environment.GetEnvironmentVariable("OASIS_WEBAPI_PASSWORD") ?? "test!";

using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
var (token, avatarId) = await TryAuthenticateAsync(client, baseUrl, HarnessConfig.Username, HarnessConfig.Password);
if (!string.IsNullOrWhiteSpace(token))
{
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    if (avatarId.HasValue)
    {
        client.DefaultRequestHeaders.Add("X-Avatar-Id", avatarId.Value.ToString());
        HarnessConfig.AvatarId = avatarId;
    }
}

using var swaggerDoc = JsonDocument.Parse(await client.GetStringAsync($"{baseUrl}/swagger/v1/swagger.json"));
var swaggerRoot = swaggerDoc.RootElement;
var endpoints = ReadEndpoints(swaggerRoot).ToList();
var failures = new List<string>();
var failures400 = new List<string>();
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

    try
    {
        var (url, content) = BuildRequest(swaggerRoot, endpoint, avatarId, baseUrl);
        var request = new HttpRequestMessage(new HttpMethod(endpoint.Method), url);
        if (content != null)
            request.Content = content;

        using var response = await client.SendAsync(request);
        var code = (int)response.StatusCode;
        Console.WriteLine($"{endpoint.Method} {endpoint.Path} => {code}");
        Console.Out.Flush();
        
        if (code >= 500)
        {
            var body = await response.Content.ReadAsStringAsync();
            var errorPreview = body.Length > 200 ? body.Substring(0, 200) + "..." : body;
            failures.Add($"{endpoint.Method} {endpoint.Path} => {code}. Body: {errorPreview}");
            if (failures.Count <= 10)
                Console.WriteLine($"  Error details: {errorPreview}");
        }
        else if (code == 400)
        {
            var body = await response.Content.ReadAsStringAsync();
            var errorPreview = body.Length > 300 ? body.Substring(0, 300) + "..." : body;
            failures400.Add($"{endpoint.Method} {endpoint.Path} => 400. {errorPreview}");
            if (failures.Count + failures400.Count < 20)
                Console.WriteLine($"  {endpoint.Method} {endpoint.Path} => {code}: {errorPreview}");
        }
    }
    catch (Exception ex)
    {
        failures.Add($"{endpoint.Method} {endpoint.Path} => exception: {ex.Message}");
        Console.WriteLine($"  Exception: {ex.Message}");
    }
}

var reportDir = Path.Combine(AppContext.BaseDirectory, "Test Results");
Directory.CreateDirectory(reportDir);
var reportPath = Path.Combine(reportDir, "test_failures_web4.txt");
using (var report = new StreamWriter(reportPath, append: false))
{
    report.WriteLine($"WEB4 Test Run {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z");
    report.WriteLine($"500s: {failures.Count}");
    foreach (var f in failures)
        report.WriteLine(f);
    report.WriteLine();
    report.WriteLine($"400s: {failures400.Count}");
    foreach (var f in failures400)
        report.WriteLine(f);
}
Console.WriteLine($"\nFull failure list written to: {reportPath}");

if (failures.Count > 0)
{
    Console.WriteLine($"\nFailures 5xx ({failures.Count}):");
    foreach (var failure in failures)
        Console.WriteLine(failure);
    Environment.ExitCode = 1;
}
else
{
    Console.WriteLine("\nAll OASIS WEB4 endpoints returned non-5xx responses.");
}
if (failures400.Count > 0)
    Console.WriteLine($"\n400s ({failures400.Count}) - see {reportPath}");

static (string Url, HttpContent? Content) BuildRequest(JsonElement swaggerRoot, (string Method, string Path) endpoint, Guid? avatarId, string baseUrl)
{
    var path = ResolvePath(endpoint.Path, avatarId);
    var url = $"{baseUrl}{path}";
    HttpContent? content = null;

    if (endpoint.Method is "POST" or "PUT" or "PATCH")
    {
        var pathLower = endpoint.Path.ToLowerInvariant();
        if (pathLower.Contains("sessions/logout"))
            content = new StringContent(JsonSerializer.Serialize(new List<string>()), Encoding.UTF8, "application/json");
        else if (pathLower.Contains("chat") && pathLower.Contains("send-message"))
            content = new StringContent(JsonSerializer.Serialize("Test message"), Encoding.UTF8, "application/json");
        else if (pathLower.Contains("start-new-chat-session"))
            content = new StringContent(JsonSerializer.Serialize(new List<Guid> { avatarId ?? Guid.NewGuid() }), Encoding.UTF8, "application/json");
        else if (pathLower.Contains("hyperdrive") && pathLower.Contains("mode") && endpoint.Method == "PUT")
            content = new StringContent(JsonSerializer.Serialize("Legacy"), Encoding.UTF8, "application/json");
        else if (pathLower.Contains("hyperdrive") && pathLower.Contains("failover/preventive"))
            content = new StringContent("[0]", Encoding.UTF8, "application/json"); // List<ProviderType>, 0 = Default
        else
        {
            var requestBody = GenerateRequestBody(swaggerRoot, endpoint);
            if (requestBody != null)
                content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        }
    }
    else if (endpoint.Method == "GET")
    {
        var queryParams = GenerateQueryParameters(swaggerRoot, endpoint, avatarId);
        if (queryParams.Any())
            url += "?" + string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
    }

    return (url, content);
}

static Dictionary<string, object>? GenerateRequestBody(JsonElement swaggerRoot, (string Method, string Path) endpoint)
{
    try
    {
        if (!swaggerRoot.TryGetProperty("paths", out var paths))
            return null;

        if (!paths.TryGetProperty(endpoint.Path, out var pathObj))
            return null;

        if (!pathObj.TryGetProperty(endpoint.Method.ToLowerInvariant(), out var methodObj))
            return null;

        if (!methodObj.TryGetProperty("requestBody", out var requestBody))
            return null;

        if (!requestBody.TryGetProperty("content", out var content))
            return null;

        if (!content.TryGetProperty("application/json", out var jsonContent))
            return null;

        if (!jsonContent.TryGetProperty("schema", out var schema))
            return null;

        JsonElement actualSchema = schema;
        if (schema.TryGetProperty("$ref", out var refProp))
        {
            var refPath = refProp.GetString()?.Replace("#/", "").Replace("/", ".");
            if (refPath != null && swaggerRoot.TryGetProperty("components", out var components))
            {
                var parts = refPath.Split('.');
                JsonElement current = components;
                foreach (var part in parts)
                {
                    if (current.TryGetProperty(part, out var next))
                        current = next;
                    else
                        return GenerateDefaultBody(endpoint.Path);
                }
                actualSchema = current;
            }
        }

        return GenerateObjectFromSchema(actualSchema, swaggerRoot);
    }
    catch
    {
        return GenerateDefaultBody(endpoint.Path);
    }
}

static Dictionary<string, object> GenerateObjectFromSchema(JsonElement schema, JsonElement swaggerRoot)
{
    var result = new Dictionary<string, object>();

    if (!schema.TryGetProperty("properties", out var properties))
        return result;

    // Get required fields
    var requiredFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (schema.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
    {
        foreach (var req in required.EnumerateArray())
        {
            if (req.ValueKind == JsonValueKind.String)
                requiredFields.Add(req.GetString() ?? "");
        }
    }

    foreach (var prop in properties.EnumerateObject())
    {
        var propName = prop.Name;
        var propSchema = prop.Value;

        if (propSchema.TryGetProperty("readOnly", out var readOnly) && readOnly.GetBoolean())
            continue;

        // Always include required fields, and include optional fields if they have good defaults
        var isRequired = requiredFields.Contains(propName);
        var value = GenerateValueFromSchema(propSchema, swaggerRoot, propName);
        
        if (value != null && (isRequired || ShouldIncludeOptionalProperty(propName, propSchema, swaggerRoot)))
            result[propName] = value;
    }

    if (result.TryGetValue("Password", out var pwd))
        result["ConfirmPassword"] = pwd;
    else if (result.TryGetValue("password", out var pwd2))
        result["confirmPassword"] = pwd2;
    if (result.TryGetValue("NewPassword", out var newPwd))
        result["ConfirmNewPassword"] = newPwd;
    else if (result.TryGetValue("newPassword", out var newPwd2))
        result["confirmNewPassword"] = newPwd2;

    return result;
}

static bool ShouldIncludeOptionalProperty(string propertyName, JsonElement propSchema, JsonElement swaggerRoot)
{
    // Always include properties that have non-null defaults
    if (propSchema.TryGetProperty("default", out var defaultVal))
    {
        if (defaultVal.ValueKind != JsonValueKind.Null)
            return true;
    }

    // Include properties that are commonly needed
    var lowerName = propertyName.ToLowerInvariant();
    if (lowerName.Contains("name") || lowerName.Contains("title") || 
        lowerName.Contains("description") || lowerName.Contains("type") ||
        lowerName.Contains("id") || lowerName.Contains("guid"))
        return true;

    // Include if it's a simple type (not complex object/array)
    if (propSchema.TryGetProperty("type", out var type))
    {
        var typeStr = type.GetString();
        if (typeStr == "string" || typeStr == "integer" || typeStr == "number" || typeStr == "boolean")
            return true;
    }

    return false;
}

static object? GenerateValueFromSchema(JsonElement schema, JsonElement swaggerRoot, string propertyName)
{
    if (schema.TryGetProperty("$ref", out var refProp))
    {
        var refPath = refProp.GetString()?.Replace("#/", "").Replace("/", ".");
        if (refPath != null && swaggerRoot.TryGetProperty("components", out var components))
        {
            var parts = refPath.Split('.');
            JsonElement current = components;
            foreach (var part in parts)
            {
                if (current.TryGetProperty(part, out var next))
                    current = next;
                else
                    return GetDefaultValue(propertyName);
            }
            schema = current;
        }
    }

    if (schema.TryGetProperty("type", out var typeProp))
    {
        var type = typeProp.GetString();
        return type switch
        {
            "string" => GetStringValue(propertyName, schema),
            "integer" => GetIntegerValue(propertyName, schema),
            "number" => GetNumberValue(propertyName, schema),
            "boolean" => true,
            "array" => GetArrayValue(schema, swaggerRoot, propertyName),
            "object" => GenerateObjectFromSchema(schema, swaggerRoot),
            _ => GetDefaultValue(propertyName)
        };
    }

    return GetDefaultValue(propertyName);
}

static string GetStringValue(string propertyName, JsonElement schema)
{
    var lowerName = propertyName.ToLowerInvariant();
    
    if (schema.TryGetProperty("enum", out var enumProp) && enumProp.ValueKind == JsonValueKind.Array && enumProp.GetArrayLength() > 0)
    {
        return enumProp[0].GetString() ?? "test";
    }

    if (schema.TryGetProperty("format", out var format))
    {
        return format.GetString() switch
        {
            "uuid" or "guid" => Guid.NewGuid().ToString(),
            "date-time" => DateTime.UtcNow.ToString("O"),
            "email" => "test@example.com",
            "uri" => "https://example.com/test",
            _ => "test"
        };
    }

    if (lowerName.Contains("name") || lowerName.Contains("title"))
        return "Test " + propertyName;
    if (lowerName.Contains("description"))
        return $"Test description for {propertyName}";
    if (lowerName.Contains("email"))
        return "test@example.com";
    if (lowerName.Contains("url") || lowerName.Contains("uri"))
        return "https://example.com/test";
    if (lowerName.Contains("path"))
        return "/test/path";
    if (lowerName.Contains("token"))
        return "test-token-123";
    if (lowerName == "avatartype")
        return "User";
    if (lowerName.Contains("username"))
        return HarnessConfig.Username;
    if (lowerName.Contains("password"))
        return HarnessConfig.Password;
    if (lowerName.Contains("search") || lowerName.Contains("query") || lowerName.Contains("term"))
        return "test";
    if (lowerName.Contains("type"))
        return "Default";
    if (lowerName.Contains("status"))
        return "Active";
    if (lowerName.Contains("category"))
        return "TestCategory";
    if (lowerName.Contains("firstname"))
        return "Test";
    if (lowerName.Contains("lastname"))
        return "User";
    if (lowerName.Contains("title") && !lowerName.Contains("name"))
        return "Mr";
    if (lowerName.Contains("holontype") || lowerName.Contains("holonType"))
        return "Default";
    if (lowerName.Contains("provider") && lowerName.Contains("type"))
        return "Default";
    if (lowerName.Contains("version"))
        return "1.0.0";

    return "test";
}

static int GetIntegerValue(string propertyName, JsonElement schema)
{
    if (schema.TryGetProperty("minimum", out var min))
        return Math.Max(min.GetInt32(), 0);
    if (schema.TryGetProperty("default", out var def) && def.ValueKind == JsonValueKind.Number)
        return def.GetInt32();
    
    var lowerName = propertyName.ToLowerInvariant();
    if (lowerName.Contains("healthcheckintervalms"))
        return 100; // API requires 1-1000
    if (lowerName.Contains("version") || lowerName.Contains("level"))
        return 1;
    if (lowerName.Contains("count") || lowerName.Contains("limit") || lowerName.Contains("size"))
        return 10;
    if (lowerName.Contains("depth") || lowerName.Contains("maxchilddepth"))
        return 0;
    
    return 1;
}

static double GetNumberValue(string propertyName, JsonElement schema)
{
    if (schema.TryGetProperty("minimum", out var min))
        return Math.Max(min.GetDouble(), 0.0);
    if (schema.TryGetProperty("default", out var def) && def.ValueKind == JsonValueKind.Number)
        return def.GetDouble();
    
    var lowerName = propertyName.ToLowerInvariant();
    if (lowerName.Contains("mass") || lowerName.Contains("radius") || lowerName.Contains("temperature"))
        return 1.0;
    if (lowerName.Contains("volume") || lowerName.Contains("area"))
        return 1.0;
    
    return 1.0;
}

static object GetArrayValue(JsonElement schema, JsonElement swaggerRoot, string propertyName)
{
    if (schema.TryGetProperty("items", out var items))
    {
        var itemValue = GenerateValueFromSchema(items, swaggerRoot, "item");
        return itemValue != null ? new[] { itemValue } : Array.Empty<object>();
    }
    return Array.Empty<object>();
}

static object? GetDefaultValue(string propertyName)
{
    var lowerName = propertyName.ToLowerInvariant();
    
    if (lowerName.Contains("id") && (lowerName.Contains("avatar") || lowerName.Contains("user")))
        return Guid.NewGuid().ToString();
    if (lowerName.Contains("id"))
        return Guid.NewGuid().ToString();
    if (lowerName.Contains("name"))
        return "Test " + propertyName;
    if (lowerName.Contains("description"))
        return "Test description";
    if (lowerName.Contains("email"))
        return "test@example.com";
    if (lowerName.Contains("url") || lowerName.Contains("uri"))
        return "https://example.com/test";
    
    return "test";
}

static Dictionary<string, object> GenerateDefaultBody(string path)
{
    var body = new Dictionary<string, object>();
    var lowerPath = path.ToLowerInvariant();

    if (lowerPath.Contains("avatar") && lowerPath.Contains("register"))
    {
        var pwd = "TestPass123!";
        body["username"] = HarnessConfig.Username + "_test_" + Guid.NewGuid().ToString().Substring(0, 8);
        body["email"] = $"test{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";
        body["password"] = pwd;
        body["confirmPassword"] = pwd;
        body["firstName"] = "Test";
        body["lastName"] = "User";
        body["avatarType"] = "User";
        body["acceptTerms"] = true;
    }
    else if (lowerPath.Contains("avatar") && lowerPath.Contains("authenticate"))
    {
        body["username"] = HarnessConfig.Username;
        body["password"] = HarnessConfig.Password;
    }
    else if (lowerPath.Contains("forgot-password"))
    {
        body["Email"] = "test@example.com";
    }
    else if (lowerPath.Contains("reset-password"))
    {
        var pwd = "TestPass123!";
        body["token"] = "test-reset-token";
        body["oldPassword"] = pwd;
        body["newPassword"] = pwd;
        body["confirmNewPassword"] = pwd;
    }
    else if (lowerPath.Contains("validate-reset-token"))
    {
        body["Token"] = "test-reset-token";
    }
    else if (lowerPath.Contains("verify-email"))
    {
        body["Token"] = "test-verify-token";
    }
    else if (lowerPath.Contains("avatar") && lowerPath.Contains("sessions") && !lowerPath.Contains("logout"))
    {
        body["serviceName"] = "TestHarness";
        body["serviceType"] = "website";
        body["deviceType"] = "desktop";
        body["deviceName"] = "TestHarness";
        body["location"] = "Test";
        body["ipAddress"] = "127.0.0.1";
    }
    else if (lowerPath.Contains("admin") && (lowerPath.Contains("oland") || lowerPath.Contains("olandunit")))
    {
        body["Price"] = 1m;
        body["Discount"] = 0m;
        body["OlandsCount"] = 1;
        body["TopSize"] = 1m;
        body["RightSize"] = 1m;
        body["UnitOfMeasure"] = "m";
        body["IsRemoved"] = false;
    }
    else if (lowerPath.Contains("inventory"))
    {
        body["Name"] = "Test Item";
        body["Description"] = "Test inventory item";
    }
    else if (lowerPath.Contains("send-to-avatar") || lowerPath.Contains("send-to-clan"))
    {
        body["Target"] = HarnessConfig.AvatarId?.ToString() ?? Guid.Empty.ToString();
        body["ItemName"] = "TestItem";
    }
    else if (lowerPath.Contains("subscription") && lowerPath.Contains("checkout"))
    {
        body["PlanId"] = "silver";
    }
    else if (lowerPath.Contains("nft"))
    {
        body["Name"] = "Test NFT";
        body["Description"] = "Test NFT description";
        body["ImageUrl"] = "https://example.com/nft.png";
        body["ThumbnailUrl"] = "https://example.com/nft-thumb.png";
    }
    else
    {
        body["Name"] = "Test";
        body["Description"] = "Test description";
    }

    return body;
}

static Dictionary<string, string> GenerateQueryParameters(JsonElement swaggerRoot, (string Method, string Path) endpoint, Guid? avatarId)
{
    var queryParams = new Dictionary<string, string>();

    try
    {
        if (!swaggerRoot.TryGetProperty("paths", out var paths))
            return queryParams;

        if (!paths.TryGetProperty(endpoint.Path, out var pathObj))
            return queryParams;

        if (!pathObj.TryGetProperty(endpoint.Method.ToLowerInvariant(), out var methodObj))
            return queryParams;

        if (!methodObj.TryGetProperty("parameters", out var parameters))
            return queryParams;

        foreach (var param in parameters.EnumerateArray())
        {
            if (param.TryGetProperty("in", out var inProp) && inProp.GetString() == "query")
            {
                if (param.TryGetProperty("name", out var nameProp))
                {
                    var name = nameProp.GetString();
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    var value = GetQueryParameterValue(param, name, avatarId, endpoint.Path);
                    if (value != null)
                        queryParams[name] = value;
                }
            }
        }
    }
    catch
    {
    }

    return queryParams;
}

static string? GetQueryParameterValue(JsonElement param, string name, Guid? avatarId, string path)
{
    var lowerName = name.ToLowerInvariant();
    var lowerPath = path?.ToLowerInvariant() ?? "";

    // Use schema enum first so we always send valid enum values (reduces 400s)
    if (param.TryGetProperty("schema", out var schema) && schema.TryGetProperty("enum", out var enumProp) && enumProp.ValueKind == JsonValueKind.Array && enumProp.GetArrayLength() > 0)
    {
        return enumProp[0].GetString();
    }

    if (param.TryGetProperty("required", out var required) && required.GetBoolean())
    {
        if (lowerName.Contains("search") || lowerName.Contains("query") || lowerName.Contains("term"))
            return "test";
        if (lowerName.Contains("itemname"))
            return "Test Item";
        // Map search-locations expects LocationType: Hub, Quest, Social, Shop, Arena, Dungeon, Custom (not Default)
        if ((lowerName == "type" || lowerName.Contains("type")) && lowerPath.Contains("search-locations"))
            return "Hub";
        if (lowerName.Contains("type") || lowerName.Contains("holontype") || lowerName.Contains("holonType"))
            return "Default";
        if (lowerName.Contains("status"))
            return "Active";
        if (lowerName.Contains("category"))
            return "TestCategory";
        if (lowerName.Contains("path"))
            return "/test/path";
        if (lowerName.Contains("publishedfilepath"))
            return "/test/published.json";
        if (lowerName.Contains("email"))
            return "test@example.com";
        if (lowerName.Contains("username"))
            return HarnessConfig.Username;
    }

    // Optional params that commonly need valid enum values to avoid 400
    if (lowerName.Contains("provider") && lowerName.Contains("type"))
        return "Default";
    if (lowerName.Contains("holontype") || lowerName.Contains("holonType") || (lowerName == "type" && !lowerName.Contains("search")))
        return "Default";

    if (param.TryGetProperty("schema", out var schema2))
    {
        if (schema2.TryGetProperty("type", out var type))
        {
            return type.GetString() switch
            {
                "string" => lowerName.Contains("id") ? (avatarId?.ToString() ?? Guid.NewGuid().ToString()) : "test",
                "integer" => "1",
                "number" => "1.0",
                "boolean" => "true",
                _ => "test"
            };
        }
    }

    return null;
}

static string ResolvePath(string path, Guid? avatarId)
{
    return Regex.Replace(path, "\\{([^}]+)\\}", m =>
    {
        var key = m.Groups[1].Value.ToLowerInvariant();
        if (key.Contains("avatarid") && avatarId.HasValue)
            return avatarId.Value.ToString();
        // Resolve known path params before generic "id" (providerType contains "id")
        if (key.Contains("version"))
            return "1";
        if (key.Contains("holontype") || key == "type")
            return "Default";
        if (key.Contains("status"))
            return "Active";
        if (key.Contains("category"))
            return "TestCategory";
        if (key.Contains("includeusernames"))
            return "true";
        if (key.Contains("includeids"))
            return "true";
        if (key.Contains("username"))
            return HarnessConfig.Username;
        if (key.Contains("email"))
            return "test@example.com";
        if (key.Contains("search") || key.Contains("query") || key.Contains("term"))
            return "test";
        if (key.Contains("itemname"))
            return "TestItem";
        if (key.Contains("providertype"))
            return "Default";
        if (key.Contains("setglobally"))
            return "false";
        if (key.Contains("autoreplicationmode"))
            return "Auto";
        if (key.Contains("autofailovermode"))
            return "Auto";
        if (key.Contains("autoloadbalancemode"))
            return "Auto";
        if (key.Contains("waitforautoreplicationresult"))
            return "false";
        if (key.Contains("showdetailedsettings"))
            return "false";
        if (key.Contains("softdelete"))
            return "false";
        if (key.Contains("loadchildren"))
            return "true";
        if (key.Contains("recursive"))
            return "false";
        if (key.Contains("maxchilddepth"))
            return "0";
        if (key == "x" || key == "y")
            return "1";
        if (key.Contains("continueonerror"))
            return "false";
        if (key.Contains("removeduplicates"))
            return "false";
        if (key.Contains("includenames"))
            return "true";
        if (key.Contains("jwttoken"))
            return "test-token";
        if (key.Contains("telosaccountname"))
            return "testaccount";
        if (key.Contains("eosioaccountname"))
            return "testaccount";
        if (key.Contains("holochainagentid"))
            return "test-agent-id";
        if (key.Contains("karmatype"))
            return path.ToLowerInvariant().Contains("get-negative-karma-weighting") ? "None" : "None";
        if (key.Contains("weighting"))
            return "1.0";
        if (key.Contains("model"))
            return "Default";
        if (key.Contains("value"))
            return "1";
        // Only treat as GUID when key is "id" or ends with "id" (e.g. sessionId, giftId) - not providerType
        if (key == "id" || key.EndsWith("id"))
            return Guid.NewGuid().ToString();
        return "test";
    });
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
        
        var avatarIdStr = FindStringRecursive(doc.RootElement, "avatarId") 
            ?? FindStringRecursive(doc.RootElement, "id");
        
        Guid? avatarId = null;
        if (!string.IsNullOrWhiteSpace(avatarIdStr) && Guid.TryParse(avatarIdStr, out var parsedId))
            avatarId = parsedId;
        
        if (!string.IsNullOrWhiteSpace(token) && !avatarId.HasValue)
            avatarId = ExtractAvatarIdFromJwt(token);
        
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
    catch { }
    
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

static class HarnessConfig
{
    public static string Username { get; set; } = "";
    public static string Password { get; set; } = "";
    public static Guid? AvatarId { get; set; }
}
