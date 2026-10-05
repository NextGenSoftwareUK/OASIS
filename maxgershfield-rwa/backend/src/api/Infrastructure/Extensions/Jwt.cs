namespace Infrastructure.Extensions;

/// <summary>
/// Extension methods for generating JWT tokens in the context of user authentication.
/// </summary>
public static class Jwt
{
    /// <summary>
    /// Generates a JWT token for a given user asynchronously.
    /// </summary>
    /// <param name="dbContext">The database context to retrieve user roles from.</param>
    /// <param name="user">The user for whom the token will be generated.</param>
    /// <param name="config">The application configuration containing JWT settings (key, issuer, audience).</param>
    /// <returns>Result containing the generated JWT token and its expiration information.</returns>
    /// <exception cref="InvalidOperationException">Thrown if JWT settings (key, issuer, or audience) are missing in the configuration.</exception>
    public static async Task<Result<LoginResponse>> GenerateTokenAsync(
        this DataContext dbContext,
        User user,
        IConfiguration config)
    {
        const string jwtKey = "Jwt:key";
        const string jwtIssuer = "Jwt:issuer";
        const string jwtAudience = "Jwt:audience";
        const string jwtExpires = "Jwt:expires";

        string key = config.GetRequiredString(jwtKey);
        string issuer = config.GetRequiredString(jwtIssuer);
        string audience = config.GetRequiredString(jwtAudience);
        int expires = config.GetRequiredInt(jwtExpires);

        SigningCredentials credentials = new(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(CustomClaimTypes.Id, user.Id.ToString()),
            new(CustomClaimTypes.UserName, user.UserName),
            new(CustomClaimTypes.Email, user.Email),
            new(CustomClaimTypes.Phone, user.PhoneNumber),
            new(CustomClaimTypes.FirstName, user.FirstName ?? ""),
            new(CustomClaimTypes.LastName, user.LastName ?? ""),
            new(CustomClaimTypes.TokenVersion, user.TokenVersion.ToString()),
        ];

        claims.AddRange(await dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => new Claim(CustomClaimTypes.Role, ur.Role.Name))
            .AsNoTracking()
            .ToListAsync());

        DateTime current = DateTime.UtcNow;


        JwtSecurityToken jwt = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: current.AddMinutes(expires),
            signingCredentials: credentials);

        return Result<LoginResponse>.Success(new(
            new JwtSecurityTokenHandler().WriteToken(jwt),
            current,
            current.AddMinutes(expires)));
    }
}