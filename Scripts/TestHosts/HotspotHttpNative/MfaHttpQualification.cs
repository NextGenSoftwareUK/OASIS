using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net.Http.Headers;
using NextGenSoftware.OASIS.API.Core.Managers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Mfa;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal static class MfaHttpQualification
{
    public static async Task RunAsync(HttpClient http, MongoDBOASIS provider, MfaService service,
        Func<DateTime> clock, Action advanceStep, OASISDNA dna, Guid actor, string username, string password,
        Func<string, JToken, bool, Task>? middlewareProbe = null)
    {
        async Task<JObject> Read(HttpResponseMessage response, string stage, bool errorExpected = false)
        {
            using (response)
            {
                var body = await response.Content.ReadAsStringAsync();
                Check(response.Content.Headers.ContentType?.MediaType == "application/json" && !string.IsNullOrWhiteSpace(body),
                    "MFA HTTP contract missing at " + stage + " (HTTP " + (int)response.StatusCode + ").");
                var envelope = JObject.Parse(body);
                var inner = envelope["result"] is JObject wrapped && wrapped["isError"] != null ? wrapped : envelope;
                Check(inner.Value<bool>("isError") == errorExpected, "Unexpected MFA result at " + stage + ".");
                if (!errorExpected) Check(response.IsSuccessStatusCode, "MFA HTTP failed at " + stage + ".");
                return inner;
            }
        }
        async Task<JObject> Post(string path, object body, string stage, bool errorExpected = false)
            => await Read(await http.PostAsJsonAsync(path, body), stage, errorExpected);
        var status = (await Read(await http.GetAsync("/api/avatar/mfa/status"), "initial-status"))["result"]!;
        Check(status.Value<bool>("nodeEnabled") && !status.Value<bool>("enabled"), "Owned MFA HTTP actor already enrolled or node disabled.");
        var enrolment = (await Post("/api/avatar/mfa/enrol", new { }, "enrol"))["result"]!;
        var secret = Totp.Base32Decode(enrolment.Value<string>("secret")!);
        try
        {
            // The host supplies the same supported clock used by the actual service.
            var pending = await service.StatusAsync(actor);
            Check(!pending.Enabled, "Enrollment activated before HTTP confirmation.");
            var code = Totp.Code(secret, Totp.Step(clock(), 30));
            var confirmed = (await Post("/api/avatar/mfa/confirm", new { code }, "confirm"))["result"]!;
            var recovery = confirmed["recoveryCodes"]!.ToObject<List<string>>()!;
            Check(recovery.Count >= 1, "HTTP confirmation did not return recovery codes.");
            var before = await provider.LoadAvatarAsync(actor);
            Check(!before.IsError && before.Result != null, "Owned MFA avatar unavailable.");
            var priorCount = before.Result.RefreshTokens?.Count ?? 0;
            http.DefaultRequestHeaders.Authorization = null;
            var challenge = (await Post("/api/avatar/authenticate", new { username, password }, "password-challenge"))["result"]!;
            Check(challenge["metaData"]?.Value<bool>(MfaService.RequiredKey) == true &&
                string.IsNullOrEmpty(challenge.Value<string>("jwtToken")) && string.IsNullOrEmpty(challenge.Value<string>("refreshToken")),
                "Password sign-in issued credentials before the required second factor.");
            var nativePending = await provider.LoadAvatarAsync(actor);
            Check(!nativePending.IsError && (nativePending.Result.RefreshTokens?.Count ?? 0) == priorCount,
                "Pending MFA sign-in persisted a refresh credential.");
            var token = challenge["metaData"]!.Value<string>(MfaService.ChallengeKey)!;
            advanceStep();
            var verified = (await Post("/api/avatar/authenticate-mfa", new {
                challengeToken = token, code = Totp.Code(secret, Totp.Step(clock(), 30)) }, "authenticator"))["result"]!;
            var verifiedPrincipal = VerifyCredentials(verified, dna, actor);
            if (middlewareProbe == null) await VerifyAccessAsync(verifiedPrincipal, actor, true);
            else await middlewareProbe("independent-issued", verified, true);
            await Post("/api/avatar/authenticate-mfa", new { challengeToken = token, recoveryCode = recovery[0] }, "consumed-challenge", true);
            var second = (await Post("/api/avatar/authenticate", new { username, password }, "recovery-challenge"))["result"]!;
            var recovered = (await Post("/api/avatar/authenticate-mfa", new {
                challengeToken = second["metaData"]!.Value<string>(MfaService.ChallengeKey), recoveryCode = recovery[0] }, "recovery"))["result"]!;
            var recoveredPrincipal = VerifyCredentials(recovered, dna, actor);
            if (middlewareProbe == null) await VerifyAccessAsync(recoveredPrincipal, actor, true);
            Check((await service.StatusAsync(actor)).RecoveryCodesLeft == recovery.Count - 1, "HTTP recovery use was not persisted.");
            var native = await provider.LoadAvatarAsync(actor);
            Check(!native.IsError && native.Result.RefreshTokens.Count == priorCount + 2,
                "Second-factor credential generations were not persisted.");
            Check(native.Result.RefreshTokens.Any(item => item.Token == verified.Value<string>("refreshToken")!.Split('.')[2] && item.IsActive) &&
                native.Result.RefreshTokens.Any(item => item.Token == recovered.Value<string>("refreshToken")!.Split('.')[2] && item.IsActive),
                "Issued access sessions do not match acknowledged native credentials.");
            // HyperDrive returns a signed provider-bound envelope, not the raw persisted token.
            // Let the shipped refresh endpoint validate that envelope and rotate its native credential.
            var rotated = (await Post("/api/avatar/refresh-token", new {
                refreshToken = recovered.Value<string>("refreshToken") }, "refresh-envelope"))["result"]!;
            var rotatedPrincipal = VerifyCredentials(rotated, dna, actor);
            if (middlewareProbe == null) await VerifyAccessAsync(rotatedPrincipal, actor, true);
            if (middlewareProbe == null) await VerifyAccessAsync(recoveredPrincipal, actor, false);
            else await middlewareProbe("rotated-credential", recovered, false);
            var afterRefresh = await provider.LoadAvatarAsync(actor);
            Check(!afterRefresh.IsError && afterRefresh.Result.RefreshTokens.Count == priorCount + 3 &&
                afterRefresh.Result.RefreshTokens.Count(item => item.Revoked.HasValue) == 1,
                "Actual refresh envelope did not rotate and persist its native credential.");
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rotated.Value<string>("jwtToken"));
            await Post("/api/avatar/revoke-token", new { token = rotated.Value<string>("refreshToken") }, "revoke-bound-session");
            if (middlewareProbe == null) await VerifyAccessAsync(rotatedPrincipal, actor, false);
            else await middlewareProbe("revoked-credential", rotated, false);
            if (middlewareProbe == null) await VerifyAccessAsync(verifiedPrincipal, actor, true);
            else await middlewareProbe("independent-remains-active", verified, true);
            http.DefaultRequestHeaders.Authorization = null;
            await Post("/api/avatar/refresh-token", new { refreshToken = rotated.Value<string>("refreshToken") }, "revoked-refresh", true);
            var afterRevoke = await provider.LoadAvatarAsync(actor);
            Check(!afterRevoke.IsError && afterRevoke.Result.RefreshTokens.Count == priorCount + 3 &&
                afterRevoke.Result.RefreshTokens.Count(item => item.Revoked.HasValue) == 2 &&
                afterRevoke.Result.Version == afterRefresh.Result.Version + 1 &&
                afterRevoke.Result.PreviousVersionId == afterRefresh.Result.VersionId &&
                afterRevoke.Result.Username == afterRefresh.Result.Username && afterRevoke.Result.Email == afterRefresh.Result.Email &&
                afterRevoke.Result.Password == afterRefresh.Result.Password,
                "Acknowledged session revocation changed unrelated account state or lost its native revision chain.");
            var missingBinding = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", actor.ToString("D")) }, "Bearer"));
            Check(!AvatarSessionIdentity.TryReadBinding(missingBinding, out _, out _), "Unbound access token accepted.");
            var duplicateBinding = new ClaimsPrincipal(new ClaimsIdentity(rotatedPrincipal.Claims.Concat(new[] {
                new Claim(AvatarSessionIdentity.SessionClaim, rotatedPrincipal.FindFirst(AvatarSessionIdentity.SessionClaim)!.Value) }), "Bearer"));
            Check(!AvatarSessionIdentity.TryReadBinding(duplicateBinding, out _, out _), "Duplicate session binding accepted.");
        }
        finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(secret); }
        Console.WriteLine("MFA_HTTP_NATIVE_PROOF: actual password challenge returned no credentials; authenticator/recovery HTTP issued signed JWT and persisted refresh generations; consumed challenge refused and actual provider-bound refresh rotated. Not browser/full Startup/hosted acceptance.");
        Console.WriteLine("ACCESS_SESSION_NATIVE_PROOF: authority-specific active access, rotation and explicit revoke invalidate matching access/refresh; independent credential remains active, revision/profile preserved, malformed bindings refused. Not seven-middleware/global logout acceptance.");
    }
    private static ClaimsPrincipal VerifyCredentials(JToken avatar, OASISDNA dna, Guid actor)
    {
        Check(Guid.TryParse(avatar.Value<string>("id"), out var returnedId) && returnedId == actor && !string.IsNullOrWhiteSpace(avatar.Value<string>("refreshToken")), "MFA HTTP returned incomplete credentials.");
        var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(avatar.Value<string>("jwtToken"),
            new TokenValidationParameters { ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(dna.OASIS.Security.SecretKey)),
                ValidateIssuer = true, ValidIssuer = "OASIS", ValidateAudience = true, ValidAudience = "OASIS",
                ValidateLifetime = true, RequireExpirationTime = true, ClockSkew = TimeSpan.Zero }, out _);
        Check(principal.FindFirst("sub")?.Value == actor.ToString(), "MFA HTTP JWT subject mismatch.");
        Check(string.IsNullOrEmpty(avatar.Value<string>("password")) && string.IsNullOrEmpty(avatar.Value<string>("verificationToken")) &&
            (avatar["refreshTokens"] == null || avatar["refreshTokens"]!.Type == JTokenType.Null),
            "Issuer response exposed native password, verification or unrelated refresh credentials.");
        var envelope = avatar.Value<string>("refreshToken")!.Split('.');
        Check(envelope.Length == 4 && envelope[0] == "oasis-r1" && envelope[1] == "MongoDBOASIS",
            "Owned HTTP credential has no expected authoritative provider.");
        Check(principal.FindAll(AvatarSessionIdentity.SessionClaim).Count() == 1 &&
            principal.FindFirst(AvatarSessionIdentity.SessionClaim)!.Value == AvatarSessionIdentity.Create(actor, envelope[2]) &&
            principal.FindAll(AvatarSessionIdentity.ProviderClaim).Count() == 1 &&
            principal.FindFirst(AvatarSessionIdentity.ProviderClaim)!.Value == envelope[1],
            "Access JWT is not bound to the same acknowledged refresh generation and provider.");
        Check(!principal.Claims.Any(claim => claim.Value == envelope[2] || claim.Value == avatar.Value<string>("refreshToken")),
            "Access JWT exposes a refresh credential.");
        return principal;
    }
    private static async Task VerifyAccessAsync(ClaimsPrincipal principal, Guid actor, bool expectedActive)
    {
        Check(AvatarSessionIdentity.TryReadBinding(principal, out var session, out var provider), "Signed session binding unreadable.");
        var loaded = await AvatarManager.Instance.LoadAvatarForAccessSessionAsync(actor, session, provider);
        if (expectedActive)
            Check(!loaded.IsError && loaded.IsLoaded && loaded.Result?.Id == actor &&
                loaded.Result.RefreshTokens == null && loaded.Result.Password == null,
                "Authoritative active session was refused or leaked its persisted credentials.");
        else
            Check(loaded.IsError && loaded.ErrorCode == "ACCESS_SESSION_REVOKED" && loaded.Result == null,
                "Revoked access session remained usable.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
