using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [TestMethod]
    public Task ReconcileKnownKarmaNullOutcomeWithoutReplayingMutation() => ReconcileExactKarmaFixture(false);

    [TestMethod]
    public Task ReconcileKnownKarmaReadGuardFixture() => ReconcileExactKarmaFixture(true);

    private async Task ReconcileExactKarmaFixture(bool readGuard)
    {
        var work = Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var pending = Path.Combine(work, "v2-karma-manager.pending.json");
        var retired = pending + (readGuard ? ".guard-reconciled" : ".reconciled");
        Assert.IsFalse(File.Exists(retired));
        Assert.AreEqual(1, Directory.GetFiles(work, "*.pending.json").Length);
        Assert.AreEqual(readGuard ? "3559342641A0F2B4E9A4A3C35CA7DFBBB5D6AA22A24B3F4F7CF8BED8AEFA0B60" : "66B120080F1F2DDF7E468CCB38545131F168CA92FD019F048706EFF2B6E8B546", Hash(await File.ReadAllTextAsync(pending)));
        Assert.AreEqual(readGuard ? "A3386A636906BA05DC3F1A914714534BBF65C8682B5368AFC613CA14C74890D5" : "CF0E90401A5B5356B839B5350CA82CE63C7BB8E010B7E72513D79144FFA12F1D",
            Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(work, "oracle-native-current.trx")))));
        var actor = Guid.Parse(readGuard ? "32377334-2b50-4304-9868-824f03cbaf37" : "3470e39d-e016-4985-b5b8-873e3b75158f");
        var expected = readGuard ? new[] {
            ("OASIS_AVATARS", actor, "6C82281C43A67202D3E231D6473D4281AC933A7A34E1F1AF903862F819437A21"),
            ("OASIS_AVATAR_DETAILS", actor, "676FA7F510BD95C17FCA57EAD78CA8AB41234B16CF032AF04EA143C7A60977B7"),
            ("OASIS_HOLONS", Guid.Parse("d2dffe86-dff9-4e5c-087c-4171332a9f1a"), "427EB6F649DFF156933828BFC05D7A856092E5F4E896EC114D80C89DBE4134FB"),
            ("OASIS_HOLONS", Guid.Parse("3c1298b6-3719-e3ba-634d-823f7624822d"), "F813037577D0E988D87C242F16433A0A70A076458E2CCCCB3D11139FA439DBF6"),
            ("OASIS_HOLONS", Guid.Parse("9d98c97b-bed4-2eb1-fa32-3e9853285919"), "5747ECB5CEBEDA7EF3C9919A42F014E8340E67A0472AA14E4ED39CCB0BD33D65"),
            ("OASIS_HOLONS", Guid.Parse("7d14236b-e3b2-9430-cc7f-6dfffd4454ea"), "D980F1C2D1ABCF08D79B90033BD63B4AFF61952A18EB5ABAF5BDCAD72F29174C"),
            ("OASIS_HOLONS", Guid.Parse("c2306b6c-2f14-39a2-9fb0-f949a4a5fe5f"), "181D2D76DDBD215F932F25FD21D0261767880CE91B559029BAA1CB35756ABB1F"),
            ("OASIS_HOLONS", Guid.Parse("b9e7ce54-09d1-c21c-c30e-7f9b85739e7e"), "85A12195C2E857F3DD1C766944D0B0FC4BD58EC46A863FCC1A58E5C3FE30735C"),
            ("OASIS_HOLONS", Guid.Parse("0a2b600e-10c6-7f4e-bd61-5bdde7587c62"), "620B34FCA6593C8495F5F0FD7A74C510FCDE6CA5D4453638357162CD2EEE8D4E")
        } : new[] {
            ("OASIS_AVATARS", actor, "BA4954464DA3FE8829A1BBE99126C6F22640AB85904FBF9651A1DCBE8BCDCB9F"),
            ("OASIS_AVATAR_DETAILS", actor, "4284E597D705C5840ECB961632954A1403D759F82C435B293734DE0C2CB15C95"),
            ("OASIS_HOLONS", Guid.Parse("e357b72f-f955-da72-476f-01c7733f09f9"), "2590F14AAD8391DC5BD2536609FE10514F7C32D1F31BDCDDD2FCA5546A158D5A"),
            ("OASIS_HOLONS", Guid.Parse("3c1298b6-3719-e3ba-634d-823f7624822d"), "E12BA3ACEB869627F091A4C7BDC8A265013C7A81E022FF9D3C98E689724EB2D7"),
            ("OASIS_HOLONS", Guid.Parse("ec7044a1-b3ca-79e6-4f1f-0fdb3f327888"), "A1F4E0948847CEC1907887E1C0FF08E57C41438209F57958924E18516D1981E9"),
            ("OASIS_HOLONS", Guid.Parse("7d14236b-e3b2-9430-cc7f-6dfffd4454ea"), "EA35E7965020BAD3D5B45877597DD59679FEF91D0A2DAC872347B49BE5657B11"),
            ("OASIS_HOLONS", Guid.Parse("c2306b6c-2f14-39a2-9fb0-f949a4a5fe5f"), "04F145430335B5FD23732955A970916CD10977A9552BF10AA021A0F52D08D01F"),
            ("OASIS_HOLONS", Guid.Parse("b9e7ce54-09d1-c21c-c30e-7f9b85739e7e"), "F80E1EBF76CEF275793AE80CD092E4F7DE611ADC54805946E79D1FFEFE5E0F85"),
            ("OASIS_HOLONS", Guid.Parse("0a2b600e-10c6-7f4e-bd61-5bdde7587c62"), "D0CCE9CCEC1DBD4B7A1B8B35C41B28D297D8DF9E58C5F1B44C83F6DEF7BE4F87")
        };
        await using var admin = new OracleConnection(Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING"));
        await admin.OpenAsync();
        await using var native = new OracleConnection(await RestrictedConnectionAsync(admin, work));
        await native.OpenAsync();
        var rows = new List<(string Table, Guid Id, string Json)>();
        foreach (var (table, id, hash) in expected)
        {
            var json = await Read(native, table, id);
            Assert.IsNotNull(json); Assert.AreEqual(hash, Hash(json));
            using var parsed = JsonDocument.Parse(json);
            Assert.AreEqual(id, parsed.RootElement.GetProperty("Id").GetGuid());
            rows.Add((table, id, json));
        }
        var counts = new Dictionary<string, int> { ["OASIS_AVATARS"] = 1, ["OASIS_AVATAR_DETAILS"] = 1, ["OASIS_HOLONS"] = 7,
            ["OASIS_WRITE_RECEIPTS"] = 0, ["OASIS_WRITE_PREPARATIONS"] = 0, ["OASIS_WRITE_OPERATION_LOCKS"] = 0 };
        foreach (var (table, count) in counts)
        {
            await using var query = new OracleCommand($"SELECT COUNT(*) FROM {table}", native);
            Assert.AreEqual(count, Convert.ToInt32(await query.ExecuteScalarAsync()));
        }
        // Remove only these exact unchanged synthetic payloads, in one native transaction.
        // A disconnect/uncertain commit leaves the original journal for independent inspection.
        await using (var transaction = (OracleTransaction)await native.BeginTransactionAsync())
        {
            foreach (var (table, id, json) in rows)
            {
                await using var command = new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0", native)
                    { BindByName = true, Transaction = transaction };
                command.Parameters.Add("Id", OracleDbType.Raw).Value = id.ToByteArray();
                command.Parameters.Add("Expected", OracleDbType.Clob).Value = json;
                Assert.AreEqual(1, await command.ExecuteNonQueryAsync());
            }
            await transaction.CommitAsync();
        }
        foreach (var (table, id, _) in rows) Assert.IsNull(await Read(native, table, id));
        File.Move(pending, retired);
        Console.WriteLine(JsonSerializer.Serialize(new { reconciledActor = actor, exactUnchangedRowsRemoved = rows.Count,
            originalMutationReplayed = false, originalNullCauseResolved = false }));
    }

    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
