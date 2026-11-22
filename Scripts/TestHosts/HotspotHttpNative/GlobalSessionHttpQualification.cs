using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

internal static class GlobalSessionHttpQualification
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    public static async Task RunAsync(HttpClient http, MongoDBOASIS mongo, OASISDNA dna,
        Guid actor, string username, string password)
    {
        const string ownedUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Check(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") == ownedUri, "Exact owned Mongo fixture required.");
        var directory = Directory.CreateTempSubdirectory("oasis-global-session-");
        SQLLiteDBOASIS? sqlite = null;
        bool pauseAttempted = false;
        Exception? failure = null;
        try
        {
            sqlite = new SQLLiteDBOASIS("Data Source=" + Path.Combine(directory.FullName, "sessions.db") + ";Pooling=False");
            Check(ProviderManager.Instance.RegisterProvider(sqlite), "Owned SQLite registration failed.");
            Check(!(await sqlite.ActivateProviderAsync()).IsError, "Owned SQLite activation failed.");
            var absentId = Guid.NewGuid();
            var absentSync = sqlite.LoadAvatar(absentId);
            var absentAsync = await sqlite.LoadAvatarAsync(absentId);
            Check(!absentSync.IsError && absentSync.Result == null && absentSync.Exception == null &&
                absentSync.ErrorCode == NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound &&
                !absentAsync.IsError && absentAsync.Result == null && absentAsync.Exception == null &&
                absentAsync.ErrorCode == NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,
                "Real SQLite absent-account reads must explicitly distinguish absence from provider failure.");
            var original = await mongo.LoadAvatarAsync(actor);
            Check(!original.IsError && original.Result != null, "Owned actor missing.");
            var copy = new Avatar { Id = actor, Username = original.Result.Username, Email = original.Result.Email,
                Password = original.Result.Password, Verified = original.Result.Verified, IsActive = true,
                AvatarType = original.Result.AvatarType, Version = 1, VersionId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow, RefreshTokens = new List<RefreshToken>() };
            var seeded = await sqlite.SaveAvatarAsync(copy);
            Check(!seeded.IsError && seeded.IsSaved && seeded.Result != null, "Native SQLite actor seed was not acknowledged.");
            await sqlite.DeActivateProviderAsync();
            await using (var legacy = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + Path.Combine(directory.FullName, "sessions.db") + ";Pooling=False"))
            {
                await legacy.OpenAsync();
                await using var command = legacy.CreateCommand();
                command.CommandText = "ALTER TABLE RefreshTokens DROP COLUMN AuthorityProvider";
                await command.ExecuteNonQueryAsync();
            }
            Check(!(await sqlite.ActivateProviderAsync()).IsError, "Existing SQLite session schema upgrade failed.");
            var upgraded = await sqlite.LoadAvatarAsync(actor);
            Check(!upgraded.IsError && upgraded.Result.VersionId == seeded.Result.VersionId &&
                upgraded.Result.Password == original.Result.Password, "SQLite session schema upgrade changed the existing account.");
            dna.OASIS.StorageProviders.AutoFailOverProvidersForAvatarLogin = "MongoDBOASIS,SQLLiteDBOASIS";
            dna.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds = 2;
            Check(!ProviderManager.Instance.SetAndReplaceAutoFailOverListForProvidersForAvatarLogin("MongoDBOASIS,SQLLiteDBOASIS").IsError,
                "Owned SSO authority policy failed.");

            async Task<JObject> Read(HttpResponseMessage response)
            {
                using (response)
                {
                    Check(response.Content.Headers.ContentType?.MediaType == "application/json",
                        "Expected real JSON API response at " + response.RequestMessage?.RequestUri?.AbsolutePath +
                        " (HTTP " + (int)response.StatusCode + ", media " + response.Content.Headers.ContentType?.MediaType + ").");
                    var envelope = JObject.Parse(await response.Content.ReadAsStringAsync());
                    return envelope["result"] is JObject wrapped && wrapped["isError"] != null ? wrapped : envelope;
                }
            }
            async Task<JToken> Login(ProviderType provider)
            {
                Check(!(await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(provider)).IsError, "Owned provider selection failed.");
                if (provider == ProviderType.SQLLiteDBOASIS)
                {
                    var prepared = await sqlite.LoadAvatarAsync(actor);
                    Check(!prepared.IsError && prepared.Result != null && prepared.Result.IsActive && prepared.Result.IsVerified &&
                        prepared.Result.Password == original.Result.Password,
                        "Native SQLite login seed did not preserve active/verified/password state.");
                }
                http.DefaultRequestHeaders.Authorization = null;
                var envelope = await Read(await http.PostAsJsonAsync("/api/avatar/authenticate", new { username, password }));
                if (envelope.Value<bool>("isError"))
                {
                    var diagnostic = (envelope.Value<string>("errorCode") ?? "unspecified") + ": " + envelope.Value<string>("message");
                    diagnostic = System.Text.RegularExpressions.Regex.Replace(diagnostic, @"mongodb(?:\+srv)?://\S+", "[connection]");
                    diagnostic = System.Text.RegularExpressions.Regex.Replace(diagnostic, @"[A-Za-z0-9+/=_-]{40,}", "[opaque-value]");
                    diagnostic = System.Text.RegularExpressions.Regex.Replace(diagnostic, @"(?i)(password|secret|token|api[-_ ]?key)\s*[:=]\s*[^ ;]+", "$1=[redacted]");
                    throw new InvalidOperationException("Real password authentication failed at " + provider + ": " + diagnostic[..Math.Min(400, diagnostic.Length)]);
                }
                var credentials = envelope["result"]!;
                var parts = credentials.Value<string>("refreshToken")!.Split('.');
                Check(parts.Length == 4 && parts[1] == provider.ToString(), "Issuer did not use the selected real provider.");
                var native = provider == ProviderType.MongoDBOASIS ? await mongo.LoadAvatarAsync(actor) : await sqlite.LoadAvatarAsync(actor);
                Check(!native.IsError && native.Result.RefreshTokens.Any(token => token.Token == parts[2] &&
                    token.IsActive && token.AuthorityProvider == provider.ToString()), "Native credential lost its issuing authority.");
                return credentials;
            }
            async Task CheckAccess(JToken credentials, ProviderType provider, bool active)
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(credentials.Value<string>("jwtToken"));
                var sid = jwt.Claims.Single(claim => claim.Type == AvatarSessionIdentity.SessionClaim).Value;
                var result = await AvatarManager.Instance.LoadAvatarForAccessSessionAsync(actor, sid, provider);
                Check(active ? !result.IsError && result.Result?.Id == actor : result.IsError && result.Result == null,
                    "Access-session state did not match native revocation.");
            }
            Check(!(await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(ProviderType.MongoDBOASIS)).IsError,
                "Owned Mongo selection failed.");
            var missingBefore = await mongo.LoadAvatarByUsernameAsync("root");
            Check(!missingBefore.IsError && missingBefore.Result != null && missingBefore.Result.Password == null,
                "Owned missing-password fixture must have no password.");
            http.DefaultRequestHeaders.Authorization = null;
            var refused = await Read(await http.PostAsJsonAsync("/api/avatar/authenticate", new { username = "root", password }));
            Check(refused.Value<bool>("isError") && refused.Value<string>("errorCode") == "AVATAR_PASSWORD_UNAVAILABLE" &&
                (refused["result"] == null || refused["result"]!.Type == JTokenType.Null), "Missing-password login did not refuse credentials without an avatar payload.");
            var missingAfter = await mongo.LoadAvatarByUsernameAsync("root");
            Check(!missingAfter.IsError && missingAfter.Result.Password == null &&
                missingAfter.Result.VersionId == missingBefore.Result.VersionId && missingAfter.Result.ModifiedDate == missingBefore.Result.ModifiedDate,
                "Missing-password authentication mutated the native account.");
            var first = await Login(ProviderType.MongoDBOASIS);
            var second = await Login(ProviderType.SQLLiteDBOASIS);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", second.Value<string>("jwtToken"));
            var allSessions = await Read(await http.GetAsync("/api/avatar/" + actor + "/sessions"));
            var expectedIds = new[] { first, second }.Select(credentials => new JwtSecurityTokenHandler()
                .ReadJwtToken(credentials.Value<string>("jwtToken")).Claims.Single(claim => claim.Type == AvatarSessionIdentity.SessionClaim).Value).ToArray();
            Check(!allSessions.Value<bool>("isError") && allSessions["result"]?["sessions"] is JArray persistedSessions &&
                expectedIds.All(id => persistedSessions.Any(row => row.Value<string>("id") == id && row.Value<bool>("isActive"))),
                "The real session list omitted an active issuing provider's credential.");
            var unknownSelection = await Read(await http.PostAsJsonAsync("/api/avatar/" + actor + "/sessions/logout",
                new[] { new string('a', 64) }));
            Check(unknownSelection.Value<bool>("isError") && !unknownSelection.Value<bool>("result"),
                "An unknown selected session produced simulated success.");
            await CheckAccess(first, ProviderType.MongoDBOASIS, true);
            await CheckAccess(second, ProviderType.SQLLiteDBOASIS, true);
            var firstSid = new JwtSecurityTokenHandler().ReadJwtToken(first.Value<string>("jwtToken"))
                .Claims.Single(claim => claim.Type == AvatarSessionIdentity.SessionClaim).Value;
            var selected = await Read(await http.PostAsJsonAsync("/api/avatar/" + actor + "/sessions/logout", new[] { firstSid }));
            Check(!selected.Value<bool>("isError") && selected.Value<bool>("isSaved") && selected.Value<bool>("result"),
                "Selected-session logout was not acknowledged by the native authority.");
            await CheckAccess(first, ProviderType.MongoDBOASIS, false);
            await CheckAccess(second, ProviderType.SQLLiteDBOASIS, true);
            first = await Login(ProviderType.MongoDBOASIS);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", second.Value<string>("jwtToken"));
            var sessions = await Read(await http.GetAsync("/api/avatar/" + actor + "/sessions"));
            Check(sessions.Value<bool>("isError") == false &&
                sessions["result"]?.Value<string>("globalRevocationProtocol") == "admitted-authorities-v1", "Real global capability was not advertised.");
            var forbidden = await Read(await http.PostAsync("/api/avatar/" + Guid.NewGuid() + "/sessions/logout-all", null));
            Check(forbidden.Value<bool>("isError"), "Foreign-account logout was not refused.");
            var revoked = await Read(await http.PostAsync("/api/avatar/" + actor + "/sessions/logout-all", null));
            Check(revoked.Value<bool>("isError") == false && revoked.Value<bool>("isSaved") && revoked.Value<bool>("result"),
                "Real global logout did not acknowledge all native authorities.");
            foreach (var row in new[] { (first, ProviderType.MongoDBOASIS), (second, ProviderType.SQLLiteDBOASIS) })
            {
                await CheckAccess(row.Item1, row.Item2, false);
                var refresh = await Read(await http.PostAsJsonAsync("/api/avatar/refresh-token", new { refreshToken = row.Item1.Value<string>("refreshToken") }));
                Check(refresh.Value<bool>("isError"), "Globally revoked native refresh credential remained usable.");
            }
            var freshMongo = await Login(ProviderType.MongoDBOASIS);
            var freshSqlite = await Login(ProviderType.SQLLiteDBOASIS);
            await CheckAccess(freshMongo, ProviderType.MongoDBOASIS, true);
            await CheckAccess(freshSqlite, ProviderType.SQLLiteDBOASIS, true);

            var inspect = JArray.Parse(await DockerAsync("inspect", "map-development-mongo"));
            Check(inspect.Count == 1 && inspect[0].Value<string>("Name") == "/map-development-mongo" &&
                inspect[0]["Config"]?.Value<string>("Image") == "mongo:8.0.15" &&
                inspect[0]["HostConfig"]?["PortBindings"]?["27029/tcp"]?[0]?.Value<string>("HostIp") == "127.0.0.1" &&
                inspect[0]["HostConfig"]?["PortBindings"]?["27029/tcp"]?[0]?.Value<string>("HostPort") == "27029",
                "Refusing to pause an unowned native Mongo container.");
            pauseAttempted = true;
            await DockerAsync("pause", "map-development-mongo");
            var incomplete = await AvatarManager.Instance.LogoutAllAvatarSessionsAsync(actor);
            Check(incomplete.IsError && !incomplete.Result && incomplete.ErrorCode == "SESSION_REVOCATION_INCOMPLETE",
                "Unavailable authority incorrectly produced global success.");
            await DockerAsync("unpause", "map-development-mongo");
            pauseAttempted = false;
            await CheckAccess(freshSqlite, ProviderType.SQLLiteDBOASIS, false);
            var after = await sqlite.LoadAvatarAsync(actor);
            Check(!after.IsError && after.Result.Password == original.Result.Password && after.Result.Email == original.Result.Email &&
                after.Result.Username == original.Result.Username && after.Result.RefreshTokens.All(token => !token.IsActive),
                "Global revocation changed identity fields or lost acknowledged SQLite revocations.");
            Console.WriteLine("GLOBAL_SESSION_HTTP_NATIVE_PROOF: real password issuance and provenance at Mongo/SQLite, actor-bound capability and POST, access/refresh refusal, fresh login, and real paused-Mongo partial failure; no global success on incomplete authority.");
        }
        catch (Exception error) { failure = error; throw; }
        finally
        {
            var errors = new List<Exception>();
            if (pauseAttempted) try { await DockerAsync("unpause", "map-development-mongo"); } catch (Exception error) { errors.Add(error); }
            try { sqlite?.Dispose(); } catch (Exception error) { errors.Add(error); }
            try
            {
                var actual = Path.GetFullPath(directory.FullName);
                Check(Path.GetDirectoryName(actual) == Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) &&
                    Path.GetFileName(actual).StartsWith("oasis-global-session-", StringComparison.Ordinal), "Unexpected owned SQLite cleanup path.");
                Directory.Delete(actual, true);
                Check(!Directory.Exists(actual), "Owned SQLite directory remains.");
            }
            catch (Exception error) { errors.Add(error); }
            if (errors.Count != 0)
            {
                if (failure != null) errors.Insert(0, failure);
                throw new AggregateException("Global-session owned cleanup failed.", errors);
            }
            Console.WriteLine("GLOBAL_SESSION_CLEANUP: owned Mongo unpaused; owned SQLite directory absent.");
        }
    }

    private static async Task<string> DockerAsync(params string[] arguments)
    {
        var start = new ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Owned Docker command did not start.");
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw new TimeoutException("Owned Docker command exceeded its deadline."); }
        await error;
        Check(process.ExitCode == 0, "Owned Docker command failed; configuration output withheld.");
        return await output;
    }
}
