using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>
/// Compiles api/data/query-cql/sql and /ask requests before usage admission. Admission, tariffs and provider
/// attempt budgets are bound to the request path, so a text query takes the path of the typed query it compiles to.
/// Routing has already chosen the text action, which then runs the compiled request.
/// Every outcome, including errors, is returned by that action so authorisation and filters apply as usual.
/// </summary>
public sealed class CqlTextMiddleware
{
    public const string Web6Client = "CqlAskWeb6";
    public const string SqlPath = CosmicQueryHttpRoutes.BasePath + "/sql";
    public const string AskPath = CosmicQueryHttpRoutes.BasePath + "/ask";
    internal const string OutcomeKey = "OASIS.CQL.Text.Outcome";
    public const int MaximumSqlBytes = 16 * 1024;
    public const int MaximumAskBytes = 4 * 1024;
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate next;
    public CqlTextMiddleware(RequestDelegate next) => this.next = next;

    public async Task InvokeAsync(HttpContext http, CqlTextCompiler compiler, IHttpClientFactory clients)
    {
        var path = http.Request.Path.Value?.TrimEnd('/');
        var ask = string.Equals(path, AskPath, StringComparison.OrdinalIgnoreCase);
        // Unauthenticated callers are left to [Authorize]; nothing is compiled for them.
        if ((ask || string.Equals(path, SqlPath, StringComparison.OrdinalIgnoreCase))
            && HttpMethods.IsPost(http.Request.Method) && http.Items["Avatar"] is IAvatar avatar)
        {
            var outcome = await PrepareAsync(http, avatar, ask, compiler, clients);
            http.Items[OutcomeKey] = outcome;
            if (outcome.Query != null && outcome.Execute)
                http.Request.Path = "/api/" + outcome.Query.Path;
        }
        await next(http);
    }

