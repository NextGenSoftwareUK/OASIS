using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CosmicCqlDemo;

/// <summary>
/// Read this file first for the production v1 HTTP language. The host supplies
/// a WEB4 URL and a token; the query code is identical for all three providers.
/// Fixture ownership and database startup belong to the host, not this client.
/// </summary>
public static class V1Walkthrough
{
    public static async Task RunAsync(
        HttpClient client,
        string bearerToken,
        string fixtureTag,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        foreach (var provider in new[] { "MongoDBOASIS", "SQLLiteDBOASIS", "Neo4jOASIS" })
        {
            await output.WriteLineAsync($"\n===== {provider}: the same v1 query contract =====\n");

            // EXAMPLE 1 — filter, sort and page natively.
            // The server adds owner/public visibility BEFORE this page. Alice
            // sees B-owned, C-public, D-owned; Skip(1).Take(1) returns C-public.
            await ShowAsync("1. Visibility before paging", new
            {
                provider,

                filters = new object[]
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
                skip = 1,
                take = 1
            });

            // EXAMPLE 2 — nested metadata and an ANDed OR group.
            // A-private cannot escape the server's visibility predicate through
            // AnyOf. Alice receives D-owned, even though both names are listed.
            await ShowAsync("2. Nested metadata + OR, with access protection", new
            {
                provider,

                filters = new object[]
                {
                    new
                    {
                        metadataPath = new[] { "location", "city" },
                        scalar = "String",
                        @operator = "Equal",
                        value = "London"
                    },
                    new
                    {
                        metadataPath = new[] { "stock" },
                        scalar = "Integer",
                        @operator = "GreaterThanOrEqual",
                        value = 7
                    }
                },

                anyOf = new[]
                {
                    new { field = "Name", @operator = "Equal", value = "A-private" },
                    new { field = "Name", @operator = "Equal", value = "D-owned" }
                },

                take = 10
            });

            // EXAMPLE 3 — exact Int64, beyond JavaScript Number precision.
            // C# serializes this long exactly. Browser demo requests retain the
            // original JSON text rather than rounding it through Number.
            await ShowAsync("3. Exact Int64 metadata", new
            {
                provider,

                filters = new[]
                {
                    new
                    {
                        metadataPath = new[] { "precision" },
                        scalar = "Integer",
                        @operator = "Equal",
                        value = 9007199254740993L
                    }
                },

                take = 10
            });
            // EXAMPLE 4 — selected child graphs over the same WEB4 route.
            // Alice sees the public child and her owned child. A private branch
            // and its otherwise-public grandchild stay outside the selected view.
            await ShowAsync("4. Public graph / native child visibility", new
            {
                provider,

                filters = new object[]
                {
                    new { metadataPath = new[] { "fixture" }, scalar = "String", @operator = "Equal", value = fixtureTag },
                    new { field = "Name", @operator = "Equal", value = "C-public" }
                },

                take = 1,

                graph = new
                {
                    recursive = true,
                    maxChildDepth = 2,
                    childType = "All",
                    maximumNodes = 3,
                    maximumPayloadBytes = 1048576,
                    consistency = "ProviderDefault"
                }
            });
            // EXAMPLE 5 � require visible children before the root page.
            // Groups are ANDed; each can match a different sibling. The server
            // supplies the signed actor, so private children cannot make a match.
            await ShowAsync("5. Parents with visible direct children", new
            {
                provider,

                filters = new object[]
                {
                    new
                    {
                        metadataPath = new[] { "fixture" },
                        scalar = "String",
                        @operator = "Equal",
                        value = fixtureTag
                    }
                },

                anyChildren = new object[]
                {
                    new { filters = new[] { new { field = "Name", @operator = "Equal", value = "public child" } } },
                    new { filters = new[] { new { field = "Name", @operator = "Equal", value = "owned child" } } }
                },

                orderBy = "Name",
                take = 1
            });
        }

        async Task ShowAsync(string title, object query)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var body = JsonSerializer.Serialize(query, options);

            // One key per NEW logical request. Keep it if the outcome becomes
            // uncertain; do not silently repeat the request with a fresh key.
            var operationKey = Guid.NewGuid().ToString("D");

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/data/query-cql")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            request.Headers.Add("Idempotency-Key", operationKey);

            await output.WriteLineAsync(title + "\n\n" + body + "\n");

            using var response = await client.SendAsync(request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"{title}: HTTP {(int)response.StatusCode}: {responseText}");

            using var result = JsonDocument.Parse(responseText);
            await output.WriteLineAsync(JsonSerializer.Serialize(result.RootElement, options));
            await output.WriteLineAsync("\nSettlement: " + response.Headers.GetValues("X-OASIS-Usage-Settlement").Single() + "\n");
        }
    }
}
