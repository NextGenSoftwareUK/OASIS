using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonMigrationFenceRequestTests
{
    private readonly CosmicStorageIdentity store = new(ProviderType.SQLLiteDBOASIS, new string('A',64), Guid.NewGuid());
    [TestMethod]
    public void ScopeIsCanonicalDetachedAndBoundToOperatorStoreAndExactReview()
    {
        var migration = Guid.NewGuid(); var actor = Guid.NewGuid(); var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var request = new HolonMigrationFenceRequest(migration, actor, store, new string('B',64), new string('C',64), ids);
        var reversed = new HolonMigrationFenceRequest(migration, actor, store, request.PreviewDigest, request.AuthorizationContextDigest, ids.AsEnumerable().Reverse());
        Assert.AreEqual(request.Fingerprint, reversed.Fingerprint);
        Assert.AreEqual(request.Fingerprint, HolonMigrationFenceRequest.ReadCanonical(request.CanonicalJson, store).Fingerprint);
        ids.Clear(); Assert.AreEqual(2, request.HolonIds.Count);
        Assert.AreNotEqual(request.Fingerprint, new HolonMigrationFenceRequest(migration, Guid.NewGuid(), store,
            request.PreviewDigest, request.AuthorizationContextDigest, request.HolonIds).Fingerprint);
        Assert.ThrowsException<ArgumentException>(() => HolonMigrationFenceRequest.ReadCanonical(request.CanonicalJson + " ", store));
        Assert.ThrowsException<ArgumentException>(() => HolonMigrationFenceRequest.ReadCanonical(request.CanonicalJson,
            new(store.Authority, store.EndpointFingerprint, Guid.NewGuid())));
    }

    [DataTestMethod]
    [DataRow("empty")] [DataRow("duplicate")] [DataRow("too-many")] [DataRow("empty-id")]
    [DataRow("operator")] [DataRow("migration")] [DataRow("preview")]
    public void InvalidOrUnboundedFenceScopeCannotBeAdmitted(string fault)
    {
        var id = Guid.NewGuid(); var ids = fault switch { "empty" => Array.Empty<Guid>(), "duplicate" => new[] { id, id },
            "too-many" => Enumerable.Range(0,4097).Select(_ => Guid.NewGuid()).ToArray(), "empty-id" => new[] { Guid.Empty }, _ => new[] { id } };
        Assert.ThrowsException<ArgumentException>(() => new HolonMigrationFenceRequest(
            fault == "migration" ? Guid.Empty : Guid.NewGuid(), fault == "operator" ? Guid.Empty : Guid.NewGuid(), store,
            fault == "preview" ? "unbound" : new string('B',64), new string('C',64), ids));
    }

    [TestMethod]
    public void AbsenceCannotContainAnOwnerAndHeldMustHaveOne()
    {
        var request = new HolonMigrationFenceRequest(Guid.NewGuid(), Guid.NewGuid(), store, new string('B',64), new string('C',64), new[] { Guid.NewGuid() });
        Assert.ThrowsException<ArgumentException>(() => new HolonMigrationFenceObservation(request, Guid.NewGuid(), HolonMigrationFenceState.Absent));
        Assert.ThrowsException<ArgumentException>(() => new HolonMigrationFenceObservation(request, Guid.Empty, HolonMigrationFenceState.Held));
    }
    [DataTestMethod]
    [DataRow("owner")] [DataRow("digest")] [DataRow("phase")]
    public void StartedMutationRequiresExactOwnerAndImmutableDigest(string fault)
    {
        var request = new HolonMigrationFenceRequest(Guid.NewGuid(), Guid.NewGuid(), store, new string('B',64), new string('C',64), new[] { Guid.NewGuid() });
        if (fault == "phase") Assert.ThrowsException<ArgumentException>(() => new HolonMigrationFenceObservation(request,
            Guid.NewGuid(), HolonMigrationFenceState.Held, new string('D',64)));
        else Assert.ThrowsException<ArgumentException>(() => new HolonMigrationMutationStartRequest(request,
            fault == "owner" ? Guid.Empty : Guid.NewGuid(), fault == "digest" ? "unbound" : new string('D',64)));
        Assert.ThrowsException<ArgumentException>(() => new HolonMigrationFenceObservation(request, Guid.NewGuid(), HolonMigrationFenceState.MutationStarted));
        var valid = new HolonMigrationMutationStartRequest(request, Guid.NewGuid(), new string('D',64));
        var started = new HolonMigrationFenceObservation(request, valid.OwnerToken, HolonMigrationFenceState.MutationStarted, valid.MutationDigest);
        Assert.AreEqual(valid.MutationDigest, started.MutationDigest);
    }

}
