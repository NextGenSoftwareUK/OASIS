using System.Net;
using System.Net.Sockets;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Xunit;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.IntegrationTests;

public sealed class HoloNativeRoutingTests
{
    private static readonly Lazy<Task> InstallIsolatedApp = new(InstallIsolatedAppAsync);

    [Theory, InlineData(false), InlineData(true)]
    public async Task LoadsCurrentAvatarCollectionsFromTheConnectedConductor(bool sync)
    {
        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        await InstallIsolatedApp.Value;
        var endpoint = new Uri(Required("OASIS_HOLOCHAIN_ADMIN_URI"));
        Assert.True(endpoint.IsLoopback);
        var holo = OpenProvider(endpoint);
        var id = Guid.NewGuid();
        var username = $"collection-{id:N}";
        var email = $"{username}@oasis.test";
        var fixtures = new List<(string Delete, byte[] Hash)>();
        try
        {
            var activated = await holo.ActivateProviderAsync();
            Assert.True(!activated.IsError && activated.Result, activated.Message);
            var client = holo.HoloNETClientAppAgent;
            async Task<byte[]> Write(string function, string delete, object payload)
            {
                var response = await client.CallZomeFunctionAsync("oasis", function, paramsObject: payload);
                Assert.False(response.IsError, response.Message);
                var hash = HoloNETClientAppAgent.ConvertHoloHashToBytes(Assert.Single(response.Records).ActionHash);
                Assert.Equal(39, hash.Length);
                fixtures.Add((delete, hash));
                return hash;
            }

            var original = await Write("create_entry_avatar", "delete_entry_avatar",
                new { id = id.ToString("D"), username, email, first_name = "Original" });
            await Write("update_entry_avatar", "delete_entry_avatar", new
            {
                original_action_hash = original,
                updated_entry = new { id = id.ToString("D"), username, email, first_name = "Current" }
            });
            await Write("create_entry_avatar_detail", "delete_entry_avatar_detail",
                new { id = id.ToString("D"), username, email });

            var avatars = sync ? holo.LoadAllAvatars() : await holo.LoadAllAvatarsAsync();
            Assert.True(!avatars.IsError && avatars.IsLoaded, avatars.Message);
            Assert.Equal("Current", Assert.Single(avatars.Result.Where(row => row.Id == id)).FirstName);
            var details = sync ? holo.LoadAllAvatarDetails() : await holo.LoadAllAvatarDetailsAsync();
            Assert.True(!details.IsError && details.IsLoaded, details.Message);
            Assert.Equal(username, Assert.Single(details.Result.Where(row => row.Id == id)).Username);
        }
        finally
        {
            if (holo.IsProviderActivated)
            {
                try
                {
                    foreach (var fixture in fixtures.AsEnumerable().Reverse())
                    {
                        var deleted = await holo.HoloNETClientAppAgent.CallZomeFunctionAsync("oasis", fixture.Delete,
                            paramsObject: fixture.Hash);
                        Assert.False(deleted.IsError, deleted.Message);
                    }
                    var avatars = await holo.LoadAllAvatarsAsync();
                    var details = await holo.LoadAllAvatarDetailsAsync();
                    Assert.True(!avatars.IsError && avatars.IsLoaded, avatars.Message);
                    Assert.True(!details.IsError && details.IsLoaded, details.Message);
                    Assert.DoesNotContain(avatars.Result, row => row.Id == id);
                    Assert.DoesNotContain(details.Result, row => row.Id == id);
                }
                finally
                {
                    await holo.DeActivateProviderAsync();
                }
            }
        }
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task SavesPublicAvatarWithoutAuthenticationState(bool useReflection)
    {
        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        await InstallIsolatedApp.Value;
        var admin = new Uri(Required("OASIS_HOLOCHAIN_ADMIN_URI"));
        Assert.True(admin.IsLoopback);
        var holo = new HoloOASIS(OpenAdmin(admin), null!, useHoloNetwork: false,
            useHoloNETORMReflection: useReflection);
        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = $"holo-public-{Guid.NewGuid():N}",
            Email = $"holo-public-{Guid.NewGuid():N}@oasis.test",
            FirstName = "Public profile",
            Password = "synthetic-password",
            JwtToken = "synthetic-session",
            RefreshToken = "synthetic-refresh",
            ResetToken = "synthetic-reset",
            VerificationToken = "synthetic-verification"
        };

        try
        {
            var active = await holo.ActivateProviderAsync();
            Assert.True(!active.IsError && active.Result, active.Message);
            var saved = await holo.SaveAvatarAsync(avatar);
            Assert.True(!saved.IsError && saved.IsSaved, saved.Message);
            try
            {
                var native = await holo.HoloNETClientAppAgent.CallZomeFunctionAsync("oasis", "get_avatar_by_id",
                    paramsObject: avatar.Id.ToString("D"));
                Assert.False(native.IsError, native.Message);
                var entry = Assert.Single(native.Records).EntryKeyValuePairs;
                Assert.Equal(avatar.Username, entry["username"]);
                foreach (var key in new[] { "password", "jwt_token", "refresh_token", "refresh_tokens",
                    "reset_token", "verification_token", "password_reset", "reset_token_expires" })
                    Assert.False(entry.ContainsKey(key), $"The native public record contained {key}.");

                var loaded = await holo.LoadAvatarAsync(avatar.Id);
                Assert.True(!loaded.IsError && loaded.IsLoaded, loaded.Message);
                Assert.Equal(avatar.Id, loaded.Result.Id);
                Assert.Equal(avatar.FirstName, loaded.Result.FirstName);
                Assert.Null(loaded.Result.Password);
                Assert.Null(loaded.Result.JwtToken);
            }
            finally
            {
                var deleted = await holo.DeleteAvatarAsync(avatar.Id, softDelete: false);
                Assert.True(!deleted.IsError && deleted.IsDeleted, deleted.Message);
            }
        }
        finally
        {
            if (holo.IsProviderActivated) await holo.DeActivateProviderAsync();
        }
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task RoutesAcrossOfficialConductorAndSqlite(bool sync)
    {
        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        await InstallIsolatedApp.Value;
        var admin = new Uri(Required("OASIS_HOLOCHAIN_ADMIN_URI"));
        Assert.True(admin.IsLoopback);
        var directory = Path.GetFullPath(Required("OASIS_HOLOCHAIN_REPORT_DIR"));
        Assert.Equal(Path.GetFullPath(Required("RUNNER_TEMP")), Path.GetDirectoryName(directory),
            ignoreCase: OperatingSystem.IsWindows());
        Assert.Equal("oasis-holo-native-results", Path.GetFileName(directory));
        var database = Path.Combine(directory, sync ? "routing-sync.db" : "routing-async.db");
        Assert.False(File.Exists(database));
        using var sqlite = new SqliteProvider($"Data Source={database};Pooling=False");
        var missing = Path.Combine(directory, "missing.db");
        using var unavailableSqlite = new SqliteProvider($"Data Source={missing};Mode=ReadWrite;Pooling=False;Default Timeout=1");

        using var closed = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        closed.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)closed.LocalEndPoint!).Port;
        var unavailableAdmin = new UriBuilder(admin) { Host = "127.0.0.1", Port = port }.Uri;
        var holo = OpenProvider(admin);
        var unavailable = OpenProvider(unavailableAdmin);

