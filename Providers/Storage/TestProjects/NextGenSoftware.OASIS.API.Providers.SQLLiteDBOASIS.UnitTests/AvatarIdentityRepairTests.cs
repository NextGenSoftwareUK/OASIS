using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

[TestClass]
public sealed class AvatarIdentityRepairTests
{
    [TestMethod]
    public async Task ActivationRepairsLegacyDuplicateUsernamesAndEmailsWithoutLosingData()
    {
        var path = Path.GetTempFileName();
        var cs = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ConnectionString;
        try
        {
            var first = NewAvatar("legacy-first");
            var second = NewAvatar("legacy-second");
            using (var provider = new SQLLiteDBOASIS(cs))
            {
                Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
                Assert.IsFalse((await provider.SaveAvatarAsync(first)).IsError);
                Assert.IsFalse((await provider.SaveAvatarAsync(second)).IsError);
            }

            // Recreate a database written before the identity indexes existed.
            await ExecAsync(cs, "DROP INDEX oasis_avatar_username_unique; DROP INDEX oasis_avatar_email_unique; "
                + $"UPDATE Avatar SET Username='legacy-first', Email='legacy-first@example.invalid' WHERE Id='{second.Id}';");

            for (var pass = 0; pass < 2; pass++)
                using (var provider = new SQLLiteDBOASIS(cs))
                {
                    var activated = await provider.ActivateProviderAsync();
                    Assert.IsFalse(activated.IsError, activated.Message);
                }

            using var connection = new SqliteConnection(cs);
            await connection.OpenAsync();
            Assert.AreEqual("legacy-first", await ScalarAsync(connection, $"SELECT Username FROM Avatar WHERE Id='{first.Id}'"));
            Assert.AreEqual($"legacy-first#legacy-{second.Id}", await ScalarAsync(connection, $"SELECT Username FROM Avatar WHERE Id='{second.Id}'"));
            Assert.AreEqual($"legacy-first@example.invalid#legacy-{second.Id}", await ScalarAsync(connection, $"SELECT Email FROM Avatar WHERE Id='{second.Id}'"));
            Assert.AreEqual("legacy-first", await ScalarAsync(connection, $"SELECT OriginalValue FROM CosmicAvatarIdentityRepair WHERE AvatarId='{second.Id}' AND Field='Username'"));
            Assert.AreEqual("legacy-first@example.invalid", await ScalarAsync(connection, $"SELECT OriginalValue FROM CosmicAvatarIdentityRepair WHERE AvatarId='{second.Id}' AND Field='Email'"));
            Assert.AreEqual(2L, await ScalarAsync(connection, "SELECT COUNT(*) FROM CosmicAvatarIdentityRepair"));
            Assert.AreEqual(2L, await ScalarAsync(connection, "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name IN ('oasis_avatar_username_unique','oasis_avatar_email_unique')"));
        }
        finally
        {
            foreach (var file in new[] { path, path + "-wal", path + "-shm", path + "-journal" })
                File.Delete(file);
        }
    }

    private static Avatar NewAvatar(string name) => new()
    {
        Id = Guid.NewGuid(), Username = name, Email = name + "@example.invalid", IsActive = true,
        AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)
    };

    private static async Task ExecAsync(string cs, string sql)
    {
        using var connection = new SqliteConnection(cs);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ScalarAsync(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync();
    }
}
