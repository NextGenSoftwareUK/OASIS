#if OASIS_SESSION_MIDDLEWARE_CHECK
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.DNA;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;

internal static class SessionMiddlewareHttpQualification
{
    private static readonly string[] Services = { "WEB4", "WEB5", "WEB6", "WEB7", "WEB8", "WEB9", "WEB10" };
    private static readonly HashSet<string> Accepted = new(StringComparer.Ordinal);

    public static void MapBranches(WebApplication app, Guid actor)
    {
        Accepted.Clear();
        foreach (var service in Services)
        {
            app.Map("/__access_session/" + service, branch =>
            {
                branch.Use(next => service switch
                {
                    "WEB4" => new NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware.JwtMiddleware(next).Invoke,
                    "WEB5" => new NextGenSoftware.OASIS.STAR.WebAPI.Middleware.JwtMiddleware(next).Invoke,
                    "WEB6" => new NextGenSoftware.OASIS.Web6.WebAPI.Middleware.JwtMiddleware(next).Invoke,
                    "WEB7" => new NextGenSoftware.OASIS.Web7.WebAPI.Middleware.JwtMiddleware(next).Invoke,
                    "WEB8" => new NextGenSoftware.OASIS.Web8.WebAPI.Middleware.JwtMiddleware(next).Invoke,
                    "WEB9" => new NextGenSoftware.OASIS.Web9.WebAPI.Middleware.JwtMiddleware(next).Invoke,
                    "WEB10" => new NextGenSoftware.OASIS.Web10.WebAPI.Middleware.JwtMiddleware(next).Invoke,
                    _ => throw new InvalidOperationException("Unreviewed middleware service.")
                });
                // Observe the real middleware's identity through the framework's actual user policy.
                // This probe is test-only; it is not a substitute shipping controller or auth handler.
                branch.Run(async context =>
                {
                    var authorization = context.RequestServices.GetRequiredService<IAuthorizationService>();
                    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                    var permitted = await authorization.AuthorizeAsync(context.User, null, policy);
                    if (!permitted.Succeeded)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new { authenticated = false });
                        return;
                    }
                    var avatar = context.Items["Avatar"] as IAvatar;
                    if (avatar?.Id != actor || OASISRequestContext.CurrentAvatarId != actor ||
                        OASISRequestContext.CurrentAvatar?.Id != actor || context.User.FindFirst("sub")?.Value != actor.ToString("D") ||
                        avatar.Password != null || avatar.RefreshTokens != null || avatar.RefreshToken != null || avatar.JwtToken != null)
                        throw new InvalidOperationException("Middleware published incomplete, inconsistent or unredacted session state.");
                    await context.Response.WriteAsJsonAsync(new { authenticated = true, avatarId = avatar.Id, service });
                });
            });
        }
    }

    public static async Task ProbeAsync(HttpClient http, string stage, JToken credentials, bool active, Guid actor, OASISDNA dna)
    {
        if (stage == "independent-issued")
        {
            var original = new JwtSecurityTokenHandler().ReadJwtToken(credentials.Value<string>("jwtToken"));
            var registered = new HashSet<string>(new[] { "iss", "aud", "exp", "nbf", "iat" }, StringComparer.Ordinal);
            var claims = original.Claims.Where(claim => !registered.Contains(claim.Type) && claim.Type != "sub")
                .Concat(new[] { new Claim("sub", Guid.NewGuid().ToString("D")) });
            var inconsistent = new JwtSecurityToken(original.Issuer, original.Audiences.Single(), claims,
                expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.ASCII.GetBytes(dna.OASIS.Security.SecretKey)), SecurityAlgorithms.HmacSha256));
            var malformed = new JObject { ["jwtToken"] = new JwtSecurityTokenHandler().WriteToken(inconsistent) };
            await ProbeAsync(http, "inconsistent-subject", malformed, false, actor, dna);
        }
        foreach (var service in Services)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/__access_session/" + service);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.Value<string>("jwtToken"));
            using var response = await http.SendAsync(request);
            var expected = active ? HttpStatusCode.OK : HttpStatusCode.Unauthorized;
            if (response.StatusCode != expected)
                throw new InvalidOperationException("Unexpected middleware status at " + service + "/" + stage + ": " + (int)response.StatusCode);
            if (active)
            {
                var body = JObject.Parse(await response.Content.ReadAsStringAsync());
                if (body.Value<bool>("authenticated") != true || body.Value<Guid>("avatarId") != actor || body.Value<string>("service") != service)
                    throw new InvalidOperationException("Actual middleware did not establish the expected native identity.");
            }
            if (!Accepted.Add(service + "/" + stage))
                throw new InvalidOperationException("Middleware cohort was repeated unexpectedly.");
        }
    }

    public static void VerifyComplete()
    {
        var stages = new[] { "independent-issued", "rotated-credential", "revoked-credential", "independent-remains-active", "inconsistent-subject" };
        if (Accepted.Count != Services.Length * stages.Length || Services.Any(service => stages.Any(stage => !Accepted.Contains(service + "/" + stage))))
            throw new InvalidOperationException("Seven-service native middleware coverage is incomplete.");
        Console.WriteLine("SESSION_MIDDLEWARE_HTTP_NATIVE_PROOF: 35 real loopback HTTP checks across WEB4-WEB10 middleware, signed native credentials and framework user policy; active/rotation/revoke/independent access and inconsistent-subject refusal. Not full Startup/global logout/browser/hosted or cross-provider failover acceptance.");
    }
}
#endif
