using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [DataTestMethod]
    [DataRow(false, false, false)] [DataRow(true, false, false)]
    [DataRow(false, true, false)] [DataRow(true, true, false)]
    [DataRow(false, false, true)] [DataRow(true, false, true)]
    [DataRow(false, true, true)] [DataRow(true, true, true)]
    public Task NativeOrdinaryRoleOwnershipSurvivesCommit(bool synchronous, bool batch, bool selected)
        => WithNativeGraph(async fixture =>
        {
            var root = fixture.Root;
            async Task<string?> StoredManifest()
            {
                using var session = fixture.Driver.AsyncSession();
                var row = await (await session.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot",
                    new { id = root.Id.ToString() })).SingleAsync();
                using var json = JsonDocument.Parse(row["snapshot"].As<string>());
                return json.RootElement.TryGetProperty("ChildRoleManifestJson", out var roles)
                    ? roles.GetString() : null;
            }
            var expected = selected
                ? HolonChildRoleManifestJson.Serialize(HolonChildCollectionState.Capture(root).CreateManifest(root.Id, child => child.Id))
                : await StoredManifest();
            Assert.IsNotNull(selected ? expected : await StoredManifest());

            // Caller metadata cannot erase a stored role observation on a root-only
            // save. A selected empty graph must return and acknowledge its own roles.
            root.ChildRoleManifestJson = null;
            await fixture.Record($"before-role-ownership-sync-{synchronous}-batch-{batch}-selected-{selected}");
            fixture.Known = false;
            Holon acknowledged;
            if (batch)
            {
                var saved = synchronous ? fixture.Provider.SaveHolons(new[] { root }, saveChildren: selected)
                    : await fixture.Provider.SaveHolonsAsync(new[] { root }, saveChildren: selected);
                fixture.Known = !saved.IsError && saved.IsSaved;
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved);
                acknowledged = (Holon)saved.Result.Single();
            }
            else
            {
                var saved = synchronous ? fixture.Provider.SaveHolon(root, saveChildren: selected)
                    : await fixture.Provider.SaveHolonAsync(root, saveChildren: selected);
                fixture.Known = !saved.IsError && saved.IsSaved;
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved);
                acknowledged = (Holon)saved.Result;
            }
            Assert.AreEqual(expected, await StoredManifest());
            Assert.AreEqual(expected, acknowledged.ChildRoleManifestJson);
            Assert.AreEqual(expected, root.ChildRoleManifestJson);
            if (selected) HolonChangeTracking.ValidateChildSelectionForSave(root);
        }, manifested: !selected, width: 0);
}