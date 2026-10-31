using FluentAssertions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public class PreparedFailureProxy : DispatchProxy, IHolonWritePreparationStorageProvider, IConditionalHolonStorageProvider
    {
        public SQLLiteDBOASIS Native = null!;
        public bool AfterCommit;
        public int NativeWrites;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            try { return method!.Invoke(Native, args); }
            catch (TargetInvocationException error) when (error.InnerException != null)
            { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        public OASISResult<HolonWritePreparation> ClaimHolonWritePreparation(HolonWritePreparation candidate) => Native.ClaimHolonWritePreparation(candidate);
        public Task<OASISResult<HolonWritePreparation>> ClaimHolonWritePreparationAsync(HolonWritePreparation candidate, CancellationToken token = default)
            => Native.ClaimHolonWritePreparationAsync(candidate, token);
        public OASISResult<IEnumerable<IHolon>> ExecuteConditionalHolonWrite(ConditionalHolonWriteRequest request)
            => Fault(request, false).GetAwaiter().GetResult();
        public Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonWriteAsync(ConditionalHolonWriteRequest request, CancellationToken token = default)
            => Fault(request, true);
        private async Task<OASISResult<IEnumerable<IHolon>>> Fault(ConditionalHolonWriteRequest request, bool asyncNative)
        {
            if (!AfterCommit) return new() { IsError = true, ErrorCode = "CONTROLLED_BEFORE_MUTATION", Message = "abandoned after durable preparation" };
            var committed = asyncNative ? await Native.ExecuteConditionalHolonWriteAsync(request) : Native.ExecuteConditionalHolonWrite(request);
            committed.IsError.Should().BeFalse(committed.Message + committed.Exception); NativeWrites++;
            return new() { IsError = true, ErrorCode = "COSMIC_WRITE_RECONCILIATION_REQUIRED", Message = "controlled lost application acknowledgement",
                Exception = new SqliteMutationReconciliationException(committed.Result.Select(row => row.Id), new IOException("application acknowledgement lost")) };
        }
    }
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public async Task DurableEncryptedPreparationRecoversAfterAbandonedDispatchOrLostCommittedAcknowledgement(bool synchronous, bool afterCommit)
    {
        using var native = new SQLLiteDBOASIS(TestConnectionString); (await native.ActivateProviderAsync()).IsError.Should().BeFalse();
        var wrapper = DispatchProxy.Create<IOASISStorageProvider, PreparedFailureProxy>();
        var fault = (PreparedFailureProxy)(object)wrapper; fault.Native = native; fault.AfterCommit = afterCommit;
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2,
            StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
        var providers = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        var writer = new HolonManager(wrapper, dna, providers) { MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver()) };
        var op = Guid.NewGuid(); var actor = Guid.NewGuid();
        Holon Input() => new() { Name = "recoverable", MetaData = new() { ["private-note"] = "original private intent" } };
        var source = Input(); var failed = synchronous
            ? writer.SaveHolon(source, actor, new CosmicWriteOptions(op), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
            : await writer.SaveHolonAsync(source, actor, new CosmicWriteOptions(op), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        failed.IsError.Should().BeTrue(); source.Id.Should().BeEmpty(); source.Original.Should().BeNull();
        source.MetaData["private-note"].Should().Be("original private intent"); fault.NativeWrites.Should().Be(afterCommit ? 1 : 0);
        var journal = ReadNativePreparation(op); journal.Should().NotContain("original private intent");
        using (var control = new DataContext(TestConnectionString)) control.Holons.AsNoTracking().Count().Should().Be(afterCommit ? 1 : 0);
        using var restarted = new SQLLiteDBOASIS(TestConnectionString); (await restarted.ActivateProviderAsync()).IsError.Should().BeFalse();
        var reader = CosmicManager(restarted, true); reader.MetadataEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver(), "key-b");
        var fresh = Input(); var recovered = synchronous
            ? reader.SaveHolon(fresh, actor, new CosmicWriteOptions(op), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
            : await reader.SaveHolonAsync(fresh, actor, new CosmicWriteOptions(op), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        recovered.IsError.Should().BeFalse(recovered.Message + recovered.Exception); fresh.Version.Should().Be(1); fresh.Id.Should().NotBeEmpty();
        fresh.MetaData["private-note"].ToString().Should().Be("original private intent"); ReadNativePreparation(op).Should().Be(journal);
        using var final = new DataContext(TestConnectionString); final.Holons.AsNoTracking().Count().Should().Be(1);
        Revision((await restarted.LoadHolonAsync(fresh.Id, loadChildren: false)).Result).Should().Be(Revision(fresh));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OlderMetadataCipherConditionalReplayUsesExplicitAuthenticatedPreparationPolicy(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        HolonManager Manager(string key)
        {
            var manager = CosmicManager(provider, true); manager.PreparationEncryptionPolicy = NativeMetadataPolicy(new NativeMetadataResolver(), key);
            manager.OASISDNA.OASIS.Security = new SecuritySettings { HolonDataEncryption = new EncryptionSettings {
                Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('e', 32) } };
            return manager;
        }
        var op = Guid.NewGuid(); var actor = Guid.NewGuid();
        Holon Input() => new() { MetaData = new() { ["private-note"] = "older format private", ["STARNETHolonId"] = "queryable-id" } };
        var original = Input(); var first = await Manager("key-a").SaveHolonAsync(original, actor, new CosmicWriteOptions(op), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        first.IsError.Should().BeFalse(first.Message + first.Exception); var revision = Revision(original); var version = original.VersionId;
        var stored = (await provider.LoadHolonAsync(original.Id, loadChildren: false)).Result;
        stored.MetaData["__oasis_enc__"].ToString().Should().StartWith("r256:"); stored.MetaData["STARNETHolonId"].ToString().Should().Be("queryable-id");
        var fresh = Input(); var retry = Manager("key-b"); var replay = synchronous
            ? retry.SaveHolon(fresh, actor, new CosmicWriteOptions(op), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS)
            : await retry.SaveHolonAsync(fresh, actor, new CosmicWriteOptions(op), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        replay.IsError.Should().BeFalse(replay.Message + replay.Exception); Revision(fresh).Should().Be(revision); fresh.VersionId.Should().Be(version);
        fresh.MetaData["private-note"].ToString().Should().Be("older format private");
    }
}
