using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS.IntegrationTests;

/// <summary>Explicit opt-in: requires the admitted reusable native fixture runner.</summary>
[TestClass]
public class SupabaseNativeCqlTests
{
    [TestMethod]
    public async Task OriginalNativeRecordsAnswerFilteredQueriesAndSurviveRestart()
    {
        var root = Environment.GetEnvironmentVariable("COSMIC_SUPABASE_ROOT");
        Assert.IsFalse(string.IsNullOrWhiteSpace(root), "Use run-cosmic-supabase-linux.py; never seed an arbitrary service.");
        using var config = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root!, "identity.json")));
        var ids = config.RootElement.GetProperty("ids").EnumerateArray().Select(v => v.GetGuid()).ToArray();
        var actor = config.RootElement.GetProperty("actor").GetGuid();
        var apiKey = await File.ReadAllTextAsync(Path.Combine(root!, "api-token"));
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var provider = new SupabaseOASIS("http://127.0.0.1:18091", apiKey, http, restApiPath: "");
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Exception?.ToString() ?? active.Message);

        // Only the first explicitly admitted run may create data. Each mutation has
        // a durable intent before dispatch; an interrupted run cannot silently replay.
        if (Environment.GetEnvironmentVariable("COSMIC_SUPABASE_SEED") == "1")
        {
            for (var i = 0; i < ids.Length; i++)
            {
                var intent = Path.Combine(root!, $"write-{i}.pending");
                using (var writer = new FileStream(intent, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var data = System.Text.Encoding.UTF8.GetBytes(ids[i].ToString("D"));
                    writer.Write(data); writer.Flush(true);
                }
                var row = new Holon { Id = ids[i], Name = $"Supabase CQL demo {i + 1}", CreatedByAvatarId = actor,
                    HolonType = HolonType.Holon, CreatedDate = DateTime.UtcNow, IsActive = true };
                var saved = await provider.SaveHolonAsync(row, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Exception?.ToString() ?? saved.Message);
                Assert.IsTrue(saved.IsSaved);
                Assert.AreEqual(1, saved.SavedCount);
                File.Move(intent, Path.Combine(root!, $"write-{i}.ack"));
            }
            var pending = Path.Combine(root!, "delete.pending");
            using (var writer = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var data = System.Text.Encoding.UTF8.GetBytes(ids[3].ToString("D"));
                writer.Write(data); writer.Flush(true);
            }
            var deleted = await provider.DeleteHolonAsync(ids[3]);
            Assert.IsFalse(deleted.IsError, deleted.Exception?.ToString() ?? deleted.Message);
            File.Move(pending, Path.Combine(root!, "delete.ack"));
        }

        // Explicit trusted record scope, then a native membership filter. Missing
        // and tombstoned records must not occupy the requested result page.
        var missing = config.RootElement.GetProperty("missing").GetGuid();
        var catalogue = ids.Append(missing).ToArray();
        var scope = CosmicQueryScope.Named(ProviderType.SupabaseOASIS).Result;
        var access = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.SupabaseOASIS, catalogue).Result;
        var query = CosmicQuery<Holon>.Create(scope).WithReadAccess(access).Result;
        async Task<Guid[]> Read(CosmicQuery<Holon> request)
        {
            var plan = CosmicQueryPlanner.Plan(request, ProviderType.SupabaseOASIS,
                new[] { new CosmicQueryTarget(ProviderType.SupabaseOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
            return result.Result.Select(row => row.Id).ToArray();
        }
        CollectionAssert.AreEqual(ids.Take(3).OrderBy(v => v.ToString("D"), StringComparer.Ordinal).ToArray(), await Read(query));
        var selected = new[] { ids[0], ids[2], ids[3], missing };
        var filtered = query.Where(row => selected.Contains(row.Id)).Result;
        CollectionAssert.AreEqual(new[] { ids[0], ids[2] }.OrderBy(v => v.ToString("D"), StringComparer.Ordinal).ToArray(), await Read(filtered));
        var page = filtered.Skip(1).Result.Take(1).Result;
        Assert.AreEqual(1, (await Read(page)).Length);
        var unsupported = query.Where(row => row.Name == "unsupported").Result;
        Assert.IsTrue(CosmicQueryPlanner.Plan(unsupported, ProviderType.SupabaseOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SupabaseOASIS, provider.QueryCapabilities) }).IsError);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}

