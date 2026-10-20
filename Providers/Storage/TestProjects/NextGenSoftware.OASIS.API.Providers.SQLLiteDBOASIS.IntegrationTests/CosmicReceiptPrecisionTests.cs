using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ConditionalReceiptReplayPreservesExactRootAndChildNumbersAfterRestart(bool synchronous)
    {
        const decimal before = 1.000000000000000000000000001m;
        const decimal after = 1.000000000000000000000000002m;
        var original = new Holon { Id = Guid.NewGuid(), Name = "original receipt", MetaData = new() {
            ["amount"] = before, ["wide"] = double.MaxValue, ["null"] = null! } };
        var child = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["nested"] = new object[] { before, "001.00" } } };
        original.Children.Add(child); var op = Guid.NewGuid(); var actor = Guid.NewGuid();
        var request = ConditionalRequest(original, null, op, actor, new[] {
            new HolonWriteCondition(original.Id, null), new HolonWriteCondition(child.Id, null) });
        Guid originalRevision; Guid logicalVersion;
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var created = synchronous ? provider.ExecuteConditionalHolonWrite(request) : await provider.ExecuteConditionalHolonWriteAsync(request);
            created.IsError.Should().BeFalse(created.Message + created.Exception);
            var first = created.Result.Single(); originalRevision = Revision(first); logicalVersion = first.VersionId;
            var latest = (await provider.LoadHolonAsync(original.Id, loadChildren: false)).Result;
            latest.Name = "later revision"; latest.MetaData["amount"] = after;
            var update = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.SQLLiteDBOASIS,
                new[] { new HolonWriteCondition(latest.Id, originalRevision) }), ConditionalHolonOperation.Save, new[] { latest }, saveChildren: false);
            var updated = await provider.ExecuteConditionalHolonWriteAsync(update); updated.IsError.Should().BeFalse(updated.Message + updated.Exception);
        }
        using var restarted = new SQLLiteDBOASIS(TestConnectionString);
        (await restarted.ActivateProviderAsync()).IsError.Should().BeFalse();
        var replay = synchronous ? restarted.ExecuteConditionalHolonWrite(request) : await restarted.ExecuteConditionalHolonWriteAsync(request);
        replay.IsError.Should().BeFalse(replay.Message + replay.Exception);
        var root = replay.Result.Single(); root.Name.Should().Be("original receipt");
        Revision(root).Should().Be(originalRevision); root.VersionId.Should().Be(logicalVersion);
        var precise = before.ToString(System.Globalization.CultureInfo.InvariantCulture);
        StoredRecordJson.Serialize(root.MetaData).Should().Contain(precise).And.Contain(double.MaxValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        root.MetaData.Should().ContainKey("null"); root.MetaData["null"].Should().BeNull();
        StoredRecordJson.Serialize(root.Children.Single().MetaData).Should().Contain(precise).And.Contain("001.00");
        var current = (await restarted.LoadHolonAsync(root.Id, loadChildren: false)).Result;
        current.Name.Should().Be("later revision"); Revision(current).Should().NotBe(originalRevision);
        StoredRecordJson.Serialize(current.MetaData).Should().Contain(after.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
