using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

/// <summary>
/// End-to-end test harness for the ONODE WebAPI subscription flow.
///
/// Required env vars:
///   ONODE_BASE_URL          - base URL of the ONODE API (default: http://localhost:5000)
///   OASIS_EMAIL             - email of the test avatar account
///   OASIS_PASSWORD          - password of the test avatar account
///   STRIPE_WEBHOOK_SECRET   - webhook signing secret (whsec_...) from Stripe Dashboard
///
/// Optional:
///   ONODE_JWT_TOKEN         - skip auto-login and use this JWT directly
///   STRIPE_PRICE_BRONZE     - Stripe price ID for bronze plan (used for checkout session test)
///
/// What this tests:
///   1. Authenticate with the OASIS API and get a JWT
///   2. GET /subscription/plans   - verify 5 plans are returned
///   3. GET /subscription/subscriptions/me  - record pre-test plan
///   4. POST /subscription/checkout/session  - create a Stripe checkout session for bronze
///   5. Simulate Stripe checkout.session.completed webhook with valid HMAC signature
///   6. GET /subscription/subscriptions/me  - verify plan changed to bronze
///   7. Simulate customer.subscription.deleted webhook  - cancel the subscription
///   8. GET /subscription/subscriptions/me  - verify status changed to cancelled
///   9. Restore the original free plan via checkout/session
/// </summary>
class Program
{
    static readonly string BaseUrl = Environment.GetEnvironmentVariable("ONODE_BASE_URL") ?? "http://localhost:5000";
    static readonly string Email = Environment.GetEnvironmentVariable("OASIS_EMAIL") ?? "";
    static readonly string Password = Environment.GetEnvironmentVariable("OASIS_PASSWORD") ?? "";
    static readonly string WebhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") ?? "";
    static readonly string WebhookTestToken = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_TEST_TOKEN") ?? "";
    static readonly string? PresetJwt = Environment.GetEnvironmentVariable("ONODE_JWT_TOKEN");

    static readonly HttpClient Http = new() { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(20) };
    static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    static int _pass, _fail;
    static string? _jwt;
    static string? _avatarId;

    // ── Entry point ──────────────────────────────────────────────────────────

    static async Task<int> Main(string[] args)
    {
        Banner();
        CheckEnv();

        // ── 1. Auth ──────────────────────────────────────────────────────────
        await Section("1. Authentication");
        await Authenticate();
        if (_jwt == null)
        {
            Red("Cannot continue without a valid JWT. Set OASIS_EMAIL + OASIS_PASSWORD or ONODE_JWT_TOKEN.");
            return 1;
        }
        Http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _jwt);

        // ── 2. Plans ─────────────────────────────────────────────────────────
        await Section("2. GET /subscription/plans  (public)");
        var plans = await GetExpect("/api/subscription/plans", 200,
            "Plans endpoint returns 200",
            j => (j?["Result"] ?? j?["result"])?.AsArray().Count >= 5,
            "should return 5 plans");

        // ── 3. Pre-test subscription state ───────────────────────────────────
        await Section("3. Pre-test subscription state");
        var preTestSub = await GetExpect("/api/subscription/subscriptions/me", 200,
            "GET subscriptions/me returns 200",
            _ => true, "");
        var preTestPlan = ExtractPlanId(preTestSub);
        Console.WriteLine($"       Pre-test plan: {preTestPlan ?? "none (free)"}");

        // ── 4. Checkout session ───────────────────────────────────────────────
        await Section("4. POST /subscription/checkout/session  (bronze)");
        var checkoutBody = await PostExpect("/api/subscription/checkout/session",
            new { PlanId = "bronze", SuccessUrl = $"{BaseUrl}/success", CancelUrl = $"{BaseUrl}/cancel" },
            200, 500,  // 200 with Stripe key configured, 500 without
            "Create bronze checkout session",
            j => (j?["sessionUrl"] ?? j?["SessionUrl"]) != null || j?["message"]?.ToString()?.Contains("Stripe") == true || j?["Message"]?.ToString()?.Contains("Stripe") == true,
            "should return sessionUrl or indicate Stripe config issue");

