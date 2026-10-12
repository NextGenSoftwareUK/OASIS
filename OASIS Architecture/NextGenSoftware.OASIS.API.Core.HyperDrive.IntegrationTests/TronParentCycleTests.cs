using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class TronParentCycleTests
{
    [Fact]
    public async Task RealEncryptedStoreRejectsRecursiveCycleAndAllowsBoundedRead()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/Use-ReusableTronRuntime.ps1"))) directory = directory.Parent;
        directory.Should().NotBeNull();
        var root = Path.Combine(directory!.FullName, "TestResults/HyperDriveTron"); Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var key = new string('0', 63) + "1";
        var contract = (await File.ReadAllTextAsync(Path.Combine(directory.FullName, "TestResults/TronRuntime/local-store-address.txt"))).Trim();
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"),
            "0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f", contractFeeLimitSun: 1000000000);
        using var storage = new TronPrivateStorageBackend(runtime.NativeTransfers, new TronNet.TronECKey(key, TronNet.TronNetwork.MainNet).GetPublicAddress(),
            contract, () => key, () => SHA256.HashData(Encoding.UTF8.GetBytes("public reusable local TRON encryption fixture")));
        var provider = new TRONOASIS(rpcEndpoint: "http://127.0.0.1:9090", nativeTransfers: runtime.NativeTransfers, storageBackend: storage);
        var id = Guid.Parse("45506b2c-414a-4bfb-9b68-019399d73287"); var saved = false;
        try
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var prior = await provider.LoadHolonAsync(id, loadChildren: false);
            prior.ErrorCode.Should().Be("OASIS_RECORD_NOT_FOUND"); prior.Result.Should().BeNull();
            (await provider.SaveHolonAsync(new Holon { Id = id, ParentHolonId = id, Name = "Owned real parent-cycle fixture" })).IsError.Should().BeFalse(); saved = true;
            var cyclic = await provider.LoadHolonAsync(id, continueOnError: false);
            cyclic.IsError.Should().BeTrue(); cyclic.Exception.Should().BeOfType<InvalidDataException>();
            cyclic.Message.Should().Contain("Cyclic holon parent relationship");
            provider.LoadHolonsForParent(id).IsError.Should().BeTrue();
            var bounded = await provider.LoadHolonAsync(id, maxChildDepth: 1);
            bounded.IsError.Should().BeFalse(bounded.Message); bounded.Result.Children.Should().ContainSingle(value => value.Id == id);
        }
        finally
        {
            if (saved) (await provider.DeleteHolonAsync(id)).IsError.Should().BeFalse();
            await provider.DeActivateProviderAsync();
        }
    }
}