        try
        {
            var active = await holo.ActivateProviderAsync();
            Assert.True(!active.IsError && active.Result, active.Message);
            active = await sqlite.ActivateProviderAsync();
            Assert.True(!active.IsError && active.Result, active.Message);
            var baseline = sync
                ? holo.LoadAllHolons(HolonType.All, loadChildren: false)
                : await holo.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
            Assert.False(baseline.IsError, baseline.Message);
            var baselineIds = baseline.Result.Select(record => record.Id).ToHashSet();
            await NativePriorityRouting.VerifyAsync(holo, sqlite, unavailable, unavailableSqlite, sync);
            var query = new SearchParams
            {
                SearchOnlyForCurrentAvatar = false,
                SearchGroups = new List<ISearchGroupBase>
                {
                    new SearchTextGroup { SearchHolons = true, HolonType = HolonType.All, SearchQuery = "unavailable conductor" }
                }
            };
            var failedSearch = sync ? unavailable.Search(query, loadChildren: false)
                : await unavailable.SearchAsync(query, loadChildren: false);
            Assert.True(failedSearch.IsError, failedSearch.Message);
            Assert.False(failedSearch.IsLoaded);
            Assert.Null(failedSearch.Result);
            Assert.False(File.Exists(missing));
            var records = await sqlite.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
            Assert.False(records.IsError, records.Message);
            var fixtures = records.Result.OrderBy(record => record.Name, StringComparer.Ordinal).ToArray();
            var expected = new[] { $"priority routing {holo.ProviderType.Value}", $"priority routing {sqlite.ProviderType.Value}" };
            Assert.Equal(expected.Order(StringComparer.Ordinal), fixtures.Select(record => record.Name));
            var collection = sync
                ? holo.LoadAllHolons(HolonType.All, loadChildren: false)
                : await holo.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
            Assert.False(collection.IsError, collection.Message);
            Assert.Equal(baselineIds.Concat(fixtures.Select(record => record.Id)).Order(),
                collection.Result.Select(record => record.Id).Order());
            foreach (var record in fixtures)
            {
                var removed = await holo.DeleteHolonAsync(record.Id);
                Assert.True(!removed.IsError && removed.IsDeleted, removed.Message);
                var absent = await holo.LoadHolonAsync(record.Id, loadChildren: false);
                Assert.Null(absent.Result);
                Assert.Equal(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, absent.ErrorCode);
            }
        }
        finally
        {
            if (holo.IsProviderActivated) await holo.DeActivateProviderAsync();
            if (unavailable.IsProviderActivated) await unavailable.DeActivateProviderAsync();
            await sqlite.DeActivateProviderAsync();
        }
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task LoadsNativeHolonByReturnedActionHash(bool sync)
    {
        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        await InstallIsolatedApp.Value;
        var admin = new Uri(Required("OASIS_HOLOCHAIN_ADMIN_URI"));
        Assert.True(admin.IsLoopback);
        var holo = OpenProvider(admin);
        var holon = new Holon { Id = Guid.NewGuid(), Name = $"action-hash fixture {Guid.NewGuid():N}", CreatedByAvatarId = Guid.NewGuid() };
        try
        {
            var active = await holo.ActivateProviderAsync();
            Assert.True(!active.IsError && active.Result, active.Message);
            var saved = await holo.SaveHolonAsync(holon, saveChildren: false);
            Assert.True(!saved.IsError && saved.IsSaved, saved.Message);
            try
            {
                var key = saved.Result.ProviderUniqueStorageKey[ProviderType.HoloOASIS];
                Assert.False(string.IsNullOrWhiteSpace(key));
                // A second provider must not change how the activated one decodes its native record.
                var otherProvider = new HoloOASIS(admin.AbsoluteUri, useHoloNetwork: false,
                    useHoloNETORMReflection: false);
                Assert.False(otherProvider.UseHoloNETORMReflection);
                Assert.True(holo.UseHoloNETORMReflection);
                var loaded = sync ? holo.LoadHolon(key, loadChildren: false)
                    : await holo.LoadHolonAsync(key, loadChildren: false);
                Assert.True(!loaded.IsError && loaded.IsLoaded, loaded.Message);
                Assert.Equal(holon.Id, loaded.Result.Id);
                Assert.Equal(holon.Name, loaded.Result.Name);

                var group = new SearchTextGroup { SearchHolons = true, HolonType = HolonType.All, SearchQuery = holon.Name };
                var search = new SearchParams { SearchOnlyForCurrentAvatar = false, SearchGroups = new List<ISearchGroupBase> { group } };
                var found = sync ? holo.Search(search, loadChildren: false) : await holo.SearchAsync(search, loadChildren: false);
                Assert.True(!found.IsError && found.IsLoaded, found.Message);
                Assert.Equal(holon.Id, Assert.Single(found.Result.SearchResultHolons).Id);
                Assert.Empty(found.Result.SearchResultAvatars);

                group.SearchQuery = $"absent {Guid.NewGuid():N}";
                var absent = sync ? holo.Search(search, loadChildren: false) : await holo.SearchAsync(search, loadChildren: false);
                Assert.True(!absent.IsError && absent.IsLoaded, absent.Message);
                Assert.Empty(absent.Result.SearchResultHolons);
            }
            finally
            {
                var removed = await holo.DeleteHolonAsync(holon.Id);
                Assert.True(!removed.IsError && removed.IsDeleted, removed.Message);
            }
        }
        finally
        {
            if (holo.IsProviderActivated) await holo.DeActivateProviderAsync();
        }
    }

    private static async Task InstallIsolatedAppAsync()
    {
        var endpoint = new Uri(Required("OASIS_HOLOCHAIN_ADMIN_URI"));
        Assert.True(endpoint.IsLoopback);
        var admin = OpenAdmin(endpoint);
        var connected = await admin.ConnectAsync();
        Assert.False(connected.IsError, connected.Message);
        try
        {
            var installed = await admin.InstallAppAsync(HoloOASIS.OASIS_HAPP_ID,
                Required("HOLONET_LIVE_HAPP_PATH"), network_seed: Guid.NewGuid().ToString("N"));
            Assert.False(installed.IsError, installed.Message);
        }
        finally
        {
            await admin.DisconnectAsync();
        }
    }

    private static HoloNETClientAdmin OpenAdmin(Uri endpoint)
        => new(new HoloNETDNA
        {
            HolochainConductorAdminURI = endpoint.AbsoluteUri,
            AutoStartHolochainConductor = false,
            AutoShutdownHolochainConductor = false
        });

    private static HoloOASIS OpenProvider(Uri endpoint)
        => new(OpenAdmin(endpoint), null!, useHoloNetwork: false);
    private static string Required(string name)
        => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value : throw new InvalidOperationException($"Missing fixture setting {name}.");
}