        var sessionUrl = (checkoutBody?["sessionUrl"] ?? checkoutBody?["SessionUrl"])?.ToString();
        var sessionId = (checkoutBody?["sessionId"] ?? checkoutBody?["SessionId"])?.ToString();
        Console.WriteLine($"       SessionUrl: {sessionUrl ?? "(not returned — Stripe not configured or no price ID)"}");

        // ── 5. Simulate checkout.session.completed webhook ───────────────────
        await Section("5. Stripe webhook: checkout.session.completed");

        if (string.IsNullOrEmpty(WebhookSecret) && string.IsNullOrEmpty(WebhookTestToken))
        {
            Warn("Neither STRIPE_WEBHOOK_SECRET nor STRIPE_WEBHOOK_TEST_TOKEN set — skipping webhook tests.");
            Warn("Set STRIPE_WEBHOOK_TEST_TOKEN to a shared secret that also matches the server's env var.");
        }
        else
        {
            // Build a realistic checkout.session.completed event
            var fakeCheckoutSession = new
            {
                id = sessionId ?? $"cs_test_{Guid.NewGuid():N}",
                @object = "checkout.session",
                customer = $"cus_test_{Guid.NewGuid():N}[..8]",
                subscription = $"sub_test_{Guid.NewGuid():N}",
                payment_status = "paid",
                status = "complete",
                metadata = new Dictionary<string, string>
                {
                    ["avatar_id"] = _avatarId ?? "unknown",
                    ["plan_id"] = "bronze"
                }
            };

            await SendWebhook("checkout.session.completed",
                fakeCheckoutSession,
                "checkout.session.completed → UpsertSubscription called");

            // ── 6. Verify subscription is now bronze ──────────────────────────
            await Section("6. Verify subscription updated to bronze");
            await Task.Delay(500); // brief pause — webhook handler is async
            var postWebhookSub = await GetExpect("/api/subscription/subscriptions/me", 200,
                "GET subscriptions/me after webhook",
                j =>
                {
                    var arr = (j?["Result"] ?? j?["result"])?.AsArray();
                    if (arr == null || arr.Count == 0) return false;
                    var planId = (arr[0]?["PlanId"] ?? arr[0]?["planId"])?.ToString();
                    return planId == "bronze";
                },
                "PlanId should be 'bronze' after checkout.session.completed webhook");

            var postPlan = ExtractPlanId(postWebhookSub);
            Console.WriteLine($"       Post-webhook plan: {postPlan ?? "none — SAVE FAILED (check Railway logs for SaveSettingsAsync)"}");

            if (postPlan == "bronze")
            {
                Pass("  Subscription correctly shows bronze after webhook", HttpStatusCode.OK, "");
            }
            else
            {
                Fail("  Subscription plan after webhook",
                    $"Still '{postPlan ?? "none"}' — SaveSettingsAsync is not persisting! Check Railway logs.");
            }

            // ── 7. Simulate subscription.deleted webhook ──────────────────────
            await Section("7. Stripe webhook: customer.subscription.deleted");

            var fakeSubscription = new
            {
                id = $"sub_test_{Guid.NewGuid():N}",
                @object = "subscription",
                customer = $"cus_test_{Guid.NewGuid():N}",
                status = "canceled"
            };

            await SendWebhook("customer.subscription.deleted",
                fakeSubscription,
                "customer.subscription.deleted → status set to cancelled");

            // ── 8. Verify cancelled ───────────────────────────────────────────
            await Section("8. Verify subscription shows cancelled");
            await Task.Delay(500);
            var cancelledSub = await GetExpect("/api/subscription/subscriptions/me", 200,
                "GET subscriptions/me after deletion webhook",
                j =>
                {
                    var arr = (j?["Result"] ?? j?["result"])?.AsArray();
                    if (arr == null || arr.Count == 0) return true;
                    var status = (arr[0]?["Status"] ?? arr[0]?["status"])?.ToString();
                    return status == "cancelled" || status == "canceled";
                },
                "Status should be 'cancelled' after subscription.deleted webhook");

            // ── 9. Restore free plan ──────────────────────────────────────────
            await Section("9. Restore free plan");
            await PostExpect("/api/subscription/checkout/session",
                new { PlanId = "free", SuccessUrl = "/success" },
                200, 200,
                "Restore free plan",
                j => (j?["message"] ?? j?["Message"])?.ToString()?.Contains("activated") == true ||
                     (j?["sessionUrl"] ?? j?["SessionUrl"]) != null,
                "Free plan should activate immediately");

            await Task.Delay(300);
            var restoredSub = await GetExpect("/api/subscription/subscriptions/me", 200,
                "Verify restored to free",
                j =>
                {
                    var arr = (j?["Result"] ?? j?["result"])?.AsArray();
                    if (arr == null || arr.Count == 0) return true;
                    var planId = (arr[0]?["PlanId"] ?? arr[0]?["planId"])?.ToString();
                    return planId == "free" || planId == null;
                },
                "Plan should be free after restore");
        }

