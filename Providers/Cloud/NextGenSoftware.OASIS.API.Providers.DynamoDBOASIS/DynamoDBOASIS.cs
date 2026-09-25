using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS
{
    /// <summary>
    /// OASIS provider for AWS DynamoDB — fully managed serverless key-value and document store.
    ///
    /// Tables (auto-created on ActivateProvider if they don't exist):
    ///   OasisAvatars        — PK: id (S), GSI on username, GSI on email
    ///   OasisAvatarDetails  — PK: id (S), GSI on username, GSI on email
    ///   OasisHolons         — PK: id (S), GSI on parentHolonId
    ///
    /// Each item stores all lookup attributes plus a data_json attribute containing the full object.
    ///
    /// Constructor parameters:
    ///   accessKey   — AWS access key ID
    ///   secretKey   — AWS secret access key
    ///   region      — AWS region code, e.g. "eu-west-1"
    ///   serviceUrl  — optional endpoint URL for DynamoDB Local / LocalStack
    /// </summary>
    public class DynamoDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly AmazonDynamoDBClient _client;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public DynamoDBOASIS(string accessKey, string secretKey, string region, string? serviceUrl = null)
        {
            var creds = new BasicAWSCredentials(accessKey, secretKey);
            var config = new AmazonDynamoDBConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(region) };
            if (!string.IsNullOrEmpty(serviceUrl)) config.ServiceURL = serviceUrl;
            _client = new AmazonDynamoDBClient(creds, config);
            ProviderName = "DynamoDBOASIS";
            ProviderDescription = "AWS DynamoDB provider (AWSSDK.DynamoDBv2 — serverless NoSQL for OASIS holons and avatars)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.DynamoDBOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private static string Ser(object o) => JsonSerializer.Serialize(o, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        // ─── Table creation helpers ───────────────────────────────────────────────

        private async Task EnsureTableAsync(string tableName, List<KeySchemaElement> keys, List<AttributeDefinition> attrs, List<GlobalSecondaryIndex>? gsis = null)
        {
            try { await _client.DescribeTableAsync(tableName); return; } catch (ResourceNotFoundException) { }
            var req = new CreateTableRequest
            {
                TableName = tableName,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = keys,
                AttributeDefinitions = attrs
            };
            if (gsis != null) req.GlobalSecondaryIndexes = gsis;
            await _client.CreateTableAsync(req);
            // Wait until table is ACTIVE
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(1000);
                var desc = await _client.DescribeTableAsync(tableName);
                if (desc.Table.TableStatus == TableStatus.ACTIVE) break;
            }
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var pkSchema = new List<KeySchemaElement> { new KeySchemaElement { AttributeName = "id", KeyType = KeyType.HASH } };
                var pkAttr = new List<AttributeDefinition> { new AttributeDefinition { AttributeName = "id", AttributeType = ScalarAttributeType.S } };

                Func<string, string, string, GlobalSecondaryIndex> MakeGsi = (gsiName, hashKey, projType) =>
                    new GlobalSecondaryIndex
                    {
                        IndexName = gsiName,
                        KeySchema = new List<KeySchemaElement> { new KeySchemaElement { AttributeName = hashKey, KeyType = KeyType.HASH } },
                        Projection = new Projection { ProjectionType = ProjectionType.ALL }
                    };

                // OasisAvatars — GSI on username, GSI on email
                await EnsureTableAsync("OasisAvatars", pkSchema, new List<AttributeDefinition> {
                    new AttributeDefinition { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "username", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "email", AttributeType = ScalarAttributeType.S }
                }, new List<GlobalSecondaryIndex> {
                    MakeGsi("username-index", "username", "ALL"),
                    MakeGsi("email-index", "email", "ALL")
                });

                // OasisAvatarDetails — same GSIs
                await EnsureTableAsync("OasisAvatarDetails", pkSchema, new List<AttributeDefinition> {
                    new AttributeDefinition { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "username", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "email", AttributeType = ScalarAttributeType.S }
                }, new List<GlobalSecondaryIndex> {
                    MakeGsi("username-index", "username", "ALL"),
                    MakeGsi("email-index", "email", "ALL")
                });

                // OasisHolons — GSI on parentHolonId
                await EnsureTableAsync("OasisHolons", pkSchema, new List<AttributeDefinition> {
                    new AttributeDefinition { AttributeName = "id", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "parentHolonId", AttributeType = ScalarAttributeType.S }
                }, new List<GlobalSecondaryIndex> {
                    MakeGsi("parentHolonId-index", "parentHolonId", "ALL")
                });

                result.Result = true; result.IsError = false; result.Message = "DynamoDBOASIS activated — tables ready.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _client.Dispose(); return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "DynamoDBOASIS deactivated." }); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.DynamoDBOASIS] = avatar.Id.ToString();
                await _client.PutItemAsync(new PutItemRequest
                {
                    TableName = "OasisAvatars",
                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["id"] = new AttributeValue { S = avatar.Id.ToString() },
                        ["username"] = new AttributeValue { S = avatar.Username ?? "" },
                        ["email"] = new AttributeValue { S = avatar.Email ?? "" },
                        ["is_deleted"] = new AttributeValue { BOOL = avatar.IsDeleted },
                        ["data_json"] = new AttributeValue { S = Ser(avatar) }
                    }
                });
                result.Result = avatar; result.IsError = false; result.Message = $"DynamoDBOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var resp = await _client.GetItemAsync("OasisAvatars", new Dictionary<string, AttributeValue> { ["id"] = new AttributeValue { S = id.ToString() } });
                if (resp.Item == null || !resp.Item.Any() || (resp.Item.TryGetValue("is_deleted", out var del) && del.BOOL)) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No avatar for ID '{id}'."); return result; }
                var avatar = Des<Avatar>(resp.Item["data_json"].S); if (avatar == null) { OASISErrorHandling.HandleError(ref result, "DynamoDBOASIS: Deserialise failed."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = "DynamoDBOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        private async Task<Avatar?> QueryAvatarByGsiAsync(string indexName, string keyName, string keyValue)
        {
            var resp = await _client.QueryAsync(new QueryRequest
            {
                TableName = "OasisAvatars",
                IndexName = indexName,
                KeyConditionExpression = "#k = :v",
                ExpressionAttributeNames = new Dictionary<string, string> { ["#k"] = keyName },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":v"] = new AttributeValue { S = keyValue } },
                Limit = 1
            });
            var item = resp.Items.FirstOrDefault(i => !(i.TryGetValue("is_deleted", out var d) && d.BOOL));
            return item == null ? null : Des<Avatar>(item["data_json"].S);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await QueryAvatarByGsiAsync("username-index", "username", username); if (a == null) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No avatar for username '{username}'."); return result; } result.Result = a; result.IsError = false; result.Message = "DynamoDBOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await QueryAvatarByGsiAsync("email-index", "email", email); if (a == null) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No avatar for email '{email}'."); return result; } result.Result = a; result.IsError = false; result.Message = "DynamoDBOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => LoadAvatarByEmailAsync(email, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string pk, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadAvatarAsync(id, version); var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"DynamoDBOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string pk, int version = 0) => LoadAvatarByProviderKeyAsync(pk, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var items = new List<Dictionary<string, AttributeValue>>();
                ScanResponse? scanResp = null;
                do
                {
                    var req = new ScanRequest { TableName = "OasisAvatars" };
                    if (scanResp?.LastEvaluatedKey?.Any() == true) req.ExclusiveStartKey = scanResp.LastEvaluatedKey;
                    scanResp = await _client.ScanAsync(req);
                    items.AddRange(scanResp.Items);
                } while (scanResp.LastEvaluatedKey?.Any() == true);

                var avatars = items.Where(i => !(i.TryGetValue("is_deleted", out var d) && d.BOOL)).Select(i => Des<Avatar>(i["data_json"].S)).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.Message = $"DynamoDBOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                {
                    var loaded = await LoadAvatarAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: Avatar '{id}' not found."); return result; }
                    var av = (Avatar)loaded.Result; av.DeletedDate = DateTime.UtcNow;
                    await _client.UpdateItemAsync(new UpdateItemRequest
                    {
                        TableName = "OasisAvatars",
                        Key = new Dictionary<string, AttributeValue> { ["id"] = new AttributeValue { S = id.ToString() } },
                        UpdateExpression = "SET is_deleted = :d, data_json = :j",
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":d"] = new AttributeValue { BOOL = true }, [":j"] = new AttributeValue { S = Ser(av) } }
                    });
                }
                else
                {
                    await _client.DeleteItemAsync("OasisAvatars", new Dictionary<string, AttributeValue> { ["id"] = new AttributeValue { S = id.ToString() } });
                }
                result.Result = true; result.IsError = false; result.Message = $"DynamoDBOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByEmail(string e, bool softDelete = true) => DeleteAvatarByEmailAsync(e, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteAvatarAsync(id, softDelete); return await DeleteAvatarByUsernameAsync(pk, softDelete); }
        public override OASISResult<bool> DeleteAvatar(string pk, bool softDelete = true) => DeleteAvatarAsync(pk, softDelete).Result;

        // ─── AvatarDetail ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail d)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (d.Id == Guid.Empty) d.Id = Guid.NewGuid();
                await _client.PutItemAsync(new PutItemRequest
                {
                    TableName = "OasisAvatarDetails",
                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["id"] = new AttributeValue { S = d.Id.ToString() },
                        ["username"] = new AttributeValue { S = d.Username ?? "" },
                        ["email"] = new AttributeValue { S = d.Email ?? "" },
                        ["data_json"] = new AttributeValue { S = Ser(d) }
                    }
                });
                result.Result = d; result.IsError = false; result.Message = "DynamoDBOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var resp = await _client.GetItemAsync("OasisAvatarDetails", new Dictionary<string, AttributeValue> { ["id"] = new AttributeValue { S = id.ToString() } });
                if (resp.Item == null || !resp.Item.Any()) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No detail for ID '{id}'."); return result; }
                var d = Des<AvatarDetail>(resp.Item["data_json"].S); if (d == null) { OASISErrorHandling.HandleError(ref result, "DynamoDBOASIS: Deserialise failed."); return result; }
                result.Result = d; result.IsError = false; result.Message = "DynamoDBOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        private async Task<AvatarDetail?> QueryDetailByGsiAsync(string indexName, string keyName, string keyValue)
        {
            var resp = await _client.QueryAsync(new QueryRequest { TableName = "OasisAvatarDetails", IndexName = indexName, KeyConditionExpression = "#k = :v", ExpressionAttributeNames = new Dictionary<string, string> { ["#k"] = keyName }, ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":v"] = new AttributeValue { S = keyValue } }, Limit = 1 });
            var item = resp.Items.FirstOrDefault();
            return item == null ? null : Des<AvatarDetail>(item["data_json"].S);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await QueryDetailByGsiAsync("username-index", "username", u); if (d == null) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No detail for username '{u}'."); return result; } result.Result = d; result.IsError = false; result.Message = "DynamoDBOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await QueryDetailByGsiAsync("email-index", "email", e); if (d == null) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsError = false; result.Message = "DynamoDBOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { ScanResponse? sr = null; var items = new List<Dictionary<string, AttributeValue>>(); do { var req = new ScanRequest { TableName = "OasisAvatarDetails" }; if (sr?.LastEvaluatedKey?.Any() == true) req.ExclusiveStartKey = sr.LastEvaluatedKey; sr = await _client.ScanAsync(req); items.AddRange(sr.Items); } while (sr.LastEvaluatedKey?.Any() == true); var details = items.Select(i => Des<AvatarDetail>(i["data_json"].S)).Where(d => d != null).Cast<IAvatarDetail>().ToList(); result.Result = details; result.IsError = false; result.Message = $"DynamoDBOASIS: Loaded {details.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holon saving ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                if (holon.ProviderUniqueStorageKey == null) holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.DynamoDBOASIS] = holon.Id.ToString();
                await _client.PutItemAsync(new PutItemRequest
                {
                    TableName = "OasisHolons",
                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["id"] = new AttributeValue { S = holon.Id.ToString() },
                        ["parentHolonId"] = new AttributeValue { S = holon.ParentHolonId.ToString() },
                        ["holon_type"] = new AttributeValue { N = ((int)holon.HolonType).ToString() },
                        ["is_deleted"] = new AttributeValue { BOOL = holon.IsDeleted },
                        ["data_json"] = new AttributeValue { S = Ser(holon) }
                    }
                });
                result.Result = holon; result.IsError = false; result.Message = $"DynamoDBOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = errors.Count > 0; result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"DynamoDBOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var resp = await _client.GetItemAsync("OasisHolons", new Dictionary<string, AttributeValue> { ["id"] = new AttributeValue { S = id.ToString() } });
                if (resp.Item == null || !resp.Item.Any() || (resp.Item.TryGetValue("is_deleted", out var del) && del.BOOL)) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No holon for ID '{id}'."); return result; }
                var holon = Des<Holon>(resp.Item["data_json"].S); if (holon == null) { OASISErrorHandling.HandleError(ref result, "DynamoDBOASIS: Deserialise failed."); return result; }
                result.Result = holon; result.IsError = false; result.Message = "DynamoDBOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"DynamoDBOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> LoadHolon(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(pk, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        private async Task<List<IHolon>> ScanHolonsAsync(string? filterExpr = null, Dictionary<string, AttributeValue>? exprVals = null)
        {
            var items = new List<Dictionary<string, AttributeValue>>();
            ScanResponse? sr = null;
            do
            {
                var req = new ScanRequest { TableName = "OasisHolons" };
                if (filterExpr != null) { req.FilterExpression = filterExpr; req.ExpressionAttributeValues = exprVals; }
                if (sr?.LastEvaluatedKey?.Any() == true) req.ExclusiveStartKey = sr.LastEvaluatedKey;
                sr = await _client.ScanAsync(req);
                items.AddRange(sr.Items);
            } while (sr.LastEvaluatedKey?.Any() == true);
            return items.Where(i => !(i.TryGetValue("is_deleted", out var d) && d.BOOL)).Select(i => Des<Holon>(i["data_json"].S)).Where(h => h != null).Cast<IHolon>().ToList();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var holons = type == HolonType.All
                    ? await ScanHolonsAsync()
                    : await ScanHolonsAsync("holon_type = :t", new Dictionary<string, AttributeValue> { [":t"] = new AttributeValue { N = ((int)type).ToString() } });
                result.Result = holons; result.IsError = false; result.Message = $"DynamoDBOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var resp = await _client.QueryAsync(new QueryRequest
                {
                    TableName = "OasisHolons",
                    IndexName = "parentHolonId-index",
                    KeyConditionExpression = "parentHolonId = :pid",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":pid"] = new AttributeValue { S = id.ToString() } }
                });
                var holons = resp.Items.Where(i => !(i.TryGetValue("is_deleted", out var d) && d.BOOL) && (type == HolonType.All || (i.TryGetValue("holon_type", out var ht) && ht.N == ((int)type).ToString()))).Select(i => Des<Holon>(i["data_json"].S)).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"DynamoDBOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"DynamoDBOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(pk, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: Holon '{id}' not found."); return result; }
                var holon = (Holon)loaded.Result; holon.DeletedDate = DateTime.UtcNow;
                await _client.UpdateItemAsync(new UpdateItemRequest
                {
                    TableName = "OasisHolons",
                    Key = new Dictionary<string, AttributeValue> { ["id"] = new AttributeValue { S = id.ToString() } },
                    UpdateExpression = "SET is_deleted = :d, data_json = :j",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":d"] = new AttributeValue { BOOL = true }, [":j"] = new AttributeValue { S = Ser(holon) } }
                });
                result.Result = holon; result.IsError = false; result.Message = $"DynamoDBOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string pk) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"DynamoDBOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> DeleteHolon(string pk) => DeleteHolonAsync(pk).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try { string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower(); var all = await LoadAllHolonsAsync(); var holons = all.Result?.ToList() ?? new List<IHolon>(); if (!string.IsNullOrEmpty(q)) holons = holons.Where(h => h.Name?.ToLower().Contains(q) == true || h.Description?.ToLower().Contains(q) == true).ToList(); result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count }; result.IsError = false; result.Message = $"DynamoDBOASIS: Found {holons.Count} result(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"DynamoDBOASIS: {holons.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKvp.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return mode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); } var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"DynamoDBOASIS: {holons.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKvp, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons); return new OASISResult<bool> { Result = !s.IsError, IsError = s.IsError, Message = s.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) { var all = await LoadAllHolonsAsync(); var h = all.Result?.Where(x => x.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = h, IsError = false, Message = $"DynamoDBOASIS: {h.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
