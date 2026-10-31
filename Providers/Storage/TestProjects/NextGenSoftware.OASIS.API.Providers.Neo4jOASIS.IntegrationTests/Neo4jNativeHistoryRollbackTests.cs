using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task OwnedNativeHistoryRollsBackArchiveWhenCurrentWriteIsRejected(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        const string constraint="oasis_owned_history_rollback_username";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspection=raw.AsyncSession();
        Assert.AreEqual(0L,(await(await inspection.RunAsync("MATCH (a) WHERE a:Avatar OR a:AvatarDetail OR a:OasisRecordVersion RETURN count(a) AS count")).SingleAsync())["count"].As<long>());
        Assert.AreEqual(0,(await(await inspection.RunAsync("SHOW CONSTRAINTS YIELD name WHERE name=$name RETURN name",new{name=constraint})).ToListAsync()).Count,
            "An existing constraint must not be replaced or removed.");
        var provider=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=true};
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var marker=Guid.NewGuid().ToString("N");
            var first=new Avatar{Id=Guid.NewGuid(),Username=$"rollback-a-{marker}",FirstName="original"};
            var second=new Avatar{Id=Guid.NewGuid(),Username=$"rollback-b-{marker}"};
            Assert.IsFalse((await provider.SaveAvatarAsync(first)).IsError);
            Assert.IsFalse((await provider.SaveAvatarAsync(second)).IsError);
            Console.WriteLine("Owned Neo history rollback fixture: "+OasisJson.Serialize(new{marker,firstId=first.Id,secondId=second.Id,constraint}));
            var originalVersion=first.Version;var originalVersionId=first.VersionId;var originalUsername=first.Username;
            await(await inspection.RunAsync("CREATE CONSTRAINT "+constraint+" FOR (a:Avatar) REQUIRE a.Username IS UNIQUE")).ConsumeAsync();
            first.Username=second.Username;first.FirstName="must roll back";
            var rejected=synchronous?provider.SaveAvatar(first):await provider.SaveAvatarAsync(first);
            Assert.IsTrue(rejected.IsError);
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(rejected.Exception),"Native rollback was acknowledged before reporting the constraint rejection.");
            var nativeError=rejected.Exception as Neo4jException;
            Assert.IsNotNull(nativeError,"Rejection must reach the actual current-record constraint, not fail earlier in archival preparation.");
            StringAssert.Contains(nativeError.Code,"ConstraintValidationFailed");
            Assert.AreEqual(originalVersion,first.Version);Assert.AreEqual(originalVersionId,first.VersionId);
            var unchanged=await provider.LoadAvatarAsync(first.Id);
            Assert.IsFalse(unchanged.IsError,unchanged.Message);
            Assert.AreEqual(originalUsername,unchanged.Result.Username);
            Assert.AreEqual("original",unchanged.Result.FirstName);
            Assert.AreEqual(originalVersionId,unchanged.Result.VersionId);
            Assert.AreEqual(0L,(await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {RecordId:$id}) RETURN count(v) AS count",new{id=first.Id.ToString()})).SingleAsync())["count"].As<long>(),
                "The earlier archive statement must roll back with rejected current update.");
            // Cleanup only after original-only inspection proves unchanged current state/no archive.
            Assert.IsFalse((await provider.DeleteAvatarAsync(first.Id,false)).IsError);
            Assert.IsFalse((await provider.DeleteAvatarAsync(second.Id,false)).IsError);
            await(await inspection.RunAsync("DROP CONSTRAINT "+constraint)).ConsumeAsync();
            Assert.AreEqual(0,(await(await inspection.RunAsync("SHOW CONSTRAINTS YIELD name WHERE name=$name RETURN name",new{name=constraint})).ToListAsync()).Count);
        }
        finally
        {
            // Failure preserves owned rows/constraint for inspection; never reset or retry the mutation.
            await provider.DeActivateProviderAsync();
        }
    }
}
