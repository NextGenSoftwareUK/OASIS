using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using MongoDB.Bson;
using MongoDB.Driver;
using NativeMongoAvatar = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
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
        Guid actor, string username, string password,
        NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.ISubscriptionCheckoutJournal checkoutJournal, IMongoDatabase ownedDatabase,
        AccountClosureAdmissionService closureService, Guid closureActor, bool verifyBillingAdmission = true,
        Func<JToken, JToken, Task>? verifySessionBrowser = null)
    {
        const string ownedUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Check(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") == ownedUri, "Exact owned Mongo fixture required.");
        Check(ownedDatabase.DatabaseNamespace.DatabaseName.StartsWith("hotspot_http_", StringComparison.Ordinal) &&
            ownedDatabase.DatabaseNamespace.DatabaseName == dna.OASIS.StorageProviders.MongoDBOASIS.DBName,
            "The raw session assertion requires the existing owned native fixture database.");
        var mongoSerializationChecked = false;
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
            dna.OASIS.OASISHyperDriveConfig.AutoFailoverEnabled = true;
            ProviderManager.Instance.IsAutoFailOverEnabled = true;
            dna.OASIS.OASISHyperDriveConfig.AutoReplicationEnabled = true;
            ProviderManager.Instance.IsAutoReplicationEnabled = true;
            ProviderManager.Instance.IsAutoLoadBalanceEnabled = false;
            Check(!ProviderManager.Instance.SetAndReplaceAutoReplicationListForProviders("MongoDBOASIS").IsError,
                "Owned replication destination policy failed.");
            Check(ProviderManager.Instance.IsAutoFailOverEnabled, "Owned SSO failover policy must be enabled.");
            Check(!ProviderManager.Instance.SetAndReplaceAutoFailOverListForProvidersForAvatarLogin("MongoDBOASIS,SQLLiteDBOASIS").IsError,
                "Owned SSO authority policy failed.");

            if (verifySessionBrowser == null)
            {
            var noAccount = Guid.NewGuid();
            var noSessions = await AvatarManager.Instance.LogoutAllAvatarSessionsAsync(noAccount);
            Check(!noSessions.IsError && noSessions.IsSaved && noSessions.Result,
                "Confirmed absence at admitted providers did not complete zero-session revocation.");
            Check(!(await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(ProviderType.MongoDBOASIS)).IsError,
                "Owned Mongo selection after absent revocation failed.");
            var absentMongoAfter = await mongo.LoadAvatarAsync(noAccount);
            Check(!(await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(ProviderType.SQLLiteDBOASIS)).IsError,
                "Owned SQLite selection after absent revocation failed.");
            var absentSqliteAfter = await sqlite.LoadAvatarAsync(noAccount);
            Check(!absentMongoAfter.IsError && absentMongoAfter.Result == null && !absentSqliteAfter.IsError && absentSqliteAfter.Result == null,
                "Zero-session revocation created a native account or the absence verification failed.");

            }

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
            async Task PauseOwnedMongoAsync()
            {
                var inspect = JArray.Parse(await DockerAsync("inspect", "map-development-mongo"));
                Check(inspect.Count == 1 && inspect[0].Value<string>("Name") == "/map-development-mongo" &&
                    inspect[0]["Config"]?.Value<string>("Image") == "mongo:8.0.15" &&
                    inspect[0]["HostConfig"]?["PortBindings"]?["27029/tcp"]?[0]?.Value<string>("HostIp") == "127.0.0.1" &&
                    inspect[0]["HostConfig"]?["PortBindings"]?["27029/tcp"]?[0]?.Value<string>("HostPort") == "27029",
                    "Refusing to pause an unowned native Mongo container.");
                pauseAttempted = true;
                await DockerAsync("pause", "map-development-mongo");
            }

            async Task<JToken> Login(ProviderType provider)
            {
                Check(mongo.IsProviderActivated,
                    "The owned Mongo primary must stay active before the connected-transport outage check.");
                if (provider == ProviderType.SQLLiteDBOASIS)
                {
                    var prepared = await sqlite.LoadAvatarAsync(actor);
                    Check(!prepared.IsError && prepared.Result != null && prepared.Result.IsActive && prepared.Result.IsVerified &&
                        prepared.Result.Password == original.Result.Password,
                        "Native SQLite login seed did not preserve active/verified/password state.");
                }
                http.DefaultRequestHeaders.Authorization = null;
                // The shipping singleton retains its configured primary affinity. Changing
                // ProviderManager.CurrentStorageProvider does not select a different issuer.
                // Exercise real SSO failover by making only the owned primary unavailable.
                var primaryBefore = provider == ProviderType.SQLLiteDBOASIS ? await mongo.LoadAvatarAsync(actor) : null;
                Check(primaryBefore == null || !primaryBefore.IsError && primaryBefore.Result != null,
                    "Owned primary before-image could not be loaded.");
                JObject envelope;
                var previousProviderTimeout = dna.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds;
                try
                {
                    if (provider == ProviderType.SQLLiteDBOASIS)
                    {
                        // A long ordinary provider budget must not consume the whole sign-in deadline.
                        dna.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds = 99;
                        await PauseOwnedMongoAsync();
                    }
                    envelope = await Read(await http.PostAsJsonAsync("/api/avatar/authenticate", new { username, password }));
                }
                finally
                {
                    dna.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds = previousProviderTimeout;
                    if (pauseAttempted)
                    {
                        await DockerAsync("unpause", "map-development-mongo");
                        pauseAttempted = false;
                    }
                }
                if (envelope.Value<bool>("isError"))
                {
                    var diagnostic = (envelope.Value<string>("errorCode") ?? "unspecified") + ": " + envelope.Value<string>("message");
                    diagnostic = System.Text.RegularExpressions.Regex.Replace(diagnostic, @"mongodb(?:\+srv)?://\S+", "[connection]");
                    diagnostic = System.Text.RegularExpressions.Regex.Replace(diagnostic, @"[A-Za-z0-9+/=_-]{40,}", "[opaque-value]");
                    diagnostic = System.Text.RegularExpressions.Regex.Replace(diagnostic, @"(?i)(password|secret|token|api[-_ ]?key)\s*[:=]\s*[^ ;]+", "$1=[redacted]");
                    throw new InvalidOperationException("Real password authentication failed at " + provider + ": " + diagnostic[..Math.Min(400, diagnostic.Length)]);
                }
                if (primaryBefore != null)
                {
                    var primaryAfter = await mongo.LoadAvatarAsync(actor);
                    Check(!primaryAfter.IsError && primaryAfter.Result.VersionId == primaryBefore.Result.VersionId,
                        "Issuing a session at SQLite changed its non-authority Mongo replica.");
                }
                var credentials = envelope["result"]!;
                var parts = credentials.Value<string>("refreshToken")!.Split('.');
                Check(parts.Length == 4 && parts[1] == provider.ToString(), "Issuer did not use the selected real provider.");
                var native = provider == ProviderType.MongoDBOASIS ? await mongo.LoadAvatarAsync(actor) : await sqlite.LoadAvatarAsync(actor);
                Check(!native.IsError && native.Result.RefreshTokens.Any(token => token.Token == parts[2] &&
                    token.IsActive && token.AuthorityProvider == provider.ToString()), "Native credential lost its issuing authority.");
                if (provider == ProviderType.MongoDBOASIS && !mongoSerializationChecked)
                {
                    var typed = ownedDatabase.GetCollection<NativeMongoAvatar>("Avatar");
                    var selector = Builders<NativeMongoAvatar>.Filter.Eq(row => row.HolonId, actor)
                        .Render(new RenderArgs<NativeMongoAvatar>(typed.DocumentSerializer, typed.Settings.SerializerRegistry));
                    var document = await ownedDatabase.GetCollection<BsonDocument>("Avatar").Find(selector)
                        .Project(Builders<BsonDocument>.Projection.Include("RefreshTokens")).SingleAsync();
                    var storedTokens = document["RefreshTokens"].AsBsonArray;
                    Check(storedTokens.Count != 0 && storedTokens.All(value => value.IsBsonDocument &&
                        !value.AsBsonDocument.Contains(nameof(RefreshToken.PendingAuthorityAssignment)) &&
                        !value.AsBsonDocument.Contains(nameof(RefreshToken.PendingAuthorityProviders)) &&
                        value.AsBsonDocument.GetValue(nameof(RefreshToken.AuthorityProvider), BsonNull.Value) == provider.ToString()),
                        "Native BSON persisted temporary issuance state or lost the durable issuing provider.");
                    mongoSerializationChecked = true;
                }
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
            if (verifySessionBrowser != null)
            {
                var browserCurrent = await Login(ProviderType.MongoDBOASIS);
                var browserIndependent = await Login(ProviderType.MongoDBOASIS);
                await verifySessionBrowser(browserCurrent, browserIndependent);
                await CheckAccess(browserCurrent, ProviderType.MongoDBOASIS, false);
                await CheckAccess(browserIndependent, ProviderType.MongoDBOASIS, false);
                Console.WriteLine("SESSION_SETTINGS_BROWSER_NATIVE_PROOF: actual Settings selected/global buttons and SDK requests; both actual issued credentials refused by native authority after sign-out.");
                return;
            }

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
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", first.Value<string>("jwtToken"));
            if (verifyBillingAdmission)
            {
            var beforeAssessment = await mongo.LoadAvatarAsync(actor);
            var assessment = await Read(await http.GetAsync("/api/avatar/account-closure/assessment"));
            var facts = assessment["result"];
            Check(!assessment.Value<bool>("isError") && facts != null && facts.Value<bool>("hasOwnedApplications") &&
                !facts.Value<bool>("subscriptionRequiresCancellation") && !facts.Value<bool>("hasOpenStripeInvoices") &&
                !facts.Value<bool>("hasPendingStripeCheckouts") && !facts.Value<bool>("hasUnsettledUsage") &&
                !facts.Value<bool>("hasUnbilledCharges") && !facts.Value<bool>("hasUnresolvedStatements"),
                "The real actor-scoped dependency assessment did not reflect its owned application and empty billing ledger.");
            var subscriptionView = await Read(await http.GetAsync("/api/subscription/subscriptions/me"));
            var recoveryLinks = subscriptionView["checkoutRecovery"];
            var pendingPath = recoveryLinks?.Value<string>("pendingUrl");
            Check(!subscriptionView.Value<bool>("isError") && recoveryLinks?.Value<string>("protocol") == "checkout-journal-v1" &&
                pendingPath != null && pendingPath.StartsWith("/", StringComparison.Ordinal) && !pendingPath.StartsWith("//", StringComparison.Ordinal),
                "The real subscription API did not advertise its usable recovery link.");
            async Task<string?> ReserveCheckout()
            {
                try { return await checkoutJournal.BeginCheckoutAsync(actor.ToString("D"), "bronze", CancellationToken.None); }
                catch (NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.CheckoutReservationException) { return null; }
            }
            var reservations = await Task.WhenAll(ReserveCheckout(), ReserveCheckout());
            Check(reservations.Count(operation => operation != null) == 1,
                "Concurrent native checkout reservations did not produce exactly one acknowledged winner.");
            var checkoutOperation = reservations.Single(operation => operation != null)!;
            var pendingList = await Read(await http.GetAsync(pendingPath));
            Check(!pendingList.Value<bool>("isError") && pendingList["result"] is JArray attempts && attempts.Count == 1 &&
                attempts[0].Value<string>("operationId") == checkoutOperation && !attempts[0].Value<bool>("hasProviderSession") && !string.IsNullOrEmpty(attempts[0].Value<string>("reconcileUrl")),
                "The actual pending-checkout API did not return the owned unresolved operation.");
            var unknownRecovery = await Read(await http.PostAsync("/api/subscription/checkout/operations/" + Guid.NewGuid().ToString("D") + "/reconcile", null));
            Check(unknownRecovery.Value<bool>("isError"), "An unknown operation bypassed actor-scoped checkout reconciliation.");
            var blockedCheckout = await Read(await http.PostAsJsonAsync("/api/subscription/checkout/session", new {
                planId = "bronze", successUrl = "https://owned.invalid/subscription", cancelUrl = "https://owned.invalid/subscription" }));
            Check(blockedCheckout.Value<bool>("isError") && blockedCheckout.Value<string>("errorCode") == "CHECKOUT_ALREADY_PENDING" &&
                (await checkoutJournal.GetPendingCheckoutsAsync(actor.ToString("D"), CancellationToken.None)).Count == 1,
                "An existing unresolved checkout did not block a replacement payment before Stripe.");
            var unknownCancel = await Read(await http.PostAsync("/api/subscription/checkout/operations/" + Guid.NewGuid().ToString("D") + "/cancel", null));
            Check(unknownCancel.Value<bool>("isError"), "An unknown operation bypassed actor-scoped checkout cancellation.");
            var pendingAssessment = await Read(await http.GetAsync("/api/avatar/account-closure/assessment"));
            Check(!pendingAssessment.Value<bool>("isError") && pendingAssessment["result"]!.Value<bool>("hasPendingStripeCheckouts"),
                "A durable customerless checkout request was omitted from dependency assessment.");
            // Native journal fixture data only, not a claim of a real Stripe-issued session.
            const string storedSessionFixture = "cs_owned_native_journal_fixture";
            await checkoutJournal.RecordCheckoutSessionAsync(actor.ToString("D"), checkoutOperation, storedSessionFixture, CancellationToken.None);
            await checkoutJournal.CloseCheckoutAsync(actor.ToString("D"), checkoutOperation, storedSessionFixture, CancellationToken.None);
            var nextCheckout = await checkoutJournal.BeginCheckoutAsync(actor.ToString("D"), "silver", CancellationToken.None);
            await checkoutJournal.CloseCheckoutAsync(actor.ToString("D"), checkoutOperation, storedSessionFixture, CancellationToken.None);
            var afterReplay = await checkoutJournal.GetPendingCheckoutsAsync(actor.ToString("D"), CancellationToken.None);
            Check(afterReplay.Count == 1 && afterReplay[0].OperationId == nextCheckout,
                "Replaying an older checkout closure released or hid a newer reservation.");
            await checkoutJournal.CloseCheckoutAsync(actor.ToString("D"), nextCheckout, "cs_owned_next_journal_fixture", CancellationToken.None);
            var closedPendingList = await Read(await http.GetAsync(pendingPath));
            Check(!closedPendingList.Value<bool>("isError") && closedPendingList["result"] is JArray remainingAttempts && remainingAttempts.Count == 0,
                "The real pending-checkout API retained an acknowledged closed operation.");
            var closedAssessment = await Read(await http.GetAsync("/api/avatar/account-closure/assessment"));
            Check(!closedAssessment.Value<bool>("isError") && !closedAssessment["result"]!.Value<bool>("hasPendingStripeCheckouts"),
                "Acknowledged native checkout closure remained unresolved in dependency assessment.");
            Console.WriteLine("ACCOUNT_DEPENDENCY_NATIVE_PROOF: actual actor-bound controller/Mongo snapshot and owned app, customerless checkout journal request/acknowledgement/closure, one concurrent reservation winner, closed-slot reuse and old-closure replay isolation; no Stripe calls or paid lifecycle acceptance.");
            var ownerClosure = await closureService.CloseBillingAdmissionAsync(actor, Guid.NewGuid(), CancellationToken.None);
            Check(ownerClosure.IsError && ownerClosure.ErrorCode == "ACCOUNT_CLOSURE_BLOCKED",
                "An account owning an app closed billing admission.");
            var billingStore = (ISubscriptionBillingRepository)checkoutJournal;
            var usageStore = (ISubscriptionUsageRepository)checkoutJournal;
            var nativeBilling = (MongoSubscriptionUsageRepository)checkoutJournal;
            var freeAccount = new SubscriptionRecord { UserId = closureActor.ToString("D"), PlanId = "free", Status = "active",
                CurrentPeriodStart = DateTime.UtcNow, CurrentPeriodEnd = DateTime.UtcNow.AddDays(1) };
            await billingStore.SaveSubscriptionAsync(freeAccount);
            var closureOperation = Guid.NewGuid();
            var admissionClosed = await closureService.CloseBillingAdmissionAsync(closureActor, closureOperation, CancellationToken.None);
            Check(!admissionClosed.IsError && admissionClosed.IsSaved && admissionClosed.Result,
                "Real free-account billing admission closure was not acknowledged.");
            var closedAccount = await billingStore.GetSubscriptionAsync(closureActor.ToString("D"));
            Check(closedAccount.AccountClosureOperationId == closureOperation.ToString("D") && !closedAccount.IsActiveAt(DateTime.UtcNow),
                "The real ledger did not retain closed billing admission.");
            var repeatedClosure = await closureService.CloseBillingAdmissionAsync(closureActor, closureOperation, CancellationToken.None);
            Check(!repeatedClosure.IsError && repeatedClosure.Result &&
                (await billingStore.GetSubscriptionAsync(closureActor.ToString("D"))).AuthorizationVersion == closedAccount.AuthorizationVersion,
                "Repeating billing closure changed its admitted state or version.");
            var checkoutBlocked = false;
            try { await checkoutJournal.BeginCheckoutAsync(closureActor.ToString("D"), "bronze", CancellationToken.None); }
            catch (CheckoutReservationException error) when (error.Code == "ACCOUNT_CLOSING") { checkoutBlocked = true; }
            Check(checkoutBlocked, "Closed billing admission accepted a new checkout reservation.");
            var freeReopenBlocked = false;
            try { await billingStore.SaveSubscriptionAsync(freeAccount); }
            catch (SubscriptionUsageConflictException) { freeReopenBlocked = true; }
            Check(freeReopenBlocked, "Free-plan activation reopened billing admission.");
            var grantReopenBlocked = false;
            try { await nativeBilling.GrantPlanAsync(closureActor.ToString("D"), "silver", actor.ToString("D"),
                "Owned closure qualification", DateTime.UtcNow.AddDays(1), CancellationToken.None); }
            catch (SubscriptionUsageConflictException) { grantReopenBlocked = true; }
            Check(grantReopenBlocked, "An administrator plan grant reopened billing admission.");
            var ownershipBlocked = false;
            try { await nativeBilling.SetAppOwnersAsync("owned-billing", new[] { actor.ToString("D"), closureActor.ToString("D") },
                actor.ToString("D"), "Owned closure qualification", CancellationToken.None); }
            catch (SubscriptionUsageConflictException) { ownershipBlocked = true; }
            Check(ownershipBlocked, "App ownership was added to an account with closed billing admission.");
            var usageBlocked = false;
            var authorizationTime = DateTime.UtcNow;
            try { await usageStore.AuthorizeAsync(new SubscriptionUsageEvent {
                OperationId = Guid.NewGuid().ToString("D"), UserId = closureActor.ToString("D"), ConsumingService = "WEB5",
                RequestFingerprint = new string('c', 64), Endpoint = "/owned/closure-proof", MeterCategory = "api.request",
                RequestedUnits = 1, Status = "authorized", AuthorizedAtUtc = authorizationTime, ExpiresAtUtc = authorizationTime.AddMinutes(5)
            }, 0, CancellationToken.None); }
            catch (SubscriptionUsageLimitException error) when (error.Code == "INACTIVE_SUBSCRIPTION") { usageBlocked = true; }
            Check(usageBlocked && (await usageStore.GetEventsAsync(closureActor.ToString("D"), 10, CancellationToken.None)).Count == 0,
                "Closed billing admission accepted or persisted a new metered operation.");
            Console.WriteLine("BILLING_CLOSURE_ADMISSION_NATIVE_PROOF: real service/Mongo snapshot and version fence, owned-app refusal, free-account closure/repeat, checkout/free-plan/ownership/new-usage refusal; no avatar deletion, Stripe calls or full account closure acceptance.");
            var afterAssessment = await mongo.LoadAvatarAsync(actor);
            Check(!afterAssessment.IsError && afterAssessment.Result.VersionId == beforeAssessment.Result.VersionId,
                "Dependency assessment mutated the native avatar.");
            }
            else Console.WriteLine("BILLING_ACCEPTANCE_RETAINED: unchanged billing source from45084348607; billing assertions not replayed in changed sign-in qualification.");
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

            await PauseOwnedMongoAsync();
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
