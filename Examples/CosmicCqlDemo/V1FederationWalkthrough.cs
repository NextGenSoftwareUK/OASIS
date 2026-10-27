using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>One global query over the same three providers, first in C#, then HTTP.</summary>
public static class V1FederationWalkthrough
{
    public static async Task<int> RunSdkAsync(
        HolonManager manager,
        IReadOnlyList<ICosmicQueryStorageProvider> adapters,
        Guid actor,
        string fixtureTag,
        TextWriter output)
    {
        // The trusted host supplies actor and eligible adapters. Provider scope
        // never grants access: bind the visibility policy before source windows.
        var access = Value(CosmicQueryReadAccess.OwnerOrPublic(actor));

        var source = Value(CosmicQuery<Holon>
            .Create(CosmicQueryScope.AllEligible)
            .WithReadAccess(access)
            .Where(row => CosmicQueryMetadata.Value<string>(row, "fixture") == fixtureTag)
            .OrderBy(row => row.Name));

        // No source Skip/Take here: each source must finish within its bound.
        // This separate window applies to the globally ordered physical copies.
        var options = Value(CosmicFederationOptions.Create(
            skip: 0,
            take: 5,
            maximumInputRows: 300,
            maximumRowsPerProvider: 100));

        var targets = adapters
            .Select(adapter => new CosmicQueryTarget(
                adapter.QueryProviderType,
                adapter.QueryCapabilities))
            .ToArray();

        var plan = Value(CosmicFederationPlanner.Plan(
            source,
            options,
            adapters[0].QueryProviderType,
            targets));

        // HolonManager reuses typed mapping, decryption and clean tracking.
        // Native filtering and receipt ownership stay in the shared CQL engine.
        var page = Value(await manager.ExecuteFederationQueryAsync(plan, adapters));

        if (page.Rows.Count != 5 || page.ObservedInputRows != 9)
            throw new InvalidOperationException("The demo expects three visible roots on each provider.");

        await output.WriteLineAsync("\nFederation C#: five global rows from nine authorized physical copies.\n");

        foreach (var row in page.Rows)
            await output.WriteLineAsync($"  {row.Selected.Provider}: {row.Selected.Row.Name}");

        return 1;
    }

    public static async Task<string> RunHttpAsync(
        HttpClient client,
        string bearerToken,
        string fixtureTag,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // Explicit provider priority selects a replica before global paging.
        // It is deterministic preference, not a freshness or causal claim.
        var providers = new[] { "MongoDBOASIS", "SQLLiteDBOASIS", "Neo4jOASIS" };

        var payload = new
        {
            scope = "Named",
            providers,

            source = new
            {
                filters = new[]
                {
                    new
                    {
                        metadataPath = new[] { "fixture" },
                        scalar = "String",
                        @operator = "Equal",
                        value = fixtureTag
                    }
                },

                orderBy = "Name",
                take = 100
            },

            identityMode = "ProviderPriority",
            providerPriority = providers.Reverse().ToArray(),
            skip = 0,
            take = 2
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/data/query-cql/federation")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(body);

        using var json = JsonDocument.Parse(body);
        var page = json.RootElement.GetProperty("result");

        if (page.GetProperty("rows").GetArrayLength() != 2 || page.GetProperty("resolvedRows").GetInt32() != 3)
            throw new InvalidOperationException("Priority must collapse nine copies to three identities before the two-row page.");

        await output.WriteLineAsync("\nFederation HTTP: two global identities; every observed copy retains provenance.\n");

        // Keep the operation ID: the host verifies the three durable attempt
        // receipts and exact cost. Never replay a read to infer its outcome.
        return response.Headers.GetValues("X-OASIS-Operation-Id").Single();
    }

    private static T Value<T>(OASISResult<T> result)
        => result.IsError ? throw new InvalidOperationException(result.ErrorCode + ": " + result.Message) : result.Result;
}
