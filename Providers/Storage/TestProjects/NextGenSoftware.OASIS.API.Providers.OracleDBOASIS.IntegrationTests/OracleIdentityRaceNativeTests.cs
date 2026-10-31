using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    private sealed class IdentityRaceOwner(string connection) : OracleDBOASIS(connection)
    {
        public bool HoldNextCommit;
        public readonly TaskCompletionSource<int> Locked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task CommitLifecycleTransactionAsync(OracleTransaction transaction)
        {
            if (HoldNextCommit)
            {
                HoldNextCommit = false;
                await using var sid = new OracleCommand("SELECT SYS_CONTEXT('USERENV','SID') FROM DUAL", transaction.Connection) { Transaction = transaction };
                Locked.TrySetResult(Convert.ToInt32(await sid.ExecuteScalarAsync()));
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(30));
            }
            await base.CommitLifecycleTransactionAsync(transaction);
        }
    }

    [DataTestMethod]
    [DataRow(false, false, false)][DataRow(false, false, true)]
    [DataRow(false, true, false)][DataRow(false, true, true)]
    [DataRow(true, false, false)][DataRow(true, false, true)]
    [DataRow(true, true, false)][DataRow(true, true, true)]
    public async Task NativeIdentitySaveAndPairDeleteSerialize(bool detailExists, bool saveFirst, bool synchronous)
    {
        var connection = Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal = Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!, $"identity-race-{detailExists}-{saveFirst}-{synchronous}.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var id = Guid.NewGuid(); var name = "owned-identity-race-" + id.ToString("N");
        var owner = new IdentityRaceOwner(connection) { IsVersionControlEnabled = true };
        var contender = new OracleDBOASIS(connection) { IsVersionControlEnabled = true };
        Task<OASISResult<IAvatarDetail>>? save = null;
        Task<OASISResult<bool>>? deletion = null;
        await using var verifier = new OracleConnection(connection);
        try
        {
            Assert.IsFalse((await owner.ActivateProviderAsync()).IsError);
            await verifier.OpenAsync();
            foreach (var table in new[] { "OASIS_AVATARS", "OASIS_AVATAR_DETAILS" }) Assert.IsNull(await Read(verifier, table, id));
            using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new { id, name, detailExists, saveFirst, synchronous, state = "before-native-identity-race" }));
                file.Flush(true);
            }
            var profile = await owner.SaveAvatarAsync(new Avatar { Id = id, Username = name, Email = name + "@example.invalid", IsActive = true });
            Assert.IsFalse(profile.IsError, profile.Exception?.ToString());
            if (detailExists)
            {
                var initial = await owner.SaveAvatarDetailAsync(new AvatarDetail { Id = id, Username = name, Email = name + "@example.invalid", Karma = 17, XP = 31, IsActive = true });
                Assert.IsFalse(initial.IsError, initial.Exception?.ToString());
            }
            var candidate = new AvatarDetail { Id = id, Username = name, Email = name + "@example.invalid", Karma = 23, XP = 47, IsActive = true };
            Task<OASISResult<IAvatarDetail>> Save(OracleDBOASIS provider) => synchronous ? Task.Run(() => provider.SaveAvatarDetail(candidate)) : provider.SaveAvatarDetailAsync(candidate);
            Task<OASISResult<bool>> Delete(OracleDBOASIS provider) => synchronous ? Task.Run(() => provider.DeleteAvatar(id, true)) : provider.DeleteAvatarAsync(id, true);
            owner.HoldNextCommit = true;
            if (saveFirst) save = Save(owner); else deletion = Delete(owner);
            var blocker = await owner.Locked.Task.WaitAsync(TimeSpan.FromSeconds(15));
            if (saveFirst) deletion = Delete(contender); else save = Save(contender);
            var deadline = DateTime.UtcNow.AddSeconds(15); var blocked = false;
            do
            {
                await using var wait = new OracleCommand("SELECT COUNT(*) FROM V$SESSION WHERE BLOCKING_SESSION=:Sid AND EVENT='enq: TX - row lock contention'", verifier) { BindByName = true };
                wait.Parameters.Add("Sid", OracleDbType.Int32).Value = blocker;
                blocked = Convert.ToInt32(await wait.ExecuteScalarAsync()) > 0;
                if (!blocked) await Task.Delay(100);
            } while (!blocked && DateTime.UtcNow < deadline);
            var bothPending = !save!.IsCompleted && !deletion!.IsCompleted;
            owner.Release.TrySetResult();
            await Task.WhenAll(save!, deletion!).WaitAsync(TimeSpan.FromSeconds(30));
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(save!.Result.Exception), save.Result.Exception?.ToString());
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(deletion!.Result.Exception), deletion.Result.Exception?.ToString());
            Assert.IsFalse(deletion.Result.IsError, deletion.Result.Exception?.ToString());
            Assert.AreEqual(!saveFirst, save.Result.IsError, save.Result.Exception?.ToString());
            if (!saveFirst) Assert.IsInstanceOfType<StorageLifecycleConflictException>(save.Result.Exception);
            var observed = new Dictionary<string, string>();
            foreach (var table in new[] { "OASIS_AVATARS", "OASIS_AVATAR_DETAILS" })
            {
                var json = await Read(verifier, table, id);
                if (table == "OASIS_AVATAR_DETAILS" && !detailExists && !saveFirst) { Assert.IsNull(json); continue; }
                Assert.IsNotNull(json);
                using var parsed = JsonDocument.Parse(json);
                Assert.AreEqual(id, parsed.RootElement.GetProperty("Id").GetGuid());
                Assert.AreEqual(name, parsed.RootElement.GetProperty("Username").GetString());
                Assert.IsTrue(parsed.RootElement.GetProperty("IsDeleted").GetBoolean());
                Assert.IsFalse(parsed.RootElement.GetProperty("IsActive").GetBoolean());
                if (table == "OASIS_AVATAR_DETAILS")
                {
                    Assert.AreEqual(saveFirst ? 23L : 17L, parsed.RootElement.GetProperty("Karma").GetInt64());
                    Assert.AreEqual(saveFirst ? 47L : 31L, parsed.RootElement.GetProperty("XP").GetInt64());
                }
                observed.Add(table, json);
            }
            foreach (var row in observed)
            {
                await using var cleanup = new OracleCommand($"DELETE FROM {row.Key} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0", verifier) { BindByName = true };
                cleanup.Parameters.Add("Id", OracleDbType.Raw).Value = id.ToByteArray();
                cleanup.Parameters.Add("Expected", OracleDbType.Clob).Value = row.Value;
                Assert.AreEqual(1, await cleanup.ExecuteNonQueryAsync());
            }
            foreach (var table in new[] { "OASIS_AVATARS", "OASIS_AVATAR_DETAILS" }) Assert.IsNull(await Read(verifier, table, id));
            File.Delete(journal);
            Assert.IsTrue(blocked, "Independent native V$SESSION observation must establish actual transaction contention.");
            Assert.IsTrue(bothPending, "Neither original operation may finish while the owner commit is held.");
        }
        finally
        {
            owner.Release.TrySetResult();
            if (save != null) await save.WaitAsync(TimeSpan.FromSeconds(30));
            if (deletion != null) await deletion.WaitAsync(TimeSpan.FromSeconds(30));
            await owner.DeActivateProviderAsync();
            await contender.DeActivateProviderAsync();
        }
    }
}