    private static async Task<CqlTextOutcome> PrepareAsync(HttpContext http, IAvatar avatar, bool ask,
        CqlTextCompiler compiler, IHttpClientFactory clients)
    {
        var maximum = ask ? MaximumAskBytes : MaximumSqlBytes;
        var body = await ReadBodyAsync(http, maximum);
        if (body == null) return CqlTextOutcome.Fail(413, "CQL_REQUEST_TOO_LARGE", $"The request may contain up to {maximum} bytes.");

        string text = null, parameters = null;
        var execute = true;
        try
        {
            using var document = JsonDocument.Parse(body);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Name.Equals(ask ? "question" : "query", StringComparison.OrdinalIgnoreCase)) text = property.Value.GetString();
                else if (!ask && property.Name.Equals("parameters", StringComparison.OrdinalIgnoreCase)) parameters = property.Value.GetRawText();
                else if (ask && property.Name.Equals("execute", StringComparison.OrdinalIgnoreCase)) execute = property.Value.GetBoolean();
            }
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return CqlTextOutcome.Fail(400, "CQL_REQUEST_INVALID", "Send a JSON object.");
        }
        text = text?.Trim();
        if (string.IsNullOrEmpty(text) || (ask && text.Length > 1000))
            return CqlTextOutcome.Fail(400, "CQL_REQUEST_INVALID",
                ask ? "Enter a question of up to 1,000 characters." : "Enter a query.");

        var catalogue = http.RequestServices.GetService<CqlHostedSources>();
        var sources = catalogue == null ? null : JsonSerializer.Serialize(catalogue.Describe(), Web);

        if (!ask)
        {
            try { return new CqlTextOutcome { Query = compiler.Compile(text, parameters, sources ?? "[]"), Sql = text, Execute = true }; }
            catch (CqlTextException error) { return CqlTextOutcome.Fail(400, "CQL_SQL_INVALID", error.Message); }
        }

        var prompt = sources == null ? null : compiler.SystemPrompt(sources);
        if (prompt == null)
            return CqlTextOutcome.Fail(503, "CQL_SOURCE_CATALOGUE_UNAVAILABLE", "No query source is ready on this host.");

        var drafted = await DraftAsync(http, avatar, prompt, text, clients);
        if (drafted.Failure != null) return drafted.Failure;
        if (drafted.Sql == "UNSUPPORTED")
            return CqlTextOutcome.Fail(422, "CQL_ASK_UNSUPPORTED", "This question cannot be expressed as a CQL query.");
        try
        {
            var query = compiler.Compile(drafted.Sql, null, sources);
            if (query.Body.TryGetProperty("virtualMappingId", out _))
                return CqlTextOutcome.Fail(422, "CQL_ASK_UNSUPPORTED", "The draft used an unselected virtual mapping. Nothing was run.");
            return new CqlTextOutcome { Query = query, Sql = drafted.Sql, Execute = execute };
        }
        catch (CqlTextException error)
        {
            return CqlTextOutcome.Fail(422, "CQL_ASK_UNSUPPORTED", "The drafted statement is not valid CQL: " + error.Message);
        }
    }

    public static string Web6BaseUrl(string host)
        => Environment.GetEnvironmentVariable("OASIS_WEB6_BASE_URL") is { Length: > 0 } configured
            ? configured.TrimEnd('/')
            : host?.StartsWith("dev.", StringComparison.OrdinalIgnoreCase) == true
                ? "https://dev.api.web6.oasisomniverse.one"
                : "https://api.web6.oasisomniverse.one";

    private static async Task<(string Sql, CqlTextOutcome Failure)> DraftAsync(HttpContext http, IAvatar avatar,
        string prompt, string question, IHttpClientFactory clients)
    {
        static (string, CqlTextOutcome) Failed(string message) => (null, CqlTextOutcome.Fail(502, "CQL_ASK_DRAFT_FAILED", message));

        var authorization = http.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authorization))
            return (null, CqlTextOutcome.Fail(401, "CQL_AUTHENTICATION_REQUIRED", "Send a WEB4 bearer token."));

        var payload = new System.Collections.Generic.Dictionary<string, object>
        {
            ["messages"] = new object[] { new { role = "system", content = prompt }, new { role = "user", content = question } },
            ["tools"] = Array.Empty<object>(), ["maxTokens"] = 350, ["temperature"] = 0,
            ["useFAHRN"] = false, ["useHolonicBraid"] = false, ["injectAvatarContext"] = false
        };
        // Sandbox demo accounts only use free models, as the sandbox page does.
        if (SandboxOptions.Enabled && avatar.Username?.StartsWith(SandboxOptions.Current.DemoAvatarUsernamePrefix, StringComparison.OrdinalIgnoreCase) == true)
            payload["freeOnly"] = true;

        using var request = new HttpRequestMessage(HttpMethod.Post, Web6BaseUrl(http.Request.Host.Host) + "/v1/complete")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        try
        {
            using var response = await clients.CreateClient(Web6Client).SendAsync(request, http.RequestAborted);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(http.RequestAborted));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return Failed($"WEB6 returned an invalid response (HTTP {(int)response.StatusCode}).");
            var isError = root.TryGetProperty("isError", out var flag) && flag.ValueKind == JsonValueKind.True;
            if (!response.IsSuccessStatusCode || isError)
            {
                var reason = root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String
                    ? " " + message.GetString() : "";
                return Failed($"WEB6 could not draft a query (HTTP {(int)response.StatusCode}).{reason}");
            }
            var result = root.TryGetProperty("result", out var inner) && inner.ValueKind == JsonValueKind.Object ? inner : root;
            if (!result.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(content.GetString()))
                return Failed("WEB6 returned no CQL statement.");
            return (content.GetString().Trim(), null);
        }
        catch (JsonException) { return Failed("WEB6 returned an invalid response."); }
        catch (HttpRequestException) { return Failed("WEB6 could not be reached."); }
        catch (OperationCanceledException) when (!http.RequestAborted.IsCancellationRequested) { return Failed("WEB6 did not answer in time."); }
    }

    /// <summary>Reads at most <paramref name="maximum"/> bytes and rewinds for model binding; null when larger.</summary>
    private static async Task<byte[]> ReadBodyAsync(HttpContext http, int maximum)
    {
        http.Request.EnableBuffering();
        var buffer = new byte[maximum + 1];
        var length = 0;
        int read;
        while (length < buffer.Length && (read = await http.Request.Body.ReadAsync(buffer.AsMemory(length), http.RequestAborted)) > 0)
            length += read;
        http.Request.Body.Position = 0;
        return length > maximum ? null : buffer.AsSpan(0, length).ToArray();
    }
}

internal sealed class CqlTextOutcome
{
    public CqlTextQuery Query { get; init; }
    public string Sql { get; init; }
    public bool Execute { get; init; }
    public int Status { get; init; }
    public string Code { get; init; }
    public string Message { get; init; }
    public static CqlTextOutcome Fail(int status, string code, string message) => new() { Status = status, Code = code, Message = message };
}
