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

            // EXAMPLE 20 — UTC source predicates use the DateTime scalar and
            // explicit Z text. The range distinguishes neighboring100ns ticks.
            await ShowAsync("20. UTC source range / detached date columns", new
            {
                provider,
                filters = scalarFilters.Concat(new object[]
                {
                    new { metadataPath = new[] { "observedAt" }, scalar = "DateTime",
                        @operator = "GreaterThanOrEqual", value = "2026-10-24T00:00:00.0000002Z" },
                    new { metadataPath = new[] { "observedAt" }, scalar = "DateTime",
                        @operator = "LessThan", value = "2026-10-24T00:00:00.0000004Z" }
                }).ToArray(),
                orderBy = "Name",
                take = 2,
                columns = new object[]
                {
                    new { alias = "Name", field = "Name" },
                    new { alias = "ObservedAt", metadataPath = new[] { "observedAt" }, valueType = "DateTime" }
                }
            }, projection: true, dateProjection: true);

            // EXAMPLE 21 — Filter exact selected date keys after reduction.
            // Result order/page are independent of the source's default order.
            await ShowAsync("21. UTC grouped dates / selected alias predicates", new
            {
                provider,
                filters = scalarFilters,
                maximumGroups = 10,
                keys = new[] { new { alias = "observed", metadataPath = new[] { "observedAt" }, valueType = "DateTime" } },
                columns = new object[]
                {
                    new { alias = "ObservedAt", keyAlias = "observed" },
                    new { alias = "Items", operation = "LongCount" }
                },
                resultFilters = new[]
                {
                    new { alias = "ObservedAt", @operator = "GreaterThanOrEqual", value = "2026-10-24T00:00:00.0000002Z" },
                    new { alias = "ObservedAt", @operator = "LessThan", value = "2026-10-24T00:00:00.0000004Z" }
                },
                resultOrder = new[] { new { alias = "ObservedAt", descending = false } },
                resultTake = 2
            }, grouped: true, dateGroups: true);

            // EXAMPLE 22 — Use digit strings for UInt64 request values. The
            // projection response also uses strings, so browsers retain every bit.
            // Source HTTP ordering is canonical Name; example23 orders UInt64 keys.
            await ShowAsync("22. UInt64 source range / exact selected columns", new
            {
                provider,
                filters = scalarFilters.Concat(new object[]
                {
                    new { metadataPath = new[] { "counter" }, scalar = "UInt64",
                        @operator = "GreaterThanOrEqual", value = "18446744073709551612" },
                    new { metadataPath = new[] { "counter" }, scalar = "UInt64",
                        @operator = "LessThan", value = "18446744073709551614" }
                }).ToArray(),
                orderBy = "Name",
                take = 2,
                columns = new object[]
                {
                    new { alias = "Name", field = "Name" },
                    new { alias = "Counter", metadataPath = new[] { "counter" }, valueType = "UInt64" }
                }
            }, projection: true, unsignedProjection: true);

            // EXAMPLE 23 — The selected alias determines UInt64 type. Bounds
            // remain strings; filtering happens before native group ordering/page.
            await ShowAsync("23. UInt64 groups / exact alias filters and ordering", new
            {
                provider,
                filters = scalarFilters,
                maximumGroups = 10,
                keys = new[] { new { alias = "counter", metadataPath = new[] { "counter" }, valueType = "UInt64" } },
                columns = new object[]
                {
                    new { alias = "Counter", keyAlias = "counter" },
                    new { alias = "Items", operation = "LongCount" }
                },
                resultFilters = new[]
                {
                    new { alias = "Counter", @operator = "GreaterThanOrEqual", value = "18446744073709551612" },
                    new { alias = "Counter", @operator = "LessThan", value = "18446744073709551614" }
                },
                resultOrder = new[] { new { alias = "Counter", descending = false } },
                resultTake = 2
            }, grouped: true, unsignedGroups: true);
            // EXAMPLE 24 — Decimal request values are exact strings. The
            // projection response also uses strings, so browsers retain every bit.
            // These prices differ by 10^-28; JSON numbers would lose precision.
            await ShowAsync("24. Decimal source range / exact selected columns", new
            {
                provider,
                filters = scalarFilters.Concat(new object[]
                {
                    new { metadataPath = new[] { "price" }, scalar = "Decimal",
                        @operator = "GreaterThanOrEqual", value = "1.0000000000000000000000000002" },
                    new { metadataPath = new[] { "price" }, scalar = "Decimal",
                        @operator = "LessThan", value = "1.0000000000000000000000000004" }
                }).ToArray(),
                orderBy = "Name",
                take = 2,
                columns = new object[]
                {
                    new { alias = "Name", field = "Name" },
                    new { alias = "Price", metadataPath = new[] { "price" }, valueType = "Decimal" }
                }
            }, projection: true, decimalProjection: true);

            // EXAMPLE 25 — The selected alias determines Decimal type. Bounds
            // remain strings; filtering happens before native group ordering/page.
            await ShowAsync("25. Decimal groups / exact alias filters and ordering", new
            {
                provider,
                filters = scalarFilters,
                maximumGroups = 10,
                keys = new[] { new { alias = "price", metadataPath = new[] { "price" }, valueType = "Decimal" } },
                columns = new object[]
                {
                    new { alias = "Price", keyAlias = "price" },
                    new { alias = "Items", operation = "LongCount" }
                },
                resultFilters = new[]
                {
                    new { alias = "Price", @operator = "GreaterThanOrEqual", value = "1.0000000000000000000000000002" },
                    new { alias = "Price", @operator = "LessThan", value = "1.0000000000000000000000000004" }
                },
                resultOrder = new[] { new { alias = "Price", descending = false } },
                resultTake = 2
            }, grouped: true, decimalGroups: true);
            // EXAMPLE 26 — Double request values are canonical round-trip strings. The
            // projection response also uses strings, so browsers retain every bit.
            // These readings differ by one binary64 unit; strings retain exact values.
            await ShowAsync("26. Double source range / exact selected columns", new
            {
                provider,
                filters = scalarFilters.Concat(new object[]
                {
                    new { metadataPath = new[] { "reading" }, scalar = "Double",
                        @operator = "GreaterThanOrEqual", value = "1.0000000000000004" },
                    new { metadataPath = new[] { "reading" }, scalar = "Double",
                        @operator = "LessThan", value = "1.0000000000000009" }
                }).ToArray(),
                orderBy = "Name",
                take = 2,
                columns = new object[]
                {
                    new { alias = "Name", field = "Name" },
                    new { alias = "Reading", metadataPath = new[] { "reading" }, valueType = "Double" }
                }
            }, projection: true, doubleProjection: true);

            // EXAMPLE 27 — The selected alias determines Double type. Bounds
            // remain strings; filtering happens before native group ordering/page.
            await ShowAsync("27. Double groups / exact alias filters and ordering", new
            {
                provider,
                filters = scalarFilters,
                maximumGroups = 10,
                keys = new[] { new { alias = "reading", metadataPath = new[] { "reading" }, valueType = "Double" } },
                columns = new object[]
                {
                    new { alias = "Reading", keyAlias = "reading" },
                    new { alias = "Items", operation = "LongCount" }
                },
                resultFilters = new[]
                {
                    new { alias = "Reading", @operator = "GreaterThanOrEqual", value = "1.0000000000000004" },
                    new { alias = "Reading", @operator = "LessThan", value = "1.0000000000000009" }
                },
                resultOrder = new[] { new { alias = "Reading", descending = false } },
                resultTake = 2
            }, grouped: true, doubleGroups: true);

            // EXAMPLE 28 — Single request values are canonical round-trip strings. The
            // projection response also uses strings, so browsers retain every bit.
            // Strings make the declared binary32 contract explicit to every client.
            await ShowAsync("28. Single source range / exact selected columns", new
            {
                provider,
                filters = scalarFilters.Concat(new object[]
                {
                    new { metadataPath = new[] { "singleReading" }, scalar = "Single",
                        @operator = "GreaterThanOrEqual", value = "1.0000002" },
                    new { metadataPath = new[] { "singleReading" }, scalar = "Single",
                        @operator = "LessThan", value = "1.0000005" }
                }).ToArray(),
                orderBy = "Name",
                take = 2,
                columns = new object[]
                {
                    new { alias = "Name", field = "Name" },
                    new { alias = "Reading", metadataPath = new[] { "singleReading" }, valueType = "Single" }
                }
            }, projection: true, singleProjection: true);

            // EXAMPLE 29 — The selected alias determines Single type. Bounds
            // remain strings; filtering happens before native group ordering/page.
            await ShowAsync("29. Single groups / exact alias filters and ordering", new
            {
                provider,
                filters = scalarFilters,
                maximumGroups = 10,
                keys = new[] { new { alias = "singleReading", metadataPath = new[] { "singleReading" }, valueType = "Single" } },
                columns = new object[]
                {
                    new { alias = "Reading", keyAlias = "singleReading" },
                    new { alias = "Items", operation = "LongCount" }
                },
                resultFilters = new[]
                {
                    new { alias = "Reading", @operator = "GreaterThanOrEqual", value = "1.0000002" },
                    new { alias = "Reading", @operator = "LessThan", value = "1.0000005" }
                },
                resultOrder = new[] { new { alias = "Reading", descending = false } },
                resultTake = 2
            }, grouped: true, singleGroups: true);
            // EXAMPLES 30/31 — The two inputs have independent filters/windows.
            // A left join retains unmatched visible rows; an inner join removes
            // them. The server binds the same signed actor to BOTH inputs.
            foreach (var leftJoin in new[] { true, false })
            {
                await ShowAsync(leftJoin
                    ? "30. Left join / unmatched presence and exact Int64"
                    : "31. Inner join / matching pairs only", new
                {
                    provider,
                    kind = leftJoin ? "Left" : "Inner",

                    left = new { filters = scalarFilters },
                    right = new
                    {
                        filters = scalarFilters.Concat(new object[]
                        {
                            new { field = "Name", @operator = "Equal", value = "C-public" }
                        }).ToArray()
                    },

                    keys = new[]
                    {
                        new { alias = "name", left = new { field = "Name" }, right = new { field = "Name" } }
                    },

                    columns = new object[]
                    {
                        new { alias = "name", source = "Left", selection = new { field = "Name" } },
                        new
                        {
                            alias = "precision", source = "Right", allowMissing = true,
                            selection = new { metadataPath = new[] { "precision" }, valueType = "Int64" }
                        },
                        new { alias = "present", source = "RightPresence" }
                    },

                    maximumLeftRows = 100,
                    maximumRightRows = 100,
                    maximumPairs = 1000,
                    maximumResults = 25
                }, join: true, leftJoin: leftJoin);
            }
        }

        return completedRequests;

        async Task ShowAsync(
            string title,
            object query,
            bool scalar = false,
            string? valueType = null,
            object? expected = null,
            bool projection = false,
            bool grouped = false,
            bool orderedGroups = false,
            bool pagedGroups = false,
            bool dateProjection = false,
            bool dateGroups = false,
            bool unsignedProjection = false,
            bool unsignedGroups = false,
            bool decimalProjection = false,
            bool decimalGroups = false,
            bool doubleProjection = false,
            bool doubleGroups = false,
            bool singleProjection = false,
            bool singleGroups = false,
            bool join = false,
            bool leftJoin = false)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var body = JsonSerializer.Serialize(query, options);

            // One key per NEW logical request. Keep it if the outcome becomes
            // uncertain; do not silently repeat the request with a fresh key.
            var operationKey = Guid.NewGuid().ToString("D");

            var route = join ? "/api/data/query-cql/join" : grouped ? "/api/data/query-cql/grouped"
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
            if (join)
            {
                var payload = result.RootElement.GetProperty("result");
                var rows = payload.GetProperty("rows").EnumerateArray().ToArray();
                var expectedNames = leftJoin ? new[] { "B-owned", "C-public", "D-owned" } : new[] { "C-public" };

                if (payload.GetProperty("version").GetString() != "cql-join-v1"
                    || payload.GetProperty("columns")[1].GetProperty("valueType").GetString() != "Int64"
                    || !rows.Select(row => row.GetProperty("values")[0].GetString()).SequenceEqual(expectedNames))
                    throw new InvalidOperationException(title + " returned the wrong joined shape or identity.");

                foreach (var row in rows)
                {
                    var present = row.GetProperty("hasRight").GetBoolean();
                    var values = row.GetProperty("values");
                    if (present != values[2].GetBoolean()
                        || (present ? values[1].GetString() != "9007199254740993" : values[1].ValueKind != JsonValueKind.Null))
                        throw new InvalidOperationException(title + " lost exact values or unmatched presence.");
                }
            }

            if (scalar)
            {
                var payload = result.RootElement.GetProperty("result");
                if (payload.GetProperty("version").GetString() != "cql-scalar-v1"
                    || payload.GetProperty("valueType").GetString() != valueType
                    || payload.GetProperty("value").GetRawText() != JsonSerializer.Serialize(expected))
                    throw new InvalidOperationException(title + " returned the wrong scalar domain or value.");
            }

            if (projection && !dateProjection && !unsignedProjection && !decimalProjection && !doubleProjection && !singleProjection)
            {
                var payload = result.RootElement.GetProperty("result");
                var row = payload.GetProperty("rows").EnumerateArray().Single();

                if (payload.GetProperty("version").GetString() != "cql-projection-v1"
                    || payload.GetProperty("columns")[1].GetProperty("valueType").GetString() != "Int64"
                    || row[0].GetString() != "C-public" || row[1].GetString() != "9007199254740993"
                    || row[2].ValueKind != JsonValueKind.Null || row[3].GetString() != "London")
                    throw new InvalidOperationException(title + " returned the wrong selected shape or exact value.");
            }

            if (grouped && !orderedGroups && !dateGroups && !unsignedGroups && !decimalGroups && !doubleGroups && !singleGroups)
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

            if (dateProjection || dateGroups)
            {
                var payload = result.RootElement.GetProperty("result");
                var rows = payload.GetProperty("rows").EnumerateArray().ToArray();
                var dateIndex = dateGroups ? 0 : 1;
                var expectedDates = new[]
                {
                    "2026-10-24T00:00:00.0000002Z",
                    "2026-10-24T00:00:00.0000003Z"
                };

                if (payload.GetProperty("version").GetString() != (dateGroups ? "cql-grouped-v1" : "cql-projection-v1")
                    || payload.GetProperty("columns")[dateIndex].GetProperty("valueType").GetString() != "DateTime"
                    || !rows.Select(row => row[dateIndex].GetString()).SequenceEqual(expectedDates)
                    || (dateGroups ? rows.Any(row => row[1].GetString() != "1")
                        : !rows.Select(row => row[0].GetString()).SequenceEqual(new[] { "B-owned", "C-public" })))
                    throw new InvalidOperationException(title + " lost exact UTC date values, shape or visibility.");
            }


            if (unsignedProjection || unsignedGroups)
            {
                var payload = result.RootElement.GetProperty("result");
                var rows = payload.GetProperty("rows").EnumerateArray().ToArray();
                var counterIndex = unsignedGroups ? 0 : 1;
                var expectedCounters = unsignedGroups
                    ? new[] { "18446744073709551612", "18446744073709551613" }
                    : new[] { "18446744073709551613", "18446744073709551612" };

                if (payload.GetProperty("version").GetString() != (unsignedGroups ? "cql-grouped-v1" : "cql-projection-v1")
                    || payload.GetProperty("columns")[counterIndex].GetProperty("valueType").GetString() != "UInt64"
                    || !rows.Select(row => row[counterIndex].GetString()).SequenceEqual(expectedCounters)
                    || (unsignedGroups ? rows.Any(row => row[1].GetString() != "1")
                        : !rows.Select(row => row[0].GetString()).SequenceEqual(new[] { "B-owned", "C-public" })))
                    throw new InvalidOperationException(title + " lost exact unsigned values, shape or visibility.");
            }

            if (decimalProjection || decimalGroups)
            {
                var payload = result.RootElement.GetProperty("result");
                var rows = payload.GetProperty("rows").EnumerateArray().ToArray();
                var priceIndex = decimalGroups ? 0 : 1;
                var expectedPrices = new[]
                {
                    "1.0000000000000000000000000002",
                    "1.0000000000000000000000000003"
                };

                if (payload.GetProperty("version").GetString() != (decimalGroups ? "cql-grouped-v1" : "cql-projection-v1")
                    || payload.GetProperty("columns")[priceIndex].GetProperty("valueType").GetString() != "Decimal"
                    || !rows.Select(row => row[priceIndex].GetString()).SequenceEqual(expectedPrices)
                    || (decimalGroups ? rows.Any(row => row[1].GetString() != "1")
                        : !rows.Select(row => row[0].GetString()).SequenceEqual(new[] { "B-owned", "C-public" })))
                    throw new InvalidOperationException(title + " lost exact Decimal prices, shape or visibility.");
            }

            if (doubleProjection || doubleGroups)
            {
                var payload = result.RootElement.GetProperty("result");
                var rows = payload.GetProperty("rows").EnumerateArray().ToArray();
                var readingIndex = doubleGroups ? 0 : 1;
                var expectedReadings = new[]
                {
                    "1.0000000000000004",
                    "1.0000000000000007"
                };

                if (payload.GetProperty("version").GetString() != (doubleGroups ? "cql-grouped-v1" : "cql-projection-v1")
                    || payload.GetProperty("columns")[readingIndex].GetProperty("valueType").GetString() != "Double"
                    || !rows.Select(row => row[readingIndex].GetString()).SequenceEqual(expectedReadings)
                    || (doubleGroups ? rows.Any(row => row[1].GetString() != "1")
                        : !rows.Select(row => row[0].GetString()).SequenceEqual(new[] { "B-owned", "C-public" })))
                    throw new InvalidOperationException(title + " lost exact Double readings, shape or visibility.");
            }
            if (singleProjection || singleGroups)
            {
                var payload = result.RootElement.GetProperty("result");
                var rows = payload.GetProperty("rows").EnumerateArray().ToArray();
                var readingIndex = singleGroups ? 0 : 1;
                var expectedReadings = new[]
                {
                    "1.0000002",
                    "1.0000004"
                };

                if (payload.GetProperty("version").GetString() != (singleGroups ? "cql-grouped-v1" : "cql-projection-v1")
                    || payload.GetProperty("columns")[readingIndex].GetProperty("valueType").GetString() != "Single"
                    || !rows.Select(row => row[readingIndex].GetString()).SequenceEqual(expectedReadings)
                    || (singleGroups ? rows.Any(row => row[1].GetString() != "1")
                        : !rows.Select(row => row[0].GetString()).SequenceEqual(new[] { "B-owned", "C-public" })))
                    throw new InvalidOperationException(title + " lost exact Single readings, shape or visibility.");
            }

            completedRequests++;
            await output.WriteLineAsync(JsonSerializer.Serialize(result.RootElement, options));
            await output.WriteLineAsync("\nSettlement: " + response.Headers.GetValues("X-OASIS-Usage-Settlement").Single() + "\n");
        }
    }
}
