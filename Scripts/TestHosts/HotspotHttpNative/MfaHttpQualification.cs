using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Mfa;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal static class MfaHttpQualification
{
    public static async Task RunAsync(HttpClient http, MongoDBOASIS provider, MfaService service,
        Func<DateTime> clock, Action advanceStep, OASISDNA dna, Guid actor, string username, string password)
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
            VerifyCredentials(verified, dna, actor);
            await Post("/api/avatar/authenticate-mfa", new { challengeToken = token, recoveryCode = recovery[0] }, "consumed-challenge", true);
            var second = (await Post("/api/avatar/authenticate", new { username, password }, "recovery-challenge"))["result"]!;
            var recovered = (await Post("/api/avatar/authenticate-mfa", new {
                challengeToken = second["metaData"]!.Value<string>(MfaService.ChallengeKey), recoveryCode = recovery[0] }, "recovery"))["result"]!;
            VerifyCredentials(recovered, dna, actor);
            Check((await service.StatusAsync(actor)).RecoveryCodesLeft == recovery.Count - 1, "HTTP recovery use was not persisted.");
            var native = await provider.LoadAvatarAsync(actor);
            Check(!native.IsError && native.Result.RefreshTokens.Count == priorCount + 2,
                "Second-factor credential generations were not persisted.");
            // HyperDrive returns a signed provider-bound envelope, not the raw persisted token.
            // Let the shipped refresh endpoint validate that envelope and rotate its native credential.
            var rotated = (await Post("/api/avatar/refresh-token", new {
                refreshToken = recovered.Value<string>("refreshToken") }, "refresh-envelope"))["result"]!;
            VerifyCredentials(rotated, dna, actor);
            var afterRefresh = await provider.LoadAvatarAsync(actor);
            Check(!afterRefresh.IsError && afterRefresh.Result.RefreshTokens.Count == priorCount + 3 &&
                afterRefresh.Result.RefreshTokens.Count(item => item.Revoked.HasValue) == 1,
                "Actual refresh envelope did not rotate and persist its native credential.");
        }
        finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(secret); }
        Console.WriteLine("MFA_HTTP_NATIVE_PROOF: actual password challenge returned no credentials; authenticator/recovery HTTP issued signed JWT and persisted refresh generations; consumed challenge refused and actual provider-bound refresh rotated. Not browser/full Startup/hosted acceptance.");
    }
    private static void VerifyCredentials(JToken avatar, OASISDNA dna, Guid actor)
    {
        Check(Guid.TryParse(avatar.Value<string>("id"), out var returnedId) && returnedId == actor && !string.IsNullOrWhiteSpace(avatar.Value<string>("refreshToken")), "MFA HTTP returned incomplete credentials.");
        var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(avatar.Value<string>("jwtToken"),
            new TokenValidationParameters { ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(dna.OASIS.Security.SecretKey)),
                ValidateIssuer = true, ValidIssuer = "OASIS", ValidateAudience = true, ValidAudience = "OASIS",
                ValidateLifetime = true, RequireExpirationTime = true, ClockSkew = TimeSpan.Zero }, out _);
        Check(principal.FindFirst("sub")?.Value == actor.ToString(), "MFA HTTP JWT subject mismatch.");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
