using System.Net;
using System.Net.Http.Json;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class BillingRestorationHttpQualification
{
    public static async Task RunAsync(HttpClient http, MongoSubscriptionUsageRepository repository, Guid actor)
    {
        static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException(message); }
        var user = actor.ToString("D");
        var before = JsonConvert.SerializeObject(await repository.GetSubscriptionAsync(user));
        using var usageResponse = await http.GetAsync("/api/subscription/usage");
        Require(usageResponse.StatusCode == HttpStatusCode.OK, "Current billing usage GET failed.");
        var usage = JObject.Parse(await usageResponse.Content.ReadAsStringAsync());
        Require(usage["currentMonth"]?.Value<long>("requests") == 24000 &&
            usage.Value<bool?>("hasBoundPersonalConsent") == false &&
            usage.Value<bool?>("payAsYouGoEnabled") == false &&
            usage.Property("paidOverageTermsSha256") != null &&
            usage["paidOverageTermsSha256"]!.Type == JTokenType.Null,
            "Usage response omitted or invented personal consent terms.");
        using var statementsResponse = await http.GetAsync("/api/subscription/overages/statements");
        Require(statementsResponse.StatusCode == HttpStatusCode.OK &&
            JArray.Parse(await statementsResponse.Content.ReadAsStringAsync()).Count == 0,
            "Fresh owned subscriber statement read is not empty.");
        var missingStatement = "ovg_" + new string('a', 64);
        using var missing = await http.PostAsJsonAsync("/api/subscription/overages/pay-now",
            new { statementId = missingStatement });
        Require(missing.StatusCode == HttpStatusCode.NotFound,
            "Canonical statement retry did not reach the subscriber-bound lookup.");
        using var ambiguous = await http.PostAsJsonAsync("/api/subscription/overages/pay-now",
            new { statementId = missingStatement, billingRequestId = Guid.NewGuid().ToString("D") });
        Require(ambiguous.StatusCode == HttpStatusCode.BadRequest,
            "Conflicting existing-statement/new-request billing intent was accepted.");
        using var noCharges = await http.PostAsJsonAsync("/api/subscription/overages/pay-now",
            new { billingRequestId = Guid.NewGuid().ToString("D"), month = DateTime.UtcNow.ToString("yyyy-MM") });
        Require(noCharges.StatusCode == HttpStatusCode.OK, "Owned no-charge billing preparation failed.");
        var result = JObject.Parse(await noCharges.Content.ReadAsStringAsync());
        Require(result.Value<bool?>("isError") == false && result.Value<bool?>("noCharges") == true,
            "No-charge request claimed collection or lacked an acknowledgement.");
        var statements = await ((IPaidOverageStatementStore)repository).GetStatementsAsync(user, 100, CancellationToken.None);
        Require(statements.Count == 0 &&
            (await repository.GetAggregateAsync(user, CancellationToken.None)).MonthlyRequests == 24000 &&
            JsonConvert.SerializeObject(await repository.GetSubscriptionAsync(user)) == before,
            "No-charge/invalid requests changed statements, quota or subscriber consent.");
        Console.WriteLine("BILLING_RESTORATION_HTTP_PROOF: real MVC/native usage exposes unbound consent; canonical statement retry404 and conflicting-intent400; explicit no-charge200 with no statement, unchanged quota/subscriber. No Stripe credential/payment, full Startup or hosted acceptance.");
    }
}
