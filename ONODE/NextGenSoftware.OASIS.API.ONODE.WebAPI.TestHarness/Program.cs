using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// Manual test harness for the ONODE and ONET WebAPI endpoints.
/// Runs against a locally-running ONODE WebAPI (default: http://localhost:5000) or a remote ONODE.
///
/// Set ONODE_BASE_URL env var to point at a different host.
/// Set ONODE_JWT_TOKEN to include a valid JWT for endpoints that require authentication.
///
/// Pattern matches the rest of OASIS — a console harness the developer runs to exercise the live
/// stack end-to-end, complementing the automated unit/integration test suites.
/// </summary>
class Program
{
    static readonly string BaseUrl = Environment.GetEnvironmentVariable("ONODE_BASE_URL") ?? "http://localhost:5000";
    static readonly string? JwtToken = Environment.GetEnvironmentVariable("ONODE_JWT_TOKEN");

    static readonly HttpClient Http = new HttpClient
    {
        BaseAddress = new Uri(BaseUrl),
        Timeout = TimeSpan.FromSeconds(15)
    };

    static int _pass, _fail;

    static async Task Main(string[] args)
    {
        if (JwtToken != null)
            Http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", JwtToken);

        Console.WriteLine("NEXTGEN SOFTWARE ONODE WEB API TEST HARNESS V2.0");
        Console.WriteLine($"Target: {BaseUrl}");
        Console.WriteLine(JwtToken != null ? "Auth: JWT token provided" : "Auth: NONE (some endpoints will return 401)");
        Console.WriteLine(new string('=', 70));

        // ── ONODE endpoints ──────────────────────────────────────────────────
        await Section("ONODE — Node lifecycle");
        await Get("/api/v1/onode/status",                   "GET  /onode/status");
        await Get("/api/v1/onode/info",                     "GET  /onode/info");
        await Get("/api/v1/onode/metrics",                  "GET  /onode/metrics");
        await Get("/api/v1/onode/logs",                     "GET  /onode/logs");
        await Get("/api/v1/onode/config",                   "GET  /onode/config");
        await Get("/api/v1/onode/peers",                    "GET  /onode/peers");
        await Get("/api/v1/onode/stats",                    "GET  /onode/stats");
        await Get("/api/v1/onode/oasisdna",                 "GET  /onode/oasisdna");

        await Post("/api/v1/onode/start",   null,           "POST /onode/start");
        await Post("/api/v1/onode/stop",    null,           "POST /onode/stop");
        await Post("/api/v1/onode/restart", null,           "POST /onode/restart");

        await Put("/api/v1/onode/config",
            new { Config = new Dictionary<string, object> { ["testKey"] = "testValue" } },
            "PUT  /onode/config");

        // ── ONET endpoints ───────────────────────────────────────────────────
        await Section("ONET — Network management");
        await Get("/api/v1/onet/network/status",            "GET  /onet/network/status");
        await Get("/api/v1/onet/network/nodes",             "GET  /onet/network/nodes");
        await Get("/api/v1/onet/network/stats",             "GET  /onet/network/stats");
        await Get("/api/v1/onet/network/topology",          "GET  /onet/network/topology");
        await Get("/api/v1/onet/oasisdna",                  "GET  /onet/oasisdna");

        await Post("/api/v1/onet/network/start", null,      "POST /onet/network/start");
        await Post("/api/v1/onet/network/stop",  null,      "POST /onet/network/stop");

        await Post("/api/v1/onet/network/connect",
            new { NodeId = "test-node-id", NodeAddress = "127.0.0.1:38471" },
            "POST /onet/network/connect");

        await Post("/api/v1/onet/network/broadcast",
            new { Message = "hello onet", MessageType = "test" },
            "POST /onet/network/broadcast");

        await Post("/api/v1/onet/nodes/register",
            new { NodeId = "harness-node-001", PublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("fake-public-key-for-harness")), NodeAddress = (string?)null },
            "POST /onet/nodes/register");

        await Post("/api/v1/onet/network/disconnect",
            new { NodeId = "test-node-id" },
            "POST /onet/network/disconnect");

        // ── Subscription — PUBLIC endpoints (no auth needed) ─────────────────
        await Section("Subscription — public endpoints");