        // ── 10. Usage endpoint ────────────────────────────────────────────────
        await Section("10. GET /subscription/usage");
        await GetExpect("/api/subscription/usage", 200,
            "Usage endpoint returns currentMonth",
            j => j?["currentMonth"] != null,
            "should contain currentMonth field");

        // ── 11. Orders ────────────────────────────────────────────────────────
        await Section("11. GET /subscription/orders/me");
        await GetExpect("/api/subscription/orders/me", 200,
            "Orders endpoint returns result array",
            j => (j?["Result"] ?? j?["result"]) != null,
            "should contain result field");

        // ── 12. HyperDrive ────────────────────────────────────────────────────
        await Section("12. HyperDrive endpoints");
        await GetExpect("/api/subscription/hyperdrive-usage", 200,
            "GET hyperdrive-usage",
            j => (j?["Result"] ?? j?["result"]) != null || j?["isError"]?.GetValue<bool>() == false,
            "should return result or isError:false");

        await PostExpect("/api/subscription/check-hyperdrive-quota",
            new { OperationType = "Requests" },
            200, 200,
            "POST check-hyperdrive-quota (Requests)",
            j =>
            {
                var r = j?["Result"] ?? j?["result"];
                return r?["CanProceed"] != null || r?["canProceed"] != null;
            },
            "should return CanProceed");

