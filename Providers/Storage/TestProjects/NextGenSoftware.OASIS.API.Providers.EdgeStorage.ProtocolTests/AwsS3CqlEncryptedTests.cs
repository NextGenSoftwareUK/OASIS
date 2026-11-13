using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

// Official signed SDK over a controlled HTTP service: not native bucket acceptance.
[TestClass]
public class AwsS3CqlEncryptedTests
{
    [TestMethod]
    public async Task SelectedEncryptedRecordsRequireNoListingAndKeepSourceUnchanged()
    {
        using var store = new AwsS3EncryptedLifecycleTests.Store();
        var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            using var provider = store.Open(key);
            var active = await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message);
            var actor = Guid.NewGuid();
            var rows = new[] { "Espresso", "Latte", "Tea" }
                .Select(name => new Holon { Id = Guid.NewGuid(), Name = name, CreatedByAvatarId = actor }).ToArray();
            foreach (var row in rows)
            {
                var saved = await provider.SaveHolonAsync(row, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message);
            }
            var original = store.Objects.ToDictionary(p => p.Key, p => p.Value);
            var pages = store.Pages;
            var writes = store.Writes;
            var deletes = store.Deletes;
            var selected = rows.Take(2).Select(r => r.Id).ToArray();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AWSS3OASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.AWSS3OASIS,
                    rows.Select(r => r.Id).ToArray()).Result).Result
                .Where(h => selected.Contains(h.Id)).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.AWSS3OASIS,
                new[] { new CosmicQueryTarget(ProviderType.AWSS3OASIS, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEquivalent(selected, result.Result.Select(r => r.Id).ToArray());
            Assert.AreEqual(2, result.LoadedCount);
            Assert.AreEqual(pages, store.Pages, "CQL must not LIST the source.");
            Assert.AreEqual(writes, store.Writes);
            Assert.AreEqual(deletes, store.Deletes);
            Assert.AreEqual(original.Count, store.Objects.Count);
            foreach (var entry in original) Assert.AreEqual(entry.Value, store.Objects[entry.Key]);
            store.AssertPrivate("Espresso", "Latte", "Tea");

            // A later key failure must discard earlier successful rows.
            var reads = 0;
            store.BeforeGetReply = _ =>
            {
                if (++reads == 2) throw new System.IO.InvalidDataException("controlled second object failure");
            };
            var failed = await provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsTrue(failed.IsError);
            Assert.AreEqual("CQL_QUERY_FAILED", failed.ErrorCode);
            Assert.IsNull(failed.Result);
            Assert.AreEqual(2, reads);
            store.BeforeGetReply = null;

            using var cancellation = new System.Threading.CancellationTokenSource();
            store.BeforeGetReply = _ => cancellation.Cancel();
            var cancelled = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: cancellation.Token);
            Assert.AreEqual("CQL_QUERY_CANCELLED", cancelled.ErrorCode);
            Assert.IsNull(cancelled.Result);
            Assert.AreEqual(typeof(OperationCanceledException), cancelled.Exception.GetType());
            store.BeforeGetReply = null;
            Assert.AreEqual(pages, store.Pages);
            Assert.AreEqual(writes, store.Writes);
            Assert.AreEqual(deletes, store.Deletes);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