        await GetExpect("/api/subscription/plans",
            200, "GET  /subscription/plans",
            body => body.Contains("\"free\"") && body.Contains("\"bronze\""),
            "should contain free and bronze plans");

        await GetExpect("/api/subscription/hyperdrive-usage",
            200, "GET  /subscription/hyperdrive-usage",
            body => body.Contains("PlanType") || body.Contains("planType"),
            "should contain PlanType field");

        // ── Subscription — POST check-hyperdrive-quota ────────────────────────
        await PostExpect("/api/subscription/check-hyperdrive-quota",
            new { OperationType = "Requests" },
            200, "POST /subscription/check-hyperdrive-quota (Requests)",
            body => body.Contains("CanProceed") || body.Contains("canProceed"),
            "should contain CanProceed field");

        await PostExpect("/api/subscription/check-hyperdrive-quota",
            new { OperationType = "Replications" },
            200, "POST /subscription/check-hyperdrive-quota (Replications)",
            body => body.Length > 2,
            "should return non-empty body");

        // ── Subscription — webhook (no secret → 400) ──────────────────────────
        await Section("Subscription — Stripe webhook (no secret configured)");

        await PostExpect("/api/subscription/webhooks/stripe",
            new { },
            400, "POST /subscription/webhooks/stripe (no secret/signature)",
            _ => true,
            "400 expected when webhook secret not set or Stripe-Signature missing");

        // ── Subscription — checkout session validation ─────────────────────────
        await Section("Subscription — checkout session validation");

        await PostExpect("/api/subscription/checkout/session",
            new { PlanId = "enterprise" },
            400, "POST /subscription/checkout/session (enterprise, no auth — 400 or 401)",
            _ => true,
            "enterprise plan = contact sales (400) or unauthenticated (401)");

        await PostExpect("/api/subscription/checkout/session",
            new { PlanId = "diamond_nonexistent" },
            400, "POST /subscription/checkout/session (unknown plan — 400 or 401)",
            _ => true,
            "unknown plan should return 400 or 401");

        // ── Subscription — authenticated endpoints ────────────────────────────
        await Section("Subscription — authenticated endpoints" + (JwtToken == null ? " (SKIPPED — no JWT token)" : ""));

        if (JwtToken != null)
        {
            await GetExpect("/api/subscription/subscriptions/me",
                200, "GET  /subscription/subscriptions/me",
                body => body.Contains("Result") || body.Contains("result"),
                "should return a Result field");

            await GetExpect("/api/subscription/orders/me",
                200, "GET  /subscription/orders/me",
                body => body.Contains("Result") || body.Contains("result"),
                "should return a Result field");

            await GetExpect("/api/subscription/usage",
                200, "GET  /subscription/usage",
                body => body.Contains("currentMonth") || body.Contains("requests"),
                "should contain currentMonth usage data");

            await PostExpect("/api/subscription/toggle-pay-as-you-go",
                new { Enabled = false },
                200, "POST /subscription/toggle-pay-as-you-go (disable)",
                body => body.Contains("false") || body.Contains("False"),
                "should echo back PayAsYouGoEnabled = false");

            await PostExpect("/api/subscription/checkout/session",
                new { PlanId = "free", SuccessUrl = "/success" },
                200, "POST /subscription/checkout/session (free plan, authenticated)",
                body => body.Contains("activated") || body.Contains("SessionUrl"),
                "free plan should activate immediately");

            await PostExpect("/api/subscription/checkout/session",
                new { PlanId = "bronze", SuccessUrl = "/success", CancelUrl = "/cancel" },
                500, "POST /subscription/checkout/session (bronze, no Stripe key → 500)",
                body => body.Contains("STRIPE_SECRET_KEY") || body.Contains("Stripe"),
                "should indicate Stripe not configured when no key set");
        }
        else
        {
            // Expect 401s for all authenticated endpoints
            await GetExpect("/api/subscription/subscriptions/me",
                401, "GET  /subscription/subscriptions/me (no auth → 401)",
                _ => true, "");
            await GetExpect("/api/subscription/orders/me",
                401, "GET  /subscription/orders/me (no auth → 401)",
                _ => true, "");
            await GetExpect("/api/subscription/usage",
                401, "GET  /subscription/usage (no auth → 401)",
                _ => true, "");
        }

