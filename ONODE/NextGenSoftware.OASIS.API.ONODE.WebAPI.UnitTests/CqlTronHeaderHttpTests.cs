using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlTronHeaderHttpTests
{
    [ReviewedPublicMongoNativeFact("COSMIC_TRON_PUBLIC_READER_KEY")]
    public async Task AuthenticatedTronHeadersAndPrivateMongoCopiesOverHttp()
    {
        Assert.Equal("owned-loopback", Environment.GetEnvironmentVariable("COSMIC_TRON_HEADER_NATIVE_HTTP"));
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var path = Path.Combine(root, "Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests/Fixtures/tron-public-header-source.json");
        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        Assert.Equal("7f1c8fe3c35ef3c45ee31be6eaef72f34f8d10185a28e2177ace267822e89c02",
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        using var catalogue = JsonDocument.Parse(bytes);
        var owner = catalogue.RootElement.GetProperty("sourceOwner").GetGuid();
        var heights = catalogue.RootElement.GetProperty("blockHeights").EnumerateArray()
            .Select(item => item.GetString()!).ToArray();
        Assert.Equal(new[] { "1", "2", "3" }, heights);
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        var key = Environment.GetEnvironmentVariable("COSMIC_TRON_PUBLIC_READER_KEY")!;
        await ReviewedPublicMongoHttpFixture.VerifyHttpAsync(ProviderType.TRONOASIS, _ =>
            (TronPublicHeaderMetadataSource.FromAuthenticatedJson(json, key), heights.Select(height =>
                NativeRecordHolonMapper.StableHolonId(owner, ProviderType.TRONOASIS,
                    "tron-public-solidified:" + TronPublicHeaderMetadataSource.PublicGenesis, height)).ToArray()),
            metadataField: TronPublicHeaderMetadataSource.AddressMetadata, metadataKeys: heights);
    }
}
