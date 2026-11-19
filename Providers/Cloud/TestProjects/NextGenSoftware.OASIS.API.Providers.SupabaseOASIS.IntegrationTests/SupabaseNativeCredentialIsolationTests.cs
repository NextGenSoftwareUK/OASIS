using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS.IntegrationTests;

[TestClass]
public sealed class SupabaseNativeCredentialIsolationTests
{
    [TestMethod]
    public async Task SharedClientSendsEachCredentialOnlyToItsPostgrestService()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SUPABASE_LOCAL_CREDENTIAL_TEST") != "1")
            Assert.Inconclusive("Enable only for the two disposable PostgREST services.");

        var firstEndpoint = RequireLoopbackEndpoint("OASIS_SUPABASE_FIRST_POSTGREST");
        var secondEndpoint = RequireLoopbackEndpoint("OASIS_SUPABASE_SECOND_POSTGREST");
        var firstKey = Environment.GetEnvironmentVariable("OASIS_SUPABASE_FIRST_JWT");
        var secondKey = Environment.GetEnvironmentVariable("OASIS_SUPABASE_SECOND_JWT");
        Assert.AreNotEqual(firstEndpoint, secondEndpoint);
        Assert.IsFalse(string.IsNullOrWhiteSpace(firstKey));
        Assert.IsFalse(string.IsNullOrWhiteSpace(secondKey));
        Assert.IsTrue(firstKey != secondKey, "Fixture credentials must differ.");

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        var first = new SupabaseOASIS(firstEndpoint, firstKey!, client, restApiPath: "");
        var second = new SupabaseOASIS(secondEndpoint, secondKey!, client, restApiPath: "");
        var crossed = new SupabaseOASIS(secondEndpoint, firstKey!, client, restApiPath: "");
        try
        {
            var denied = await crossed.ActivateProviderAsync();
            Assert.IsTrue(denied.IsError && !crossed.IsProviderActivated);
            Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, (denied.Exception as HttpRequestException)?.StatusCode);

            var activated = await Task.WhenAll(first.ActivateProviderAsync(), second.ActivateProviderAsync());
            foreach (var result in activated)
                Assert.IsTrue(result.Result && !result.IsError, result.Message);
            Assert.IsFalse(client.DefaultRequestHeaders.Contains("apikey"));
            Assert.IsNull(client.DefaultRequestHeaders.Authorization);

            var firstHolon = new Holon { Id = Guid.NewGuid(), Name = "first provider", HolonType = HolonType.Holon, Version = 1 };
            var secondHolon = new Holon { Id = Guid.NewGuid(), Name = "second provider", HolonType = HolonType.Holon, Version = 1 };
            var saves = await Task.WhenAll(
                first.SaveHolonAsync(firstHolon, saveChildren: false),
                second.SaveHolonAsync(secondHolon, saveChildren: false));
            foreach (var result in saves)
                Assert.IsTrue(result.IsSaved && !result.IsError, result.Message);

            var loaded = await Task.WhenAll(
                first.LoadHolonAsync(firstHolon.Id, loadChildren: false),
                second.LoadHolonAsync(secondHolon.Id, loadChildren: false));
            Assert.IsTrue(loaded.All(result => result.IsLoaded && !result.IsError));
            CollectionAssert.AreEqual(new[] { firstHolon.Id, secondHolon.Id }, loaded.Select(result => result.Result.Id).ToArray());
        }
        finally
        {
            await Task.WhenAll(first.DeActivateProviderAsync(), second.DeActivateProviderAsync(), crossed.DeActivateProviderAsync());
        }
    }

    private static string RequireLoopbackEndpoint(string variable)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        Assert.IsTrue(Uri.TryCreate(value, UriKind.Absolute, out var endpoint) && endpoint.IsLoopback &&
            endpoint.Scheme == Uri.UriSchemeHttp && endpoint.AbsolutePath == "/" && string.IsNullOrEmpty(endpoint.UserInfo) &&
            string.IsNullOrEmpty(endpoint.Query) && string.IsNullOrEmpty(endpoint.Fragment), "Use an isolated loopback PostgREST service.");
        return value!;
    }
}
