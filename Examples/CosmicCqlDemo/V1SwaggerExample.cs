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
            // Keep keys and aggregate descriptions separate. Each aggregate
            // selects an exact input domain; Average returns Decimal text.
            OpenApiObject Aggregate(string alias, string operation) => new()
            {
                ["alias"] = new OpenApiString(alias),
                ["operation"] = new OpenApiString(operation),
                ["selection"] = new OpenApiObject
                {
                    ["metadataPath"] = new OpenApiArray { new OpenApiString("extremum") },
                    ["valueType"] = new OpenApiString("Int64")
                }
            };

            schema.Example = new OpenApiObject
            {
                ["provider"] = new OpenApiString("MongoDBOASIS"),
                ["take"] = new OpenApiInteger(2),
                ["maximumGroups"] = new OpenApiInteger(10),
                ["resultSkip"] = new OpenApiInteger(0),
                ["resultTake"] = new OpenApiInteger(1),
                ["resultOrder"] = new OpenApiArray
                {
                    new OpenApiObject { ["alias"] = new OpenApiString("Total"), ["descending"] = new OpenApiBoolean(true) },
                    new OpenApiObject { ["alias"] = new OpenApiString("Stock") }
                },

                ["keys"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["alias"] = new OpenApiString("stock"),
                        ["metadataPath"] = new OpenApiArray { new OpenApiString("stock") },
                        ["valueType"] = new OpenApiString("Int64")
                    }
                },
                ["columns"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["alias"] = new OpenApiString("Stock"),
                        ["keyAlias"] = new OpenApiString("stock")
                    },
                    new OpenApiObject
                    {
                        ["alias"] = new OpenApiString("Items"),
                        ["operation"] = new OpenApiString("LongCount")
                    },
                    Aggregate("Total", "Sum"),
                    Aggregate("Mean", "Average")
                },

                // Copy the live tag from /demo/session; never scan another
                // session's fixture or application data for this walkthrough.
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
