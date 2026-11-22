using System.Diagnostics;
using System.Security.Cryptography;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    [Fact]
    public async Task ALoginDeadlineDoesNotSuppressTheRealSqliteFallback()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("Only the exact owned Mongo fixture is allowed.");
        var database = "hyperdrive_login_failover_" + Guid.NewGuid().ToString("N");
        var directory = Directory.CreateTempSubdirectory("oasis-login-failover-");
        var path = Path.Combine(directory.FullName, "fallback.db");
        var client = new MongoClient(uri);
        var dna = new OASISDNA { OASIS = new() {
            HyperDriveMode = HyperDriveModes.V2,
            OASISHyperDriveConfig = new() { AutoFailoverEnabled = true, AutoReplicationEnabled = false, AutoLoadBalancingEnabled = false },
            Security = new() { SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)) },
            Logging = new() { LogToConsole = false, LogToFile = false }, ErrorHandling = new(), Email = new(),
            SubscriptionConfig = new() { PlanType = "Gold" },
            StorageProviders = new() { AvatarLoginTimeoutSeconds = 1, ProviderMethodCallTimeOutSeconds = 2,
                AutoFailOverProviders = "MongoDBOASIS,SQLLiteDBOASIS", AutoFailOverProvidersForAvatarLogin = "MongoDBOASIS,SQLLiteDBOASIS",
                AutoLoadBalanceProviders = "MongoDBOASIS", AutoReplicationProviders = "MongoDBOASIS",
                MongoDBOASIS = new() { ConnectionString = uri, DBName = database, NativeImportDBName = database } } } };
        var mongo = new MongoDBOASIS(uri, database, dna);
        var sqlite = new SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var pauseAttempted = false;
        Exception? testFailure = null;
        try
        {
            Assert.DoesNotContain(database, await (await client.ListDatabaseNamesAsync()).ToListAsync());
            var manager = new ProviderManager(null, dna) {
                IsAutoFailOverEnabled = true, IsAutoLoadBalanceEnabled = false, IsAutoReplicationEnabled = false };
            Assert.True(manager.RegisterProvider(mongo));
            Assert.True(manager.RegisterProvider(sqlite));
            Assert.False((await mongo.ActivateProviderAsync()).IsError);
            Assert.False((await sqlite.ActivateProviderAsync()).IsError);
            Assert.False(manager.SetAndActivateCurrentStorageProvider(mongo).IsError);
            var alternatives = new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
            Assert.False(manager.SetAndReplaceAutoFailOverListForProviders(alternatives).IsError);
            Assert.False(manager.SetAndReplaceAutoFailOverListForProvidersForAvatarLogin(alternatives).IsError);
            var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var actor = Guid.NewGuid();
            Avatar Account() => new() { Id = actor, Username = "login-" + actor.ToString("N"), Email = actor.ToString("N") + "@example.invalid",
                Password = BCrypt.Net.BCrypt.HashPassword(password), AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                Verified = DateTime.UtcNow, IsActive = true, CreatedDate = DateTime.UtcNow,
                Version = 1, VersionId = Guid.NewGuid() };
            Assert.False((await mongo.SaveAvatarAsync(Account())).IsError);
            Assert.False((await sqlite.SaveAvatarAsync(Account())).IsError);
            var avatars = new AvatarManager(mongo, dna, manager);
            var container = JObject.Parse(await LoginFixtureDockerAsync("container", "inspect", "--format", "{{json .}}", "map-development-mongo"));
            Assert.Equal("/map-development-mongo", container.Value<string>("Name"));
            Assert.Equal("mongo:8.0.15", container["Config"]!.Value<string>("Image"));
            Assert.Equal("127.0.0.1", container["HostConfig"]!["PortBindings"]!["27029/tcp"]![0]!.Value<string>("HostIp"));
            pauseAttempted = true;
            await LoginFixtureDockerAsync("pause", "map-development-mongo");
            var first = await avatars.AuthenticateAsync("login-" + actor.ToString("N"), password, "127.0.0.1");
            Assert.True(first.IsError);
            Assert.Contains("timed out", first.Message, StringComparison.OrdinalIgnoreCase);
            // Allow the real bounded primary attempt and fallback to complete inside the next request.
            // This changes only this isolated runtime DNA, never a tracked/deployed setting.
            dna.OASIS.StorageProviders.AvatarLoginTimeoutSeconds = 12;
            var second = await avatars.AuthenticateAsync("login-" + actor.ToString("N"), password, "127.0.0.1");
            Assert.False(second.IsError, second.Message);
            Assert.True(second.IsSaved);
            Assert.Equal(actor, second.Result.Id);
            Assert.False(string.IsNullOrWhiteSpace(second.Result.JwtToken));
            Assert.StartsWith("oasis-r1.SQLLiteDBOASIS.", second.Result.RefreshToken);
            var stored = await sqlite.LoadAvatarAsync(actor);
            Assert.False(stored.IsError);
            Assert.Contains(stored.Result.RefreshTokens, token => token.IsActive &&
                token.Token == second.Result.RefreshToken.Split('.')[2]);
            Assert.Equal(new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS },
                manager.GetProviderAutoFailOverListForAvatarLogin().Select(value => value.Value));
            Console.WriteLine("LOGIN_FAILOVER_NATIVE_PROOF: after one actual primary deadline, a second request used the real SQLite account and acknowledged session; no primary-only suppression. Not every-provider or cross-provider active-session failover acceptance.");
        }
        catch (Exception error) { testFailure = error; throw; }
        finally
        {
            var cleanup = new List<Exception>();
            var primaryAvailable = !pauseAttempted;
            try
            {
                if (pauseAttempted)
                {
                    var state = await LoginFixtureDockerAsync("inspect", "--format", "{{.State.Paused}}", "map-development-mongo");
                    if (state.Trim() == "true") await LoginFixtureDockerAsync("unpause", "map-development-mongo");
                    Assert.Equal("false", (await LoginFixtureDockerAsync("inspect", "--format", "{{.State.Paused}}", "map-development-mongo")).Trim());
                }
                primaryAvailable = true;
            }
            catch (Exception error) { cleanup.Add(error); }
            try { sqlite.Dispose(); } catch (Exception error) { cleanup.Add(error); }
            try { await mongo.DeActivateProviderAsync(); } catch (Exception error) { cleanup.Add(error); }
            if (primaryAvailable)
            {
                try
                {
                    await client.DropDatabaseAsync(database);
                    Assert.DoesNotContain(database, await (await client.ListDatabaseNamesAsync()).ToListAsync());
                }
                catch (Exception error) { cleanup.Add(error); }
            }
            // The runner's always-cleanup removes the exact owned container even if unpause failed.
            try
            {
                var actual = Path.GetFullPath(directory.FullName);
                var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!actual.StartsWith(temp, StringComparison.Ordinal) || !Path.GetFileName(actual).StartsWith("oasis-login-failover-", StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected owned SQLite cleanup path.");
                Directory.Delete(actual, recursive: true);
                Assert.False(Directory.Exists(actual));
            }
            catch (Exception error) { cleanup.Add(error); }
            if (cleanup.Count > 0)
            {
                if (testFailure != null) cleanup.Insert(0, testFailure);
                throw new AggregateException("Owned login-failover cleanup was incomplete.", cleanup);
            }
            Console.WriteLine("LOGIN_FAILOVER_CLEANUP: primary unpaused; owned Mongo database and SQLite directory absent.");
        }
    }

    private static async Task<string> LoginFixtureDockerAsync(params string[] arguments)
    {
        var start = new ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Owned Docker operation did not start.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw new TimeoutException("Owned Docker operation exceeded its deadline."); }
        await error; // Never print container configuration or command output on failure.
        if (process.ExitCode != 0) throw new InvalidOperationException("Owned Docker " + arguments[0] + " failed with exit " + process.ExitCode + ".");
        return await output;
    }
}