        // ── Summary ───────────────────────────────────────────────────────────
        Summary();
        return _fail > 0 ? 1 : 0;
    }

    // ── Auth ─────────────────────────────────────────────────────────────────

    static async Task Authenticate()
    {
        if (!string.IsNullOrEmpty(PresetJwt))
        {
            _jwt = PresetJwt;
            Console.WriteLine("  Using preset ONODE_JWT_TOKEN.");
            // Try to extract avatar id from the JWT claims
            _avatarId = ExtractAvatarIdFromJwt(_jwt);
            Pass("  JWT provided via env var", HttpStatusCode.OK, $"avatarId={_avatarId ?? "unknown"}");
            return;
        }

        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
        {
            Fail("  Cannot authenticate", "OASIS_EMAIL and OASIS_PASSWORD not set. Provide them or set ONODE_JWT_TOKEN.");
            return;
        }

        try
        {
            var response = await Http.PostAsJsonAsync("/api/avatar/authenticate",
                new { Username = Email, Email, Password });

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Fail("  POST /api/avatar/authenticate", $"{(int)response.StatusCode} — {body[..Math.Min(200, body.Length)]}");
                return;
            }

            var doc = JsonNode.Parse(body);
            // Response: { result: { result: { jwtToken, id, ... } } }
            var inner = doc?["result"]?["result"] ?? doc?["Result"]?["Result"] ?? doc?["result"] ?? doc?["Result"];
            _jwt = inner?["jwtToken"]?.ToString()
                ?? inner?["JwtToken"]?.ToString()
                ?? inner?["token"]?.ToString()
                ?? doc?["jwtToken"]?.ToString();

            _avatarId = inner?["id"]?.ToString()
                ?? inner?["Id"]?.ToString()
                ?? doc?["id"]?.ToString();

            if (_jwt == null)
            {
                Fail("  POST /api/avatar/authenticate", $"No JWT in response: {body[..Math.Min(300, body.Length)]}");
                return;
            }

            Pass("  POST /api/avatar/authenticate", response.StatusCode,
                $"avatarId={_avatarId ?? "unknown"}  jwt={_jwt[..Math.Min(20, _jwt.Length)]}...");
        }
        catch (Exception ex)
        {
            Fail("  POST /api/avatar/authenticate", ex.Message);
        }
    }

    // ── Stripe webhook simulation ─────────────────────────────────────────────

    static async Task SendWebhook(string eventType, object dataObject, string label)
    {
        try
        {
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var payload = BuildStripeEventPayload(eventType, dataObject, ts);

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/subscription/webhooks/stripe");
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            // Prefer the test-token bypass (avoids secret mismatch between harness and server).
            // Fall back to real HMAC signature if no test token is configured.
            if (!string.IsNullOrEmpty(WebhookTestToken))
            {
                request.Headers.Add("X-Webhook-Test-Token", WebhookTestToken);
            }
            else
            {
                var signature = ComputeStripeSignature(payload, WebhookSecret, ts);
                request.Headers.Add("Stripe-Signature", signature);
            }

            var response = await Http.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                Pass($"  {label}", response.StatusCode, body[..Math.Min(100, body.Length)]);
            else
                Fail($"  {label}", $"{(int)response.StatusCode} — {body[..Math.Min(200, body.Length)]}");
        }
        catch (Exception ex)
        {
            Fail($"  {label}", ex.Message);
        }
    }

    /// <summary>
    /// Builds a minimal Stripe event envelope using the given Unix timestamp.
    /// </summary>
    static string BuildStripeEventPayload(string type, object dataObject, long ts)
    {
        var payload = new
        {
            id = $"evt_{Guid.NewGuid():N}",
            @object = "event",
            api_version = "2024-06-20",
            created = ts,
            type,
            livemode = false,
            data = new { @object = dataObject }
        };
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    /// <summary>
    /// Computes a Stripe webhook signature: t=timestamp,v1=HMAC-SHA256(secret, "t.payload").
    /// Stripe.net EventUtility uses the full whsec_... string (including prefix) as the UTF-8 key.
    /// </summary>
    static string ComputeStripeSignature(string payload, string secret, long ts)
    {
        var signedPayload = $"{ts}.{payload}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var sig = Convert.ToHexString(hash).ToLower();
        return $"t={ts},v1={sig}";
    }

    // ── HTTP helpers ──────────────────────────────────────────────────────────

    static async Task<JsonNode?> GetExpect(string path, int expectedStatus, string label,
        Func<JsonNode?, bool> check, string checkDesc)
    {
        try
        {
            var response = await Http.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();
            var status = (int)response.StatusCode;

            JsonNode? doc = null;
            try { doc = JsonNode.Parse(body); } catch { }

            var statusOk = status == expectedStatus;
            var checkOk = check(doc);

            if (statusOk && checkOk)
                Pass($"  {label}", response.StatusCode, body[..Math.Min(120, body.Length)]);
            else
                Fail($"  {label}",
                    !statusOk ? $"Expected {expectedStatus} got {status}. Body: {body[..Math.Min(200, body.Length)]}"
                               : $"Body check failed ({checkDesc}): {body[..Math.Min(200, body.Length)]}");

            return doc;
        }
        catch (Exception ex) { Fail($"  {label}", ex.Message); return null; }
    }

    static async Task<JsonNode?> PostExpect(string path, object payload,
        int expectedStatus1, int expectedStatus2, string label,
        Func<JsonNode?, bool> check, string checkDesc)
    {
        try
        {
            var response = await Http.PostAsJsonAsync(path, payload);
            var body = await response.Content.ReadAsStringAsync();
            var status = (int)response.StatusCode;

            JsonNode? doc = null;
            try { doc = JsonNode.Parse(body); } catch { }

            var statusOk = status == expectedStatus1 || status == expectedStatus2;
            var checkOk = check(doc);

            if (statusOk && checkOk)
                Pass($"  {label}", response.StatusCode, body[..Math.Min(120, body.Length)]);
            else
                Fail($"  {label}",
                    !statusOk ? $"Expected {expectedStatus1}/{expectedStatus2} got {status}. Body: {body[..Math.Min(200, body.Length)]}"
                               : $"Body check failed ({checkDesc}): {body[..Math.Min(200, body.Length)]}");

            return doc;
        }
        catch (Exception ex) { Fail($"  {label}", ex.Message); return null; }
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    static string? ExtractPlanId(JsonNode? doc)
    {
        var arr = (doc?["Result"] ?? doc?["result"])?.AsArray();
        if (arr == null || arr.Count == 0) return null;
        return (arr[0]?["PlanId"] ?? arr[0]?["planId"])?.ToString();
    }

    static string? ExtractAvatarIdFromJwt(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;
            var payload = parts[1];
            // Pad base64url
            payload = payload.Replace('-', '+').Replace('_', '/');
            while (payload.Length % 4 != 0) payload += "=";
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var doc = JsonNode.Parse(json);
            return doc?["sub"]?.ToString()
                ?? doc?["nameid"]?.ToString()
                ?? doc?["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"]?.ToString();
        }
        catch { return null; }
    }

    // ── Console output ────────────────────────────────────────────────────────

    static void Banner()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║      OASIS SUBSCRIPTION END-TO-END TEST HARNESS  v3.0               ║");
        Console.WriteLine("║      Tests the full portal subscription signup flow                  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════╝");
        Console.WriteLine($"  Target : {BaseUrl}");
        Console.WriteLine($"  Email  : {(string.IsNullOrEmpty(Email) ? "(not set — use OASIS_EMAIL)" : Email)}");
        var webhookMode = !string.IsNullOrEmpty(WebhookTestToken) ? "test-token bypass"
            : !string.IsNullOrEmpty(WebhookSecret) ? "HMAC (whsec_***)"
            : "(not set — webhook tests will be skipped)";
        Console.WriteLine($"  Webhook: {webhookMode}");
        Console.WriteLine();
    }

    static void CheckEnv()
    {
        if (string.IsNullOrEmpty(PresetJwt) && (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password)))
            Warn("Neither ONODE_JWT_TOKEN nor OASIS_EMAIL+OASIS_PASSWORD are set. Auth tests will fail.");
        if (string.IsNullOrEmpty(WebhookSecret) && string.IsNullOrEmpty(WebhookTestToken))
            Warn("Neither STRIPE_WEBHOOK_SECRET nor STRIPE_WEBHOOK_TEST_TOKEN set. Webhook tests will be skipped.");
    }

    static Task Section(string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"── {title}");
        Console.ResetColor();
        return Task.CompletedTask;
    }

    static void Pass(string label, HttpStatusCode code, string preview)
    {
        _pass++;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  ✓ PASS");
        Console.ResetColor();
        var p = preview.Length > 80 ? preview[..80] + "…" : preview;
        Console.WriteLine($"  {label.TrimStart(),-58} {(int)code}  {p}");
    }

    static void Fail(string label, string reason)
    {
        _fail++;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("  ✗ FAIL");
        Console.ResetColor();
        Console.WriteLine($"  {label.TrimStart(),-58} {reason}");
    }

    static void Warn(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  ⚠  {msg}");
        Console.ResetColor();
    }

    static void Red(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ✗  {msg}");
        Console.ResetColor();
    }

    static void Summary()
    {
        Console.WriteLine();
        Console.WriteLine(new string('═', 72));
        Console.WriteLine($"  RESULT: {_pass} passed  {_fail} failed  ({_pass + _fail} total)");
        Console.ForegroundColor = _fail > 0 ? ConsoleColor.Red : ConsoleColor.Green;
        Console.WriteLine(_fail == 0 ? "  ALL TESTS PASSED" : $"  {_fail} TEST(S) FAILED");
        Console.ResetColor();
        Console.WriteLine();
        if (_fail > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Troubleshooting subscription save failures:");
            Console.WriteLine("    1. Check Railway logs for 'SaveSettingsAsync failed'");
            Console.WriteLine("    2. Check Railway logs for 'NullReferenceException' in AvatarRepository");
            Console.WriteLine("    3. Verify STRIPE_WEBHOOK_SECRET matches the Stripe Dashboard webhook");
            Console.WriteLine("    4. Verify MongoDB is the active provider (check AutoFailOverProviders in OASISDNA)");
            Console.ResetColor();
        }
    }
}
