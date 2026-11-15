using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS.IntegrationTests;

[TestClass]
public sealed class AlgorandRecordBoxTests
{
    [TestMethod]
    public async Task PublishesAndListsEnrolledAvatarsSeparatelyFromDetailsAndHolons()
    {
        var storageKey = Convert.FromBase64String(Required("KEY"));
        var writer = Create(Required("PROGRAM"), storageKey, Convert.FromBase64String(Required("WRITER_SEED")));
        var reader = Create(Required("PROGRAM"), storageKey, null);
        var active = await writer.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);
        try
        {
            var id = Guid.NewGuid();
            var avatar = new Avatar { Id = id, Version = 1, VersionId = Guid.NewGuid(), Username = "enrolled-avatar", Email = "native@example.invalid" };
            var created = await writer.SaveAvatarAsync(avatar);
            Assert.IsFalse(created.IsError, created.Exception?.ToString() ?? created.Message);
            var detail = new AvatarDetail { Id = id, Version = 1, VersionId = Guid.NewGuid(), Username = "enrolled-detail" };
            var detailWrite = await writer.SaveAvatarDetailAsync(detail);
            Assert.IsFalse(detailWrite.IsError, detailWrite.Exception?.ToString() ?? detailWrite.Message);
            var holonWrite = await writer.SaveStoredHolonAsync(new Holon { Id = id, Name = "Ordinary holon" }, 0);
            Assert.IsFalse(holonWrite.IsError, holonWrite.Exception?.ToString() ?? holonWrite.Message);

            var byUsername = await writer.LoadAvatarByUsernameAsync(" ENROLLED-AVATAR ");
            var byEmail = await writer.LoadAvatarByEmailAsync(" NATIVE@EXAMPLE.INVALID ");
            Assert.IsFalse(byUsername.IsError, byUsername.Message);
            Assert.IsFalse(byEmail.IsError, byEmail.Message);
            Assert.AreEqual(id, byUsername.Result.Id);
            Assert.AreEqual(id, byEmail.Result.Id);
            var collisionId = Guid.NewGuid();
            var collision = await writer.SaveAvatarAsync(new Avatar
                { Id = collisionId, Version = 1, VersionId = Guid.NewGuid(), Username = avatar.Username.ToUpperInvariant() });
            Assert.IsTrue(collision.IsError);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await writer.LoadAvatarAsync(collisionId)).ErrorCode);

            avatar.Version = 2;
            avatar.VersionId = Guid.NewGuid();
            avatar.PreviousVersionId = created.Result.VersionId;
            avatar.Username = "enrolled-avatar-updated";
            avatar.Email = "updated@example.invalid";
            var updated = await writer.SaveAvatarAsync(avatar);
            Assert.IsFalse(updated.IsError, updated.Exception?.ToString() ?? updated.Message);

            var readerActivation = await reader.ActivateProviderAsync();
            Assert.IsFalse(readerActivation.IsError, readerActivation.Message);
            var latest = await reader.LoadAvatarByProviderKeyAsync(id.ToString("D"));
            var historical = await reader.LoadAvatarAsync(id, version: 1);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await reader.LoadAvatarByUsernameAsync("enrolled-avatar")).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await reader.LoadAvatarByEmailAsync("native@example.invalid")).ErrorCode);
            Assert.AreEqual(id, (await reader.LoadAvatarByUsernameAsync(avatar.Username)).Result.Id);
            Assert.AreEqual(id, (await reader.LoadAvatarByEmailAsync(avatar.Email)).Result.Id);
            Assert.AreEqual("enrolled-avatar", (await reader.LoadAvatarByUsernameAsync(avatar.Username, 1)).Result.Username);
            var all = await reader.LoadAllAvatarsAsync();
            var allDetails = await reader.LoadAllAvatarDetailsAsync();
            Assert.IsFalse(latest.IsError, latest.Message);
            Assert.IsFalse(historical.IsError, historical.Message);
            Assert.IsFalse(all.IsError, all.Message);
            Assert.IsFalse(allDetails.IsError, allDetails.Message);
            Assert.AreEqual("enrolled-avatar-updated", latest.Result.Username);
            Assert.AreEqual(avatar.Email, latest.Result.Email);
            Assert.AreEqual("enrolled-avatar", historical.Result.Username);
            Assert.AreEqual(created.Result.VersionId, latest.Result.PreviousVersionId);
            Assert.AreEqual(id, all.Result.Single().Id);
            Assert.AreEqual("enrolled-detail", allDetails.Result.Single().Username);
            Assert.AreEqual(id, (await reader.DiscoverStoredHolonIdsAsync()).Result.Single());
            Assert.AreEqual("Ordinary holon", (await reader.LoadStoredHolonAsync(id)).Result.Name);

            var softDeleted = await writer.DeleteAvatarByUsernameAsync(avatar.Username);
            Assert.IsFalse(softDeleted.IsError, softDeleted.Exception?.ToString() ?? softDeleted.Message);
            Assert.IsTrue(softDeleted.IsDeleted);
            var avatarTombstone = await reader.LoadAvatarAsync(id);
            var detailTombstone = await reader.LoadAvatarDetailAsync(id);
            Assert.IsFalse(avatarTombstone.IsError, avatarTombstone.Message);
            Assert.IsFalse(detailTombstone.IsError, detailTombstone.Message);
            Assert.IsTrue(avatarTombstone.Result.IsDeleted);
            Assert.IsTrue(detailTombstone.Result.IsDeleted);
            Assert.AreEqual(avatarTombstone.Result.DeletedDate, detailTombstone.Result.DeletedDate);
            Assert.AreEqual(3, avatarTombstone.Result.Version);
            Assert.AreEqual(2, detailTombstone.Result.Version);
            Assert.AreEqual("enrolled-avatar", (await reader.LoadAvatarAsync(id, version: 1)).Result.Username);
            Assert.IsTrue((await writer.DeleteAvatarAsync(id)).Result);
            Assert.AreEqual(3, (await reader.LoadAvatarAsync(id)).Result.Version);

            var deleted = await writer.DeleteAvatarByEmailAsync(avatar.Email, softDelete: false);
            Assert.IsFalse(deleted.IsError, deleted.Exception?.ToString() ?? deleted.Message);
            Assert.IsTrue(deleted.IsDeleted);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await reader.LoadAvatarAsync(id)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await reader.LoadAvatarAsync(id, version: 1)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await reader.LoadAvatarDetailAsync(id)).ErrorCode);
            Assert.AreEqual("Ordinary holon", (await reader.LoadStoredHolonAsync(id)).Result.Name);
            Assert.IsTrue((await writer.DeleteAvatarAsync(id, softDelete: false)).Result);

            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await reader.LoadAvatarByEmailAsync(avatar.Email)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await reader.LoadAvatarByUsernameAsync(avatar.Username)).ErrorCode);

            var avatarOnlyId = Guid.NewGuid();
            var avatarOnly = new Avatar { Id = avatarOnlyId, Version = 1, VersionId = Guid.NewGuid(), Username = avatar.Username, Email = avatar.Email };
            var avatarOnlySaved = await writer.SaveAvatarAsync(avatarOnly);
            Assert.IsFalse(avatarOnlySaved.IsError, avatarOnlySaved.Exception?.ToString() ?? avatarOnlySaved.Message);
            var archived = await writer.DeleteAvatarAsync(avatarOnlyId);
            Assert.IsFalse(archived.IsError, archived.Exception?.ToString() ?? archived.Message);
            var createdTombstone = await reader.LoadAvatarDetailAsync(avatarOnlyId);
            Assert.IsFalse(createdTombstone.IsError, createdTombstone.Message);
            Assert.IsTrue(createdTombstone.Result.IsDeleted);
            var removed = await writer.DeleteAvatarAsync(avatarOnlyId, softDelete: false);
            Assert.IsFalse(removed.IsError, removed.Exception?.ToString() ?? removed.Message);
            var lateDetail = await writer.SaveAvatarDetailAsync(new AvatarDetail
                { Id = avatarOnlyId, Version = 1, VersionId = Guid.NewGuid(), Username = "late-detail" });
            Assert.IsTrue(lateDetail.IsError, "A deleted avatar must not acquire a detail record.");
        }
        finally
        {
            await reader.DeActivateProviderAsync();
            await writer.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task PublishesTypedAvatarDetailHistoryWithoutReplacingTheHolon()
    {
        var storageKey = Convert.FromBase64String(Required("KEY"));
        var writer = Create(Required("PROGRAM"), storageKey, Convert.FromBase64String(Required("WRITER_SEED")));
        var reader = Create(Required("PROGRAM"), storageKey, null);
        var active = await writer.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);
        try
        {
            var empty = await writer.LoadAllAvatarDetailsAsync();
            Assert.IsFalse(empty.IsError, empty.Message);
            Assert.AreEqual(0, empty.LoadedCount);
            var id = Guid.NewGuid();
            var ordinary = await writer.SaveStoredHolonAsync(new Holon { Id = id, Name = "Ordinary holon" }, 0);
            Assert.IsFalse(ordinary.IsError, ordinary.Exception?.ToString() ?? ordinary.Message);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await writer.LoadAvatarDetailAsync(id)).ErrorCode);

            var detail = new AvatarDetail { Id = id, Version = 1, VersionId = Guid.NewGuid(), Username = "native-profile" };
            var first = await writer.SaveAvatarDetailAsync(detail);
            Assert.IsFalse(first.IsError, first.Exception?.ToString() ?? first.Message);

            detail.Version = 2;
            detail.VersionId = Guid.NewGuid();
            detail.PreviousVersionId = Guid.NewGuid();
            detail.Username = "native-profile-updated";
            var stale = await writer.SaveAvatarDetailAsync(detail);
            Assert.IsTrue(stale.IsError);
            Assert.AreEqual(1, (await writer.LoadAvatarDetailAsync(id)).Result.Version);
            detail.PreviousVersionId = first.Result.VersionId;
            var second = await writer.SaveAvatarDetailAsync(detail);
            Assert.IsFalse(second.IsError, second.Exception?.ToString() ?? second.Message);

            var readerActivation = await reader.ActivateProviderAsync();
            Assert.IsFalse(readerActivation.IsError, readerActivation.Message);
            var latest = await reader.LoadAvatarDetailAsync(id);
            var historical = await reader.LoadAvatarDetailAsync(id, version: 1);
            Assert.IsFalse(latest.IsError, latest.Message);
            Assert.IsFalse(historical.IsError, historical.Message);
            Assert.AreEqual("native-profile-updated", latest.Result.Username);
            Assert.AreEqual(first.Result.VersionId, latest.Result.PreviousVersionId);
            Assert.AreEqual("native-profile", historical.Result.Username);
            Assert.AreEqual("Ordinary holon", (await reader.LoadStoredHolonAsync(id)).Result.Name);

            var softDeleted = await writer.DeleteAvatarByUsernameAsync(avatar.Username);
            Assert.IsFalse(softDeleted.IsError, softDeleted.Exception?.ToString() ?? softDeleted.Message);
            Assert.IsTrue(softDeleted.IsDeleted);
            var avatarTombstone = await reader.LoadAvatarAsync(id);
            var detailTombstone = await reader.LoadAvatarDetailAsync(id);
            Assert.IsFalse(avatarTombstone.IsError, avatarTombstone.Message);
            Assert.IsFalse(detailTombstone.IsError, detailTombstone.Message);
            Assert.IsTrue(avatarTombstone.Result.IsDeleted);
            Assert.IsTrue(detailTombstone.Result.IsDeleted);
            Assert.AreEqual(avatarTombstone.Result.DeletedDate, detailTombstone.Result.DeletedDate);
            Assert.AreEqual(3, avatarTombstone.Result.Version);
            Assert.AreEqual(2, detailTombstone.Result.Version);
            Assert.AreEqual("enrolled-avatar", (await reader.LoadAvatarAsync(id, version: 1)).Result.Username);
            Assert.IsTrue((await writer.DeleteAvatarAsync(id)).Result);
            Assert.AreEqual(3, (await reader.LoadAvatarAsync(id)).Result.Version);

            var deleted = await writer.DeleteAvatarByEmailAsync(avatar.Email, softDelete: false);
            Assert.IsFalse(deleted.IsError, deleted.Exception?.ToString() ?? deleted.Message);
            Assert.IsTrue(deleted.IsDeleted);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await reader.LoadAvatarAsync(id)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await reader.LoadAvatarAsync(id, version: 1)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await reader.LoadAvatarDetailAsync(id)).ErrorCode);
            Assert.AreEqual("Ordinary holon", (await reader.LoadStoredHolonAsync(id)).Result.Name);
            Assert.IsTrue((await writer.DeleteAvatarAsync(id, softDelete: false)).Result);

            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await reader.LoadAvatarByEmailAsync(avatar.Email)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await reader.LoadAvatarByUsernameAsync(avatar.Username)).ErrorCode);

            var avatarOnlyId = Guid.NewGuid();
            var avatarOnly = new Avatar { Id = avatarOnlyId, Version = 1, VersionId = Guid.NewGuid(), Username = avatar.Username, Email = avatar.Email };
            var avatarOnlySaved = await writer.SaveAvatarAsync(avatarOnly);
            Assert.IsFalse(avatarOnlySaved.IsError, avatarOnlySaved.Exception?.ToString() ?? avatarOnlySaved.Message);
            var archived = await writer.DeleteAvatarAsync(avatarOnlyId);
            Assert.IsFalse(archived.IsError, archived.Exception?.ToString() ?? archived.Message);
            var createdTombstone = await reader.LoadAvatarDetailAsync(avatarOnlyId);
            Assert.IsFalse(createdTombstone.IsError, createdTombstone.Message);
            Assert.IsTrue(createdTombstone.Result.IsDeleted);
            var removed = await writer.DeleteAvatarAsync(avatarOnlyId, softDelete: false);
            Assert.IsFalse(removed.IsError, removed.Exception?.ToString() ?? removed.Message);
            var lateDetail = await writer.SaveAvatarDetailAsync(new AvatarDetail
                { Id = avatarOnlyId, Version = 1, VersionId = Guid.NewGuid(), Username = "late-detail" });
            Assert.IsTrue(lateDetail.IsError, "A deleted avatar must not acquire a detail record.");
            var discovered = await reader.DiscoverStoredHolonIdsAsync();
            Assert.IsFalse(discovered.IsError, discovered.Message);
            CollectionAssert.AreEqual(new[] { id }, discovered.Result.ToArray());

            var latestDetails = await reader.LoadAllAvatarDetailsAsync();
            var historicalDetails = await reader.LoadAllAvatarDetailsAsync(version: 1);
            var missingDetails = await reader.LoadAllAvatarDetailsAsync(version: 3);
            Assert.IsFalse(latestDetails.IsError, latestDetails.Message);
            Assert.IsFalse(historicalDetails.IsError, historicalDetails.Message);
            Assert.IsFalse(missingDetails.IsError, missingDetails.Message);
            Assert.AreEqual(id, latestDetails.Result.Single().Id);
            Assert.AreEqual(latest.Result.Username, latestDetails.Result.Single().Username);
            Assert.AreEqual(historical.Result.Username, historicalDetails.Result.Single().Username);
            Assert.AreEqual(0, missingDetails.LoadedCount);
        }
        finally
        {
            await reader.DeActivateProviderAsync();
            await writer.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task ResumesPurgingFromTheNativeCheckpoint()
    {
        var id = Guid.Parse(Required("ID"));
        var storageKey = Convert.FromBase64String(Required("KEY"));
        var provider = Create(Required("PROGRAM"), storageKey, Convert.FromBase64String(Required("WRITER_SEED")));
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);
        try
        {
            using var node = CreateNativeAlgod();
            Assert.IsTrue((await NativeBoxNames(node)).Length > 1);
            var discovery = await provider.DiscoverStoredHolonIdsAsync();
            Assert.IsFalse(discovery.IsError, discovery.Exception?.ToString() ?? discovery.Message);
            Assert.AreEqual(0, discovery.Result.Count);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,
                (await provider.LoadStoredHolonAsync(id, version: 1)).ErrorCode);
            Assert.IsTrue((await provider.SaveStoredHolonAsync(new Holon { Id = id, Name = "Rejected" }, 0)).IsError);

            var resumed = await provider.DeleteHolonAsync(id);
            Assert.IsFalse(resumed.IsError, resumed.Exception?.ToString() ?? resumed.Message);
            Assert.IsTrue(resumed.IsDeleted);
            Assert.AreEqual(1, resumed.DeletedCount);
            var key = HMACSHA256.HashData(storageKey, Encoding.UTF8.GetBytes($"oasis:algorand:box:v1:holon:{id:D}"));
            var remaining = await NativeBoxNames(node);
            CollectionAssert.AreEqual(new[] { Convert.ToBase64String([(byte)'h', ..key]) }, remaining);
            Assert.AreEqual(0, (await provider.DeleteHolonAsync(id)).DeletedCount);
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task PurgesPublishedRevisionsAndFencesRepublishing()
    {
        var id = Guid.Parse(Required("ID"));
        var storageKey = Convert.FromBase64String(Required("KEY"));
        var provider = Create(Required("PROGRAM"), storageKey, Convert.FromBase64String(Required("WRITER_SEED")));
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);
        try
        {
            using var node = CreateNativeAlgod();

            var unrelatedId = Guid.NewGuid();
            var unrelated = await provider.SaveStoredHolonAsync(new Holon { Id = unrelatedId, Name = "Keep this record" }, 0);
            Assert.IsFalse(unrelated.IsError, unrelated.Exception?.ToString() ?? unrelated.Message);
            var unrelatedBoxes = await NativeBoxNames(node);

            var record = new Holon { Id = id, Name = "First", Description = new string('x', 850) };
            var first = await provider.SaveStoredHolonAsync(record, 0);
            Assert.IsFalse(first.IsError, first.Exception?.ToString() ?? first.Message);
            record.Name = "Second";
            var second = await provider.SaveStoredHolonAsync(record, 1);
            Assert.IsFalse(second.IsError, second.Exception?.ToString() ?? second.Message);
            Assert.IsTrue((await NativeBoxNames(node)).Length > unrelatedBoxes.Length + 1);

            var deleted = await provider.DeleteHolonAsync(id.ToString("D"));
            Assert.IsFalse(deleted.IsError, deleted.Exception?.ToString() ?? deleted.Message);
            Assert.IsTrue(deleted.IsDeleted);
            Assert.AreEqual(1, deleted.DeletedCount);
            Assert.AreEqual("Second", deleted.Result.Name);
            var opaqueKey = HMACSHA256.HashData(storageKey, Encoding.UTF8.GetBytes($"oasis:algorand:box:v1:holon:{id:D}"));
            var fenceName = Convert.ToBase64String([(byte)'h', ..opaqueKey]);
            CollectionAssert.AreEquivalent(unrelatedBoxes.Append(fenceName).ToArray(), await NativeBoxNames(node));
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await provider.LoadStoredHolonAsync(id)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,
                (await provider.LoadStoredHolonAsync(id, version: 1)).ErrorCode);
            Assert.AreEqual(unrelatedId, (await provider.DiscoverStoredHolonIdsAsync()).Result.Single());
            Assert.IsTrue((await provider.SaveStoredHolonAsync(record, 0)).IsError);
            var repeated = await provider.DeleteHolonAsync(id);
            Assert.IsFalse(repeated.IsError, repeated.Message);
            Assert.IsTrue(repeated.IsDeleted);
            Assert.AreEqual(0, repeated.DeletedCount);
            CollectionAssert.AreEquivalent(unrelatedBoxes.Append(fenceName).ToArray(), await NativeBoxNames(node));
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task SoftDeletesStoredHolonWithDurableAuditAndHistory()
    {
        var id = Guid.Parse(Required("ID"));
        var actor = Guid.NewGuid();
        var storageKey = Convert.FromBase64String(Required("KEY"));
        var provider = Create(Required("PROGRAM"), storageKey, Convert.FromBase64String(Required("WRITER_SEED")));
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);
        try
        {
            var first = await provider.SaveStoredHolonAsync(new Holon { Id = id, Name = "Owned soft delete", IsActive = true }, 0);
            Assert.IsFalse(first.IsError, first.Exception?.ToString() ?? first.Message);
            Assert.IsTrue((await provider.SoftDeleteStoredHolonAsync(id, Guid.Empty)).IsError);

            var deleted = await provider.SoftDeleteStoredHolonAsync(id, actor);
            Assert.IsFalse(deleted.IsError, deleted.Exception?.ToString() ?? deleted.Message);
            Assert.IsTrue(deleted.IsDeleted);
            Assert.AreEqual(1, deleted.DeletedCount);
            Assert.AreEqual(2, deleted.Result.Version);
            Assert.AreEqual(first.Result.VersionId, deleted.Result.PreviousVersionId);

            var repeated = await provider.SoftDeleteStoredHolonAsync(id, Guid.NewGuid());
            Assert.IsFalse(repeated.IsError, repeated.Message);
            Assert.AreEqual(0, repeated.DeletedCount);
            Assert.AreEqual(actor, repeated.Result.DeletedByAvatarId);
            Assert.AreEqual(deleted.Result.VersionId, repeated.Result.VersionId);

            var reader = Create(Required("PROGRAM"), storageKey);
            Assert.IsFalse((await reader.ActivateProviderAsync()).IsError);
            try
            {
                var current = await reader.LoadStoredHolonAsync(id);
                Assert.IsFalse(current.IsError, current.Message);
                Assert.IsTrue(current.Result.IsDeleted);
                Assert.IsFalse(current.Result.IsActive);
                Assert.AreEqual(actor, current.Result.DeletedByAvatarId);
                var original = await reader.LoadStoredHolonAsync(id, version: 1);
                Assert.IsFalse(original.IsError, original.Message);
                Assert.IsFalse(original.Result.IsDeleted);
                Assert.IsTrue(original.Result.IsActive);
            }
            finally
            {
                await reader.DeActivateProviderAsync();
            }
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task PublishesSignedEncryptedRevisionsThroughNativeBoxes()
    {
        var id = Guid.Parse(Required("ID"));
        var storageKey = Convert.FromBase64String(Required("KEY"));
        var provider = Create(Required("PROGRAM"), storageKey, Convert.FromBase64String(Required("WRITER_SEED")));
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);
        try
        {
            var holon = new Holon { Id = id, Version = 1, Name = "Signed native version one", Description = new string('b', 950) };
            var initial = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsFalse(initial.IsError, initial.Exception?.ToString() ?? initial.Message);
            Assert.AreEqual(1, initial.Result.Version);

            holon.Name = "Signed native version two";
            Assert.IsTrue((await provider.SaveHolonAsync(holon, saveChildren: false)).IsError);
            holon.Version = 2;
            var update = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsFalse(update.IsError, update.Exception?.ToString() ?? update.Message);
            Assert.AreEqual(2, update.Result.Version);
            Assert.AreEqual(initial.Result.VersionId, update.Result.PreviousVersionId);
            Assert.AreNotEqual(initial.Result.VersionId, update.Result.VersionId);
            var secondId = Guid.NewGuid();
            var second = await provider.SaveHolonAsync(new Holon
                { Id = secondId, ParentHolonId = id, Name = "Another record" }, saveChildren: false);
            Assert.IsFalse(second.IsError, second.Exception?.ToString() ?? second.Message);

            var separateReader = Create(Required("PROGRAM"), storageKey);
            Assert.IsFalse((await separateReader.ActivateProviderAsync()).IsError);
            try
            {
                var latest = await separateReader.LoadHolonAsync(id, loadChildren: false);
                Assert.IsFalse(latest.IsError, latest.Exception?.ToString() ?? latest.Message);
                Assert.AreEqual("Signed native version two", latest.Result.Name);
                Assert.AreEqual(950, latest.Result.Description.Length);
                var original = separateReader.LoadHolon(id.ToString("D"), loadChildren: false, version: 1);
                Assert.IsFalse(original.IsError, original.Exception?.ToString() ?? original.Message);
                Assert.AreEqual("Signed native version one", original.Result.Name);
                var discovered = await separateReader.DiscoverStoredHolonIdsAsync();
                Assert.IsFalse(discovered.IsError, discovered.Exception?.ToString() ?? discovered.Message);
                CollectionAssert.AreEquivalent(new[] { id, secondId }, System.Linq.Enumerable.ToArray(discovered.Result));
                var all = await separateReader.LoadAllHolonsAsync(loadChildren: false);
                Assert.IsFalse(all.IsError, all.Exception?.ToString() ?? all.Message);
                CollectionAssert.AreEquivalent(new[] { id, secondId },
                    System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(all.Result, record => record.Id)));
                var children = separateReader.LoadHolonsForParent(id.ToString("D"), loadChildren: false);
                Assert.IsFalse(children.IsError, children.Exception?.ToString() ?? children.Message);
                Assert.AreEqual(secondId, System.Linq.Enumerable.Single(children.Result).Id);
            }
            finally
            {
                await separateReader.DeActivateProviderAsync();
            }
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task ReadsEncryptedCurrentAndHistoricalRecordsFromNativeBoxes()
    {
        var id = Guid.Parse(Required("ID"));
        var key = Convert.FromBase64String(Required("KEY"));
        var program = Required("PROGRAM");

        var rejected = Create(new string('0', 64), key);
        Assert.IsTrue((await rejected.ActivateProviderAsync()).IsError);
        Assert.IsTrue((await rejected.LoadStoredHolonAsync(id)).IsError);

        var provider = Create(program, key);
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);
        try
        {
            var latest = await provider.LoadStoredHolonAsync(id);
            Assert.IsFalse(latest.IsError, latest.Exception?.ToString() ?? latest.Message);
            Assert.AreEqual(id, latest.Result.Id);
            Assert.AreEqual(2, latest.Result.Version);
            Assert.AreEqual("Native version 2", latest.Result.Name);

            var prior = await provider.LoadStoredHolonAsync(id, version: 1);
            Assert.IsFalse(prior.IsError, prior.Exception?.ToString() ?? prior.Message);
            Assert.AreEqual(1, prior.Result.Version);
            Assert.AreEqual("Native version 1", prior.Result.Name);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,
                (await provider.LoadStoredHolonAsync(id, version: 3)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,
                (await provider.LoadStoredHolonAsync(Guid.NewGuid())).ErrorCode);
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    private static AlgorandOASIS Create(string programDigest, byte[] storageKey, byte[]? writerSeed = null) => new(
        "http://127.0.0.1:4001", "http://127.0.0.1:8980", new string('a', 64), null,
        new AlgorandRecordBoxSettings(ulong.Parse(Required("APP"), CultureInfo.InvariantCulture),
            Required("GENESIS"), Required("WRITER"), programDigest, storageKey, writerSeed));

    private static HttpClient CreateNativeAlgod()
    {
        var node = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:4001") };
        node.DefaultRequestHeaders.Add("X-Algo-API-Token", new string('a', 64));
        return node;
    }

    private static async Task<string[]> NativeBoxNames(HttpClient node)
    {
        var appId = ulong.Parse(Required("APP"), CultureInfo.InvariantCulture);
        var response = await node.GetStringAsync($"/v2/applications/{appId}/boxes");
        using var document = JsonDocument.Parse(response);
        return document.RootElement.GetProperty("boxes").EnumerateArray()
            .Select(box => box.GetProperty("name").GetString()!).ToArray();
    }

    private static string Required(string name) => Environment.GetEnvironmentVariable("OASIS_ALGORAND_RECORD_" + name)
        ?? throw new InvalidOperationException($"Missing native record fixture setting {name}.");
}
