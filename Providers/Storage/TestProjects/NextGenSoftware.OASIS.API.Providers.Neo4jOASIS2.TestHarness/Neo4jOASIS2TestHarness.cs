using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Neo4jProvider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS2;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS2.TestHarness;

public static class Neo4jOASIS2TestHarness
{
    public static async Task<int> Main()
    {
        var endpoint = Environment.GetEnvironmentVariable("OASIS_NEO4J_V2_URI");
        var username = Environment.GetEnvironmentVariable("OASIS_NEO4J_V2_USERNAME");
        var password = Environment.GetEnvironmentVariable("OASIS_NEO4J_V2_PASSWORD");
        if (Environment.GetEnvironmentVariable("OASIS_NEO4J_V2_CONFORMANCE") != "1" ||
            !Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("bolt" or "bolt+s" or "bolt+ssc" or "neo4j" or "neo4j+s" or "neo4j+ssc") ||
            !string.IsNullOrEmpty(uri.UserInfo) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Console.Error.WriteLine("Enable conformance and configure the dedicated Neo4j fixture URI and credentials.");
            return 1;
        }

        var provider = new Neo4jProvider(endpoint, username, password);
        try
        {
            var active = await provider.ActivateProviderAsync();
            Require(active.Result && !active.IsError, active.Message);
            try
            {
                await VerifyStorageAsync(provider);
            }
            finally
            {
                var inactive = await provider.DeActivateProviderAsync();
                Require(inactive.Result && !inactive.IsError, inactive.Message);
            }

            Console.WriteLine("Neo4j V2 native avatar, holon and search checks passed.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }

    private static async Task VerifyStorageAsync(Neo4jProvider provider)
    {
        var marker = Guid.NewGuid().ToString("N");
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = $"neo4jv2-{marker}", Email = $"{marker}@example.invalid", Version = 1 };
        var holon = new Holon { Id = Guid.NewGuid(), Name = $"neo4jv2-{marker}", HolonType = HolonType.Holon, Version = 1 };

        var avatarSave = await provider.SaveAvatarAsync(avatar);
        Require(avatarSave.IsSaved && !avatarSave.IsError, avatarSave.Message);
        try
        {
            var avatarLoad = await provider.LoadAvatarAsync(avatar.Id);
            Require(!avatarLoad.IsError && avatarLoad.Result?.Username == avatar.Username, avatarLoad.Message);

            var holonSave = await provider.SaveHolonAsync(holon, saveChildren: false);
            Require(holonSave.IsSaved && !holonSave.IsError, holonSave.Message);
            try
            {
                var holonLoad = await provider.LoadHolonAsync(holon.Id, loadChildren: false);
                Require(!holonLoad.IsError && holonLoad.Result?.Name == holon.Name, holonLoad.Message);

                var search = await provider.SearchAsync(new SearchParams
                {
                    SearchOnlyForCurrentAvatar = false,
                    SearchGroups = new()
                    {
                        new SearchTextGroup
                        {
                            SearchHolons = true,
                            SearchQuery = marker,
                            HolonType = HolonType.All,
                            HolonSearchParams = new SearchHolonParams { Name = true }
                        }
                    }
                }, loadChildren: false);
                Require(!search.IsError && search.Result?.SearchResultHolons.SingleOrDefault()?.Id == holon.Id, search.Message);
            }
            finally
            {
                var deleted = await provider.DeleteHolonAsync(holon.Id);
                Require(deleted.IsDeleted && !deleted.IsError, deleted.Message);
            }
        }
        finally
        {
            var deleted = await provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
            Require(deleted.IsDeleted && !deleted.IsError, deleted.Message);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
