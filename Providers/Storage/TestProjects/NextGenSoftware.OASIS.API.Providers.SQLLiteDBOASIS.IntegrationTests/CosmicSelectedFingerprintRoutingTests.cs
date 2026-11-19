using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public async Task SelectedFingerprintPreservesNativeReplayAndReceiptGuards(int boundary)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var activation = await provider.ActivateProviderAsync();
        Assert.False(activation.IsError, activation.Message + activation.Exception);
        var actor = Guid.NewGuid(); var operation = Guid.NewGuid();
        var root = new Holon { Id = Guid.NewGuid(), Name = "native owner", IsActive = true,
            MetaData = new() { ["Score"] = 9007199254740993L, ["Other"] = "before" } };
        ConditionalHolonWriteRequest Request(IHolon value, Guid? revision, Guid op, Guid by)
            => new(new ConditionalHolonWrite(op, by, ProviderType.SQLLiteDBOASIS,
                new[] { new HolonWriteCondition(value.Id, revision) }), ConditionalHolonOperation.Save,
                new[] { value }, false, false, 0);
        ConditionalHolonPropertyWriteRequest Selected(IHolon value, Guid revision, Guid by)
            => new(Request(value, revision, operation, by), new Dictionary<Guid, IReadOnlyList<string>>
                { [root.Id] = new[] { "/MetaData/Score" } });
        (string Hash, string Json, long Count) Receipt()
        {
            using var context = new DataContext(TestConnectionString);
            var connection = context.Database.GetDbConnection(); connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT RequestHash, ResultJson, (SELECT COUNT(*) FROM CosmicWriteReceipt) FROM CosmicWriteReceipt WHERE OperationId=$id";
            var parameter = command.CreateParameter(); parameter.ParameterName = "$id";
            parameter.Value = operation.ToString("D"); command.Parameters.Add(parameter);
            using var reader = command.ExecuteReader(); Assert.True(reader.Read());
            return (reader.GetString(0), reader.GetString(1), reader.GetInt64(2));
        }

        var created = provider.ExecuteConditionalHolonWrite(Request(root, null, Guid.NewGuid(), actor));
        Assert.False(created.IsError, created.Message + created.Exception);
        var initial = Revision(created.Result.Single());
        var selected = StoredRecordJson.Project<Holon>(created.Result.Single());
        selected.MetaData["Score"] = 9007199254740995L;
        var request = Selected(selected, initial, actor);
        var saved = await provider.ExecuteConditionalHolonPropertyWriteAsync(request);
        Assert.False(saved.IsError, saved.Message + saved.Exception);
        var selectedRevision = Revision(saved.Result.Single());
        var originalReceipt = Receipt();
        Assert.Equal(6, JObject.Parse(originalReceipt.Json).Value<int>("fingerprintFormat"));
        Assert.Equal(64, originalReceipt.Hash.Length);

        var newer = StoredRecordJson.Project<Holon>(saved.Result.Single());
        newer.MetaData["Other"] = "newer authoritative sibling";
        var updated = provider.ExecuteConditionalHolonWrite(Request(newer, selectedRevision, Guid.NewGuid(), actor));
        Assert.False(updated.IsError, updated.Message + updated.Exception);
        var latestRevision = Revision(updated.Result.Single());
        var replay = provider.ExecuteConditionalHolonPropertyWrite(request);
        Assert.False(replay.IsError, replay.Message + replay.Exception);
        Assert.Equal(selectedRevision, Revision(replay.Result.Single()));
        Assert.Equal(originalReceipt.Json, Receipt().Json);

        if (boundary == 0) selected.MetaData["Score"] = 9007199254740997L;
        if (boundary >= 2)
        {
            using var control = new DataContext(TestConnectionString);
            if (boundary == 2)
                control.Database.ExecuteSqlRaw("UPDATE CosmicWriteReceipt SET ResultJson=json_set(ResultJson,'$.fingerprintFormat',5) WHERE OperationId={0}", operation.ToString("D"));
            else
                control.Database.ExecuteSqlRaw("UPDATE CosmicWriteReceipt SET ResultJson=json_set(ResultJson,'$.digest',{0}) WHERE OperationId={1}", new string('0', 64), operation.ToString("D"));
        }
        var beforeRefusal = Receipt();
        var rejected = provider.ExecuteConditionalHolonPropertyWrite(boundary == 1
            ? Selected(selected, initial, Guid.NewGuid()) : request);
        Assert.True(rejected.IsError);
        Assert.Equal(boundary == 2 ? "COSMIC_RECEIPT_FINGERPRINT_RECONCILIATION_REQUIRED"
            : boundary == 3 ? "COSMIC_RECEIPT_INVALID" : "COSMIC_OPERATION_ID_REUSED", rejected.ErrorCode);
        Assert.Equal(beforeRefusal, Receipt());
        Assert.Equal(3, beforeRefusal.Count);
        var loaded = await provider.LoadHolonAsync(root.Id, loadChildren: false);
        Assert.False(loaded.IsError, loaded.Message + loaded.Exception);
        Assert.Equal(latestRevision, Revision(loaded.Result));
        var sibling = StoredRecordJson.CaptureRuntimeToken(loaded.Result.MetaData["Other"]);
        Assert.Equal(JTokenType.String, sibling.Type);
        Assert.Equal("newer authoritative sibling", sibling.Value<string>());
        var score = StoredRecordJson.CaptureRuntimeToken(loaded.Result.MetaData["Score"]);
        Assert.True(StoredRecordValueEquality.IsNumber(score));
        Assert.Equal("9007199254740995", StoredRecordValueEquality.CanonicalNumberText(score));
        Assert.False(StoredRecordValueEquality.Equivalent(score, new JValue("9007199254740995")));
        Assert.False(StoredRecordValueEquality.Equivalent(score, new JRaw("9007199254740996")));
    }
}
