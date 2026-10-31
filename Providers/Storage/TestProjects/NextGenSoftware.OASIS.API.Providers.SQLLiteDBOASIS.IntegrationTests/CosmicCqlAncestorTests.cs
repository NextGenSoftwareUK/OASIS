using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task NativeCqlAncestorWalkAdmitsExactlyWhatTheInheritedRuleDescribes()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actor = Guid.NewGuid(); var group = Guid.NewGuid(); var owner = Guid.NewGuid();
        var expected = new HashSet<Guid>();
        var made = new HashSet<Guid>();

        async Task<Guid> Add(Guid? parent, bool visible, bool isPublic = false, Guid? creator = null, params string[] flags)
        {
            var holon = new Holon { Id = Guid.NewGuid(), Name = "n" + Guid.NewGuid().ToString("N")[..6], ParentHolonId = parent ?? Guid.NewGuid(),
                CreatedByAvatarId = creator ?? owner, IsPublic = isPublic };
            foreach (var flag in flags) holon.MetaData[flag] = "true";
            var saved = await provider.SaveHolonAsync(holon, saveChildren: false);
            saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
            made.Add(holon.Id);
            if (visible) expected.Add(holon.Id);
            return holon.Id;
        }
        string Inherit(Guid who) => HolonReadIndex.InheritKeyFor(who);

        var plain = new HashSet<Guid>
        {
            await Add(null, true, creator: actor),
            await Add(null, true, isPublic: true),
            await Add(null, true, flags: HolonReadIndex.KeyFor(actor)),
            await Add(null, true, flags: HolonReadIndex.KeyFor(group)),
        };
        await Add(null, false);
        await Add(null, false, flags: HolonReadIndex.KeyFor(Guid.NewGuid()));

        var root = await Add(null, true, flags: Inherit(actor));
        var middle = await Add(root, true);
        await Add(middle, true);
        var other = await Add(null, false, flags: Inherit(Guid.NewGuid()));
        await Add(other, false);
        var groupRoot = await Add(null, true, flags: Inherit(group));
        await Add(groupRoot, true);
        var publicRoot = await Add(null, true, flags: HolonReadIndex.InheritPublicKey);
        await Add(publicRoot, true);

        // the 16th level below the flag is reachable, the 17th is not
        var parent = await Add(null, true, flags: Inherit(actor));
        for (var level = 1; level <= 20; level++) parent = await Add(parent, level <= 16);

        // a parent that is not a stored holon ends the walk without error
        await Add(Guid.NewGuid(), false);

        var access = CosmicQueryReadAccess.OwnerPublicOrShared(actor, new[] { group }, includeInherited: true);
        access.IsError.Should().BeFalse(access.Message);
        var query = CosmicQuery<Holon>.Create().WithReadAccess(access.Result).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.SQLLiteDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, SQLLiteDBOASIS.CqlCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);

        using var context = new DataContext(TestConnectionString);
        var repository = new HolonRepository(context, () => false);
        var read = await repository.ExecuteCosmicQueryAsync(plan.Result, 1000, default);

        read.IsError.Should().BeFalse(read.Message + read.Exception);
        // The database may hold other holons; judge exactly the ones this test made, hidden ones included.
        var returned = read.Result!.Select(h => h.Id).Where(made.Contains).ToHashSet();
        returned.Should().BeEquivalentTo(expected);
        expected.Count.Should().BeLessThan(made.Count);

        // Control: without inheritance only the plain-rule holons come back, so the walk is what admitted the rest.
        var plainAccess = CosmicQueryReadAccess.OwnerPublicOrShared(actor, new[] { group }).Result;
        var plainPlan = CosmicQueryPlanner.Plan(CosmicQuery<Holon>.Create().WithReadAccess(plainAccess).Result, ProviderType.SQLLiteDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, SQLLiteDBOASIS.CqlCapabilities) }).Result;
        var plainRead = await repository.ExecuteCosmicQueryAsync(plainPlan, 1000, default);
        plainRead.IsError.Should().BeFalse(plainRead.Message + plainRead.Exception);
        plainRead.Result!.Select(h => h.Id).Where(made.Contains).ToHashSet().Should().BeEquivalentTo(
            plain);
    }
}
