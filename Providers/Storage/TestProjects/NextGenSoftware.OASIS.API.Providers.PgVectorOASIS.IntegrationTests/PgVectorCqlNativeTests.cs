using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Npgsql;

namespace NextGenSoftware.OASIS.API.Providers.PgVectorOASIS.IntegrationTests;

[TestClass]
public class PgVectorCqlNativeTests
{
    [TestMethod]
    [TestCategory("PgVectorNativeCql")]
    public async Task RealPgVectorRequiresTheVectorSchemaAndReadsWithRestrictedRole()
    {
        Assert.AreEqual("1", Environment.GetEnvironmentVariable("COSMIC_PGVECTOR_DISPOSABLE"));
        const string owner = "Host=127.0.0.1;Port=55439;Database=oasis_teaching;Username=postgres;Password=disposable-owner";
        const string reader = "Host=127.0.0.1;Port=55439;Database=oasis_teaching;Username=teaching_reader;Password=disposable-reader;Maximum Pool Size=1";
        const ProviderType type = ProviderType.PgVectorOASIS;
        await using var setup = new NpgsqlConnection(owner);
        await setup.OpenAsync();
        await using (var fresh = new NpgsqlCommand("SELECT to_regclass('public.oasis_holons') IS NULL", setup))
            Assert.AreEqual(true, await fresh.ExecuteScalarAsync());
        var initialized = await new PgVectorOASIS(owner).ActivateProviderAsync();
        Assert.IsFalse(initialized.IsError, initialized.Exception?.ToString() ?? initialized.Message);

        await using (var roles = new NpgsqlCommand("""
            CREATE ROLE teaching_reader LOGIN PASSWORD 'disposable-reader';
            GRANT USAGE ON SCHEMA public TO teaching_reader;
            GRANT SELECT (id, doc) ON public.oasis_holons TO teaching_reader;
            CREATE SCHEMA foreign_fixture;
            CREATE TABLE foreign_fixture.oasis_holons (id UUID PRIMARY KEY, doc JSONB NOT NULL);
            GRANT USAGE ON SCHEMA foreign_fixture TO teaching_reader;
            GRANT SELECT (id, doc) ON foreign_fixture.oasis_holons TO teaching_reader;
            """, setup))
            await roles.ExecuteNonQueryAsync();

        var ids = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid())
            .OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var actor = Guid.NewGuid();
        for (var position = 0; position < 5; position++)
        {
            var id = ids[position];
            await using var insert = new NpgsqlCommand("""
                INSERT INTO public.oasis_holons (id, doc) VALUES (@id, @doc::jsonb)
                """, setup);
            insert.Parameters.AddWithValue("id", id);
            insert.Parameters.AddWithValue("doc", JsonSerializer.Serialize(new
            {
                Id = id, CreatedByAvatarId = actor, Name = "pgvector-" + id.ToString("D"),
                IsPublic = position != 3, DeletedDate = position == 4 ? DateTime.UtcNow : DateTime.MinValue,
                Children = new[] { new { Id = ids[3], IsPublic = false } }
            }));
            Assert.AreEqual(1, await insert.ExecuteNonQueryAsync());
        }

        var provider = new PgVectorOASIS(reader, ids[0]);
        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString() ?? activated.Message);
        CosmicQuery<Holon> Query(params Guid[] keys) => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(type).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, type, keys).Result).Result;
        CosmicQueryPlan<Holon> Plan(CosmicQuery<Holon> query) =>
            CosmicQueryPlanner.Plan(query, type, new[] { new CosmicQueryTarget(type, provider.QueryCapabilities) }).Result;
        var query = Query(ids[0], ids[1], ids[2], ids[4], ids[5]);
        var all = await provider.ExecuteCosmicQueryAsync(Plan(query));
        Assert.IsFalse(all.IsError, all.Exception?.ToString() ?? all.Message);
        CollectionAssert.AreEqual(ids.Take(3).ToArray(), all.Result.Select(row => row.Id).ToArray());
        Assert.IsTrue(all.Result.All(row => !row.Children.Any()));
        var subset = new[] { ids[0], ids[2] };
        var filtered = await provider.ExecuteCosmicQueryAsync(Plan(query.Where(row => subset.Contains(row.Id)).Result));
        Assert.IsFalse(filtered.IsError, filtered.Exception?.ToString() ?? filtered.Message);
        CollectionAssert.AreEqual(subset, filtered.Result.Select(row => row.Id).ToArray());
        var absent = await provider.ExecuteCosmicQueryAsync(Plan(Query(ids[5])));
        Assert.IsFalse(absent.IsError, absent.Exception?.ToString() ?? absent.Message);
        Assert.AreEqual(0, absent.Result.Count);
        var denied = await provider.ExecuteCosmicQueryAsync(Plan(Query(ids[0], ids[3])));
        Assert.IsTrue(denied.IsError);
        Assert.IsNull(denied.Result);

        await using (var copy = new NpgsqlCommand("""
            INSERT INTO foreign_fixture.oasis_holons (id, doc)
            SELECT id, doc FROM public.oasis_holons WHERE id = @id
            """, setup))
        {
            copy.Parameters.AddWithValue("id", ids[0]);
            Assert.AreEqual(1, await copy.ExecuteNonQueryAsync());
        }
        var foreign = new PgVectorOASIS(reader + ";Search Path=foreign_fixture", ids[0]);
        Assert.IsTrue((await foreign.ActivateQuerySourceAsync()).IsError);
        Assert.IsFalse(foreign.IsProviderActivated);
        var writer = new PgVectorOASIS(owner, ids[0]);
        Assert.IsTrue((await writer.ActivateQuerySourceAsync()).IsError);
        Assert.IsFalse(writer.IsProviderActivated);
    }
}
