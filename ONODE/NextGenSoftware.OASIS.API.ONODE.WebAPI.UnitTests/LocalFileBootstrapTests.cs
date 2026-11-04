using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class LocalFileBootstrapTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly string parent;
        internal readonly string Case;
        internal string DirectoryPath => Path.Combine(Case, "source");
        internal readonly Guid Owner = Guid.NewGuid();
        internal readonly Guid[] Ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        internal readonly Dictionary<Guid, byte[]> Bytes = new();
        internal readonly LocalFilePublicCqlProfile Source;
        internal string SourceJson { get; }
        internal string Bundle { get; }
        internal string LockPath => Path.Combine(Case, ".source.cql-bootstrap.lock");

        internal Fixture(string invalid = "")
        {
            DirectoryInfo? root = new(AppContext.BaseDirectory);
            while (root != null && !System.IO.File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1")))
                root = root.Parent;
            if (root == null) throw new DirectoryNotFoundException("Assigned checkout required.");
            parent = Path.Combine(root.FullName, "TestResults/HostedLocalFileSourceCurrent/bootstrap-cases");
            Case = Path.Combine(parent, Guid.NewGuid().ToString("D"));
            if (Directory.Exists(Case)) throw new IOException("Fresh owned test case required.");
            Directory.CreateDirectory(Case);
            for (int index = 0; index < Ids.Length; index++)
                Bytes[Ids[index]] = Encoding.UTF8.GetBytes(OasisJson.Serialize(new Holon {
                    Id = Ids[index], Name = "Public teaching coffee " + index,
                    IsPublic = invalid != "private" || index != 2,
                    CreatedByAvatarId = invalid == "owner" && index == 2 ? Guid.NewGuid() : Owner }));
            var hashes = Bytes.ToDictionary(pair => pair.Key, pair => Convert.ToHexString(SHA256.HashData(pair.Value)).ToLowerInvariant());
            Source = new(DirectoryPath + Path.DirectorySeparatorChar, Owner, hashes);
            SourceJson = JsonSerializer.Serialize(new { schema = 1, directory = DirectoryPath,
                sourceOwner = Owner, records = hashes.Select(pair => new { id = pair.Key, sha256 = pair.Value }).ToArray() });
            Bundle = JsonSerializer.Serialize(new { schema = 1, records = Bytes.Select(pair => new {
                id = pair.Key, sha256 = hashes[pair.Key], contentBase64 = Convert.ToBase64String(pair.Value) }).ToArray() });
        }

        public void Dispose()
        {
            Source.Dispose();
            if (Path.GetDirectoryName(Path.GetFullPath(Case)) != Path.GetFullPath(parent))
                throw new InvalidOperationException("Owned cleanup escaped test parent.");
            Directory.Delete(Case, true);
        }
    }

    [Fact]
    public async Task ExplicitBootstrapCreatesCanonicalPublicFilesAndReusesAcknowledgedBytesUnchanged()
    {
        using var fixture = new Fixture();
        await LocalFilePublicBootstrap.FromJson(fixture.Source, fixture.Bundle).StartAsync(default);
        Assert.False((await fixture.Source.ActivateQuerySourceAsync(default)).IsError);
        var access = CosmicQueryReadAccess.AuthoritativeRecords(fixture.Owner, ProviderType.LocalFileOASIS, fixture.Ids).Result;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.LocalFileOASIS).Result).WithReadAccess(access).Result;
        foreach (var count in new[] { 3, 2 })
        {
            var selected = fixture.Ids.Take(count).ToArray();
            var plan = CosmicQueryPlanner.Plan(query.Where(row => selected.Contains(row.Id)).Result, ProviderType.LocalFileOASIS,
                new[] { new CosmicQueryTarget(ProviderType.LocalFileOASIS, fixture.Source.QueryCapabilities) });
            Assert.False(plan.IsError, plan.Message);
            var result = await fixture.Source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(result.IsError, result.Message);
            Assert.Equal(count, result.Result.Count);
        }
        var before = Directory.GetFiles(fixture.DirectoryPath, "*", SearchOption.AllDirectories)
            .ToDictionary(path => path, path => (Bytes: System.IO.File.ReadAllBytes(path), Modified: System.IO.File.GetLastWriteTimeUtc(path)));
        await LocalFilePublicBootstrap.FromJson(fixture.Source, fixture.Bundle).StartAsync(default);
        foreach (var entry in before)
        {
            Assert.Equal(entry.Value.Bytes, System.IO.File.ReadAllBytes(entry.Key));
            Assert.Equal(entry.Value.Modified, System.IO.File.GetLastWriteTimeUtc(entry.Key));
        }
    }

    [Fact]
    public async Task PartialOriginalSourceWithoutAckIsNeverAdoptedOrRewritten()
    {
        using var fixture = new Fixture();
        Directory.CreateDirectory(Path.Combine(fixture.DirectoryPath, "Holons"));
        var original = Path.Combine(fixture.DirectoryPath, "Holons", fixture.Ids[0] + ".json");
        System.IO.File.WriteAllBytes(original, fixture.Bytes[fixture.Ids[0]]);
        await Assert.ThrowsAsync<InvalidDataException>(() => LocalFilePublicBootstrap.FromJson(fixture.Source, fixture.Bundle).StartAsync(default));
        Assert.Equal(fixture.Bytes[fixture.Ids[0]], System.IO.File.ReadAllBytes(original));
        Assert.Single(Directory.GetFiles(Path.Combine(fixture.DirectoryPath, "Holons")));
        Assert.False(System.IO.File.Exists(Path.Combine(fixture.DirectoryPath, "cql-bootstrap.acknowledged.json")));
    }

    [Fact]
    public async Task ChangedAcknowledgedRecordFailsWithoutRestoringOldData()
    {
        using var fixture = new Fixture();
        var bootstrap = LocalFilePublicBootstrap.FromJson(fixture.Source, fixture.Bundle);
        await bootstrap.StartAsync(default);
        var path = Path.Combine(fixture.DirectoryPath, "Holons", fixture.Ids[1] + ".json");
        System.IO.File.WriteAllText(path, "changed owned test record");
        await Assert.ThrowsAsync<InvalidDataException>(() => bootstrap.StartAsync(default));
        Assert.Equal("changed owned test record", System.IO.File.ReadAllText(path));
    }

    [Theory]
    [InlineData("private")]
    [InlineData("owner")]
    public void FullBundleValidationRejectsInvalidLastRecordBeforeAnyDatasetWrite(string mode)
    {
        using var fixture = new Fixture(mode);
        Assert.Throws<ArgumentException>(() => LocalFilePublicBootstrap.FromJson(fixture.Source, fixture.Bundle));
        Assert.False(Directory.Exists(fixture.DirectoryPath));
        Assert.Empty(Directory.GetFiles(fixture.Case));
    }

    [Fact]
    public void CorruptLastPayloadCannotCreatePartOfTheEarlierValidRecords()
    {
        using var fixture = new Fixture();
        var json = JsonNode.Parse(fixture.Bundle)!;
        json["records"]![2]!["contentBase64"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("wrong hash"));
        Assert.Throws<ArgumentException>(() => LocalFilePublicBootstrap.FromJson(fixture.Source, json.ToJsonString()));
        Assert.False(Directory.Exists(fixture.DirectoryPath));
    }

    [Fact]
    public async Task BusyBootstrapLeaseDoesNotStartAnotherProducer()
    {
        using var fixture = new Fixture();
        using var held = new FileStream(fixture.LockPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        await Assert.ThrowsAsync<IOException>(() => LocalFilePublicBootstrap.FromJson(fixture.Source, fixture.Bundle).StartAsync(default));
        Assert.False(Directory.Exists(fixture.DirectoryPath));
    }

    [Fact]
    public async Task CancellationBeforeOwnershipCreatesNeitherSourceNorLock()
    {
        using var fixture = new Fixture();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => LocalFilePublicBootstrap.FromJson(fixture.Source, fixture.Bundle)
            .StartAsync(new CancellationToken(true)));
        Assert.False(Directory.Exists(fixture.DirectoryPath));
        Assert.False(System.IO.File.Exists(fixture.LockPath));
    }

    [Fact]
    public void BootstrapRequiresSandboxModeBeforeAnyRegistration()
    {
        using var fixture = new Fixture();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            [LocalFileCqlRegistration.ConfigurationKey] = fixture.SourceJson,
            [LocalFileCqlRegistration.BootstrapConfigurationKey] = fixture.Bundle }).Build();
        var services = new ServiceCollection();
        Assert.Throws<ArgumentException>(() => LocalFileCqlRegistration.AddConfigured(services, configuration, sandboxEnabled: false));
        Assert.Empty(services);
        Assert.False(Directory.Exists(fixture.DirectoryPath));
    }

    [Fact]
    public async Task RegistrationStartsBootstrapBeforeSourceActivationAndDoesNotGrantReadAccess()
    {
        using var fixture = new Fixture();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            [LocalFileCqlRegistration.ConfigurationKey] = fixture.SourceJson,
            [LocalFileCqlRegistration.BootstrapConfigurationKey] = fixture.Bundle }).Build();
        var services = new ServiceCollection();
        LocalFileCqlRegistration.AddConfigured(services, configuration, sandboxEnabled: true);
        using var provider = services.BuildServiceProvider();
        var profile = Assert.IsType<LocalFilePublicCqlProfile>(Assert.Single(provider.GetServices<CqlReadSourceProfile>()).Storage);
        Assert.False(profile.IsProviderActivated);
        await Assert.Single(provider.GetServices<IHostedService>()).StartAsync(default);
        Assert.False(profile.IsProviderActivated);
        Assert.False((await profile.ActivateQuerySourceAsync(default)).IsError);
        Assert.Empty(provider.GetServices<CqlPublicReadCatalogue>());
        provider.Dispose();
        Assert.False(profile.IsProviderActivated);
    }
}
