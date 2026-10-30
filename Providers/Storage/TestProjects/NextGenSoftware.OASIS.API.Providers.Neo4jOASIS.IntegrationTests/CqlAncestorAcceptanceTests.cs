using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
public sealed class CqlAncestorAcceptanceTests
{
    /// <summary>A throwaway Neo4j bolt URI (password oasis-local-test-only). Unset: the native check is inconclusive.</summary>
    private const string NativeUri = "OASIS_CQL_ANCESTOR_NEO4J";

    [TestMethod]
    public void TheRuleIsAdmittedAndRenderedAsAnInlineQuantifiedPath()
    {
        var access = CosmicQueryReadAccess.OwnerPublicOrShared(Guid.NewGuid(), new[] { Guid.NewGuid() }, includeInherited: true).Result;
        var query = CosmicQuery<Holon>.Create().WithReadAccess(access).Result;
        Assert.IsFalse(Neo4jOASIS.CqlCapabilities.Admit(query).IsError);

        var translator = new NeoCqlTranslator(access);
        var cypher = translator.Translate(query.Predicates.Single());
        StringAssert.Contains(cypher, "<-[:HAS_CHILD]-");
        StringAssert.Contains(cypher, "){1,16}(");
    }

    [TestMethod]
    public async Task NativeWalkAdmitsExactlyWhatTheInheritedRuleDescribes()
    {
        var uri = Environment.GetEnvironmentVariable(NativeUri);
        if (string.IsNullOrEmpty(uri)) Assert.Inconclusive($"Set {NativeUri} to a throwaway Neo4j to run the native check.");

        var actor = Guid.NewGuid(); var group = Guid.NewGuid(); var owner = Guid.NewGuid();
        var expected = new HashSet<Guid>(); var made = new HashSet<Guid>();
        var plainExpected = new HashSet<Guid>();

        Holon Make(bool visible, bool plain = false, bool isPublic = false, Guid? creator = null, params string[] flags)
        {
            var holon = new Holon { Id = Guid.NewGuid(), Name = "n" + Guid.NewGuid().ToString("N")[..6], CreatedByAvatarId = creator ?? owner, IsPublic = isPublic };
            foreach (var flag in flags) holon.MetaData[flag] = "true";
            made.Add(holon.Id);
            if (visible) expected.Add(holon.Id);
            if (plain) plainExpected.Add(holon.Id);
            return holon;
        }
        Holon Child(Holon parent, bool visible)
        {
            var child = Make(visible);
            child.ParentHolonId = parent.Id;
            parent.Children.Add(child);
            return child;
        }
        string Inherit(Guid who) => HolonReadIndex.InheritKeyFor(who);

        var provider = new Neo4jOASIS(uri, "neo4j", "oasis-local-test-only") { IsVersionControlEnabled = false };
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var roots = new List<Holon>
        {
            Make(true, plain: true, creator: actor),
            Make(true, plain: true, isPublic: true),
            Make(true, plain: true, flags: HolonReadIndex.KeyFor(actor)),
            Make(true, plain: true, flags: HolonReadIndex.KeyFor(group)),
            Make(false),
            Make(false, flags: HolonReadIndex.KeyFor(Guid.NewGuid())),
        };

        // a tree granted to me, someone else's tree, my group's tree, a tree granted to everyone
        var root = Make(true, flags: Inherit(actor)); roots.Add(root);
        Child(Child(root, true), true);
        var other = Make(false, flags: Inherit(Guid.NewGuid())); roots.Add(other);
        Child(other, false);
        var groupRoot = Make(true, flags: Inherit(group)); roots.Add(groupRoot);
        Child(groupRoot, true);
        var publicRoot = Make(true, flags: HolonReadIndex.InheritPublicKey); roots.Add(publicRoot);
        Child(publicRoot, true);

        // depth: with the flag at the top, the 16th level below is reachable and the 17th is not
        var top = Make(true, flags: Inherit(actor)); roots.Add(top);
        var parent = top;
        for (var level = 1; level <= 20; level++) parent = Child(parent, level <= 16);

        foreach (var holon in roots)
        {
            var saved = await provider.SaveHolonAsync(holon, saveChildren: true);
            Assert.IsFalse(saved.IsError, saved.Message);
        }

        var adapter = (ICosmicQueryStorageProvider)provider;
        async Task<HashSet<Guid>> Read(bool inherited)
        {
            var access = CosmicQueryReadAccess.OwnerPublicOrShared(actor, new[] { group }, inherited).Result;
            var query = CosmicQuery<Holon>.Create().WithReadAccess(access).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.Neo4jOASIS,
                new[] { new CosmicQueryTarget(ProviderType.Neo4jOASIS, Neo4jOASIS.CqlCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var read = await adapter.ExecuteCosmicQueryAsync(plan.Result, maximumResults: 1000);
            Assert.IsFalse(read.IsError, read.Message);
            return read.Result!.Select(h => h.Id).Where(made.Contains).ToHashSet();
        }

        // The database may hold other holons; judge exactly the ones this test made, hidden ones included.
        CollectionAssert.AreEquivalent(expected.ToList(), (await Read(inherited: true)).ToList());
        // Control: without inheritance only the plain-rule holons come back, so the walk is what admitted the rest.
        CollectionAssert.AreEquivalent(plainExpected.ToList(), (await Read(inherited: false)).ToList());
        Assert.IsTrue(expected.Count < made.Count);
    }
}
