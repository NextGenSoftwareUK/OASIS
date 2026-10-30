using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlAncestorTests
{
    /// <summary>Set to a mongod URI (a throwaway instance) to run the native test; otherwise it is inconclusive.</summary>
    private const string NativeUri = "OASIS_CQL_ANCESTOR_MONGO";

    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly Guid Group = Guid.NewGuid();
    private static readonly Guid Owner = Guid.NewGuid();

    private static MongoCqlCommand Compile(bool inherited)
    {
        var access = CosmicQueryReadAccess.OwnerPublicOrShared(Actor, new[] { Group }, inherited);
        Assert.IsFalse(access.IsError, access.Message);
        var query = CosmicQuery<Holon>.Create().WithReadAccess(access.Result).Result;
        return new MongoCqlTranslator().CompileExecution(query.OrderBy(holon => holon.Name).Result, 1000);
    }

    [TestMethod]
    public void TheInheritedRuleWalksUpOneBoundedLookupPerLevelThenCleansUp()
    {
        var pipeline = Compile(inherited: true).Pipeline;

        Assert.AreEqual(16, pipeline.Count(stage => stage.Contains("$lookup")));
        Assert.IsTrue(pipeline.Length <= 50, $"Inherited access must retain all16levels within hosted Mongo's50stage ceiling; observed{pipeline.Length}.");
        Assert.IsTrue(pipeline.Where(stage => stage.Contains("$lookup")).All(stage => stage["$lookup"]["from"] == "Holon"));
        Assert.IsTrue(pipeline.Any(stage => stage.Contains("$unset") && stage["$unset"].IsBsonArray && stage["$unset"].AsBsonArray.Contains("_anc0")));
        // The temporary fields never reach the caller, and the filter comes after the walk.
        var firstLookup = Array.FindIndex(pipeline, stage => stage.Contains("$lookup"));
        var match = Array.FindIndex(pipeline, stage => stage.Contains("$match"));
        Assert.IsTrue(firstLookup < match);
    }

    [TestMethod]
    public void WithoutInheritanceThereIsNoWalk()
    {
        Assert.AreEqual(0, Compile(inherited: false).Pipeline.Count(stage => stage.Contains("$lookup")));
    }

    private static BsonDocument Doc(Guid id, Guid? parent = null, Guid? creator = null, bool isPublic = false, params string[] flags)
    {
        var meta = new BsonDocument();
        foreach (var flag in flags) meta[flag] = "true";
        return new BsonDocument
        {
            { "HolonId", id.ToString("D") }, { "ParentHolonId", (parent ?? Guid.NewGuid()).ToString("D") },
            { "CreatedByAvatarId", (creator ?? Owner).ToString("D") }, { "IsPublic", isPublic }, { "IsActive", true },
            { "Name", id.ToString("N")[..8] }, { "Version", 1 }, { "MetaData", meta },
        };
    }

    [TestMethod]
    public void NativeWalkAdmitsExactlyWhatTheRuleDescribes()
    {
        var uri = Environment.GetEnvironmentVariable(NativeUri);
        if (string.IsNullOrEmpty(uri)) Assert.Inconclusive($"Set {NativeUri} to a throwaway mongod to run the native check.");

        var client = new MongoClient(uri);
        const string databaseName = "cosmic_cql_ancestor_tests";
        const string owner = "cosmic-cql-ancestor-tests-v1";
        var database = client.GetDatabase(databaseName);
        bool owned = false;
        try
        {
            Assert.AreEqual(0, database.ListCollectionNames().ToList().Count, "Never adopt/reset an existing namespace.");
            database.GetCollection<BsonDocument>("cosmic_test_owner").InsertOne(new BsonDocument("_id", owner));
            owned = true;
            var holons = database.GetCollection<BsonDocument>("Holon");
            var expected = new HashSet<string>();
            var all = new List<BsonDocument>();
            Guid Add(Guid? parent, bool visible, bool isPublic = false, Guid? creator = null, params string[] flags)
            {
                var id = Guid.NewGuid();
                all.Add(Doc(id, parent, creator, isPublic, flags));
                if (visible) expected.Add(id.ToString("D"));
                return id;
            }
            string Inherit(Guid who) => HolonReadIndex.InheritKeyFor(who);

            // plain rules
            Add(null, true, creator: Actor);                                                  // my own
            Add(null, true, isPublic: true);                                                  // public
            Add(null, true, flags: HolonReadIndex.KeyFor(Actor));                              // shared with me
            Add(null, true, flags: HolonReadIndex.KeyFor(Group));                              // shared with my group
            Add(null, false);                                                                  // nothing
            Add(null, false, flags: HolonReadIndex.KeyFor(Guid.NewGuid()));                    // shared with someone else

            // a tree: the granting holon, its child and grandchild
            var root = Add(null, true, flags: Inherit(Actor));
            var middle = Add(root, true);
            Add(middle, true);
            // someone else's tree
            var other = Add(null, false, flags: Inherit(Guid.NewGuid()));
            Add(other, false);
            // my group's tree, and a tree granted to everyone
            var groupRoot = Add(null, true, flags: Inherit(Group));
            Add(groupRoot, true);
            var publicRoot = Add(null, true, flags: HolonReadIndex.InheritPublicKey);
            Add(publicRoot, true);

            // bounded depth: with the flag at the top, the 16th level below is reachable and the 17th is not
            var parent = Add(null, true, flags: Inherit(Actor));
            for (var level = 1; level <= 20; level++) parent = Add(parent, level <= 16);

            // a parent that does not exist ends the walk quietly
            Add(Guid.NewGuid(), false);
            // a loop terminates, and the flagged half of it is visible
            var a = Guid.NewGuid();
            var b = Guid.NewGuid();
            all.Add(Doc(a, b, flags: Inherit(Actor)));
            all.Add(Doc(b, a));
            expected.Add(a.ToString("D")); expected.Add(b.ToString("D"));

            holons.InsertMany(all);

            var command = Compile(inherited: true);
            var found = holons.Aggregate<BsonDocument>(command.Pipeline).ToList();
            var ids = found.Select(row => (row.Contains("row") ? row["row"].AsBsonDocument : row)["HolonId"].AsString).ToHashSet();

            CollectionAssert.AreEquivalent(expected.OrderBy(x => x).ToList(), ids.OrderBy(x => x).ToList());
            Assert.IsFalse(found.Any(row => row.Names.Any(name => name.StartsWith("_anc"))), "Temporary fields leaked into the result.");
        }
        finally {
            if (owned) {
                var marker = database.GetCollection<BsonDocument>("cosmic_test_owner").Find(FilterDefinition<BsonDocument>.Empty).ToList();
                Assert.AreEqual(1, marker.Count); Assert.AreEqual(owner, marker.Single()["_id"].AsString);
                client.DropDatabase(databaseName);
            }
        }
    }
}
