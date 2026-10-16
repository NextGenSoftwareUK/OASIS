using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [TestMethod]
    public async Task OwnedNativeTransientDescendantFailuresRequireExplicitPartialDiagnostics()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = raw.AsyncSession();
        Assert.AreEqual(0L, (await (await inspection.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync())["count"].As<long>());
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var root = new Holon { Id = Guid.NewGuid() }; root.MetaData["selected"] = "root";
            var bad = new Holon { Id = Guid.NewGuid() }; var good = new Holon { Id = Guid.NewGuid() };
            var hidden = new Holon { Id = Guid.NewGuid() }; var visible = new Holon { Id = Guid.NewGuid() };
            root.Children = new List<IHolon> { bad, good }; bad.Children = new List<IHolon> { hidden }; good.Children = new List<IHolon> { visible };
            Assert.IsFalse((await provider.SaveHolonAsync(root)).IsError);
            var field = typeof(Provider).GetField("_driver", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var healthy = (IDriver)field.GetValue(provider)!;
            foreach (var scope in new[] { "point", "parent", "collection", "metadata" })
            foreach (var synchronous in new[] { false, true })
            foreach (var continuation in new[] { false, true })
            {
                var cause = new IOException("owned transient descendant read failure"); var observations = 0;
                field.SetValue(provider, Neo4jReadFaultProxy.Wrap(healthy, bad.Id.ToString(), cause, () => observations++));
                try
                {
                    IHolon? badRead = null; IHolon? goodRead = null;
                    bool error, loaded, warning; Exception failure; Dictionary<string, string> metadata;
                    if (scope == "point")
                    {
                        var read = synchronous ? provider.LoadHolon(root.Id, continueOnError:continuation) : await provider.LoadHolonAsync(root.Id, continueOnError:continuation);
                        error = read.IsError; loaded = read.IsLoaded; warning = read.IsWarning; failure = read.Exception; metadata = read.MetaData;
                        if (!error) { badRead = read.Result.Children.Single(child => child.Id == bad.Id); goodRead = read.Result.Children.Single(child => child.Id == good.Id); }
                    }
                    else
                    {
                        var read = scope == "parent"
                            ? synchronous ? provider.LoadHolonsForParent(root.Id, continueOnError:continuation) : await provider.LoadHolonsForParentAsync(root.Id, continueOnError:continuation)
                            : scope == "metadata"
                                ? synchronous ? provider.LoadHolonsByMetaData("selected", "root", continueOnError:continuation) : await provider.LoadHolonsByMetaDataAsync("selected", "root", continueOnError:continuation)
                                : synchronous ? provider.LoadAllHolons(continueOnError:continuation) : await provider.LoadAllHolonsAsync(continueOnError:continuation);
                        error = read.IsError; loaded = read.IsLoaded; warning = read.IsWarning; failure = read.Exception; metadata = read.MetaData;
                        if (!error)
                        {
                            var children = scope == "parent" ? read.Result : read.Result.Single(record => record.Id == root.Id).Children;
                            badRead = children.Single(child => child.Id == bad.Id); goodRead = children.Single(child => child.Id == good.Id);
                        }
                    }
                    Assert.IsTrue(observations > 0, "Actual SDK read boundary must be exercised.");
                    if (!continuation) { Assert.IsTrue(error); Assert.IsFalse(loaded); Assert.AreSame(cause, failure); }
                    else
                    {
                        Assert.IsFalse(error); Assert.IsTrue(loaded); Assert.IsTrue(warning);
                        Assert.AreEqual("true", metadata["partialHolonReads"]);
                        StringAssert.Contains(metadata["failedHolonReads"], bad.Id.ToString());
                        Assert.AreEqual(0, badRead!.Children.Count()); Assert.AreEqual(visible.Id, goodRead!.Children.Single().Id);
                    }
                }
                finally { field.SetValue(provider, healthy); }
            }
            await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",
                new { ids = new[] { root.Id, bad.Id, good.Id, hidden.Id, visible.Id }.Select(id => id.ToString()).ToList() })).ConsumeAsync();
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