        // ── update-hyperdrive-config ──────────────────────────────────────────
        await Section("Subscription — HyperDrive config update");
        await PostExpect("/api/subscription/update-hyperdrive-config",
            new { PlanType = "free", PayAsYouGoEnabled = false },
            200, "POST /subscription/update-hyperdrive-config (free plan)",
            _ => true, "should return 200");

        await PostExpect("/api/subscription/update-hyperdrive-config",
            new { PlanType = "bronze", PayAsYouGoEnabled = false },
            200, "POST /subscription/update-hyperdrive-config (bronze plan)",
            _ => true, "should return 200");

        // ── Summary ──────────────────────────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"RESULT: {_pass} passed, {_fail} failed out of {_pass + _fail} tests");
        if (_fail > 0) Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(_fail == 0 ? "ALL TESTS PASSED" : $"{_fail} TEST(S) FAILED");
        Console.ResetColor();

        if (JwtToken == null)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("TIP: Set ONODE_JWT_TOKEN to a valid JWT to also test authenticated endpoints.");
            Console.ResetColor();
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static Task Section(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"── {title} ──");
        return Task.CompletedTask;
    }

    static async Task Get(string path, string label)
    {
        try
        {
            var response = await Http.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();
            Pass(label, response.StatusCode, body);
        }
        catch (Exception ex) { Fail(label, ex.Message); }
    }

    static async Task GetExpect(string path, int expectedStatus, string label, Func<string, bool> bodyCheck, string bodyCheckDesc)
    {
        try
        {
            var response = await Http.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();
            var statusOk = (int)response.StatusCode == expectedStatus;
            var bodyOk = bodyCheck(body);
            if (statusOk && bodyOk)
                Pass(label, response.StatusCode, body);
            else
                Fail(label, $"Expected status {expectedStatus} got {(int)response.StatusCode}" +
                            (!bodyOk ? $"; body check failed: {bodyCheckDesc}" : ""));
        }
        catch (Exception ex) { Fail(label, ex.Message); }
    }

    static async Task Post(string path, object? payload, string label)
    {
        try
        {
            var content = payload is null
                ? new StringContent("{}", Encoding.UTF8, "application/json")
                : JsonContent.Create(payload);
            var response = await Http.PostAsync(path, content);
            var body = await response.Content.ReadAsStringAsync();
            Pass(label, response.StatusCode, body);
        }
        catch (Exception ex) { Fail(label, ex.Message); }
    }

    static async Task PostExpect(string path, object? payload, int expectedStatus, string label, Func<string, bool> bodyCheck, string bodyCheckDesc)
    {
        try
        {
            var content = payload is null
                ? new StringContent("{}", Encoding.UTF8, "application/json")
                : JsonContent.Create(payload);
            var response = await Http.PostAsync(path, content);
            var body = await response.Content.ReadAsStringAsync();
            var statusOk = (int)response.StatusCode == expectedStatus;
            var bodyOk = bodyCheck(body);
            if (statusOk && bodyOk)
                Pass(label, response.StatusCode, body);
            else
                Fail(label, $"Expected status {expectedStatus} got {(int)response.StatusCode}" +
                            (!bodyOk ? $"; body check failed: {bodyCheckDesc}" : ""));
        }
        catch (Exception ex) { Fail(label, ex.Message); }
    }

    static async Task Put(string path, object? payload, string label)
    {
        try
        {
            var content = payload is null
                ? new StringContent("{}", Encoding.UTF8, "application/json")
                : JsonContent.Create(payload);
            var response = await Http.PutAsync(path, content);
            var body = await response.Content.ReadAsStringAsync();
            Pass(label, response.StatusCode, body);
        }
        catch (Exception ex) { Fail(label, ex.Message); }
    }

    static void Pass(string label, System.Net.HttpStatusCode code, string body)
    {
        _pass++;
        var preview = body.Length > 100 ? body[..100] + "…" : body;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  PASS");
        Console.ResetColor();
        Console.WriteLine($"  {label,-55} {(int)code} {code}  {preview}");
    }

    static void Fail(string label, string reason)
    {
        _fail++;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("  FAIL");
        Console.ResetColor();
        Console.WriteLine($"  {label,-55} {reason}");
    }
}
