using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CosmicCqlDemo;

// Prefill the actual shipping request DTO, so Try it out is useful immediately.
public sealed class V1SwaggerExample : ISchemaFilter, IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType?.Name != "CqlV1Controller") return;

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key", In = ParameterLocation.Header, Required = true,
            Description = "Use a new key for a new intent. Repeating the same key demonstrates no-replay HTTP 409.",
            Schema = new OpenApiSchema { Type = "string" },
            Example = new OpenApiString("swagger-demo-intent-1")
        });

        // Named examples keep the existing UTC request while also exposing the
        // UInt64 contract. Digits remain strings in Swagger's editable payload.
        var grouped = context.MethodInfo.GetParameters().Any(p => p.ParameterType.Name == "CqlGroupedV1Request");
        var projection = context.MethodInfo.GetParameters().Any(p => p.ParameterType.Name == "CqlProjectionV1Request");
        if (operation.RequestBody != null && (grouped || projection))
        {
            foreach (var media in operation.RequestBody.Content.Values)
            {
                var schemaId = media.Schema.Reference?.Id;
                if (schemaId != null && context.SchemaRepository.Schemas.TryGetValue(schemaId, out var current)
                    && current.Example != null)
                    media.Examples["UtcDates"] = new OpenApiExample { Summary = "Exact UTC dates", Value = current.Example };

                media.Examples["ExactUInt64"] = new OpenApiExample
                {
                    Summary = grouped ? "UInt64 grouped keys and exact result filters" : "UInt64 source range and selected columns",
                    Value = ExactNumericExample(grouped, decimalPrices: false)
                };
                media.Examples["ExactDecimal"] = new OpenApiExample
                {
                    Summary = grouped ? "Exact Decimal price groups and alias filters" : "Exact Decimal price range and selected columns",
                    Value = ExactNumericExample(grouped, decimalPrices: true)
                };
                media.Examples["FiniteDouble"] = new OpenApiExample
                {
                    Summary = grouped ? "Finite Double reading groups and alias filters" : "Finite Double reading range and selected columns",
                    Value = ExactNumericExample(grouped, decimalPrices: false, doubleReadings: true)
                };
                media.Examples["FiniteSingle"] = new OpenApiExample
                {
                    Summary = grouped ? "Finite Single reading groups and alias filters" : "Finite Single reading range and selected columns",
                    Value = ExactNumericExample(grouped, decimalPrices: false, singleReadings: true)
                };
            }
        }
        var scalar = context.MethodInfo.GetParameters().Any(p => p.ParameterType.Name == "CqlScalarV1Request");
        if (operation.RequestBody != null && (grouped || scalar))
        {
            foreach (var media in operation.RequestBody.Content.Values)
            {
                // Match walkthrough examples 41–46. Source Take selects inputs;
                // grouped resultTake pages the rounded aggregate results.
                foreach (var floatingType in new[] { "Single", "Double" })
                {
                    media.Examples[floatingType + (grouped ? "TotalsAndMeans" : "Average")] = new OpenApiExample
                    {
                        Summary = floatingType + " nullable mean with one final ties-to-even round",
                        Value = NumericAggregateExample(grouped, floatingType: floatingType)
                    };

                    if (scalar)
                        media.Examples[floatingType + "Sum"] = new OpenApiExample
                        {
                            Summary = floatingType + " total with exact binary accumulation",
                            Value = NumericAggregateExample(false, average: false, floatingType: floatingType)
                        };
                }

                media.Examples[grouped ? "DecimalTotalsAndMeans" : "DecimalAverage"] = new OpenApiExample
                {
                    Summary = grouped ? "Exact Decimal totals and rounded means with result filters" : "Decimal Average with final ties-to-even rounding",
                    Value = NumericAggregateExample(grouped, decimalPrices: true)
                };
                if (scalar)
                    media.Examples["DecimalSum"] = new OpenApiExample
                    {
                        Summary = "Exact Decimal price total",
                        Value = NumericAggregateExample(grouped: false, average: false, decimalPrices: true)
                    };
                media.Examples["UInt64Average"] = new OpenApiExample
                {
                    Summary = grouped ? "Grouped UInt64 input with exact Decimal mean" : "UInt64 input with exact Decimal average",
                    Value = NumericAggregateExample(grouped)
                };
                if (scalar)
                    media.Examples["UInt64Sum"] = new OpenApiExample
                    {
                        Summary = "UInt64 Sum as exact tagged text",
                        Value = NumericAggregateExample(grouped: false, average: false)
                    };
            }
        }
    }

    private static OpenApiObject NumericAggregateExample(bool grouped, bool average = true, bool decimalPrices = false, string? floatingType = null)
    {
        var floating = floatingType != null;
        var single = floatingType == "Single";
        var path = floating ? single ? "singleReading" : "reading" : decimalPrices ? "price" : "counter";
        var valueType = floatingType ?? (decimalPrices ? "Decimal" : "UInt64");

        var selection = new OpenApiObject
        {
            ["metadataPath"] = new OpenApiArray { new OpenApiString(path) },
            ["valueType"] = new OpenApiString(valueType)
        };
        if (floating)
            selection["nullable"] = new OpenApiBoolean(average);

        var request = new OpenApiObject
        {
            ["provider"] = new OpenApiString("MongoDBOASIS"),
            ["filters"] = new OpenApiArray { new OpenApiObject
            {
                ["metadataPath"] = new OpenApiArray { new OpenApiString("fixture") },
                ["scalar"] = new OpenApiString("String"),
                ["operator"] = new OpenApiString("Equal"),
                ["value"] = new OpenApiString("REPLACE_WITH_DEMO_FIXTURE_TAG")
            } },
            ["orderBy"] = new OpenApiString("Name"),
            ["take"] = new OpenApiInteger(average || decimalPrices || floating ? 2 : 1)
        };
        if (grouped)
        {
            request["keys"] = new OpenApiArray { new OpenApiObject
            {
                ["alias"] = new OpenApiString("stock"),
                ["metadataPath"] = new OpenApiArray { new OpenApiString("stock") },
                ["valueType"] = new OpenApiString("Int64")
            } };
            request["columns"] = new OpenApiArray { new OpenApiObject
            {
                ["alias"] = new OpenApiString("Mean"),
                ["operation"] = new OpenApiString("Average"),
                ["selection"] = selection
            } };
            request["resultOrder"] = new OpenApiArray { new OpenApiObject
            {
                ["alias"] = new OpenApiString("Mean"), ["descending"] = new OpenApiBoolean(true)
            } };
            request["resultTake"] = new OpenApiInteger(1);
            if (decimalPrices || floating)
            {
                // Keep Sum required and Average nullable in the floating example.
                // Separate objects prevent mutating the Average selection by alias.
                var sumSelection = floating ? new OpenApiObject
                {
                    ["metadataPath"] = new OpenApiArray { new OpenApiString(path) },
                    ["valueType"] = new OpenApiString(valueType),
                    ["nullable"] = new OpenApiBoolean(false)
                } : selection;
                ((OpenApiArray)request["columns"]).Insert(0, new OpenApiObject
                {
                    ["alias"] = new OpenApiString("Total"),
                    ["operation"] = new OpenApiString("Sum"),
                    ["selection"] = sumSelection
                });
                request["resultFilters"] = new OpenApiArray
                {
                    new OpenApiObject { ["alias"] = new OpenApiString("Total"), ["operator"] = new OpenApiString("Equal"),
                        ["value"] = new OpenApiString(floating ? single ? "2.0000005" : "2.000000000000001" : "2.0000000000000000000000000005") },
                    new OpenApiObject { ["alias"] = new OpenApiString("Mean"), ["operator"] = new OpenApiString("Equal"),
                        ["value"] = new OpenApiString(floating ? single ? "1.0000002" : "1.0000000000000004" : "1.0000000000000000000000000002") }
                };
                ((OpenApiObject)((OpenApiArray)request["resultOrder"])[0])["alias"] = new OpenApiString(floating ? "Mean" : "Total");
            }
        }
        else
        {
            request["operation"] = new OpenApiString(average ? "Average" : "Sum");
            request["selection"] = selection;
        }
        return request;
    }

    private static OpenApiObject ExactNumericExample(bool grouped, bool decimalPrices, bool doubleReadings = false, bool singleReadings = false)
    {
        // One shape owner keeps source and selected-result examples consistent.
        var scalar = singleReadings ? "Single" : doubleReadings ? "Double" : decimalPrices ? "Decimal" : "UInt64";
        var path = singleReadings ? "singleReading" : doubleReadings ? "reading" : decimalPrices ? "price" : "counter";
        var alias = singleReadings ? "Reading" : doubleReadings ? "Reading" : decimalPrices ? "Price" : "Counter";
        var minimum = singleReadings ? "1.0000002" : doubleReadings ? "1.0000000000000004" : decimalPrices ? "1.0000000000000000000000000002" : "18446744073709551612";
        var maximum = singleReadings ? "1.0000005" : doubleReadings ? "1.0000000000000009" : decimalPrices ? "1.0000000000000000000000000004" : "18446744073709551614";

        OpenApiObject Filter(string scalarType, string metadataKey, string operation, string value) => new()
        {
            ["metadataPath"] = new OpenApiArray { new OpenApiString(metadataKey) },
            ["scalar"] = new OpenApiString(scalarType),
            ["operator"] = new OpenApiString(operation),
            ["value"] = new OpenApiString(value)
        };
        var filters = new OpenApiArray
        {
            Filter("String", "fixture", "Equal", "REPLACE_WITH_DEMO_FIXTURE_TAG")
        };
        var body = new OpenApiObject
        {
            ["provider"] = new OpenApiString("MongoDBOASIS"),
            ["filters"] = filters
        };
        if (grouped)
        {
            body["maximumGroups"] = new OpenApiInteger(10);
            body["keys"] = new OpenApiArray
            {
                new OpenApiObject { ["alias"] = new OpenApiString(path),
                    ["metadataPath"] = new OpenApiArray { new OpenApiString(path) }, ["valueType"] = new OpenApiString(scalar) }
            };
            body["columns"] = new OpenApiArray
            {
                new OpenApiObject { ["alias"] = new OpenApiString(alias), ["keyAlias"] = new OpenApiString(path) },
                new OpenApiObject { ["alias"] = new OpenApiString("Items"), ["operation"] = new OpenApiString("LongCount") }
            };
            body["resultFilters"] = new OpenApiArray
            {
                new OpenApiObject { ["alias"] = new OpenApiString(alias), ["operator"] = new OpenApiString("GreaterThanOrEqual"), ["value"] = new OpenApiString(minimum) },
                new OpenApiObject { ["alias"] = new OpenApiString(alias), ["operator"] = new OpenApiString("LessThan"), ["value"] = new OpenApiString(maximum) }
            };
            body["resultOrder"] = new OpenApiArray { new OpenApiObject { ["alias"] = new OpenApiString(alias), ["descending"] = new OpenApiBoolean(false) } };
            body["resultTake"] = new OpenApiInteger(2);
        }
        else
        {
            filters.Add(Filter(scalar, path, "GreaterThanOrEqual", minimum));
            filters.Add(Filter(scalar, path, "LessThan", maximum));
            body["orderBy"] = new OpenApiString("Name");
            body["take"] = new OpenApiInteger(2);
            body["columns"] = new OpenApiArray
            {
                new OpenApiObject { ["alias"] = new OpenApiString("Name"), ["field"] = new OpenApiString("Name") },
                new OpenApiObject { ["alias"] = new OpenApiString(alias), ["metadataPath"] = new OpenApiArray { new OpenApiString(path) }, ["valueType"] = new OpenApiString(scalar) }
            };
        }
        return body;
    }

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.Name == "CqlCompositionV1Request")
        {
            schema.Example = new OpenApiObject
            {
                ["provider"] = new OpenApiString("MongoDBOASIS"),
                ["source"] = new OpenApiObject
                {
                    ["filters"] = new OpenApiArray { new OpenApiObject
                    {
                        ["metadataPath"] = new OpenApiArray { new OpenApiString("fixture") },
                        ["scalar"] = new OpenApiString("String"), ["operator"] = new OpenApiString("Equal"),
                        ["value"] = new OpenApiString("REPLACE_WITH_DEMO_FIXTURE_TAG")
                    } },
                    ["orderBy"] = new OpenApiString("Name"), ["skip"] = new OpenApiInteger(1), ["take"] = new OpenApiInteger(2),
                    ["columns"] = new OpenApiArray
                    {
                        new OpenApiObject { ["alias"] = new OpenApiString("Name"), ["field"] = new OpenApiString("Name") },
                        new OpenApiObject { ["alias"] = new OpenApiString("Precision"),
                            ["metadataPath"] = new OpenApiArray { new OpenApiString("precision") }, ["valueType"] = new OpenApiString("Int64") }
                    }
                },
                ["stages"] = new OpenApiArray
                {
                    new OpenApiObject { ["operation"] = new OpenApiString("Where"), ["filters"] = new OpenApiArray
                    {
                        new OpenApiObject { ["alias"] = new OpenApiString("Name"), ["operator"] = new OpenApiString("NotEqual"), ["value"] = new OpenApiString("C-public") }
                    } },
                    new OpenApiObject { ["operation"] = new OpenApiString("Take"), ["count"] = new OpenApiInteger(1) }
                }
            };
            return;
        }

        if (context.Type.Name == "CqlJoinV1Request")
        {
            // Copy the live /demo/session tag into BOTH independent sources.
            // The signed actor is supplied by WEB4, never by the request body.
            OpenApiObject FixtureFilter() => new()
            {
                ["metadataPath"] = new OpenApiArray { new OpenApiString("fixture") },
                ["scalar"] = new OpenApiString("String"),
                ["operator"] = new OpenApiString("Equal"),
                ["value"] = new OpenApiString("REPLACE_WITH_DEMO_FIXTURE_TAG")
            };
            OpenApiObject NamePath() => new() { ["field"] = new OpenApiString("Name") };

            schema.Example = new OpenApiObject
            {
                ["provider"] = new OpenApiString("MongoDBOASIS"),
                ["kind"] = new OpenApiString("Left"),
                ["left"] = new OpenApiObject { ["filters"] = new OpenApiArray { FixtureFilter() } },
                ["right"] = new OpenApiObject
                {
                    ["filters"] = new OpenApiArray
                    {
                        FixtureFilter(),
                        new OpenApiObject { ["field"] = new OpenApiString("Name"),
                            ["operator"] = new OpenApiString("Equal"), ["value"] = new OpenApiString("C-public") }
                    }
                },
                ["keys"] = new OpenApiArray
                {
                    new OpenApiObject { ["alias"] = new OpenApiString("name"), ["left"] = NamePath(), ["right"] = NamePath() }
                },
                ["columns"] = new OpenApiArray
                {
                    new OpenApiObject { ["alias"] = new OpenApiString("name"), ["source"] = new OpenApiString("Left"), ["selection"] = NamePath() },
                    new OpenApiObject
                    {
                        ["alias"] = new OpenApiString("precision"), ["source"] = new OpenApiString("Right"),
                        ["allowMissing"] = new OpenApiBoolean(true),
                        ["selection"] = new OpenApiObject { ["metadataPath"] = new OpenApiArray { new OpenApiString("precision") },
                            ["valueType"] = new OpenApiString("Int64") }
                    },
                    new OpenApiObject { ["alias"] = new OpenApiString("present"), ["source"] = new OpenApiString("RightPresence") }
                },
                ["maximumResults"] = new OpenApiInteger(25)
            };
            return;
        }

        if (context.Type.Name == "CqlGroupedV1Request")
        {
            // Date keys keep exact UTC identity. These selected-result bounds
            // are independent of source ordering and participation.
            schema.Example = new OpenApiObject
            {
                ["provider"] = new OpenApiString("MongoDBOASIS"),
                ["maximumGroups"] = new OpenApiInteger(10),
                ["resultTake"] = new OpenApiInteger(2),
                ["keys"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["alias"] = new OpenApiString("observed"),
                        ["metadataPath"] = new OpenApiArray { new OpenApiString("observedAt") },
                        ["valueType"] = new OpenApiString("DateTime")
                    }
                },
                ["columns"] = new OpenApiArray
                {
                    new OpenApiObject { ["alias"] = new OpenApiString("ObservedAt"), ["keyAlias"] = new OpenApiString("observed") },
                    new OpenApiObject { ["alias"] = new OpenApiString("Items"), ["operation"] = new OpenApiString("LongCount") }
                },
                ["resultFilters"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["alias"] = new OpenApiString("ObservedAt"),
                        ["operator"] = new OpenApiString("GreaterThanOrEqual"),
                        ["value"] = new OpenApiString("2026-10-24T00:00:00.0000002Z")
                    },
                    new OpenApiObject
                    {
                        ["alias"] = new OpenApiString("ObservedAt"),
                        ["operator"] = new OpenApiString("LessThan"),
                        ["value"] = new OpenApiString("2026-10-24T00:00:00.0000004Z")
                    }
                },
                ["resultOrder"] = new OpenApiArray
                {
                    new OpenApiObject { ["alias"] = new OpenApiString("ObservedAt"), ["descending"] = new OpenApiBoolean(false) }
                },
                // Copy the live tag from /demo/session. Never query another
                // session's fixture or unrelated application data in the demo.
                ["filters"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["metadataPath"] = new OpenApiArray { new OpenApiString("fixture") },
                        ["scalar"] = new OpenApiString("String"),
                        ["operator"] = new OpenApiString("Equal"),
                        ["value"] = new OpenApiString("REPLACE_WITH_DEMO_FIXTURE_TAG")
                    }
                }
            };
            return;
        }

        if (context.Type.Name == "CqlScalarV1Request")
        {
            // Average uses an ordered two-row window over exact Int64 input.
            // The response tags Decimal and returns exact decimal text.
            schema.Example = new OpenApiObject
            {
                ["provider"] = new OpenApiString("MongoDBOASIS"),
                ["operation"] = new OpenApiString("Average"),
                ["orderBy"] = new OpenApiString("Name"),
                ["take"] = new OpenApiInteger(2),
                ["selection"] = new OpenApiObject
                {
                    ["metadataPath"] = new OpenApiArray { new OpenApiString("extremum") },
                    ["valueType"] = new OpenApiString("Int64")
                },
                ["filters"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["metadataPath"] = new OpenApiArray { new OpenApiString("location"), new OpenApiString("city") },
                        ["scalar"] = new OpenApiString("String"),
                        ["operator"] = new OpenApiString("Equal"),
                        ["value"] = new OpenApiString("London")
                    }
                }
            };
            return;
        }

        if (context.Type.Name == "CqlProjectionV1Request")
        {
            // Select only native scalar columns; Int64 remains exact JSON text.
            schema.Example = new OpenApiObject
            {
                ["provider"] = new OpenApiString("MongoDBOASIS"),
                ["orderBy"] = new OpenApiString("Name"),
                ["take"] = new OpenApiInteger(3),
                ["filters"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["metadataPath"] = new OpenApiArray { new OpenApiString("location"), new OpenApiString("city") },
                        ["scalar"] = new OpenApiString("String"), ["operator"] = new OpenApiString("Equal"),
                        ["value"] = new OpenApiString("London")
                    },
                    new OpenApiObject
                    {
                        ["metadataPath"] = new OpenApiArray { new OpenApiString("observedAt") },
                        ["scalar"] = new OpenApiString("DateTime"),
                        ["operator"] = new OpenApiString("GreaterThanOrEqual"),
                        ["value"] = new OpenApiString("2026-10-24T00:00:00.0000002Z")
                    },
                    new OpenApiObject
                    {
                        ["metadataPath"] = new OpenApiArray { new OpenApiString("observedAt") },
                        ["scalar"] = new OpenApiString("DateTime"),
                        ["operator"] = new OpenApiString("LessThan"),
                        ["value"] = new OpenApiString("2026-10-24T00:00:00.0000004Z")
                    }
                },
                ["columns"] = new OpenApiArray
                {
                    new OpenApiObject { ["alias"] = new OpenApiString("label"), ["field"] = new OpenApiString("Name") },
                    new OpenApiObject
                    {
                        ["alias"] = new OpenApiString("precision"),
                        ["metadataPath"] = new OpenApiArray { new OpenApiString("precision") },
                        ["valueType"] = new OpenApiString("Int64")
                    },
                    new OpenApiObject
                    {
                        ["alias"] = new OpenApiString("ObservedAt"),
                        ["metadataPath"] = new OpenApiArray { new OpenApiString("observedAt") },
                        ["valueType"] = new OpenApiString("DateTime")
                    }
                }
            };
            return;
        }

        if (context.Type.Name != "CqlV1Request") return;

        schema.Example = new OpenApiObject
        {
            ["provider"] = new OpenApiString("MongoDBOASIS"),
            ["filters"] = new OpenApiArray
            {
                new OpenApiObject
                {
                    ["metadataPath"] = new OpenApiArray { new OpenApiString("location"), new OpenApiString("city") },
                    ["scalar"] = new OpenApiString("String"),
                    ["operator"] = new OpenApiString("Equal"),
                    ["value"] = new OpenApiString("London")
                }
            },
            ["orderBy"] = new OpenApiString("Name"),
            ["skip"] = new OpenApiInteger(0),
            ["take"] = new OpenApiInteger(3),
            ["anyChildren"] = new OpenApiArray
            {
                new OpenApiObject
                {
                    ["filters"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["field"] = new OpenApiString("Name"),
                            ["operator"] = new OpenApiString("Equal"),
                            ["value"] = new OpenApiString("public child")
                        }
                    }
                }
            },
            ["anyDescendants"] = new OpenApiArray
            {
                new OpenApiObject
                {
                    ["maximumDepth"] = new OpenApiInteger(2),
                    ["maximumNodes"] = new OpenApiInteger(2),
                    ["filters"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["field"] = new OpenApiString("Name"),
                            ["operator"] = new OpenApiString("Equal"),
                            ["value"] = new OpenApiString("owned child")
                        }
                    }
                }
            },
            ["graph"] = new OpenApiObject
            {
                ["maxChildDepth"] = new OpenApiInteger(2),
                ["maximumNodes"] = new OpenApiInteger(256),
                ["childType"] = new OpenApiString("All"),
                ["consistency"] = new OpenApiString("ProviderDefault")
            }
        };
    }
}
