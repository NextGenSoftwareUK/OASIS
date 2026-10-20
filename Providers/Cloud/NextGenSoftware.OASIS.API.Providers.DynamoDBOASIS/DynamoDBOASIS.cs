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
    public partial class DynamoDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider, IOASISNETProvider, IProviderTransportFailureClassifier
    {
        // AWS SDK converts its own HTTP deadline cancellation to TimeoutException.
        // Public storage calls do not supply caller tokens; raw cancellation remains unclassified.
        public bool IsProviderTransportFailure(Exception failure)=>failure is TimeoutException;
        private AmazonDynamoDBClient _client;
        private readonly Func<AmazonDynamoDBClient> _createClient;
        private bool _clientDisposed;
        protected virtual ProviderType StorageProviderType => Core.Enums.ProviderType.DynamoDBOASIS;

        private static void ValidateAcknowledgedCaller(Action validateUnchanged,Guid acknowledgedId)
        {
            try{validateUnchanged();}
            catch(Exception cause){throw new DynamoDBPartialMutationException(new[]{acknowledgedId},cause);}
        }

        public DynamoDBOASIS(string accessKey, string secretKey, string region, string? serviceUrl = null)
            :this(accessKey,secretKey,region,serviceUrl,null,false)
        { }

        public DynamoDBOASIS(string accessKey,string secretKey,string region,string? serviceUrl,string? sessionToken,bool useDefaultCredentials)
        {
            if(useDefaultCredentials && (!string.IsNullOrEmpty(accessKey) || !string.IsNullOrEmpty(secretKey) || !string.IsNullOrEmpty(sessionToken)))
                throw new ArgumentException("Choose SDK default credentials or explicit credentials, not both.");
            if(!useDefaultCredentials && !string.IsNullOrEmpty(sessionToken)
                && (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey)))
                throw new ArgumentException("Session credentials require access key, secret key and session token.");
            AWSCredentials? creds=useDefaultCredentials ? null:string.IsNullOrEmpty(sessionToken)
                ? new BasicAWSCredentials(accessKey,secretKey):new SessionAWSCredentials(accessKey,secretKey,sessionToken);
            // A retry after a lost write acknowledgement must not masquerade as a known CAS conflict.
            var config = new AmazonDynamoDBConfig { MaxErrorRetry=0 };
            if (string.IsNullOrWhiteSpace(serviceUrl))
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
            else
            {
                config.ServiceURL = serviceUrl;
                config.AuthenticationRegion = region;
                config.UseHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
            }
            _createClient = () => useDefaultCredentials ? new AmazonDynamoDBClient(config):new AmazonDynamoDBClient(creds,config);
            _client = _createClient();
            ProviderName = "DynamoDBOASIS";
            ProviderDescription = "AWS DynamoDB provider (AWSSDK.DynamoDBv2 — serverless NoSQL for OASIS holons and avatars)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.DynamoDBOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private static string Ser(object o) => NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(o);

        private static T ReadCanonicalIdentity<T>(Dictionary<string,AttributeValue> item,Guid requested) where T:class,IHolonBase
        {
            if(requested==Guid.Empty || !item.TryGetValue("id",out var key) || key.S!=requested.ToString()
                || !item.TryGetValue("data_json",out var json) || string.IsNullOrWhiteSpace(json.S))
                throw new System.IO.InvalidDataException("DynamoDB native record key/payload differs from requested identity.");
            var record=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.DeserializeStored<T>(json.S);
            if(record==null || record.Id!=requested)
                throw new System.IO.InvalidDataException("DynamoDB canonical record identity differs from its native key.");
            if(item.TryGetValue("is_deleted",out var deleted) && deleted.BOOL!=record.IsDeleted)
                throw new System.IO.InvalidDataException("DynamoDB deletion projection differs from canonical record.");
            if(record is IAvatar || record is IAvatarDetail)
                foreach(var field in new[] {"username","email"})
                    if(item.TryGetValue(field,out var projected) && projected.S!=CanonicalIdentityValue(record,field))
                        throw new System.IO.InvalidDataException("DynamoDB identity projection differs from canonical record.");
            if(record is IHolon holon)
            {
                if(item.TryGetValue("parentHolonId",out var parent) && parent.S!=holon.ParentHolonId.ToString())
                    throw new System.IO.InvalidDataException("DynamoDB parent projection differs from canonical record.");
                if(item.TryGetValue("holon_type",out var type) && type.N!=((int)holon.HolonType).ToString(System.Globalization.CultureInfo.InvariantCulture))
                    throw new System.IO.InvalidDataException("DynamoDB type projection differs from canonical record.");
            }
            return record;
        }

        private static T ReadStoredIdentity<T>(Dictionary<string,AttributeValue> item,Guid requested,int version=0) where T:class,IHolonBase
            => ReadStoredVersion<T>(item,requested,version);

        private static T ReadStoredIdentity<T>(Dictionary<string,AttributeValue> item,int version=0) where T:class,IHolonBase
        {
            if(!item.TryGetValue("id",out var key) || !Guid.TryParse(key.S,out var id))
                throw new System.IO.InvalidDataException("DynamoDB native record identity is invalid.");
            return ReadStoredIdentity<T>(item,id,version);
        }

        private static string CanonicalIdentityValue(IHolonBase record,string field)
        {
            if(record is IAvatar avatar) return (field=="username" ? avatar.Username:avatar.Email) ?? string.Empty;
            if(record is IAvatarDetail detail) return (field=="username" ? detail.Username:detail.Email) ?? string.Empty;
            throw new ArgumentException("Canonical avatar or detail required.",nameof(record));
        }

        private static bool IsNativeValidationRejection(AmazonDynamoDBException cause)
            => cause.StatusCode==System.Net.HttpStatusCode.BadRequest
                &&string.Equals(cause.ErrorCode,"ValidationException",StringComparison.Ordinal);

        private static async Task<T> ObserveMutationAsync<T>(Func<Task<T>> mutation,string table,string key)
        {
            try {return await mutation();}
            // A failed condition is explicit server evidence that this item mutation did not execute.
            catch(ConditionalCheckFailedException) {throw;}
            // A native HTTP400 ValidationException rejects the request; it is not a lost ACK.
            // Do not generalize this to arbitrary400s, transport failures or server errors.
            catch(AmazonDynamoDBException cause)when(IsNativeValidationRejection(cause)){throw;}
            catch(Exception cause) {throw new DynamoDBMutationReconciliationException(table,key,cause);}
        }
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
                ArgumentNullException.ThrowIfNull(avatar);
                var source=avatar;var publication=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordPublicationSnapshot<Avatar>.Capture(source);
                avatar=publication.Proposal;
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[StorageProviderType] = avatar.Id.ToString();
                var committed=await WriteRecordAsync<Avatar>(new PutItemRequest
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
                },IsVersionControlEnabled,publication.ValidateUnchanged);
                ValidateAcknowledgedCaller(publication.ValidateUnchanged,committed.Id);
                source.Version=committed.Version;source.VersionId=committed.VersionId;source.PreviousVersionId=committed.PreviousVersionId;
                source.Id=avatar.Id;
                source.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                source.ProviderUniqueStorageKey[StorageProviderType]=avatar.Id.ToString();
                result.Result = source; result.IsSaved=true; result.IsError = false; result.Message = $"DynamoDBOASIS: Avatar '{avatar.Username}' saved.";
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
                ValidateRecordVersion(version);
                var resp = await _client.GetItemAsync(new GetItemRequest {TableName="OasisAvatars",ConsistentRead=true,Key=new Dictionary<string, AttributeValue> { ["id"] = new AttributeValue { S = id.ToString() } }});
                RequireItemRead(resp);
                if (resp.Item.Count==0) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No avatar for ID '{id}'."); result.ErrorCode=OASISResultErrorCodes.RecordNotFound; return result; }
                var avatar = ReadStoredIdentity<Avatar>(resp.Item,id,version);
                if(avatar==null||avatar.IsDeleted) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;return result;}
                result.Result = avatar; result.IsLoaded=true; result.IsError = false; result.Message = "DynamoDBOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        private async Task<Avatar?> ReadAvatarSelectorAsync(string keyName, string keyValue,int version=0)
            => await ReadUniqueAvatarAsync(request => _client.ScanAsync(request), keyName, keyValue,version);

        private static async Task<Avatar?> ReadUniqueAvatarAsync(Func<ScanRequest, Task<ScanResponse>> read, string keyName, string keyValue,int version=0)
            => await ReadUniqueIdentityAsync<Avatar>(read,"OasisAvatars",keyName,keyValue,version);

        private static async Task<T?> ReadUniqueIdentityAsync<T>(Func<ScanRequest, Task<ScanResponse>> read, string tableName, string keyName, string keyValue,int version=0) where T:class,IHolonBase
        {
            if(string.IsNullOrWhiteSpace(keyValue)) throw new ArgumentException("Avatar selector must not be blank.",nameof(keyValue));
            return await ReadUniqueIdentityAsync<T>(read,tableName,record=>string.Equals(CanonicalIdentityValue(record,keyName),keyValue,StringComparison.Ordinal),version);
        }

        private static async Task<T?> ReadUniqueIdentityAsync<T>(Func<ScanRequest, Task<ScanResponse>> read,string tableName,Func<T,bool> matches,int version=0) where T:class,IHolonBase
        {
            T? match = null;
            await foreach(var item in ReadScanRowsAsync(read,tableName))
            {
                var avatar=ReadStoredIdentity<T>(item,version);
                if(avatar==null||avatar.IsDeleted)continue;
                if(!matches(avatar))continue;
                if(match!=null)throw new StorageIdentityAmbiguousException();
                match=avatar;
            }
            return match;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { ValidateRecordVersion(version); var a = await ReadAvatarSelectorAsync("username", username,version); if (a == null) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No avatar for username '{username}'."); result.ErrorCode=OASISResultErrorCodes.RecordNotFound; return result; } result.Result = a; result.IsLoaded=true; result.IsError = false; result.Message = "DynamoDBOASIS: Avatar loaded."; }
            catch (StorageIdentityAmbiguousException ex) { result.Exception=ex; OASISErrorHandling.HandleError(ref result,ex.Message); result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { ValidateRecordVersion(version); var a = await ReadAvatarSelectorAsync("email", email,version); if (a == null) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No avatar for email '{email}'."); result.ErrorCode=OASISResultErrorCodes.RecordNotFound; return result; } result.Result = a; result.IsLoaded=true; result.IsError = false; result.Message = "DynamoDBOASIS: Avatar loaded."; }
            catch (StorageIdentityAmbiguousException ex) { result.Exception=ex; OASISErrorHandling.HandleError(ref result,ex.Message); result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => LoadAvatarByEmailAsync(email, version).Result;

        private async Task<OASISResult<IAvatar>> LoadAvatarByCredentialSelectorAsync(string selector,Func<Avatar,bool> matches,int version)
        {
            var result=new OASISResult<IAvatar>();
            try
            {
                ValidateRecordVersion(version);
                if(string.IsNullOrWhiteSpace(selector)) throw new ArgumentException("A nonempty avatar credential selector is required.",nameof(selector));
                if(version!=0)throw new NotSupportedException("Credential/token authentication requires current authoritative records, never historical versions.");
                var avatar=await ReadUniqueIdentityAsync<Avatar>(request=>_client.ScanAsync(request),"OasisAvatars",matches);
                if(avatar==null)
                {
                    OASISErrorHandling.HandleError(ref result,"DynamoDBOASIS: Avatar credential selector was not found.");
                    result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
                    return result;
                }
                result.Result=avatar;result.IsLoaded=true;
            }
            catch(StorageIdentityAmbiguousException ex)
            {
                result.Exception=ex;OASISErrorHandling.HandleError(ref result,ex.Message);
                result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            }
            catch(Exception ex)
            {
                result.Exception=ex;OASISErrorHandling.HandleError(ref result,"DynamoDBOASIS: Avatar credential lookup failed.");
                if(ex is ArgumentException || ex is NotSupportedException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
            }
            return result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string token,int version=0)
            => LoadAvatarByCredentialSelectorAsync(token,avatar=>string.Equals(avatar.VerificationToken,token,StringComparison.Ordinal),version);
        public override OASISResult<IAvatar> LoadAvatarByVerificationToken(string token,int version=0)
            => LoadAvatarByVerificationTokenAsync(token,version).GetAwaiter().GetResult();
        public override Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string token,int version=0)
            => LoadAvatarByCredentialSelectorAsync(token,avatar=>string.Equals(avatar.ResetToken,token,StringComparison.Ordinal),version);
        public override OASISResult<IAvatar> LoadAvatarByResetToken(string token,int version=0)
            => LoadAvatarByResetTokenAsync(token,version).GetAwaiter().GetResult();
        public override Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string token,int version=0)
            => LoadAvatarByCredentialSelectorAsync(token,avatar=>avatar.RefreshTokens?.Any(value=>value!=null && string.Equals(value.Token,token,StringComparison.Ordinal))==true,version);
        public override OASISResult<IAvatar> LoadAvatarByRefreshToken(string token,int version=0)
            => LoadAvatarByRefreshTokenAsync(token,version).GetAwaiter().GetResult();

        public Task<OASISResult<IAvatar>> LoadAvatarByPublicKeyAsync(string publicKey,int version=0)
            => LoadAvatarByCredentialSelectorAsync(publicKey,avatar=>avatar.ProviderWallets?.Values.Any(wallets=>
                wallets?.Any(wallet=>wallet!=null && string.Equals(wallet.PublicKey,publicKey,StringComparison.Ordinal))==true)==true,version);
        public OASISResult<IAvatar> LoadAvatarByPublicKey(string publicKey,int version=0)
            => LoadAvatarByPublicKeyAsync(publicKey,version).GetAwaiter().GetResult();
        public Task<OASISResult<IAvatar>> LoadAvatarByPrivateKeyAsync(string privateKey,int version=0)
            => LoadAvatarByCredentialSelectorAsync(privateKey,avatar=>avatar.ProviderWallets?.Values.Any(wallets=>
                wallets?.Any(wallet=>wallet!=null && string.Equals(wallet.PrivateKey,privateKey,StringComparison.Ordinal))==true)==true,version);
        public OASISResult<IAvatar> LoadAvatarByPrivateKey(string privateKey,int version=0)
            => LoadAvatarByPrivateKeyAsync(privateKey,version).GetAwaiter().GetResult();
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string pk, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadAvatarAsync(id, version); var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"DynamoDBOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string pk, int version = 0) => LoadAvatarByProviderKeyAsync(pk, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                ValidateRecordVersion(version);
                var items=await ReadScanItemsAsync(request=>_client.ScanAsync(request),"OasisAvatars");

                var avatars = items.Select(i => ReadStoredIdentity<Avatar>(i,version)).Where(row=>row!=null&&!row.IsDeleted).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsLoaded=true; result.IsError = false; result.Message = $"DynamoDBOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
            => ApplyAvatarLifecycleAsync(id,softDelete);

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
                ArgumentNullException.ThrowIfNull(d);
                var source=d;var publication=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordPublicationSnapshot<AvatarDetail>.Capture(source);
                d=publication.Proposal;
                if (d.Id == Guid.Empty) d.Id = Guid.NewGuid();
                d.ProviderUniqueStorageKey ??=new Dictionary<Core.Enums.ProviderType,string>();
                d.ProviderUniqueStorageKey[StorageProviderType]=d.Id.ToString();
                var committed=await WriteRecordAsync<AvatarDetail>(new PutItemRequest
                {
                    TableName = "OasisAvatarDetails",
                    Item = WithOptionalIdentityIndexes(new Dictionary<string, AttributeValue>
                    {
                        ["id"] = new AttributeValue { S = d.Id.ToString() },
                        ["username"] = new AttributeValue { S = d.Username ?? "" },
                        ["email"] = new AttributeValue { S = d.Email ?? "" },
                        ["is_deleted"] = new AttributeValue {BOOL=d.IsDeleted},
                        ["data_json"] = new AttributeValue { S = Ser(d) }
                    })
                },IsVersionControlEnabled,publication.ValidateUnchanged);
                ValidateAcknowledgedCaller(publication.ValidateUnchanged,committed.Id);
                source.Version=committed.Version;source.VersionId=committed.VersionId;source.PreviousVersionId=committed.PreviousVersionId;
                source.Id=d.Id;
                source.ProviderUniqueStorageKey ??=new Dictionary<Core.Enums.ProviderType,string>();
                source.ProviderUniqueStorageKey[StorageProviderType]=d.Id.ToString();
                result.Result = source; result.IsSaved=true; result.IsError = false; result.Message = "DynamoDBOASIS: AvatarDetail saved.";
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
                ValidateRecordVersion(version);
                var resp = await _client.GetItemAsync(new GetItemRequest {TableName="OasisAvatarDetails",ConsistentRead=true,Key=new Dictionary<string, AttributeValue> { ["id"] = new AttributeValue { S = id.ToString() } }});
                RequireItemRead(resp);
                if (resp.Item.Count==0) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No detail for ID '{id}'."); result.ErrorCode=OASISResultErrorCodes.RecordNotFound; return result; }
                var d = ReadStoredIdentity<AvatarDetail>(resp.Item,id,version);
                if(d==null){result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;return result;}
                result.Result = d; result.IsLoaded=true; result.IsError = false; result.Message = "DynamoDBOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        private async Task<AvatarDetail?> ReadDetailSelectorAsync(string keyName, string keyValue,int version=0)
            => await ReadUniqueIdentityAsync<AvatarDetail>(request=>_client.ScanAsync(request),"OasisAvatarDetails",keyName,keyValue,version);

        private async Task<OASISResult<IAvatarDetail>> LoadSelectedDetailAsync(string key,string value,int version)
        {
            var result=new OASISResult<IAvatarDetail>();
            try {
                ValidateRecordVersion(version);
                var detail=await ReadDetailSelectorAsync(key,value,version);
                if(detail==null) {OASISErrorHandling.HandleError(ref result,"DynamoDBOASIS: No matching detail.");result.ErrorCode=OASISResultErrorCodes.RecordNotFound;return result;}
                result.Result=detail;result.IsLoaded=true;
            }
            catch(Exception cause) {
                result.Exception=cause;OASISErrorHandling.HandleError(ref result,cause.Message);
                if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            }
            return result;
        }
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) => LoadSelectedDetailAsync("username",u,version);
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) => LoadSelectedDetailAsync("email",e,version);
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try {
                ValidateRecordVersion(version);
                var items=await ReadScanItemsAsync(request=>_client.ScanAsync(request),"OasisAvatarDetails");
                var details=items.Select(item=>ReadStoredIdentity<AvatarDetail>(item,version)).Where(row=>row!=null).Cast<IAvatarDetail>().ToList();
                result.Result=details;result.IsLoaded=true;result.Message=$"DynamoDBOASIS: Loaded {details.Count} detail(s).";
            }
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
                ArgumentNullException.ThrowIfNull(holon);
                if(maxChildDepth<0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
                if(saveChildren)
                {
                    var graph=await SaveHolonsAsync(new[] {holon},true,recursive,maxChildDepth,0,continueOnError,saveChildrenOnProvider);
                    var converted=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(graph);
                    if(!graph.IsError) converted.Result=holon;
                    return converted;
                }
                var source=holon;var publication=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordPublicationSnapshot<Holon>.Capture(source);
                holon=publication.Proposal;
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                if (holon.ProviderUniqueStorageKey == null) holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[StorageProviderType] = holon.Id.ToString();
                var committed=await WriteRecordAsync<Holon>(new PutItemRequest
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
                },IsVersionControlEnabled,publication.ValidateUnchanged);
                ValidateAcknowledgedCaller(publication.ValidateUnchanged,committed.Id);
                source.Version=committed.Version;source.VersionId=committed.VersionId;source.PreviousVersionId=committed.PreviousVersionId;
                source.Id=holon.Id;
                source.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                source.ProviderUniqueStorageKey[StorageProviderType]=holon.Id.ToString();
                result.Result = source; result.IsSaved=true; result.IsError = false; result.Message = $"DynamoDBOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var failures=new List<Exception>();
            try {
                var plan=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphWritePlan.Create(holons,saveChildren,recursive,maxChildDepth,curentChildDepth);
                var failedParents=new HashSet<Guid>();
                var publications=plan.Nodes.Select(node=>(Node:node,OriginalId:node.Source.Id)).ToArray();
                foreach(var publication in publications) {
                    var node=publication.Node;
                    NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordPublication.EnsureIdentityUnchanged(node.Source,publication.OriginalId);
                    if(failedParents.Contains(node.Snapshot.ParentHolonId)) {
                        failedParents.Add(node.Snapshot.Id);failures.Add(new InvalidOperationException("Descendant skipped because its parent failed."));continue;
                    }
                    var written=await SaveHolonAsync(node.Snapshot,false,recursive,maxChildDepth,continueOnError,saveChildrenOnProvider);
                    if(written.IsError || written.Result==null) {
                        failedParents.Add(node.Snapshot.Id);
                        if(!result.IsError) result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,IEnumerable<IHolon>>(written);
                        result.IsError=true;
                        failures.Add(written.Exception ?? new InvalidOperationException(written.Message ?? "DynamoDB batch item returned no acknowledgement."));
                        if(!continueOnError || NativeMutationReconciliation.IsRequired(written.Exception)) break;
                    }
                    else {
                        saved.Add(written.Result);
                        NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordPublication.EnsureIdentityUnchanged(node.Source,publication.OriginalId);
                        node.Source.Id=node.Snapshot.Id;node.Source.ParentHolonId=node.Snapshot.ParentHolonId;
                        node.Source.Version=written.Result.Version;node.Source.VersionId=written.Result.VersionId;node.Source.PreviousVersionId=written.Result.PreviousVersionId;
                        node.Source.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                        node.Source.ProviderUniqueStorageKey[StorageProviderType]=node.Snapshot.Id.ToString();
                    }
                }
                result.Result=failures.Count==0 ? plan.Roots:saved; result.IsSaved=failures.Count==0;
                if(failures.Count>0) {
                    var cause=failures.Count==1 ? failures[0]:new AggregateException(failures);
                    result.Exception=saved.Count>0 ? new DynamoDBPartialMutationException(saved.Select(row=>row.Id),cause):cause;
                }
            }
            catch(Exception cause) {
                result.Exception=saved.Count>0 ? new DynamoDBPartialMutationException(saved.Select(row=>row.Id),cause):cause;
                result.IsSaved=false;OASISErrorHandling.HandleError(ref result,cause.Message);
            }
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
                RequireItemRead(resp);
                if (resp.Item.Count==0) { OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: No holon for ID '{id}'."); result.ErrorCode=OASISResultErrorCodes.RecordNotFound; return result; }
                var holon = ReadStoredIdentity<Holon>(resp.Item,id,version);
                if(holon==null||holon.IsDeleted) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;return result;}
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

        private async Task<List<IHolon>> ScanHolonsAsync(int version=0)
        {
            var items=await ReadScanItemsAsync(request=>_client.ScanAsync(request),"OasisHolons");
            return items.Select(i => ReadStoredIdentity<Holon>(i,version)).Where(row=>row!=null&&!row.IsDeleted).Cast<IHolon>().ToList();
        }

        private static async Task<List<Dictionary<string,AttributeValue>>> ReadQueryItemsAsync(Func<QueryRequest,Task<QueryResponse>> query,QueryRequest request)
        {
            var items=new List<Dictionary<string,AttributeValue>>();
            await foreach(var item in ReadNativePagesAsync(async cursor=>{
                request.ExclusiveStartKey=cursor;
                var response=await query(request);
                RequireReadAcknowledgement(response,response?.Items!=null);
                return(response.Items,response.LastEvaluatedKey);
            },request.ExclusiveStartKey))items.Add(item);
            return items;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                ValidateGraphRead(maxChildDepth,curentChildDepth,version,type);
                var records=await ScanHolonsAsync(version);
                var childPool=loadChildren&&loadChildrenFromProvider&&version>0?await ScanHolonsAsync():records;
                var project=CreateGraphProjection(childPool,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider);
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
                ValidateGraphRead(maxChildDepth,curentChildDepth,version,type);
                var holons=(await ScanHolonsAsync(version))
                    .Where(row=>row.ParentHolonId==id && (type==HolonType.All || row.HolonType==type)).ToList();
                var records=loadChildren && loadChildrenFromProvider ? await ScanHolonsAsync():holons;
                var project=CreateGraphProjection(records,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider);
                result.Result = holons.Select(project).ToList(); result.IsLoaded=true;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"DynamoDBOASIS: {ex.Message}"); }
            return result;
        }

        private static void ValidateRecordVersion(int version)
        {
            if(version<0) throw new ArgumentOutOfRangeException(nameof(version));
        }

        private static void ValidateGraphRead(int maxDepth,int initialDepth,int version,HolonType? type=null)
        {
            if(type.HasValue && !System.Enum.IsDefined(typeof(HolonType),type.Value)) throw new ArgumentOutOfRangeException(nameof(type));
            if(maxDepth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            ValidateRecordVersion(version);
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
                if(id==Guid.Empty) throw new ArgumentException("A nonempty holon identity is required.");
                var item=await ReadLifecycleItemAsync("OasisHolons",id);
                if(item.Count==0) return new OASISResult<IHolon> {IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="DynamoDB holon not found."};
                ReadStoredIdentity<Holon>(item,id);
                var expected=item["data_json"].S;
                var original=await ObserveMutationAsync(async()=>
                {
                    var deleted=await _client.DeleteItemAsync(new DeleteItemRequest {TableName="OasisHolons",Key=LifecycleKey(id),ReturnValues=ReturnValue.ALL_OLD,
                        ConditionExpression="data_json = :expected",ExpressionAttributeValues=new() {{":expected",new AttributeValue {S=expected}}}});
                    if(deleted.HttpStatusCode!=System.Net.HttpStatusCode.OK || deleted.Attributes==null
                        || !deleted.Attributes.TryGetValue("data_json",out var oldJson) || oldJson.S!=expected)
                        throw new System.IO.InvalidDataException("DynamoDB holon deletion lacks matching native acknowledgement.");
                    return ReadStoredIdentity<Holon>(deleted.Attributes,id);
                },"OasisHolons",id.ToString());
                original.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                original.ProviderUniqueStorageKey[StorageProviderType]=id.ToString();
                result.Result=original;result.IsSaved=true;result.IsDeleted=true;result.DeletedCount=1;
            }
            catch(ConditionalCheckFailedException cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);result.ErrorCode=StorageLifecycleConflictException.ErrorCode;}
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
                NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Validate(searchParams,version,maxChildDepth,supportsHistory:true);
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

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var saved = await SaveHolonsAsync(holons,continueOnError:false); var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,bool>(saved); result.Result=!saved.IsError && saved.IsSaved; return result; }
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
