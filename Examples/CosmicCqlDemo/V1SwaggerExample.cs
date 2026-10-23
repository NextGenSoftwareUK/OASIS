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
