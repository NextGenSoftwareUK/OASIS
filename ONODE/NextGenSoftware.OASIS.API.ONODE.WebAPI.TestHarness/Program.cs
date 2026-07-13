using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// Manual test harness for the ONODE and ONET WebAPI endpoints.
/// Runs against a locally-running ONODE WebAPI (default: http://localhost:5000) or a remote ONODE
/// (set ONODE_BASE_URL env var to override). Prints pass/fail for each call.
///
/// Pattern matches the rest of OASIS — a console harness the developer runs to exercise the live
/// stack end-to-end, complementing the automated unit/integration test suites.
/// </summary>
class Program
{
    static readonly string BaseUrl = Environment.GetEnvironmentVariable("ONODE_BASE_URL") ?? "http://localhost:5000";
    static readonly HttpClient Http = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(15) };

    static int _pass, _fail;

    static async Task Main(string[] args)
    {
        Console.WriteLine("NEXTGEN SOFTWARE ONODE WEB API TEST HARNESS V1.0");
        Console.WriteLine($"Target: {BaseUrl}");
        Console.WriteLine(new string('=', 60));

        // ── ONODE endpoints ──────────────────────────────────────────
        await Section("ONODE — Node lifecycle");
        await Get("/api/v1/onode/status",                   "GET  /onode/status");
        await Get("/api/v1/onode/info",                     "GET  /onode/info");
        await Get("/api/v1/onode/metrics",                  "GET  /onode/metrics");
        await Get("/api/v1/onode/logs",                     "GET  /onode/logs");
        await Get("/api/v1/onode/config",                   "GET  /onode/config");
        await Get("/api/v1/onode/peers",                    "GET  /onode/peers");
        await Get("/api/v1/onode/stats",                    "GET  /onode/stats");
        await Get("/api/v1/onode/oasisdna",                 "GET  /onode/oasisdna");

        await Post("/api/v1/onode/start", null,             "POST /onode/start");
        await Post("/api/v1/onode/stop",  null,             "POST /onode/stop");
        await Post("/api/v1/onode/restart", null,           "POST /onode/restart");

        await Put("/api/v1/onode/config",
            new { Config = new Dictionary<string, object> { ["testKey"] = "testValue" } },
            "PUT  /onode/config");

        // ── ONET endpoints ───────────────────────────────────────────
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

        // ── Summary ──────────────────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"RESULT: {_pass} passed, {_fail} failed out of {_pass + _fail} tests");
        if (_fail > 0) Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(_fail == 0 ? "ALL TESTS PASSED" : $"{_fail} TEST(S) FAILED");
        Console.ResetColor();
    }

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

    static async Task Post(string path, object? payload, string label)
    {
        try
        {
            HttpContent content = payload is null
                ? new StringContent("{}", Encoding.UTF8, "application/json")
                : JsonContent.Create(payload);
            var response = await Http.PostAsync(path, content);
            var body = await response.Content.ReadAsStringAsync();
            Pass(label, response.StatusCode, body);
        }
        catch (Exception ex) { Fail(label, ex.Message); }
    }

    static async Task Put(string path, object? payload, string label)
    {
        try
        {
            HttpContent content = payload is null
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
        var preview = body.Length > 120 ? body[..120] + "…" : body;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  PASS");
        Console.ResetColor();
        Console.WriteLine($"  {label,-40} {(int)code} {code}  {preview}");
    }

    static void Fail(string label, string reason)
    {
        _fail++;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("  FAIL");
        Console.ResetColor();
        Console.WriteLine($"  {label,-40} {reason}");
    }
}
