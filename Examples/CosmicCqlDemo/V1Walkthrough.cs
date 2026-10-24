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
    public static async Task<int> RunAsync(
        HttpClient client,
        string bearerToken,
        string fixtureTag,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        var completedRequests = 0;

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
            // EXAMPLE 5 — require visible children before the root page.
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
            // EXAMPLE 6 — bounded descendants, independent of graph output.
            // The server prunes private intermediates before visiting a branch.
            await ShowAsync("6. Bounded descendant existence", new
            {
                provider,

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

                anyDescendants = new[]
                {
                    new
                    {
                        maximumDepth = 2,
                        maximumNodes = 2,
                        filters = new[]
                        {
                            new { field = "Name", @operator = "Equal", value = "owned child" }
                        }
                    }
                },

                orderBy = "Name",
                take = 1
            });

            // EXAMPLE 7 — count the complete visible input. Unlike row queries,
            // the scalar route has no implicit Take(25). Int64 is always a JSON
            // string so a browser cannot round values above 2^53.
            var scalarFilters = new object[]
            {
                new { metadataPath = new[] { "fixture" }, scalar = "String",
                    @operator = "Equal", value = fixtureTag }
            };

            await ShowAsync("7. LongCount / no implicit row page", new
            {
                provider,
                operation = "LongCount",
                filters = scalarFilters
            }, scalar: true, valueType: "Int64", expected: "3");

            // EXAMPLE 8 — explicit source windows apply BEFORE the terminal.
            // Count uses a JSON Int32 number; it never changes to a string.
            await ShowAsync("8. Count / explicit source window", new
            {
                provider,
                operation = "Count",
                filters = scalarFilters,
                skip = 1,
                take = 1
            }, scalar: true, valueType: "Int32", expected: 1);

            // EXAMPLE 9 — Any returns a Boolean, with server-owned visibility.
            // An inaccessible private root cannot leak through existence tests.
            await ShowAsync("9. Any / private root remains invisible", new
            {
                provider,
                operation = "Any",
                filters = scalarFilters,
                anyOf = new[] { new { field = "Name", @operator = "Equal", value = "A-private" } }
            }, scalar: true, valueType: "Boolean", expected: false);

            // EXAMPLE 10 — select a detached native shape. Metadata types are
            // explicit; canonical types are inferred. Int64 cells are decimal
            // text, nullable leaves stay null, and columns retain their order.
            await ShowAsync("10. Native projection / exact selected values", new
            {
                provider,
                filters = scalarFilters,
                orderBy = "Name",
                skip = 1,
                take = 1,
                columns = new object[]
                {
                    new { alias = "label", field = "Name" },
                    new { alias = "precision", metadataPath = new[] { "precision" }, valueType = "Int64" },
                    new { alias = "optional", metadataPath = new[] { "nullable" }, valueType = "Int64", nullable = true },
                    new { alias = "city", metadataPath = new[] { "location", "city" }, valueType = "String" }
                }
            }, projection: true);

            // EXAMPLE 11 — selected Min uses the same actor-protected source.
            // Int64 metadata is explicit; its exact wire value is decimal text.
            await ShowAsync("11. Min / exact signed selected value", new
            {
                provider,
                operation = "Min",
                selection = new { metadataPath = new[] { "extremum" }, valueType = "Int64" },
                filters = scalarFilters
            }, scalar: true, valueType: "Int64", expected: "-9007199254740991");

            // EXAMPLE 12 — Max changes only the terminal. Private roots contain
            // more extreme values, but the server excludes them before reduction.
            await ShowAsync("12. Max / protected native reduction", new
            {
                provider,
                operation = "Max",
                selection = new { metadataPath = new[] { "extremum" }, valueType = "Int64" },
                filters = scalarFilters
            }, scalar: true, valueType: "Int64", expected: "-9007199254740989");
            // EXAMPLE 13 — Sum is exact across the complete visible input.
            // Int64 results remain text to avoid rounding in browser clients.
            await ShowAsync("13. Sum / exact signed total", new
            {
                provider,
                operation = "Sum",
                selection = new { metadataPath = new[] { "extremum" }, valueType = "Int64" },
                filters = scalarFilters
            }, scalar: true, valueType: "Int64", expected: "-27021597764222970");

            // EXAMPLE 14 — Average takes an Int64 selector but returns Decimal.
            // The first two names form the same window as the C# example.
            await ShowAsync("14. Average / exact Decimal fraction", new
            {
                provider,
                operation = "Average",
                selection = new { metadataPath = new[] { "extremum" }, valueType = "Int64" },
                filters = scalarFilters,
                orderBy = "Name",
                take = 2
            }, scalar: true, valueType: "Decimal", expected: "-9007199254740990.5");
            // EXAMPLE 15 — Keys and aggregate columns are separate descriptions.
            // Take selects the source window; maximumGroups limits only output.
            await ShowAsync("15. GroupBy / exact multiple aggregates", new
            {
                provider,
                filters = scalarFilters,
                orderBy = "Name",
                take = 2,
                maximumGroups = 10,

                keys = new[] { new { alias = "stock", metadataPath = new[] { "stock" }, valueType = "Int64" } },
                columns = new object[]
                {
                    new { alias = "Stock", keyAlias = "stock" },
                    new { alias = "Items", operation = "LongCount" },
                    new { alias = "Total", operation = "Sum", selection = new { metadataPath = new[] { "extremum" }, valueType = "Int64" } },
                    new { alias = "Mean", operation = "Average", selection = new { metadataPath = new[] { "extremum" }, valueType = "Int64" } }
                }
            }, grouped: true);

            // EXAMPLE 16 — Source orderBy and selected resultOrder are separate.
            // Aggregate values are exact Int64 text; all three providers sort
            // these adjacent totals natively, with no JavaScript number rounding.
            await ShowAsync("16. Grouped ordering / adjacent exact Int64 sums", new
            {
                provider,
                filters = scalarFilters,
                maximumGroups = 10,

                keys = new[] { new { alias = "name", field = "Name" } },
                columns = new object[]
                {
                    new { alias = "Name", keyAlias = "name" },
                    new { alias = "Total", operation = "Sum",
                        selection = new { metadataPath = new[] { "extremum" }, valueType = "Int64" } }
                },

                resultOrder = new[]
                {
                    new { alias = "Total", descending = true },
                    new { alias = "Name", descending = false }
                }
            }, grouped: true, orderedGroups: true);

            // EXAMPLE 17 — Result paging follows grouping and result ordering.
            // It cannot reduce aggregate participation or hide off-page errors.
            await ShowAsync("17. Grouped result page / independent native window", new
            {
                provider,
                filters = scalarFilters,
                maximumGroups = 10,

                keys = new[] { new { alias = "name", field = "Name" } },
                columns = new object[]
                {
                    new { alias = "Name", keyAlias = "name" },
                    new { alias = "Total", operation = "Sum",
                        selection = new { metadataPath = new[] { "extremum" }, valueType = "Int64" } }
                },

                resultOrder = new[]
                {
                    new { alias = "Total", descending = true },
                    new { alias = "Name", descending = false }
                },
                resultSkip = 1,
                resultTake = 1
            }, grouped: true, orderedGroups: true, pagedGroups: true);

            // EXAMPLE 18 — Filter selected aggregate aliases before paging.
            // Int64 literals are JSON strings here, preserving every digit in
            // browser clients. Only matching groups consume resultSkip.
            await ShowAsync("18. Filter grouped totals / exact Int64 threshold", new
            {
                provider,
                filters = scalarFilters,
                maximumGroups = 10,

                keys = new[] { new { alias = "name", field = "Name" } },
                columns = new object[]
                {
                    new { alias = "Name", keyAlias = "name" },
                    new { alias = "Total", operation = "Sum",
                        selection = new { metadataPath = new[] { "extremum" }, valueType = "Int64" } }
                },

                resultFilters = new[]
                {
                    new { alias = "Total", @operator = "GreaterThan", value = "-9007199254740991" }
                },
                resultOrder = new[]
                {
                    new { alias = "Total", descending = true },
                    new { alias = "Name", descending = false }
                },
                resultSkip = 1,
                resultTake = 1
            }, grouped: true, orderedGroups: true, pagedGroups: true);

            // EXAMPLE 19 — Compare the published Decimal mean, not a Float.
            // Source take2 determines participation. Result filters/order/take
            // apply after grouping; Decimal literals stay exact JSON strings.
            await ShowAsync("19. Filter and order normalized Decimal means", new
            {
                provider,
                filters = scalarFilters,
                orderBy = "Name",
                take = 2,
                maximumGroups = 10,

                keys = new[] { new { alias = "stock", metadataPath = new[] { "stock" }, valueType = "Int64" } },
                columns = new object[]
                {
                    new { alias = "Stock", keyAlias = "stock" },
                    new { alias = "Items", operation = "LongCount" },
                    new
                    {
                        alias = "Total",
                        operation = "Sum",
                        selection = new { metadataPath = new[] { "extremum" }, valueType = "Int64" }
                    },
                    new
                    {
                        alias = "Mean",
                        operation = "Average",
                        selection = new { metadataPath = new[] { "extremum" }, valueType = "Int64" }
                    }
                },

                resultFilters = new[]
                {
                    new { alias = "Mean", @operator = "GreaterThanOrEqual", value = "-9007199254740990.5" },
                    new { alias = "Mean", @operator = "LessThan", value = "-9007199254740990" }
                },
                resultOrder = new[] { new { alias = "Mean", descending = false } },
                resultTake = 1
            }, grouped: true);
        }

        return completedRequests;

        async Task ShowAsync(string title, object query, bool scalar = false, string? valueType = null, object? expected = null, bool projection = false, bool grouped = false, bool orderedGroups = false, bool pagedGroups = false)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var body = JsonSerializer.Serialize(query, options);

            // One key per NEW logical request. Keep it if the outcome becomes
            // uncertain; do not silently repeat the request with a fresh key.
            var operationKey = Guid.NewGuid().ToString("D");

            var route = grouped ? "/api/data/query-cql/grouped"
                : projection ? "/api/data/query-cql/projection"
                : scalar ? "/api/data/query-cql/scalar" : "/api/data/query-cql";

            using var request = new HttpRequestMessage(HttpMethod.Post, route)
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
            if (scalar)
            {
                var payload = result.RootElement.GetProperty("result");
                if (payload.GetProperty("version").GetString() != "cql-scalar-v1"
                    || payload.GetProperty("valueType").GetString() != valueType
                    || payload.GetProperty("value").GetRawText() != JsonSerializer.Serialize(expected))
                    throw new InvalidOperationException(title + " returned the wrong scalar domain or value.");
            }

            if (projection)
            {
                var payload = result.RootElement.GetProperty("result");
                var row = payload.GetProperty("rows").EnumerateArray().Single();

                if (payload.GetProperty("version").GetString() != "cql-projection-v1"
                    || payload.GetProperty("columns")[1].GetProperty("valueType").GetString() != "Int64"
                    || row[0].GetString() != "C-public" || row[1].GetString() != "9007199254740993"
                    || row[2].ValueKind != JsonValueKind.Null || row[3].GetString() != "London")
                    throw new InvalidOperationException(title + " returned the wrong selected shape or exact value.");
            }

            if (grouped && !orderedGroups)
            {
                var payload = result.RootElement.GetProperty("result");
                var row = payload.GetProperty("rows").EnumerateArray().Single();

                if (payload.GetProperty("version").GetString() != "cql-grouped-v1"
                    || payload.GetProperty("columns")[3].GetProperty("valueType").GetString() != "Decimal"
                    || row[0].GetString() != "7" || row[1].GetString() != "2"
                    || row[2].GetString() != "-18014398509481981" || row[3].GetString() != "-9007199254740990.5")
                    throw new InvalidOperationException(title + " returned an incorrect grouped shape or exact aggregate.");
            }

            if (orderedGroups)
            {
                var payload = result.RootElement.GetProperty("result");
                var rows = payload.GetProperty("rows").EnumerateArray().ToArray();

                var expectedNames = pagedGroups
                    ? new[] { "C-public" }
                    : new[] { "D-owned", "C-public", "B-owned" };

                var expectedTotals = pagedGroups
                    ? new[] { "-9007199254740990" }
                    : new[] { "-9007199254740989", "-9007199254740990", "-9007199254740991" };

                if (payload.GetProperty("version").GetString() != "cql-grouped-v1"
                    || !rows.Select(row => row[0].GetString()).SequenceEqual(expectedNames)
                    || !rows.Select(row => row[1].GetString()).SequenceEqual(expectedTotals))
                    throw new InvalidOperationException(title + " returned incorrect exact ordered groups.");
            }

            completedRequests++;
            await output.WriteLineAsync(JsonSerializer.Serialize(result.RootElement, options));
            await output.WriteLineAsync("\nSettlement: " + response.Headers.GetValues("X-OASIS-Usage-Settlement").Single() + "\n");
        }
    }
}
