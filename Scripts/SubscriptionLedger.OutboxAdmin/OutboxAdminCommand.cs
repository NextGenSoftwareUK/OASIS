using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.SubscriptionLedger.OutboxAdmin;

/// <summary>Private infrastructure operator command. The actor always comes from WEB4's exact JWT and administrator policy.</summary>
public static class OutboxAdminCommand
{
    public const string Usage = "Usage: requeue <operation-uuid> <action-uuid> <reason>. Read configuration and administrator token from the protected environment; never put secrets on the command line.";

    public static async Task<int> RunAsync(string[] args, IConfiguration configuration, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        if (args.Length == 1 && args[0] is "--help" or "-h") { await output.WriteLineAsync(Usage); return 0; }
        if (args.Length != 4 || args[0] != "requeue" || !Guid.TryParseExact(args[1], "D", out _) || !Guid.TryParseExact(args[2], "D", out _) ||
            string.IsNullOrWhiteSpace(args[3]) || args[3].Length > 1000)
        { await WriteErrorAsync(error, "INVALID_ARGUMENTS", Usage); return 2; }
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            string actor = await AuthenticateAdministratorAsync(configuration, deadline.Token);
            string connection = Required(configuration, "SUBSCRIPTION_OUTBOX_MONGO_CONNECTION_STRING");
            string database = Required(configuration, "SUBSCRIPTION_OUTBOX_DATABASE");
            var store = new MongoUsageOutboxStore(connection, database);
            // No InitializeAsync here: the operator role needs find/update on the existing outbox, not schema-management privileges.
            await store.RequeueDeadletterAsync(args[1], args[2], actor, args[3], deadline.Token);
            await output.WriteLineAsync(JsonSerializer.Serialize(new { isError = false, code = "DEADLETTER_REQUEUED", operationId = args[1], actionId = args[2], actor,
                message = "The original immutable settlement is queued for WEB4 delivery. Monitor its acknowledgement; provider execution was not repeated." }));
            return 0;
        }
        catch (SecurityTokenException)
        { await WriteErrorAsync(error, "ADMIN_AUTHENTICATION_FAILED", "Administrator JWT signature, issuer, audience, lifetime or avatar binding is invalid."); return 3; }
        catch (UnauthorizedAccessException)
        { await WriteErrorAsync(error, "ADMIN_AUTHORIZATION_FAILED", "An authenticated JWT with the subscription administrator claim and an allowlisted avatar is required."); return 3; }
        catch (UsageProtocolException ex)
        { await WriteErrorAsync(error, ex.Code, ex.Message); return 4; }
        catch (OperationCanceledException)
        { await WriteErrorAsync(error, "REQUEUE_ACKNOWLEDGEMENT_UNKNOWN", "The action timed out or was cancelled. Retry the same action UUID and reason to determine its durable outcome."); return 5; }
        catch (MongoException)
        { await WriteErrorAsync(error, "OUTBOX_STORAGE_UNAVAILABLE", "The outbox action was not acknowledged. Restore storage access and retry the same action UUID; connection details are intentionally omitted."); return 5; }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is JsonException || ex is IOException || ex is KeyNotFoundException)
        { await WriteErrorAsync(error, "OPERATOR_CONFIGURATION_INVALID", "Required protected configuration or deployed OASIS DNA is missing or invalid. No credential values are printed."); return 2; }
    }

    public static async Task<string> AuthenticateAdministratorAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        string token = Required(configuration, "SUBSCRIPTION_ADMIN_BEARER_TOKEN");
        string dnaPath = Required(configuration, "OASIS_DNA_PATH");
        if (!Path.IsPathFullyQualified(dnaPath)) throw new InvalidOperationException("OASIS_DNA_PATH must identify the deployed DNA by its absolute path.");
        // Parse only the existing deployed DNA. Never boot the engine, generate a signing key, or alter configuration.
        using var dna = JsonDocument.Parse(await File.ReadAllTextAsync(dnaPath, cancellationToken));
        var security = dna.RootElement.GetProperty("OASIS").GetProperty("Security");
        string secret = security.GetProperty("SecretKey").GetString();
        string issuer = security.TryGetProperty("Oidc", out var oidc) && oidc.ValueKind == JsonValueKind.Object && oidc.TryGetProperty("Issuer", out var configuredIssuer)
            ? configuredIssuer.GetString() : null;
        System.Security.Claims.ClaimsPrincipal principal;
        try { principal = OasisJwtValidation.Validate(token, secret, issuer); }
        catch (ArgumentException ex) { throw new SecurityTokenValidationException("The administrator JWT is malformed.", ex); }
        var context = new DefaultHttpContext { User = principal };
        return SubscriptionServiceIdentity.RequireAdministrator(context, configuration);
    }

    private static string Required(IConfiguration configuration, string key) => !string.IsNullOrWhiteSpace(configuration[key]) ? configuration[key]
        : throw new InvalidOperationException(key + " is required.");
    private static Task WriteErrorAsync(TextWriter error, string code, string message) => error.WriteLineAsync(JsonSerializer.Serialize(new { isError = true, code, message }));
}
