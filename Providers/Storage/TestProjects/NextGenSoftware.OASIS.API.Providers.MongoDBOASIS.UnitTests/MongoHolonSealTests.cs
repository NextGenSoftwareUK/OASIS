using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoHolonSealTests
{
    private sealed class FakePolicy : IHolonSealPolicy
    {
        public bool Master = true;
        public HashSet<Guid> Owners = new();
        public HashSet<string> Apps = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, HolonSealKey> Keys = new() { ["k1"] = new HolonSealKey("k1", "test-secret-one") };
        public string Signing = "k1";
        public bool IsAvailable => Master && SigningKey is { IsUsable: true };
        public HolonSealKey SigningKey => Keys.TryGetValue(Signing, out var k) ? k : null;
        public bool ShouldSeal(Guid owner, string appId) => Owners.Contains(owner) || (appId != null && Apps.Contains(appId));
        public HolonSealKey FindKey(string id) => id != null && Keys.TryGetValue(id, out var k) ? k : null;
    }

    private static readonly Guid Holon = Guid.NewGuid();
    private static readonly Guid Owner = Guid.NewGuid();
    private FakePolicy _policy;

    [TestInitialize] public void Setup() { _policy = new FakePolicy(); _policy.Owners.Add(Owner); HolonSealing.Policy = _policy; HolonSealing.CurrentAppId = null; }
    [TestCleanup] public void Teardown() { HolonSealing.Policy = null; HolonSealing.CurrentAppId = null; }

    private static BsonDocument Next(BsonDocument observed, string name, Guid? owner = null)
    {
        var version = observed == null ? 1 : observed["Version"].ToInt32() + 1;
        var doc = new BsonDocument
        {
            { "HolonId", Holon.ToString() }, { "Version", version }, { "VersionId", Guid.NewGuid().ToString() },
            { "PreviousVersionId", observed == null ? Guid.Empty.ToString() : observed["VersionId"].AsString },
            { "Name", name }, { "MetaData", new BsonDocument { { "b", 2 }, { "a", "x" } } }
        };
        MongoHolonSeal.Apply(doc, observed, owner ?? Owner);
        return doc;
    }

    // Builds the stored document: the latest version with an archive of all earlier ones, as the Mongo history stores them.
    private static BsonDocument Stored(params BsonDocument[] versions)
    {
        var current = (BsonDocument)versions.Last().DeepClone();
        var records = new BsonArray(versions.Take(versions.Length - 1).Select(v => new BsonDocument { { "bson", v.DeepClone() }, { "sha256", "x" } }));
        current[MongoNativeHolonHistory.Field] = new BsonDocument { { "schema", 2 }, { "records", records } };
        return current;
    }

    private HolonSealVerification Verify(BsonDocument stored) => HolonSealing.VerifyChain(Holon, MongoHolonSeal.Entries(stored), _policy);

    private BsonDocument[] Chain(int count)
    {
        var list = new List<BsonDocument>();
        BsonDocument prev = null;
        for (var i = 1; i <= count; i++) { prev = Next(prev, "name" + i); list.Add(prev); }
        return list.ToArray();
    }

    [TestMethod] public void SealedChainVerifies()
    {
        var v = Chain(4); var r = Verify(Stored(v));
        Assert.AreEqual(HolonSealStatus.Verified, r.Status, r.Message);
        Assert.AreEqual(1, r.SealedFromVersion); Assert.AreEqual(4, r.SealedVersionCount); Assert.AreEqual(v[3]["VersionSeal"].AsString, r.LatestSeal);
        Assert.IsFalse(v[0].Contains("PreviousVersionSeal")); Assert.IsTrue(v[1].Contains("PreviousVersionSeal"));
    }

    [TestMethod] public void ChangedContentInTheMiddleIsBroken()
    {
        var v = Chain(4); v[1]["Name"] = "edited";
        var r = Verify(Stored(v));
        Assert.AreEqual(HolonSealStatus.Broken, r.Status); Assert.AreEqual(2, r.BrokenAtVersion);
    }

    [TestMethod] public void ChangedMetaDataIsBroken()
    {
        var v = Chain(3); v[0]["MetaData"]["a"] = "tampered";
        var r = Verify(Stored(v));
        Assert.AreEqual(HolonSealStatus.Broken, r.Status); Assert.AreEqual(1, r.BrokenAtVersion);
    }

    [TestMethod] public void RemovedMiddleVersionIsBroken()
    {
        var v = Chain(4); var r = Verify(Stored(v[0], v[2], v[3]));
        Assert.AreEqual(HolonSealStatus.Broken, r.Status); Assert.AreEqual(3, r.BrokenAtVersion);
    }

    [TestMethod] public void InsertedForgedVersionIsBroken()
    {
        var v = Chain(3);
        var forged = new BsonDocument { { "HolonId", Holon.ToString() }, { "Version", 2 }, { "VersionId", Guid.NewGuid().ToString() }, { "PreviousVersionId", v[0]["VersionId"].AsString }, { "Name", "forged" } };
        var r = Verify(Stored(v[0], forged, v[2]));
        Assert.AreEqual(HolonSealStatus.Broken, r.Status);
    }

    [TestMethod] public void ForgedVersionWithStolenSealIsBroken()
    {
        var v = Chain(3); var forged = (BsonDocument)v[1].DeepClone(); forged["Name"] = "forged";
        Assert.AreEqual(HolonSealStatus.Broken, Verify(Stored(v[0], forged, v[2])).Status);
    }

    [TestMethod] public void StrippedSealOnMiddleVersionIsBroken()
    {
        var v = Chain(3); var stripped = (BsonDocument)v[1].DeepClone(); stripped.Remove("VersionSeal"); stripped.Remove("PreviousVersionSeal"); stripped.Remove("VersionSealKeyId");
        Assert.AreEqual(HolonSealStatus.Broken, Verify(Stored(v[0], stripped, v[2])).Status);
    }

    [TestMethod] public void RemovingOnlyTheNewestVersionIsNotDetected()
    {
        // Documented limitation: the surviving chain is still valid. Only an outside copy of the head seal reveals it.
        var v = Chain(4); var r = Verify(Stored(v[0], v[1], v[2]));
        Assert.AreEqual(HolonSealStatus.Verified, r.Status); Assert.AreEqual(3, r.LatestVersion);
    }

    [TestMethod] public void OldUnsealedVersionsReadAsUnsealedAndNewChainVerifies()
    {
        _policy.Owners.Clear();
        var v1 = Next(null, "old1"); var v2 = Next(v1, "old2");
        Assert.IsFalse(v1.Contains("VersionSeal")); Assert.IsFalse(v2.Contains("VersionSeal"));
        Assert.AreEqual(HolonSealStatus.Unsealed, Verify(Stored(v1, v2)).Status);
        _policy.Owners.Add(Owner);
        var v3 = Next(v2, "new3"); var v4 = Next(v3, "new4");
        var r = Verify(Stored(v1, v2, v3, v4));
        Assert.AreEqual(HolonSealStatus.Verified, r.Status, r.Message); Assert.AreEqual(3, r.SealedFromVersion); Assert.AreEqual(2, r.UnsealedVersionCount);
        Assert.IsFalse(v3.Contains("PreviousVersionSeal"));
    }

    [TestMethod] public void SwitchTurnedOffMidChainIsPartialAndEarlierSealsStillVerify()
    {
        var v1 = Next(null, "a"); var v2 = Next(v1, "b");
        _policy.Owners.Clear();
        var v3 = Next(v2, "c");
        Assert.IsFalse(v3.Contains("VersionSeal"));
        var r = Verify(Stored(v1, v2, v3));
        Assert.AreEqual(HolonSealStatus.Partial, r.Status);
        _policy.Owners.Add(Owner);
        var v4 = Next(v3, "d");
        Assert.IsFalse(v4.Contains("PreviousVersionSeal")); // new segment after the gap
        Assert.AreEqual(HolonSealStatus.Partial, Verify(Stored(v1, v2, v3, v4)).Status);
    }

    [TestMethod] public void SwitchMatrix()
    {
        var other = Guid.NewGuid();
        bool Sealed(Guid owner, string app, bool master, bool ownerOn, bool appOn)
        {
            _policy.Master = master; _policy.Owners.Clear(); _policy.Apps.Clear();
            if (ownerOn) _policy.Owners.Add(owner); if (appOn) _policy.Apps.Add("piper");
            HolonSealing.CurrentAppId = app;
            return Next(null, "x", owner).Contains("VersionSeal");
        }
        Assert.IsFalse(Sealed(other, "piper", true, false, false), "neither");
        Assert.IsTrue(Sealed(other, "piper", true, true, false), "avatar only");
        Assert.IsTrue(Sealed(other, "piper", true, false, true), "app only");
        Assert.IsTrue(Sealed(other, "piper", true, true, true), "both");
        Assert.IsFalse(Sealed(other, "other-app", true, false, true), "different app");
        Assert.IsFalse(Sealed(other, "piper", false, true, true), "master off");
        _policy.Master = true; _policy.Owners.Add(other); HolonSealing.Policy = null;
        Assert.IsFalse(Next(null, "x", other).Contains("VersionSeal"), "no policy registered");
    }

    [TestMethod] public void StaleOrRepeatedVersionIsRejectedOnTheSealedPathOnly()
    {
        var v1 = Next(null, "a"); var v2 = Next(v1, "b");
        var stale = new BsonDocument { { "HolonId", Holon.ToString() }, { "Version", 2 }, { "Name", "racer" } };
        Assert.ThrowsException<StorageLifecycleConflictException>(() => MongoHolonSeal.Apply(stale, v2, Owner));
        _policy.Owners.Clear();
        var unsealedStale = new BsonDocument { { "HolonId", Holon.ToString() }, { "Version", 2 }, { "Name", "racer" } };
        MongoHolonSeal.Apply(unsealedStale, v2, Owner); // behaves exactly as before: no check, no seal
        Assert.IsFalse(unsealedStale.Contains("VersionSeal"));
    }

    [TestMethod] public void OffLeavesTheDocumentUntouchedAndClearsCarriedSeals()
    {
        _policy.Owners.Clear();
        var doc = new BsonDocument { { "Version", 2 }, { "Name", "n" }, { "VersionSeal", "old" }, { "PreviousVersionSeal", "older" }, { "VersionSealKeyId", "k1" } };
        MongoHolonSeal.Apply(doc, null, Owner);
        Assert.IsFalse(doc.Contains("VersionSeal") || doc.Contains("PreviousVersionSeal") || doc.Contains("VersionSealKeyId"));
        Assert.AreEqual(2, doc.ElementCount);
    }

    [TestMethod] public void WrongSecretIsBrokenAndUnknownKeyIdOrNoPolicyIsUnavailable()
    {
        var v = Chain(3); var stored = Stored(v);
        _policy.Keys["k1"] = new HolonSealKey("k1", "a-different-secret");
        Assert.AreEqual(HolonSealStatus.Broken, Verify(stored).Status);
        _policy.Keys.Clear();
        Assert.AreEqual(HolonSealStatus.Unavailable, Verify(stored).Status);
        Assert.AreEqual(HolonSealStatus.Unavailable, HolonSealing.VerifyChain(Holon, MongoHolonSeal.Entries(stored), null).Status);
    }

    [TestMethod] public void KeyRotationKeepsOldChainsVerifiable()
    {
        var v1 = Next(null, "a");
        _policy.Keys["k2"] = new HolonSealKey("k2", "second-secret"); _policy.Signing = "k2";
        var v2 = Next(v1, "b");
        Assert.AreEqual("k2", v2["VersionSealKeyId"].AsString);
        Assert.AreEqual(HolonSealStatus.Verified, Verify(Stored(v1, v2)).Status);
    }

    [TestMethod] public void SealCoversHolonIdAndExcludesReplicaFields()
    {
        var a = Next(null, "same");
        var b = (BsonDocument)a.DeepClone(); b["HolonId"] = Guid.NewGuid().ToString();
        Assert.AreNotEqual(MongoHolonSeal.Canonical(a), MongoHolonSeal.Canonical(b));
        var c = (BsonDocument)a.DeepClone(); c["ProviderRevisionId"] = Guid.NewGuid().ToString(); c["_id"] = ObjectId.GenerateNewId();
        Assert.AreEqual(MongoHolonSeal.Canonical(a), MongoHolonSeal.Canonical(c));
    }
}
