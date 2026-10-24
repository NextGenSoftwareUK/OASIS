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
    }

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
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
