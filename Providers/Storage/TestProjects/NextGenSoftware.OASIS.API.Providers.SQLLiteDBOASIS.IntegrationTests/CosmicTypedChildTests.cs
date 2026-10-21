using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public class TypedSaveRoot : Holon
    {
        private readonly List<IHolon> _members = new();
        public void Add(IHolon child) => _members.Add(child);
        public override IReadOnlyCollection<IHolon> AllChildren => Children.Concat(_members).ToArray();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()
            => base.CaptureChildCollections().Concat(new[] {
                new HolonChildCollection("Members", _members, values => {
                    _members.Clear(); _members.AddRange(values);
                }) }).ToArray();
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task V2PersistsTypedChildrenAndTracksTheirEdits(bool synchronous, bool providerChildren)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CosmicManager(provider, true);
        var root = new TypedSaveRoot { Name = "typed-root" };
        var child = new Holon { Name = "typed-child" };
        root.Add(child);
        var actor = Guid.NewGuid();
        var saved = synchronous
            ? manager.SaveHolon(root, actor, saveChildrenOnProvider: providerChildren, providerType: ProviderType.SQLLiteDBOASIS)
            : await manager.SaveHolonAsync(root, actor, saveChildrenOnProvider: providerChildren, providerType: ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
        child.Id.Should().NotBeEmpty();
        child.ParentHolonId.Should().Be(root.Id);
        root.Children.Should().BeEmpty("caller collection roles must remain intact");
        root.HasHolonChanged().Should().BeFalse();
        var loaded = await provider.LoadHolonAsync(root.Id);
        loaded.IsError.Should().BeFalse(loaded.Message + loaded.Exception);
        loaded.Result.Children.Should().ContainSingle().Which.Id.Should().Be(child.Id);
        child.Name = "edited-typed-child";
        var updated = await manager.SaveHolonAsync(root, actor, saveChildrenOnProvider: providerChildren, providerType: ProviderType.SQLLiteDBOASIS);
        updated.IsError.Should().BeFalse(updated.Message + updated.Exception);
        (await provider.LoadHolonAsync(child.Id)).Result.Name.Should().Be(child.Name);
        root.HasHolonChanged().Should().BeFalse();
    }
}
