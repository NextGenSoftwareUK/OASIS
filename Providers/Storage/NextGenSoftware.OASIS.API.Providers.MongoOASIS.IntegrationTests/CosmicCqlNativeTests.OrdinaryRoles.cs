using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(true, false, false)]
    [InlineData(false, true, false)] [InlineData(true, true, false)]
    [InlineData(false, false, true)] [InlineData(true, false, true)]
    [InlineData(false, true, true)] [InlineData(true, true, true)]
    public Task NativeOrdinaryRoleOwnershipSurvivesCommit(bool synchronous, bool batch, bool selected)
        => WithMongoGraph(async fixture =>
        {
            var root = fixture.Root;
            var before = Assert.Single(await fixture.Records.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync());
            var expected = selected
                ? HolonChildRoleManifestJson.Serialize(HolonChildCollectionState.Capture(root).CreateManifest(root.Id, child => child.Id))
                : before["ChildRoleManifestJson"].AsString;

            // A root-only update must retain the stored role observation even if
            // caller metadata is cleared. Selected empty membership must capture
            // and acknowledge its canonical manifest, including on the single path.
            root.ChildRoleManifestJson = null;
            await fixture.Database.GetCollection<BsonDocument>("CosmicOwnedFixture").InsertOneAsync(
                new BsonDocument { { "ordinaryRoleOwnershipProbe", true }, { "root", root.Id.ToString() },
                    { "synchronous", synchronous }, { "batch", batch }, { "selected", selected } });
            fixture.KnownOutcome = false;
            Holon acknowledged;
            if (batch)
            {
                var saved = synchronous ? fixture.Provider.SaveHolons(new[] { root }, saveChildren: selected)
                    : await fixture.Provider.SaveHolonsAsync(new[] { root }, saveChildren: selected);
                fixture.KnownOutcome = !saved.IsError && saved.IsSaved;
                Assert.False(saved.IsError, saved.Message + saved.Exception);
                Assert.True(saved.IsSaved);
                acknowledged = Assert.IsType<Holon>(Assert.Single(saved.Result));
            }
            else
            {
                var saved = synchronous ? fixture.Provider.SaveHolon(root, saveChildren: selected)
                    : await fixture.Provider.SaveHolonAsync(root, saveChildren: selected);
                fixture.KnownOutcome = !saved.IsError && saved.IsSaved;
                Assert.False(saved.IsError, saved.Message + saved.Exception);
                Assert.True(saved.IsSaved);
                acknowledged = Assert.IsType<Holon>(saved.Result);
            }

            var stored = Assert.Single(await fixture.Records.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync());
            Assert.Equal(expected, stored.GetValue("ChildRoleManifestJson", BsonNull.Value).IsBsonNull
                ? null : stored["ChildRoleManifestJson"].AsString);
            Assert.Equal(expected, acknowledged.ChildRoleManifestJson);
            Assert.Equal(expected, root.ChildRoleManifestJson);
            if (selected)
            {
                // A successful acknowledgement grants a subsequent selected save;
                // mere assignment of manifest JSON would not grant that observation.
                HolonChangeTracking.ValidateChildSelectionForSave(root);
            }
        }, manifest: !selected, width: 0);
}