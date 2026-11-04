using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class RetainedLocalFileCandidateFactAttribute : FactAttribute
{
    public RetainedLocalFileCandidateFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("OASIS_VALIDATE_LOCALFILE_HOSTED_SOURCE") != "1")
            Skip = "Explicit retained public-source candidate validation required;no source creation in normal tests.";
    }
}

public class LocalFileRetainedCandidateTests
{
    [RetainedLocalFileCandidateFact]
    public void ExactRetainedExportIsAdmittedByShippingBootstrapWithoutWritesOrCqlReplay()
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !System.IO.File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1")))
            root = root.Parent;
        Assert.NotNull(root);
        var state = Path.Combine(root.FullName, "LocalState/COSMIC/HostedLocalFilePublicDatasetCurrent");
        using var ack = JsonDocument.Parse(System.IO.File.ReadAllBytes(Path.Combine(state, "export.acknowledged.json")));
        Assert.Equal(3, ack.RootElement.GetProperty("sdkFull").GetInt32());
        Assert.Equal(2, ack.RootElement.GetProperty("sdkSubset").GetInt32());
        var sourceBytes = System.IO.File.ReadAllBytes(Path.Combine(state, "source-config.json"));
        var bundleBytes = System.IO.File.ReadAllBytes(Path.Combine(state, "bootstrap.json"));
        Assert.Equal(ack.RootElement.GetProperty("sourceConfigSha256").GetString(),
            Convert.ToHexString(SHA256.HashData(sourceBytes)).ToLowerInvariant());
        Assert.Equal(ack.RootElement.GetProperty("bootstrapSha256").GetString(),
            Convert.ToHexString(SHA256.HashData(bundleBytes)).ToLowerInvariant());
        var hosted = JsonNode.Parse(sourceBytes)!;
        var local = JsonNode.Parse(System.IO.File.ReadAllBytes(Path.Combine(state, "local-source.json")))!;
        Assert.Equal("/data/cql-local-file-demo", hosted["directory"]!.GetValue<string>());
        // The target path is deliberately Linux metadata. Validate identical
        // schema/IDs/owner/hashes using the already verified Windows source path.
        hosted["directory"] = local["directory"]!.GetValue<string>();
        Assert.True(JsonNode.DeepEquals(hosted, local));
        using var source = LocalFilePublicCqlProfile.FromJson(local.ToJsonString());
        Assert.NotNull(LocalFilePublicBootstrap.FromJson(source, System.Text.Encoding.UTF8.GetString(bundleBytes)));
        Assert.False(source.IsProviderActivated);
        // FromJson validates the full canonical public bundle;StartAsync is
        // intentionally not called and no acknowledged CQL query is repeated.
    }

    [RetainedLocalFileCandidateFact]
    public async Task ExactRetainedBundleStartsAndActivatesInAnIsolatedDirectory()
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !System.IO.File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1")))
            root = root.Parent;
        if (root == null) throw new DirectoryNotFoundException("Assigned checkout required.");
        var state = Path.Combine(root.FullName, "LocalState/COSMIC/HostedLocalFilePublicDatasetCurrent");
        var sourceConfig = JsonNode.Parse(await System.IO.File.ReadAllTextAsync(Path.Combine(state, "source-config.json")))!;
        var bundle = await System.IO.File.ReadAllTextAsync(Path.Combine(state, "bootstrap.json"));
        var parent = Path.Combine(root.FullName, "TestResults/HostedLocalFileSourceCurrent/replacement-bootstrap-cases");
        var owned = Path.GetFullPath(Path.Combine(parent, Guid.NewGuid().ToString("D")));
        if (Path.GetDirectoryName(owned) != Path.GetFullPath(parent) || Directory.Exists(owned))
            throw new IOException("A fresh owned bootstrap directory is required.");
        Directory.CreateDirectory(owned);
        try
        {
            sourceConfig["directory"] = Path.Combine(owned, "source");
            using var profile = LocalFilePublicCqlProfile.FromJson(sourceConfig.ToJsonString());
            await LocalFilePublicBootstrap.FromJson(profile, bundle).StartAsync(default);
            var activation = await profile.ActivateQuerySourceAsync(default);
            Assert.False(activation.IsError, activation.Message);
            Assert.True(profile.IsProviderActivated);
        }
        finally
        {
            Directory.Delete(owned, recursive: true);
        }
    }
}
