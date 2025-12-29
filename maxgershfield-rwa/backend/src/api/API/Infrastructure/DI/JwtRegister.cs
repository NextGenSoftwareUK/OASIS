namespace API.Infrastructure.DI;

/// <summary>
/// This class registers JWT authentication and authorization services within the application's DI container.
/// It configures the authentication middleware to validate and process JWT tokens in HTTP requests.
/// </summary>
public static class JwtRegister
{
    /// <summary>
    /// Registers JWT authentication and authorization services in the DI container.
    /// This method configures the JWT authentication scheme, including validation parameters such as
    /// issuer, audience, signing key, and token lifetime.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance to register services with.</param>
    /// <returns>The WebApplicationBuilder instance with JWT authentication services registered.</returns>
    public static WebApplicationBuilder AddJwtService(this WebApplicationBuilder builder)
    {
        string? jwtKey = builder.Configuration["Jwt:key"];
        string? issuer = builder.Configuration["Jwt:issuer"];
        string? audience = builder.Configuration["Jwt:audience"];

        if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            throw new InvalidOperationException("JWT key, issuer, or audience is missing in configuration.");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }
}