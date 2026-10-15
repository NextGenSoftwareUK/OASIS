using System;
using System.Collections.Generic;
using System.Linq;
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
        private AmazonDynamoDBClient _client;
        private readonly Func<AmazonDynamoDBClient> _createClient;
        private bool _clientDisposed;
        protected virtual ProviderType StorageProviderType => Core.Enums.ProviderType.DynamoDBOASIS;

        public DynamoDBOASIS(string accessKey, string secretKey, string region, string? serviceUrl = null)
        {
            var creds = new BasicAWSCredentials(accessKey, secretKey);
            var config = new AmazonDynamoDBConfig();
            if (string.IsNullOrWhiteSpace(serviceUrl))
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
            else
            {
                config.ServiceURL = serviceUrl;
                config.AuthenticationRegion = region;
                config.UseHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
            }
            _createClient = () => new AmazonDynamoDBClient(creds, config);
            _client = _createClient();
            ProviderName = "DynamoDBOASIS";
            ProviderDescription = "AWS DynamoDB provider (AWSSDK.DynamoDBv2 — serverless NoSQL for OASIS holons and avatars)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.DynamoDBOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private static string Ser(object o) => NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(o);
        private static T? Des<T>(string? json) => json == null ? default : NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<T>(json);

        private static async Task<T> ObserveMutationAsync<T>(Func<Task<T>> mutation,string table,string key)
        {
            try {return await mutation();}
            // A failed condition is explicit server evidence that this item mutation did not execute.
            catch(ConditionalCheckFailedException) {throw;}
            catch(Exception cause) {throw new DynamoDBMutationReconciliationException(table,key,cause);}
        }
        private Task<PutItemResponse> PutItemAsync(PutItemRequest request)
            => ObserveMutationAsync(()=>_client.PutItemAsync(request),request.TableName,request.Item["id"].S);
        private Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest request)
            => ObserveMutationAsync(()=>_client.UpdateItemAsync(request),request.TableName,request.Key["id"].S);
        private Task<DeleteItemResponse> DeleteItemAsync(string table,Dictionary<string,AttributeValue> key)
            => ObserveMutationAsync(()=>_client.DeleteItemAsync(table,key),table,key["id"].S);

        private static Dictionary<string, AttributeValue> WithOptionalIdentityIndexes(Dictionary<string, AttributeValue> item)
        {
            // DynamoDB GSI key attributes cannot contain empty strings. Missing selectors belong to the sparse index's absent set.
            foreach(var key in new[] {"username", "email"})
                if(item.TryGetValue(key,out var value) && string.IsNullOrEmpty(value.S)) item.Remove(key);
            return item;
        }

        // ─── Table creation helpers ───────────────────────────────────────────────

        private async Task EnsureTableAsync(string tableName, List<KeySchemaElement> keys, List<AttributeDefinition> attrs, List<GlobalSecondaryIndex>? gsis = null)
        {
            bool exists;
            try { await _client.DescribeTableAsync(tableName); exists=true; } catch (ResourceNotFoundException) { exists=false; }
            if (!exists)
            {
            var req = new CreateTableRequest
            {
                TableName = tableName,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema = keys,
                AttributeDefinitions = attrs
            };
            if (gsis != null) req.GlobalSecondaryIndexes = gsis;
            await _client.CreateTableAsync(req);
            }
            await WaitForTableActiveAsync(() => _client.DescribeTableAsync(tableName), () => Task.Delay(1000));
        }

        private static async Task WaitForTableActiveAsync(Func<Task<DescribeTableResponse>> describe, Func<Task> delay)
        {
            for (int i = 0; i < 30; i++)
            {
                var desc = await describe();
                if (desc?.Table == null) throw new InvalidOperationException("DynamoDB table description is missing.");
                if (desc.Table.TableStatus == TableStatus.ACTIVE) return;
                if (desc.Table.TableStatus != TableStatus.CREATING && desc.Table.TableStatus != TableStatus.UPDATING)
                    throw new InvalidOperationException($"DynamoDB table is not available: {desc.Table.TableStatus}.");
                if(i<29) await delay();
            }
            throw new TimeoutException("DynamoDB table did not become ACTIVE within the readiness observation window.");
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_clientDisposed) { _client = _createClient(); _clientDisposed = false; }
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

                IsProviderActivated = true;
                result.Result = true; result.IsError = false; result.Message = $"{ProviderName} activated — DynamoDB tables ready.";
            }
            catch (Exception ex) { IsProviderActivated = false; result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"{ProviderName}: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { if(!_clientDisposed) { _client.Dispose(); _clientDisposed = true; } IsProviderActivated = false; return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = $"{ProviderName} deactivated." }); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[StorageProviderType] = avatar.Id.ToString();
                await PutItemAsync(new PutItemRequest
                {
                    TableName = "OasisAvatars",
                    Item = WithOptionalIdentityIndexes(new Dictionary<string, AttributeValue>
                    {
                        ["id"] = new AttributeValue { S = avatar.Id.ToString() },
                        ["username"] = new AttributeValue { S = avatar.Username ?? "" },
                        ["email"] = new AttributeValue { S = avatar.Email ?? "" },
                        ["is_deleted"] = new AttributeValue { BOOL = avatar.IsDeleted },
                        ["data_json"] = new AttributeValue { S = Ser(avatar) }
                    })
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
                var resp = await _client.GetItemAsync(new GetItemRequest {TableName="OasisAvatars",ConsistentRead=true,Key=new Dictionary<string, AttributeValue> { ["id"] = new AttributeValue { S = id.ToString() } }});
                if (resp.Item == null || !resp.Item.Any() || (resp.Item.TryGetValue("is_deleted", out var del) && del.BOOL)) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No avatar for ID '{id}'."); result.ErrorCode=OASISResultErrorCodes.RecordNotFound; return result; }
                var avatar = Des<Avatar>(resp.Item["data_json"].S); if (avatar == null) { OASISErrorHandling.HandleError(ref result, "DynamoDBOASIS: Deserialise failed."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = "DynamoDBOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        private async Task<Avatar?> QueryAvatarByGsiAsync(string indexName, string keyName, string keyValue)
            => await QueryUniqueAvatarAsync(request => _client.QueryAsync(request), indexName, keyName, keyValue);

        private static async Task<Avatar?> QueryUniqueAvatarAsync(Func<QueryRequest, Task<QueryResponse>> query, string indexName, string keyName, string keyValue)
            => await QueryUniqueIdentityAsync<Avatar>(query,"OasisAvatars",indexName,keyName,keyValue,avatar=>avatar.Id);

        private static async Task<T?> QueryUniqueIdentityAsync<T>(Func<QueryRequest, Task<QueryResponse>> query, string tableName, string indexName, string keyName, string keyValue, Func<T,Guid> identity) where T:class
        {
            if(string.IsNullOrWhiteSpace(keyValue)) throw new ArgumentException("Avatar selector must not be blank.",nameof(keyValue));
            var request = new QueryRequest
            {
                TableName = tableName,
                IndexName = indexName,
                KeyConditionExpression = "#k = :v",
                ExpressionAttributeNames = new Dictionary<string, string> { ["#k"] = keyName },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":v"] = new AttributeValue { S = keyValue } },
            };
            T? match = null;
            do
            {
                var response = await query(request);
                foreach(var item in response.Items)
                {
                    if(item.TryGetValue("is_deleted",out var deleted) && deleted.BOOL) continue;
                    var avatar=Des<T>(item["data_json"].S)
                        ?? throw new InvalidOperationException("Stored DynamoDB avatar is null.");
                    if(identity(avatar)==Guid.Empty || !item.TryGetValue("id",out var id) || id.S!=identity(avatar).ToString())
                        throw new InvalidOperationException("Stored DynamoDB avatar identity does not match its key.");
                    if(match!=null) throw new StorageIdentityAmbiguousException();
                    match=avatar;
                }
                request.ExclusiveStartKey=response.LastEvaluatedKey;
            } while(request.ExclusiveStartKey?.Count>0);
            return match;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await QueryAvatarByGsiAsync("username-index", "username", username); if (a == null) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No avatar for username '{username}'."); return result; } result.Result = a; result.IsError = false; result.Message = "DynamoDBOASIS: Avatar loaded."; }
            catch (StorageIdentityAmbiguousException ex) { result.Exception=ex; OASISErrorHandling.HandleError(ref result,ex.Message); result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await QueryAvatarByGsiAsync("email-index", "email", email); if (a == null) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No avatar for email '{email}'."); return result; } result.Result = a; result.IsError = false; result.Message = "DynamoDBOASIS: Avatar loaded."; }
            catch (StorageIdentityAmbiguousException ex) { result.Exception=ex; OASISErrorHandling.HandleError(ref result,ex.Message); result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity; }
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
                    var loaded = await LoadAvatarAsync(id);
                    if (loaded.IsError || loaded.Result == null)
                    {
                        result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,bool>(loaded);
                        result.Result=false; result.IsSaved=false;
                        if(!result.IsError) OASISErrorHandling.HandleError(ref result,$"DynamoDBOASIS: Avatar '{id}' not found.");
                        return result;
                    }
                    var av = (Avatar)loaded.Result; av.DeletedDate = DateTime.UtcNow;
                    await UpdateItemAsync(new UpdateItemRequest
                    {
                        TableName = "OasisAvatars",
                        Key = new Dictionary<string, AttributeValue> { ["id"] = new AttributeValue { S = id.ToString() } },
                        UpdateExpression = "SET is_deleted = :d, data_json = :j",
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":d"] = new AttributeValue { BOOL = true }, [":j"] = new AttributeValue { S = Ser(av) } }
                    });
                }
                else
                {
                    await DeleteItemAsync("OasisAvatars", new Dictionary<string, AttributeValue> { ["id"] = new AttributeValue { S = id.ToString() } });
                }
                result.Result = true; result.IsError = false; result.Message = $"DynamoDBOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        private async Task<OASISResult<bool>> DeleteSelectedAvatarAsync(Task<OASISResult<IAvatar>> lookup, bool softDelete)
        {
            var selected=await lookup;
            if(selected.IsError || selected.Result==null)
            {
                var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,bool>(selected);
                result.Result=false;
                result.IsSaved=false;
                if(!result.IsError) OASISErrorHandling.HandleError(ref result,"DynamoDBOASIS: Avatar not found.");
                return result;
            }
            return await DeleteAvatarAsync(selected.Result.Id,softDelete);
        }
        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) => DeleteSelectedAvatarAsync(LoadAvatarByUsernameAsync(u),softDelete);
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).GetAwaiter().GetResult();
        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) => DeleteSelectedAvatarAsync(LoadAvatarByEmailAsync(e),softDelete);
        public override OASISResult<bool> DeleteAvatarByEmail(string e, bool softDelete = true) => DeleteAvatarByEmailAsync(e, softDelete).GetAwaiter().GetResult();
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteAvatarAsync(id, softDelete); return await DeleteAvatarByUsernameAsync(pk, softDelete); }
        public override OASISResult<bool> DeleteAvatar(string pk, bool softDelete = true) => DeleteAvatarAsync(pk, softDelete).Result;

        // ─── AvatarDetail ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail d)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (d.Id == Guid.Empty) d.Id = Guid.NewGuid();
                await PutItemAsync(new PutItemRequest
                {
                    TableName = "OasisAvatarDetails",
                    Item = WithOptionalIdentityIndexes(new Dictionary<string, AttributeValue>
                    {
                        ["id"] = new AttributeValue { S = d.Id.ToString() },
                        ["username"] = new AttributeValue { S = d.Username ?? "" },
                        ["email"] = new AttributeValue { S = d.Email ?? "" },
                        ["data_json"] = new AttributeValue { S = Ser(d) }
                    })
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
                var resp = await _client.GetItemAsync(new GetItemRequest {TableName="OasisAvatarDetails",ConsistentRead=true,Key=new Dictionary<string, AttributeValue> { ["id"] = new AttributeValue { S = id.ToString() } }});
                if (resp.Item == null || !resp.Item.Any()) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No detail for ID '{id}'."); result.ErrorCode=OASISResultErrorCodes.RecordNotFound; return result; }
                var d = Des<AvatarDetail>(resp.Item["data_json"].S); if (d == null) { OASISErrorHandling.HandleError(ref result, "DynamoDBOASIS: Deserialise failed."); return result; }
                result.Result = d; result.IsError = false; result.Message = "DynamoDBOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        private async Task<AvatarDetail?> QueryDetailByGsiAsync(string indexName, string keyName, string keyValue)
            => await QueryUniqueIdentityAsync<AvatarDetail>(request=>_client.QueryAsync(request),"OasisAvatarDetails",indexName,keyName,keyValue,detail=>detail.Id);

        private async Task<OASISResult<IAvatarDetail>> LoadSelectedDetailAsync(string index,string key,string value)
        {
            var result=new OASISResult<IAvatarDetail>();
            try {
                var detail=await QueryDetailByGsiAsync(index,key,value);
                if(detail==null) {OASISErrorHandling.HandleError(ref result,"DynamoDBOASIS: No matching detail.");return result;}
                result.Result=detail;result.IsLoaded=true;
            }
            catch(Exception cause) {
                result.Exception=cause;OASISErrorHandling.HandleError(ref result,cause.Message);
                if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            }
            return result;
        }
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) => LoadSelectedDetailAsync("username-index","username",u);
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) => LoadSelectedDetailAsync("email-index","email",e);
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
                holon.ProviderUniqueStorageKey[StorageProviderType] = holon.Id.ToString();
                await PutItemAsync(new PutItemRequest
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
                ValidateGraphRead(maxChildDepth,0,version);
                if(id==Guid.Empty) throw new ArgumentException("A nonempty holon identity is required.",nameof(id));
                var resp = await _client.GetItemAsync(new GetItemRequest {TableName="OasisHolons",ConsistentRead=true,Key=new Dictionary<string, AttributeValue> { ["id"] = new AttributeValue { S = id.ToString() } }});
                if (resp.Item == null || !resp.Item.Any() || (resp.Item.TryGetValue("is_deleted", out var del) && del.BOOL)) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No holon for ID '{id}'."); result.ErrorCode=OASISResultErrorCodes.RecordNotFound; return result; }
                var holon = Des<Holon>(resp.Item["data_json"].S); if (holon == null) { OASISErrorHandling.HandleError(ref result, "DynamoDBOASIS: Deserialise failed."); return result; }
                if(holon.Id!=id) throw new InvalidOperationException("Stored DynamoDB holon identity does not match its key.");
                var records=loadChildren && loadChildrenFromProvider ? await ScanHolonsAsync():new List<IHolon> {holon};
                result.Result = CreateGraphProjection(records,loadChildren,recursive,maxChildDepth,0,loadChildrenFromProvider)(holon);
                result.IsLoaded=true;
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

        private static async Task<List<Dictionary<string,AttributeValue>>> ReadQueryItemsAsync(Func<QueryRequest,Task<QueryResponse>> query,QueryRequest request)
        {
            var items=new List<Dictionary<string,AttributeValue>>();
            do {
                var response=await query(request);
                if(response?.Items==null) throw new InvalidOperationException("DynamoDB query returned no item collection.");
                items.AddRange(response.Items);
                request.ExclusiveStartKey=response.LastEvaluatedKey;
            } while(request.ExclusiveStartKey?.Count>0);
            return items;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                ValidateGraphRead(maxChildDepth,curentChildDepth,version);
                var records=await ScanHolonsAsync();
                var project=CreateGraphProjection(records,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider);
                result.Result = records.Where(row=>type==HolonType.All || row.HolonType==type).Select(project).ToList();
                result.IsLoaded=true;
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
                ValidateGraphRead(maxChildDepth,curentChildDepth,version);
                var items = await ReadQueryItemsAsync(request=>_client.QueryAsync(request),new QueryRequest
                {
                    TableName = "OasisHolons",
                    IndexName = "parentHolonId-index",
                    KeyConditionExpression = "parentHolonId = :pid",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":pid"] = new AttributeValue { S = id.ToString() } }
                });
                var holons = items.Where(i => !(i.TryGetValue("is_deleted", out var d) && d.BOOL) && (type == HolonType.All || (i.TryGetValue("holon_type", out var ht) && ht.N == ((int)type).ToString()))).Select(i => Des<Holon>(i["data_json"].S)).Where(h => h != null).Cast<IHolon>().ToList();
                var records=loadChildren && loadChildrenFromProvider ? await ScanHolonsAsync():holons;
                var project=CreateGraphProjection(records,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider);
                result.Result = holons.Select(project).ToList(); result.IsLoaded=true;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        private static void ValidateGraphRead(int maxDepth,int initialDepth,int version)
        {
            if(maxDepth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            if(version!=0) throw new NotSupportedException("DynamoDB holon collections currently retain only the latest version (0).");
        }

        private static Func<IHolon,IHolon> CreateGraphProjection(IEnumerable<IHolon> records,bool children,bool recursive,int maxDepth,int initialDepth,bool fromProvider)
            => NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphProjection.Create(records,children,recursive,maxDepth,initialDepth,fromProvider,
                source=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<Holon>(Ser(source)));

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
                await UpdateItemAsync(new UpdateItemRequest
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
            try {
                NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Validate(searchParams,version,maxChildDepth);
                var groups=searchParams.SearchGroups ?? new List<ISearchGroupBase>();
                var wantsAvatars=groups.Count==0 || groups.Any(group=>group.SearchAvatars || !group.SearchHolons);
                var wantsHolons=groups.Count==0 || groups.Any(group=>group.SearchHolons || !group.SearchAvatars);
                var avatars=new List<IAvatar>(); var holons=new List<IHolon>();
                if(wantsAvatars) {
                    var loaded=await LoadAllAvatarsAsync(version);
                    if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IAvatar>,ISearchResults>(loaded);
                    avatars=loaded.Result.ToList();
                }
                if(wantsHolons || searchParams.ParentId!=Guid.Empty) {
                    var loaded=await LoadAllHolonsAsync(HolonType.All,loadChildren,recursive,maxChildDepth,0,continueOnError,true,version);
                    if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,ISearchResults>(loaded);
                    holons=loaded.Result.ToList();
                }
                var found=new SearchResults();
                found.SearchResultAvatars.AddRange(avatars.Where(row=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Matches(row,true,searchParams,holons)));
                if(wantsHolons) found.SearchResultHolons.AddRange(holons.Where(row=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Matches(row,false,searchParams,holons)));
                found.NumberOfResults=found.SearchResultAvatars.Count+found.SearchResultHolons.Count;
                result.Result=found;result.IsLoaded=true;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { return await ReadMetadataAsync(new Dictionary<string,string> {{metaKey,metaValue}},MetaKeyValuePairMatchMode.All,type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { return await ReadMetadataAsync(metaKvp,mode,type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version); }

        private async Task<OASISResult<IEnumerable<IHolon>>> ReadMetadataAsync(Dictionary<string,string> pairs,MetaKeyValuePairMatchMode mode,HolonType type,
            bool children,bool recursive,int maxDepth,int initialDepth,bool continueOnError,bool fromProvider,int version)
        {
            var result=new OASISResult<IEnumerable<IHolon>>();
            try {
                if(pairs==null || pairs.Count==0 || pairs.Keys.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Nonempty metadata pairs and keys are required.");
                if(mode is not (MetaKeyValuePairMatchMode.All or MetaKeyValuePairMatchMode.Any)) throw new ArgumentOutOfRangeException(nameof(mode));
                var requested=pairs.ToArray();
                var loaded=await LoadAllHolonsAsync(type,children,recursive,maxDepth,initialDepth,continueOnError,fromProvider,version);
                if(loaded.IsError) return loaded;
                bool Matches(IHolon row,KeyValuePair<string,string> pair) => row.MetaData!=null && row.MetaData.TryGetValue(pair.Key,out var stored)
                    && string.Equals(stored==null ? null:Convert.ToString(stored,System.Globalization.CultureInfo.InvariantCulture),pair.Value,StringComparison.Ordinal);
                result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IEnumerable<IHolon>>(loaded);
                result.Result=loaded.Result.Where(row=>mode==MetaKeyValuePairMatchMode.All ? requested.All(pair=>Matches(row,pair)):requested.Any(pair=>Matches(row,pair))).ToList();
            }
            catch(Exception cause) {result.Exception=cause;OASISErrorHandling.HandleError(ref result,cause.Message);}
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKvp, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons); return new OASISResult<bool> { Result = !s.IsError, IsError = s.IsError, Message = s.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync(loadChildrenFromProvider:true,version:version);
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result=new OASISResult<IEnumerable<IHolon>>();
            try {
                if(avatarId==Guid.Empty) throw new ArgumentException("A nonempty avatar identity is required.");
                var loaded=await LoadAllHolonsAsync(loadChildrenFromProvider:true,version:version);
                if(loaded.IsError) return loaded;
                result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IEnumerable<IHolon>>(loaded);
                result.Result=loaded.Result.Where(row=>row.CreatedByAvatarId==avatarId).ToList();
            }
            catch(Exception cause) {result.Exception=cause;OASISErrorHandling.HandleError(ref result,cause.Message);}
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) => ExportSelectedAvatarAsync(u,false,version);
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) => ExportSelectedAvatarAsync(e,true,version);
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;

        private async Task<OASISResult<IEnumerable<IHolon>>> ExportSelectedAvatarAsync(string selector,bool email,int version)
        {
            var result=new OASISResult<IEnumerable<IHolon>>();
            try {
                if(string.IsNullOrWhiteSpace(selector)) throw new ArgumentException("A nonblank avatar selector is required.");
                ValidateGraphRead(0,0,version);
                var selected=email ? await LoadAvatarByEmailAsync(selector,version):await LoadAvatarByUsernameAsync(selector,version);
                if(selected.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IEnumerable<IHolon>>(selected);
                if(selected.Result==null) {OASISErrorHandling.HandleError(ref result,"DynamoDBOASIS: No avatar matches export selector.");return result;}
                return await ExportAllDataForAvatarByIdAsync(selected.Result.Id,version);
            }
            catch(Exception cause) {result.Exception=cause;OASISErrorHandling.HandleError(ref result,cause.Message);}
            return result;
        }
    }
}
