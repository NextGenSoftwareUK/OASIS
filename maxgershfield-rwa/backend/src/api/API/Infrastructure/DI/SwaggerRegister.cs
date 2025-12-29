namespace API.Infrastructure.DI;

/// <summary>
/// This class provides an extension method for configuring Swagger services in a WebApplicationBuilder.
/// It enables the generation of Swagger documentation for the API, enhances route descriptions,
/// and configures security definitions (e.g., JWT Bearer token) to facilitate secure API consumption.
/// </summary>
public static class SwaggerRegister
{
    /// <summary>
    /// Adds Swagger services to the application, enabling Swagger UI and API documentation generation.
    /// It also configures a custom security definition for JWT Bearer token authorization, allowing users
    /// to interact with the API through a secure Bearer token.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance used to register services.</param>
    /// <returns>The WebApplicationBuilder instance to allow method chaining.</returns>
    public static WebApplicationBuilder AddSwaggerService(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => type.Name);
            options.ResolveConflictingActions(apiDescriptions =>
            {
                Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription first = apiDescriptions.First();
                Console.WriteLine($"Conflict in route Swagger: {first.RelativePath}");
                return first;
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                BearerFormat = "JWT",
                Scheme = "Bearer",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer 1234aaabbbccc\""
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Name = "Authorization",
                        In = ParameterLocation.Header
                    },
                    new List<string>()
                }
            });

            options.EnableAnnotations();
        });

        return builder;
    }
}