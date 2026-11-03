using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.IntegrationTests;

[TestClass]
public class LocalFilePublicCqlProfileTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly string parent;
        internal readonly string Root;
        internal readonly Guid Owner = Guid.NewGuid();
        internal readonly Guid[] Ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        internal readonly Dictionary<Guid, string> Hashes = new();
        internal string FilePath(Guid id) => Path.Combine(Root, "Holons", id.ToString("D") + ".json");

        internal Fixture()
        {
            parent = Path.GetFullPath(Path.Combine(FindRoot(), "TestResults/HostedLocalFileSourceCurrent/cases"));
            Root = Path.Combine(parent, Guid.NewGuid().ToString("D"));
            if (Directory.Exists(Root)) throw new IOException("New test case already exists.");
            Directory.CreateDirectory(Path.Combine(Root, "Holons"));
            for (int index = 0; index < Ids.Length; index++) Write(Ids[index], "Public coffee " + index);
        }

        private static string FindRoot()
        {
            for (var current = new DirectoryInfo(AppContext.BaseDirectory); current != null; current = current.Parent)
                if (File.Exists(Path.Combine(current.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1")))
                    return current.FullName;
            throw new DirectoryNotFoundException("Assigned checkout root required.");
        }

        internal void Write(Guid id, string name, bool isPublic = true, Guid? owner = null)
        {
            var text = OasisJson.Serialize(new Holon { Id = id, Name = name, IsPublic = isPublic,
                CreatedByAvatarId = owner ?? Owner });
            File.WriteAllText(FilePath(id), text);
            Hashes[id] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(FilePath(id))));
        }

        internal LocalFilePublicCqlProfile Profile() => new(Root, Owner, Hashes);
        public void Dispose()
        {
            if (Path.GetDirectoryName(Path.GetFullPath(Root)) != parent)
                throw new InvalidOperationException("Owned test cleanup escaped its exact parent.");
            Directory.Delete(Root, true); // Only this newly created synthetic case.
        }
    }

    private static Task<OASISResult<IReadOnlyList<IHolon>>> Read(Fixture fixture,
        ICosmicQueryStorageProvider adapter, Guid[]? selected = null)
    {
        var ids = selected ?? fixture.Ids;
        var scope = CosmicQueryScope.Named(ProviderType.LocalFileOASIS).Result;
        var access = CosmicQueryReadAccess.AuthoritativeRecords(fixture.Owner, ProviderType.LocalFileOASIS, ids).Result;
        var query = CosmicQuery<Holon>.Create(scope).WithReadAccess(access).Result;
        query = query.Where(row => ids.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.LocalFileOASIS,
            new[] { new CosmicQueryTarget(ProviderType.LocalFileOASIS, adapter.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return adapter.ExecuteCosmicQueryAsync(plan.Result);
    }

    [TestMethod]
    public async Task PublicProfileReadsThreeToTwoThroughNativeDecoderWithoutChangingSource()
    {
        using var fixture = new Fixture();
        using var profile = fixture.Profile();
        var original = fixture.Ids.ToDictionary(id => id, id => File.ReadAllBytes(fixture.FilePath(id)));
        var active = await profile.ActivateQuerySourceAsync(default);
        Assert.IsFalse(active.IsError, active.Message);
        var full = await Read(fixture, profile);
        var subset = await Read(fixture, profile, fixture.Ids.Take(2).ToArray());
        Assert.IsFalse(full.IsError, full.Message);
        Assert.IsFalse(subset.IsError, subset.Message);
        Assert.AreEqual(3, full.Result.Count);
        Assert.AreEqual(2, subset.Result.Count);
        foreach (var id in fixture.Ids) CollectionAssert.AreEqual(original[id], File.ReadAllBytes(fixture.FilePath(id)));
        Assert.IsFalse(Directory.Exists(Path.Combine(fixture.Root, "Avatars")));
        Assert.IsFalse(Directory.Exists(Path.Combine(fixture.Root, "AvatarDetails")));
    }

    [DataTestMethod]
    [DataRow("private")]
    [DataRow("owner")]
    [DataRow("hash")]
    public async Task ActivationRejectsNonPublicForeignOrChangedDeclaredSource(string mode)
    {
        using var fixture = new Fixture();
        if (mode == "private") fixture.Write(fixture.Ids[0], "private", false);
        else if (mode == "owner") fixture.Write(fixture.Ids[0], "foreign", owner: Guid.NewGuid());
        else fixture.Hashes[fixture.Ids[0]] = new string('0', 64);
        using var profile = fixture.Profile();
        var result = await profile.ActivateQuerySourceAsync(default);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", result.ErrorCode);
        Assert.IsFalse(profile.IsProviderActivated);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task LaterPrivateOrForeignRowCannotBePublishedThroughInterface(bool foreignOwner)
    {
        using var fixture = new Fixture();
        using var profile = fixture.Profile();
        Assert.IsFalse((await profile.ActivateQuerySourceAsync(default)).IsError);
        fixture.Write(fixture.Ids[1], "changed", !foreignOwner ? false : true,
            foreignOwner ? Guid.NewGuid() : fixture.Owner);
        var result = await Read(fixture, (ICosmicQueryStorageProvider)profile);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        Assert.IsFalse(result.IsLoaded);
    }

    [TestMethod]
    public async Task CallerGrantAndMutableInputCatalogueCannotExpandPublicNativeSelection()
    {
        using var fixture = new Fixture();
        using var profile = fixture.Profile();
        var foreign = Guid.NewGuid();
        fixture.Write(foreign, "unlisted-private", false);
        // Constructor owns a snapshot; modifying the original dictionary grants nothing.
        Assert.IsFalse((await profile.ActivateQuerySourceAsync(default)).IsError);
        var result = await Read(fixture, profile, new[] { foreign });
        Assert.AreEqual("CQL_INVALID_SOURCE_KEY", result.ErrorCode);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task MissingSourceAndCancelledActivationNeverCreateDirectoriesOrPublishReadiness()
    {
        using var fixture = new Fixture();
        var absent = Path.Combine(fixture.Root, "absent");
        using var missing = new LocalFilePublicCqlProfile(absent, fixture.Owner, fixture.Hashes);
        Assert.IsTrue((await missing.ActivateQuerySourceAsync(default)).IsError);
        Assert.IsFalse(Directory.Exists(absent));
        using var cancelled = fixture.Profile();
        var result = await cancelled.ActivateQuerySourceAsync(new CancellationToken(true));
        Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.IsFalse(cancelled.IsProviderActivated);
    }
}
