using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Microsoft.Data.SqlClient;
using NextGenSoftware.OASIS.Common;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class SqlServerNativeStorageConformanceTests
{
    [TestMethod]
    public async Task HolonPointReadHonorsChildDepth()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_POINT_GRAPH") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        var provider = new SQLServerDBOASIS(@"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true");
        var root = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.NewGuid(), Name = "Point graph root" };
        var child = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, Name = "Point graph child" };
        var leaf = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.NewGuid(), ParentHolonId = child.Id, Name = "Point graph leaf" };
        var savedIds = new List<Guid>();
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            foreach (var node in new[] { root, child, leaf })
            {
                var write = await provider.SaveHolonAsync(node, saveChildren: false);
                Assert.IsTrue(write.IsSaved && !write.IsError, write.Message);
                savedIds.Add(node.Id);
            }
            var deep = await provider.LoadHolonAsync(root.Id, continueOnError: false);
            Assert.IsTrue(deep.IsLoaded && !deep.IsError, deep.Message);
            Assert.AreEqual(leaf.Id, deep.Result.Children.Single().Children.Single().Id);
            var bounded = await provider.LoadHolonAsync(root.Id, maxChildDepth: 1, continueOnError: false);
            Assert.IsTrue(bounded.IsLoaded && !bounded.IsError, bounded.Message);
            Assert.AreEqual(child.Id, bounded.Result.Children.Single().Id);
            Assert.IsFalse(bounded.Result.Children.Single().Children?.Any() == true);
            var direct = await provider.LoadHolonAsync(root.Id, loadChildren: false);
            Assert.IsTrue(direct.IsLoaded && !direct.IsError, direct.Message);
            Assert.IsFalse(direct.Result.Children?.Any() == true);
        }
        finally
        {
            foreach (var id in savedIds)
            {
                var deleted = await provider.DeleteHolonWithSoftDeleteAsync(id, false);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task HolonListValidatesStoredIdentity()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_HOLON_LIST_IDENTITY") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var holon = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.NewGuid(), Name = "List identity fixture", Version = 1 };
        var saved = false;
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            var write = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsTrue(write.IsSaved && !write.IsError, write.Message);
            saved = true;
            var listed = await provider.LoadAllHolonsAsync();
            Assert.IsTrue(listed.IsLoaded && listed.Result.Any(row => row.Id == holon.Id), listed.Message);

            await using var sql = new SqlConnection(connection);
            await sql.OpenAsync();
            await using var update = new SqlCommand("UPDATE OASISHolons SET DataJson=N'{}' OUTPUT deleted.DataJson WHERE Id=@Id", sql);
            update.Parameters.AddWithValue("@Id", holon.Id);
            var original = (string)(await update.ExecuteScalarAsync())!;

            try
            {
                listed = await provider.LoadAllHolonsAsync();
                Assert.IsTrue(listed.IsError && listed.Exception is System.IO.InvalidDataException, listed.Message);
                Assert.IsNull(listed.Result);
            }
            finally
            {
                update.CommandText = "UPDATE OASISHolons SET DataJson=@Original WHERE Id=@Id";
                update.Parameters.AddWithValue("@Original", original);
                Assert.AreEqual(1, await update.ExecuteNonQueryAsync());
            }
        }
        finally
        {
            if (saved)
            {
                var deleted = await provider.DeleteHolonAsync(holon.Id);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task AvatarHistoryRetainsHistoricalSelectors()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_AVATAR_HISTORY") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var id = Guid.NewGuid();
        var avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = id, Version = 1, Username = $"fixture-{id:N}", Email = $"fixture-{id:N}@example.invalid" };
        var detail = new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail { Id = id, Version = 1, Username = avatar.Username, Email = avatar.Email };
        var saved = false;
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            var write = await provider.SaveAvatarAsync(avatar);
            Assert.IsTrue(write.IsSaved && !write.IsError, write.Message);
            saved = true;
            var detailWrite = await provider.SaveAvatarDetailAsync(detail);
            Assert.IsTrue(detailWrite.IsSaved && !detailWrite.IsError, detailWrite.Message);

            var oldUsername = avatar.Username;
            var oldEmail = avatar.Email;
            avatar.Username += "-updated";
            avatar.Email = $"updated-{id:N}@example.invalid";
            avatar.Version = 2;
            detail.Username = avatar.Username;
            detail.Email = avatar.Email;
            detail.Version = 2;
            var update = await provider.SaveAvatarAsync(avatar);
            Assert.IsTrue(update.IsSaved && !update.IsError, update.Message);
            var detailUpdate = await provider.SaveAvatarDetailAsync(detail);
            Assert.IsTrue(detailUpdate.IsSaved && !detailUpdate.IsError, detailUpdate.Message);

            avatar.Version = 0;
            detail.Version = 1;
            var staleAvatar = await provider.SaveAvatarAsync(avatar);
            var staleDetail = await provider.SaveAvatarDetailAsync(detail);
            Assert.IsTrue(staleAvatar.IsError && !staleAvatar.IsSaved, staleAvatar.Message);
            Assert.IsTrue(staleDetail.IsError && !staleDetail.IsSaved, staleDetail.Message);
            Assert.AreEqual(2, (await provider.LoadAvatarAsync(id)).Result?.Version);
            Assert.AreEqual(2, (await provider.LoadAvatarDetailAsync(id)).Result?.Version);

            var archived = await provider.LoadAvatarByUsernameAsync(oldUsername, version: 1);
            var archivedDetail = await provider.LoadAvatarDetailByEmailAsync(oldEmail, version: 1);
            Assert.AreEqual(id, archived.Result?.Id, archived.Message);
            Assert.AreEqual(id, archivedDetail.Result?.Id, archivedDetail.Message);
            Assert.AreEqual(oldEmail, archived.Result!.Email);
            Assert.AreEqual(oldUsername, archivedDetail.Result!.Username);
            Assert.AreEqual(avatar.Username, (await provider.LoadAvatarAsync(id)).Result?.Username);
            Assert.AreEqual(detail.Email, (await provider.LoadAvatarDetailAsync(id)).Result?.Email);
            Assert.AreEqual(oldUsername, (await provider.LoadAllAvatarsAsync(version: 1)).Result.Single(row => row.Id == id).Username);
            Assert.AreEqual(oldEmail, (await provider.LoadAllAvatarDetailsAsync(version: 1)).Result.Single(row => row.Id == id).Email);

            await using var sql = new SqlConnection(connection);
            await sql.OpenAsync();
            await using var corrupt = new SqlCommand(@"
UPDATE OASISAvatarHistory SET DataJson=JSON_MODIFY(DataJson, '$.version', 99) WHERE Id=@Id AND Version=1;
UPDATE OASISAvatarDetailHistory SET DataJson=JSON_MODIFY(DataJson, '$.Version', 99) WHERE Id=@Id AND Version=1;", sql);
            corrupt.Parameters.AddWithValue("@Id", id);
            Assert.AreEqual(2, await corrupt.ExecuteNonQueryAsync());
            var badAvatar = await provider.LoadAvatarAsync(id, version: 1);
            var badDetail = await provider.LoadAvatarDetailAsync(id, version: 1);
            Assert.IsTrue(badAvatar.IsError && badAvatar.Exception is System.IO.InvalidDataException, badAvatar.Message);
            Assert.IsTrue(badDetail.IsError && badDetail.Exception is System.IO.InvalidDataException, badDetail.Message);

            var softDeleted = await provider.DeleteAvatarAsync(id);
            Assert.IsTrue(softDeleted.IsDeleted && !softDeleted.IsError, softDeleted.Message);
            foreach (var version in new[] { 0, 1 })
            {
                var hidden = await provider.LoadAvatarDetailAsync(id, version);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, hidden.ErrorCode, hidden.Message);
                var listed = await provider.LoadAllAvatarDetailsAsync(version);
                Assert.IsTrue(listed.IsLoaded && !listed.IsError, listed.Message);
                Assert.IsFalse(listed.Result.Any(row => row.Id == id));
            }
        }
        finally
        {
            if (saved)
            {
                var deleted = await provider.DeleteAvatarAsync(id, softDelete: false);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
                await using var inspection = new SqlConnection(connection);
                await inspection.OpenAsync();
                await using var command = new SqlCommand(@"
SELECT (SELECT COUNT(*) FROM OASISAvatarHistory WHERE Id=@Id)
     + (SELECT COUNT(*) FROM OASISAvatarDetailHistory WHERE Id=@Id);", inspection);
                command.Parameters.AddWithValue("@Id", id);
                Assert.AreEqual(0, (int)(await command.ExecuteScalarAsync())!);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task AvatarReadsFilterRequestedVersion()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_AVATAR_VERSION") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var id = Guid.NewGuid();
        var avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = id, Version = 2, Username = $"fixture-{id:N}", Email = $"fixture-{id:N}@example.invalid" };
        var detail = new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail { Id = id, Version = 2, Username = avatar.Username, Email = avatar.Email };
        var saved = false;
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            var write = await provider.SaveAvatarAsync(avatar);
            Assert.IsTrue(write.IsSaved && !write.IsError, write.Message);
            saved = true;
            var detailWrite = await provider.SaveAvatarDetailAsync(detail);
            Assert.IsTrue(detailWrite.IsSaved && !detailWrite.IsError, detailWrite.Message);

            var loaded = await provider.LoadAvatarByUsernameAsync(avatar.Username, version: 2);
            var loadedDetail = await provider.LoadAvatarDetailByEmailAsync(avatar.Email, version: 2);
            Assert.AreEqual(id, loaded.Result?.Id, loaded.Message);
            Assert.AreEqual(id, loadedDetail.Result?.Id, loadedDetail.Message);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await provider.LoadAvatarAsync(id, version: 1)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, (await provider.LoadAvatarDetailAsync(id, version: 1)).ErrorCode);
            Assert.IsTrue((await provider.LoadAllAvatarsAsync(version: 2)).Result.Any(row => row.Id == id));
            Assert.IsTrue((await provider.LoadAllAvatarDetailsAsync(version: 2)).Result.Any(row => row.Id == id));
            Assert.IsFalse((await provider.LoadAllAvatarsAsync(version: 1)).Result.Any(row => row.Id == id));
            Assert.IsFalse((await provider.LoadAllAvatarDetailsAsync(version: 1)).Result.Any(row => row.Id == id));
        }
        finally
        {
            if (saved)
            {
                var deleted = await provider.DeleteAvatarAsync(id, softDelete: false);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task AvatarListsValidateStoredIdentity()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_AVATAR_LIST_IDENTITY") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var id = Guid.NewGuid();
        var avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = id, Username = $"fixture-{id:N}", Email = $"fixture-{id:N}@example.invalid" };
        var detail = new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail { Id = id, Username = avatar.Username, Email = avatar.Email };
        var saved = false;
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            var write = await provider.SaveAvatarAsync(avatar);
            Assert.IsTrue(write.IsSaved && !write.IsError, write.Message);
            saved = true;
            var detailWrite = await provider.SaveAvatarDetailAsync(detail);
            Assert.IsTrue(detailWrite.IsSaved && !detailWrite.IsError, detailWrite.Message);

            var avatars = await provider.LoadAllAvatarsAsync();
            var details = await provider.LoadAllAvatarDetailsAsync();
            Assert.IsTrue(avatars.IsLoaded && avatars.Result.Any(record => record.Id == id), avatars.Message);
            Assert.IsTrue(details.IsLoaded && details.Result.Any(record => record.Id == id), details.Message);

            await using var sql = new SqlConnection(connection);
            await sql.OpenAsync();
            await using var update = new SqlCommand(
                @"UPDATE OASISAvatars SET DataJson=N'{}' WHERE Id=@Id;
UPDATE OASISAvatarDetails SET DataJson=N'{}' WHERE Id=@Id;", sql);
            update.Parameters.AddWithValue("@Id", id);
            Assert.AreEqual(2, await update.ExecuteNonQueryAsync());

            avatars = await provider.LoadAllAvatarsAsync();
            details = await provider.LoadAllAvatarDetailsAsync();
            Assert.IsTrue(avatars.IsError && avatars.Exception is System.IO.InvalidDataException, avatars.Message);
            Assert.IsTrue(details.IsError && details.Exception is System.IO.InvalidDataException, details.Message);
            Assert.IsNull(avatars.Result);
            Assert.IsNull(details.Result);
        }
        finally
        {
            if (saved)
            {
                var deleted = await provider.DeleteAvatarAsync(id, softDelete: false);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task DuplicateAvatarSelectorsRejectOwnerExport()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_DUPLICATE_AVATAR") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var username = $"fixture-{Guid.NewGuid():N}";
        var email = username + "@example.invalid";
        var saved = new List<Guid>();
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            for (var i = 0; i < 2; i++)
            {
                var avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar
                {
                    Id = Guid.NewGuid(), Username = username, Email = email
                };
                var write = await provider.SaveAvatarAsync(avatar);
                Assert.IsTrue(write.IsSaved && !write.IsError, write.Message);
                saved.Add(avatar.Id);
            }

            var byUsername = await provider.ExportAllDataForAvatarByUsernameAsync(username);
            var byEmail = await provider.ExportAllDataForAvatarByEmailAsync(email);
            foreach (var result in new[] { byUsername, byEmail })
            {
                Assert.IsTrue(result.IsError);
                Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity, result.ErrorCode);
                Assert.IsNull(result.Result);
            }
        }
        finally
        {
            foreach (var id in saved)
            {
                var deleted = await provider.DeleteAvatarAsync(id, softDelete: false);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task DuplicateAvatarDetailSelectorsRejectLookup()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_DUPLICATE_DETAIL") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var username = $"fixture-{Guid.NewGuid():N}";
        var email = username + "@example.invalid";
        var saved = new List<Guid>();
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            for (var i = 0; i < 2; i++)
            {
                var avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar
                {
                    Id = Guid.NewGuid(), Username = username, Email = email
                };
                var write = await provider.SaveAvatarAsync(avatar);
                Assert.IsTrue(write.IsSaved && !write.IsError, write.Message);
                saved.Add(avatar.Id);
                var detail = new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail
                {
                    Id = avatar.Id, Username = username, Email = email
                };
                var detailWrite = await provider.SaveAvatarDetailAsync(detail);
                Assert.IsTrue(detailWrite.IsSaved && !detailWrite.IsError, detailWrite.Message);
                var byId = await provider.LoadAvatarDetailAsync(avatar.Id);
                Assert.IsTrue(byId.Result?.Id == avatar.Id && !byId.IsError, byId.Message);
            }

            var byUsername = await provider.LoadAvatarDetailByUsernameAsync(username);
            var byEmail = await provider.LoadAvatarDetailByEmailAsync(email);
            foreach (var result in new[] { byUsername, byEmail })
            {
                Assert.IsTrue(result.IsError);
                Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity, result.ErrorCode);
                Assert.IsNull(result.Result);
            }
        }
        finally
        {
            foreach (var id in saved)
            {
                var deleted = await provider.DeleteAvatarAsync(id, softDelete: false);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task DuplicateAvatarSelectorsRejectDeletion()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_DUPLICATE_DELETE") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var username = $"fixture-{Guid.NewGuid():N}";
        var email = username + "@example.invalid";
        var saved = new List<(Guid Id, string Key)>();
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            for (var i = 0; i < 2; i++)
            {
                var avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar
                {
                    Id = Guid.NewGuid(), Username = username, Email = email
                };
                var write = await provider.SaveAvatarAsync(avatar);
                Assert.IsTrue(write.IsSaved && !write.IsError, write.Message);
                saved.Add((avatar.Id, write.Result!.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLServerDBOASIS]));
            }

            foreach (var softDelete in new[] { true, false })
            {
                var results = new[]
                {
                    await provider.DeleteAvatarByUsernameAsync(username, softDelete),
                    await provider.DeleteAvatarByEmailAsync(email, softDelete)
                };
                foreach (var result in results)
                {
                    Assert.IsTrue(result.IsError);
                    Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity, result.ErrorCode);
                    Assert.AreEqual(0, result.DeletedCount);
                }
            }

            foreach (var (id, key) in saved)
            {
                var loaded = await provider.LoadAvatarAsync(id);
                Assert.IsTrue(loaded.Result?.Id == id && !loaded.IsError, loaded.Message);
            }
        }
        finally
        {
            foreach (var (id, key) in saved)
            {
                var deleted = await provider.DeleteAvatarAsync(key, softDelete: false);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task HistoricalCollectionsUseStoredParent()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_HISTORY_COLLECTION") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var originalParent = Guid.NewGuid();
        var newParent = Guid.NewGuid();
        var child = new NextGenSoftware.OASIS.API.Core.Holons.Holon
        {
            Id = Guid.NewGuid(), ParentHolonId = originalParent, Name = "Historical child", Version = 1
        };
        var saved = false;
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            var first = await provider.SaveHolonAsync(child, saveChildren: false);
            Assert.IsTrue(first.IsSaved && !first.IsError, first.Message);
            saved = true;
            child.ParentHolonId = newParent;
            child.Version = 2;
            var moved = await provider.SaveHolonAsync(child, saveChildren: false);
            Assert.IsTrue(moved.IsSaved && !moved.IsError, moved.Message);

            var historical = await provider.LoadHolonsForParentAsync(originalParent, loadChildren: false, version: 1);
            Assert.IsTrue(historical.IsLoaded && !historical.IsError, historical.Message);
            Assert.AreEqual(child.Id, historical.Result.Single().Id);
            var oldParent = await provider.LoadHolonsForParentAsync(originalParent, loadChildren: false);
            Assert.IsTrue(oldParent.IsLoaded && !oldParent.IsError, oldParent.Message);
            Assert.AreEqual(0, oldParent.LoadedCount);
            var current = await provider.LoadHolonsForParentAsync(newParent.ToString("D"), loadChildren: false);
            Assert.IsTrue(current.IsLoaded && !current.IsError, current.Message);
            Assert.AreEqual(child.Id, current.Result.Single().Id);

            var all = await provider.LoadAllHolonsAsync(loadChildren: false, version: 1);
            Assert.IsTrue(all.IsLoaded && !all.IsError, all.Message);
            Assert.AreEqual(originalParent, all.Result.Single(row => row.Id == child.Id).ParentHolonId);
        }
        finally
        {
            if (saved)
            {
                var deleted = await provider.DeleteHolonWithSoftDeleteAsync(child.Id, false);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task StaleChildRollsBackParentAndArchive()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_HISTORY_ROLLBACK") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var parent = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.NewGuid(), Name = "Original", Version = 1 };
        var child = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.NewGuid(), Name = "Child", Version = 2 };
        parent.Children.Add(child);
        var saved = false;
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            var initial = await provider.SaveHolonAsync(parent);
            Assert.IsTrue(initial.IsSaved && !initial.IsError, initial.Message);
            Assert.AreEqual(2, initial.SavedCount);
            saved = true;

            parent.Version = 2;
            parent.Name = "Must roll back";
            child.Version = 1;
            var rejected = await provider.SaveHolonAsync(parent);
            Assert.IsTrue(rejected.IsError && !rejected.IsSaved, rejected.Message);
            var current = await provider.LoadHolonAsync(parent.Id, loadChildren: false);
            Assert.IsTrue(current.IsLoaded && !current.IsError, current.Message);
            Assert.AreEqual("Original", current.Result.Name);
            Assert.AreEqual(1, current.Result.Version);

            await using var inspection = new SqlConnection(connection);
            await inspection.OpenAsync();
            await using var cmd = new SqlCommand("SELECT COUNT(*) FROM OASISHolonHistory WHERE Id=@Id", inspection);
            cmd.Parameters.AddWithValue("@Id", parent.Id);
            Assert.AreEqual(0, (int)(await cmd.ExecuteScalarAsync())!);
        }
        finally
        {
            if (saved)
            {
                foreach (var id in new[] { child.Id, parent.Id })
                {
                    var deleted = await provider.DeleteHolonWithSoftDeleteAsync(id, false);
                    Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
                }
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task HolonHistoryRetainsSupersededSnapshot()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_HISTORY") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var id = Guid.NewGuid();
        var holon = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = id, Name = "Original", Version = 1 };
        var saved = false;
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            var first = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsTrue(first.IsSaved && !first.IsError, first.Message);
            saved = true;

            holon.Version = 2;
            holon.Name = "Updated";
            var second = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsTrue(second.IsSaved && !second.IsError, second.Message);

            var historical = await provider.LoadHolonAsync(id, loadChildren: false, version: 1);
            Assert.IsTrue(historical.IsLoaded && !historical.IsError, historical.Message);
            Assert.AreEqual("Original", historical.Result.Name);
            var current = await provider.LoadHolonAsync(id.ToString("D"), loadChildren: false, version: 2);
            Assert.IsTrue(current.IsLoaded && !current.IsError, current.Message);
            Assert.AreEqual("Updated", current.Result.Name);
        }
        finally
        {
            if (saved)
            {
                var deleted = await provider.DeleteHolonWithSoftDeleteAsync(id, false);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
                await using var inspection = new SqlConnection(connection);
                await inspection.OpenAsync();
                await using var cmd = new SqlCommand("SELECT COUNT(*) FROM OASISHolonHistory WHERE Id=@Id", inspection);
                cmd.Parameters.AddWithValue("@Id", id);
                Assert.AreEqual(0, (int)(await cmd.ExecuteScalarAsync())!);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task OwnerExportReturnsRequestedSnapshot()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_HISTORY_EXPORT") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var id = Guid.NewGuid();
        var holon = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = id, Name = "Original", Version = 1, CreatedByAvatarId = Guid.NewGuid() };
        var saved = false;
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            var first = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsTrue(first.IsSaved && !first.IsError, first.Message);
            saved = true;

            holon.Version = 2;
            holon.Name = "Updated";
            var second = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsTrue(second.IsSaved && !second.IsError, second.Message);

            var historical = await provider.ExportAllDataForAvatarByIdAsync(holon.CreatedByAvatarId, version: 1);
            Assert.IsTrue(historical.IsLoaded && !historical.IsError, historical.Message);
            Assert.AreEqual("Original", historical.Result.Single(row => row.Id == id).Name);
            var current = await provider.ExportAllDataForAvatarByIdAsync(holon.CreatedByAvatarId);
            Assert.IsTrue(current.IsLoaded && !current.IsError, current.Message);
            Assert.AreEqual("Updated", current.Result.Single(row => row.Id == id).Name);
        }
        finally
        {
            if (saved)
            {
                var deleted = await provider.DeleteHolonWithSoftDeleteAsync(id, false);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
                await using var inspection = new SqlConnection(connection);
                await inspection.OpenAsync();
                await using var cmd = new SqlCommand("SELECT COUNT(*) FROM OASISHolonHistory WHERE Id=@Id", inspection);
                cmd.Parameters.AddWithValue("@Id", id);
                Assert.AreEqual(0, (int)(await cmd.ExecuteScalarAsync())!);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task SearchSelectsArchivedHolonText()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_HISTORY_SEARCH") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var id = Guid.NewGuid();
        var holon = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = id, Name = id.ToString("N") + "%_", Version = 1 };
        var saved = false;
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            var first = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsTrue(first.IsSaved && !first.IsError, first.Message);
            saved = true;

            holon.Version = 2;
            holon.Name = "Updated";
            var second = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsTrue(second.IsSaved && !second.IsError, second.Message);

            var search = new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams
            {
                SearchGroups = new()
                {
                    new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup { SearchQuery = id.ToString("N") + "%_" }
                }
            };
            var historical = await provider.SearchAsync(search, loadChildren: false, version: 1);
            Assert.IsTrue(historical.IsLoaded && !historical.IsError, historical.Message);
            Assert.AreEqual(id, historical.Result.SearchResultHolons.Single().Id);
            var current = await provider.SearchAsync(search, loadChildren: false);
            Assert.IsTrue(current.IsLoaded && !current.IsError, current.Message);
            Assert.AreEqual(0, current.LoadedCount);
        }
        finally
        {
            if (saved)
            {
                var deleted = await provider.DeleteHolonWithSoftDeleteAsync(id, false);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
                await using var inspection = new SqlConnection(connection);
                await inspection.OpenAsync();
                await using var cmd = new SqlCommand("SELECT COUNT(*) FROM OASISHolonHistory WHERE Id=@Id", inspection);
                cmd.Parameters.AddWithValue("@Id", id);
                Assert.AreEqual(0, (int)(await cmd.ExecuteScalarAsync())!);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task MetadataReadsSelectArchivedSnapshot()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_HISTORY_METADATA") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var id = Guid.NewGuid();
        var holon = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = id, Name = "Original", Version = 1, MetaData = new() { [id.ToString("N")] = "before" } };
        var saved = false;
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            var first = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsTrue(first.IsSaved && !first.IsError, first.Message);
            saved = true;

            holon.Version = 2;
            holon.MetaData[id.ToString("N")] = "after";
            var second = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsTrue(second.IsSaved && !second.IsError, second.Message);

            var key = id.ToString("N");
            var historical = await provider.LoadHolonsByMetaDataAsync(key, "before", loadChildren: false, version: 1);
            Assert.IsTrue(historical.IsLoaded && !historical.IsError, historical.Message);
            Assert.AreEqual(id, historical.Result.Single().Id);

            var filters = new Dictionary<string, string> { [key] = "before" };
            var current = await provider.LoadHolonsByMetaDataAsync(filters,
                NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All, loadChildren: false);
            Assert.IsTrue(current.IsLoaded && !current.IsError, current.Message);
            Assert.AreEqual(0, current.LoadedCount);
            var archived = await provider.LoadHolonsByMetaDataAsync(filters,
                NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All, loadChildren: false, version: 1);
            Assert.IsTrue(archived.IsLoaded && !archived.IsError, archived.Message);
            Assert.AreEqual(id, archived.Result.Single().Id);
        }
        finally
        {
            if (saved)
            {
                var deleted = await provider.DeleteHolonWithSoftDeleteAsync(id, false);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
                await using var inspection = new SqlConnection(connection);
                await inspection.OpenAsync();
                await using var cmd = new SqlCommand("SELECT COUNT(*) FROM OASISHolonHistory WHERE Id=@Id", inspection);
                cmd.Parameters.AddWithValue("@Id", id);
                Assert.AreEqual(0, (int)(await cmd.ExecuteScalarAsync())!);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task MetadataQueriesMatchLiteralPropertyNames()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_METADATA_PATH") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var id = Guid.NewGuid();
        var key = $"fixture.{id:N}";
        var holon = new NextGenSoftware.OASIS.API.Core.Holons.Holon
        {
            Id = id, Name = key, MetaData = new() { [key] = "literal", ["scope"] = id.ToString("D") }
        };
        var saved = false;
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            var write = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsTrue(write.IsSaved && !write.IsError, write.Message);
            saved = true;

            var single = await provider.LoadHolonsByMetaDataAsync(key, "literal", loadChildren: false);
            Assert.IsTrue(single.IsLoaded && !single.IsError, single.Message);
            Assert.AreEqual(id, single.Result.Single().Id);

            var multiple = await provider.LoadHolonsByMetaDataAsync(
                new Dictionary<string, string> { [key] = "literal", ["scope"] = id.ToString("D") },
                NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All, loadChildren: false);
            Assert.IsTrue(multiple.IsLoaded && !multiple.IsError, multiple.Message);
            Assert.AreEqual(id, multiple.Result.Single().Id);
        }
        finally
        {
            if (saved)
            {
                var deleted = await provider.DeleteHolonWithSoftDeleteAsync(id, false);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task SearchMatchesLiteralSqlWildcardCharacters()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_LITERAL_SEARCH") != "1")
            Assert.Inconclusive("Enable only for the dedicated LocalDB provider fixture.");

        const string connection = @"Server=(localdb)\MSSQLLocalDB;Database=oasis_hyperdrive_provider_test;Integrated Security=true;TrustServerCertificate=true";
        var provider = new SQLServerDBOASIS(connection);
        var marker = Guid.NewGuid().ToString("N");
        var expected = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.NewGuid(), Name = marker + "%_[" };
        var other = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.NewGuid(), Name = marker + "abc" };
        var saved = new List<Guid>();
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.IsTrue(active.Result && !active.IsError, active.Message);
            foreach (var holon in new[] { expected, other })
            {
                var write = await provider.SaveHolonAsync(holon, saveChildren: false);
                Assert.IsTrue(write.IsSaved && !write.IsError, write.Message);
                saved.Add(holon.Id);
            }

            var query = new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams
            {
                SearchGroups = new()
                {
                    new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup { SearchQuery = marker + "%_" }
                }
            };
            var result = await provider.SearchAsync(query, loadChildren: false);
            Assert.IsTrue(result.IsLoaded && !result.IsError, result.Message);
            Assert.AreEqual(1, result.LoadedCount);
            Assert.AreEqual(expected.Id, result.Result.SearchResultHolons.Single().Id);
        }
        finally
        {
            foreach (var id in saved)
            {
                var deleted = await provider.DeleteHolonWithSoftDeleteAsync(id, false);
                Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task FinishRetainedLifecycleDeletes(bool sync)
    {
        if (Environment.GetEnvironmentVariable("OASIS_SQLSERVER_RETAINED_LIFECYCLE") != "1")
            Assert.Inconclusive("Explicit opt-in is required for retained fixture deletion.");

        var connection = Environment.GetEnvironmentVariable("OASIS_SQLSERVER_CONFORMANCE_CONNECTION_STRING");
        Assert.IsFalse(string.IsNullOrWhiteSpace(connection));
        var settings = new SqlConnectionStringBuilder(connection);
        Assert.AreEqual("oasis_hyperdrive_provider_test", settings.InitialCatalog);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts")))
            root = root.Parent;
        Assert.IsNotNull(root);

        var directory = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent",
            nameof(SQLServerDBOASIS), sync ? "sync" : "async");
        using var lease = new FileStream(Path.Combine(directory, "environment.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var report = JsonSerializer.Deserialize<CoreStorageConformance.Report>(
            await File.ReadAllTextAsync(Path.Combine(directory, "report.json")));
        Assert.IsNotNull(report);
        Assert.AreEqual(nameof(SQLServerDBOASIS), report.Provider);
        Assert.AreEqual("owner-export", report.CurrentCase);
        Assert.IsFalse(report.Succeeded);

        var journalPath = Path.Combine(directory, "delete-verification.jsonl");
        var resume = Environment.GetEnvironmentVariable("OASIS_SQLSERVER_RESUME_CHILD_CLEANUP") == "1";
        if (resume)
        {
            var entries = await File.ReadAllLinesAsync(journalPath);
            var phases = new[] { "soft-delete-intent", "soft-delete-confirmed", "hard-delete-intent" };
            Assert.AreEqual(phases.Length, entries.Length);
            for (var index = 0; index < entries.Length; index++)
            {
                using var entry = JsonDocument.Parse(entries[index]);
                Assert.AreEqual(phases[index], entry.RootElement.GetProperty("phase").GetString());
                Assert.AreEqual(report.ChildId, entry.RootElement.GetProperty("id").GetGuid());
            }
            await using var database = new SqlConnection(connection);
            await database.OpenAsync();
            await using var command = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.OASISHolons WHERE Id=@Id AND IsDeleted=1 AND TRY_CONVERT(uniqueidentifier, JSON_VALUE(DataJson,'$.createdByAvatarId'))=@Owner",
                database);
            command.Parameters.AddWithValue("@Id", report.ChildId);
            command.Parameters.AddWithValue("@Owner", report.OwnerId);
            Assert.AreEqual(1, (int)(await command.ExecuteScalarAsync())!);
        }

        var provider = new SQLServerDBOASIS(connection!);
        var owned = await provider.ExportAllDataForAvatarByIdAsync(report.OwnerId);
        Assert.IsFalse(owned.IsError);
        var ids = new[] { report.ChildId, report.RootId, report.ImportId }.Concat(report.BatchIds).ToArray();
        CollectionAssert.AreEquivalent(resume ? ids.Skip(1).ToArray() : ids, owned.Result.Select(record => record.Id).ToArray());
        Assert.IsTrue(owned.Result.All(record => record.CreatedByAvatarId == report.OwnerId));
        var avatar = await provider.LoadAvatarAsync(report.OwnerId);
        Assert.AreEqual("conformance-" + report.OwnerId.ToString("N"), avatar.Result?.Username);

        await using var journal = new FileStream(journalPath,
            resume ? FileMode.Append : FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        async Task Record(string phase, Guid id)
        {
            var entry = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { phase, id }) + "\n");
            await journal.WriteAsync(entry);
            journal.Flush(flushToDisk: true);
        }

        foreach (var id in ids)
        {
            if (!resume || id != report.ChildId)
            {
                await Record("soft-delete-intent", id);
                var deleted = sync ? provider.DeleteHolon(id) : await provider.DeleteHolonAsync(id);
                Assert.IsFalse(deleted.IsError);
                Assert.IsTrue(deleted.IsDeleted);
                Assert.AreEqual(1, deleted.DeletedCount);
                var observed = sync ? provider.LoadHolon(id, loadChildren: false)
                    : await provider.LoadHolonAsync(id, loadChildren: false);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, observed.ErrorCode);
                Assert.IsNull(observed.Result);
                await Record("soft-delete-confirmed", id);

                if (id == report.ChildId)
                {
                    var children = sync ? provider.LoadHolonsForParent(report.RootId, loadChildren: false)
                        : await provider.LoadHolonsForParentAsync(report.RootId, loadChildren: false);
                    Assert.IsFalse(children.IsError);
                    Assert.IsFalse(children.Result.Any(child => child.Id == id));
                }

                await Record("hard-delete-intent", id);
            }
            var cleaned = sync ? provider.DeleteHolonWithSoftDelete(id, false)
                : await provider.DeleteHolonWithSoftDeleteAsync(id, false);
            Assert.IsFalse(cleaned.IsError);
            Assert.IsTrue(cleaned.IsDeleted);
            Assert.AreEqual(1, cleaned.DeletedCount);
            await Record("hard-delete-confirmed", id);
        }

        await Record("avatar-delete-intent", report.OwnerId);
        var removed = sync ? provider.DeleteAvatar(report.OwnerId, false)
            : await provider.DeleteAvatarAsync(report.OwnerId, false);
        Assert.IsFalse(removed.IsError);
        Assert.IsTrue(removed.IsDeleted);
        Assert.AreEqual(1, removed.DeletedCount);
        var afterAvatar = sync ? provider.LoadAvatar(report.OwnerId) : await provider.LoadAvatarAsync(report.OwnerId);
        var afterDetail = sync ? provider.LoadAvatarDetail(report.OwnerId) : await provider.LoadAvatarDetailAsync(report.OwnerId);
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, afterAvatar.ErrorCode);
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, afterDetail.ErrorCode);
        Assert.IsNull(afterAvatar.Result);
        Assert.IsNull(afterDetail.Result);
        var inactive = sync ? provider.DeActivateProvider() : await provider.DeActivateProviderAsync();
        Assert.IsFalse(inactive.IsError);
        Assert.IsTrue(inactive.Result);
        await Record("complete", report.OwnerId);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ExportRetainedOwnerRecords(bool sync)
    {
        var connection = Environment.GetEnvironmentVariable("OASIS_SQLSERVER_CONFORMANCE_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connection))
            Assert.Inconclusive("The dedicated SQL Server fixture is required.");

        var settings = new SqlConnectionStringBuilder(connection);
        Assert.AreEqual("oasis_hyperdrive_provider_test", settings.InitialCatalog);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts")))
            root = root.Parent;
        Assert.IsNotNull(root);

        var path = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent",
            nameof(SQLServerDBOASIS), sync ? "sync" : "async", "report.json");
        if (!File.Exists(path))
            Assert.Inconclusive("The original failed owner export report is required.");

        var report = JsonSerializer.Deserialize<CoreStorageConformance.Report>(await File.ReadAllTextAsync(path));
        Assert.IsNotNull(report);
        Assert.AreEqual(nameof(SQLServerDBOASIS), report.Provider);
        Assert.AreEqual("owner-export", report.CurrentCase);
        Assert.IsFalse(report.Succeeded);

        var provider = new SQLServerDBOASIS(connection!);
        var result = sync ? provider.ExportAllDataForAvatarById(report.OwnerId)
            : await provider.ExportAllDataForAvatarByIdAsync(report.OwnerId);
        Assert.IsFalse(result.IsError, result.Exception?.GetType().FullName ?? "Owner export failed.");
        Assert.IsTrue(result.IsLoaded);
        var expected = new[] { report.RootId, report.ChildId, report.ImportId }.Concat(report.BatchIds).ToArray();
        Assert.AreEqual(expected.Length, result.LoadedCount);
        Assert.IsNotNull(result.Result);
        var records = result.Result.ToArray();
        CollectionAssert.AreEquivalent(expected, records.Select(record => record.Id).ToArray());
        Assert.IsTrue(records.All(record => record.CreatedByAvatarId == report.OwnerId));
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ReadRetainedAvatarDetail(bool sync)
    {
        var connection = Environment.GetEnvironmentVariable("OASIS_SQLSERVER_CONFORMANCE_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connection))
            Assert.Inconclusive("The dedicated SQL Server fixture is required.");

        var settings = new SqlConnectionStringBuilder(connection);
        Assert.AreEqual("oasis_hyperdrive_provider_test", settings.InitialCatalog);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts")))
            root = root.Parent;
        Assert.IsNotNull(root);

        var path = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent",
            nameof(SQLServerDBOASIS), sync ? "sync" : "async", "report.json");
        if (!File.Exists(path))
            Assert.Inconclusive("The original failed detail fixture report is required.");

        var report = JsonSerializer.Deserialize<CoreStorageConformance.Report>(await File.ReadAllTextAsync(path));
        Assert.IsNotNull(report);
        Assert.AreEqual(nameof(SQLServerDBOASIS), report.Provider);
        Assert.AreEqual("detail-id", report.CurrentCase);
        Assert.IsFalse(report.Succeeded);

        var provider = new SQLServerDBOASIS(connection!);
        var result = sync ? provider.LoadAvatarDetail(report.OwnerId) : await provider.LoadAvatarDetailAsync(report.OwnerId);
        var diagnostic = result.Exception is Newtonsoft.Json.JsonSerializationException serializationError
            ? $"{serializationError.GetType().Name} at {serializationError.Path}"
            : result.Exception?.GetType().FullName;
        Assert.IsFalse(result.IsError, diagnostic ?? "Detail read failed.");
        Assert.IsTrue(result.IsLoaded);
        Assert.AreEqual(1, result.LoadedCount);
        Assert.IsNotNull(result.Result);
        Assert.AreEqual(report.OwnerId, result.Result.Id);
        Assert.AreEqual(17L, result.Result.Karma);
        Assert.AreEqual(31, result.Result.XP);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CoreStorageSuiteAgainstSqlServer(bool sync)
    {
        var connection = Environment.GetEnvironmentVariable("OASIS_SQLSERVER_CONFORMANCE_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connection))
            Assert.Inconclusive("Set OASIS_SQLSERVER_CONFORMANCE_CONNECTION_STRING to a dedicated SQL Server test database.");

        var settings = new SqlConnectionStringBuilder(connection);
        Assert.AreEqual("oasis_hyperdrive_provider_test", settings.InitialCatalog,
            "Use the dedicated database reserved for HyperDrive provider conformance.");

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts")))
            root = root.Parent;
        Assert.IsNotNull(root, "Run this test from the repository.");

        var directory = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent",
            nameof(SQLServerDBOASIS), sync ? "sync" : "async");
        Directory.CreateDirectory(directory);
        using var lease = new FileStream(Path.Combine(directory, "environment.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        var reportPath = Path.Combine(directory, "report.json");
        var pendingPath = reportPath + ".next";
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(pendingPath),
            "Preserve the previous native report and resolve its fixture before running again.");

        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using (var file = new FileStream(pendingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, report);
                file.Flush(flushToDisk: true);
            }
            File.Move(pendingPath, reportPath, overwrite: true);
        }

        var provider = new SQLServerDBOASIS(connection!);
        var result = await CoreStorageConformance.RunAsync(
            provider, nameof(SQLServerDBOASIS), "native", sync, Checkpoint, expectedDefaultDeletion: "absent");

        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
    }
}
