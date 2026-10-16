using System;
using System.IO;
using System.Data;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Google.Cloud.Storage.V1;
using Google.Cloud.Firestore;
using Google.Cloud.BigQuery.V2;
using Google.Api.Gax;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS
{
    public partial class GoogleCloudOASIS
    {

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();
            if(!_enableStorage && !_enableFirestore && !_enableBigQuery)
            {
                IsProviderActivated=false;
                var cause=new InvalidOperationException("At least one Google Cloud service must be enabled before activation.");
                OASISErrorHandling.HandleError(ref result,cause.Message,cause);
                result.ErrorCode="GOOGLE_CLOUD_SERVICE_REQUIRED";
                return result;
            }
            if(IsProviderActivated) return new OASISResult<bool>(true);

            try
            {
                // Initialize Google Cloud Storage client if enabled
                if (_enableStorage)
                {
                    var storageBuilder=new StorageClientBuilder();
                    if(!string.IsNullOrWhiteSpace(_credentialsPath)) storageBuilder.CredentialsPath=_credentialsPath;
                    _storageClient = await storageBuilder.BuildAsync();
                    
                    // Test connection by creating a test bucket if it doesn't exist
                    try
                    {
                        await _storageClient.GetBucketAsync(_bucketName);
                    }
                    catch(Google.GoogleApiException cause) when(cause.HttpStatusCode==System.Net.HttpStatusCode.NotFound)
                    {
                        // Create bucket if it doesn't exist
                        await _storageClient.CreateBucketAsync(_projectId, _bucketName);
                    }
                }
                
                // Initialize Firestore client if enabled
                if (_enableFirestore)
                {
                    var firestoreBuilder = new FirestoreDbBuilder
                    {
                        ProjectId = _projectId,
                        DatabaseId = _firestoreDatabaseId,
                        EmulatorDetection = EmulatorDetection.EmulatorOrProduction
                    };
                    if (!string.IsNullOrWhiteSpace(_credentialsPath))
                        firestoreBuilder.CredentialsPath = _credentialsPath;
                    _firestoreDb = firestoreBuilder.Build();

                    // Client construction does not contact Firestore. Perform a real,
                    // reversible write so an unreachable or unauthorized provider can
                    // never be advertised to HyperDrive as active.
                    var activationProbe = _firestoreDb.Collection("_oasis_health").Document(Guid.NewGuid().ToString("N"));
                    await activationProbe.SetAsync(new Dictionary<string, object>
                    {
                        ["provider"] = "GoogleCloudOASIS",
                        ["checkedAt"] = Timestamp.GetCurrentTimestamp()
                    });
                    await activationProbe.DeleteAsync();
                }
                
                // Initialize BigQuery client if enabled
                if (_enableBigQuery)
                {
                    var bigQueryBuilder=new BigQueryClientBuilder {ProjectId=_projectId};
                    if(!string.IsNullOrWhiteSpace(_credentialsPath)) bigQueryBuilder.CredentialsPath=_credentialsPath;
                    _bigQueryClient = await bigQueryBuilder.BuildAsync();
                    await _bigQueryClient.GetDatasetAsync(_bigQueryDatasetId);
                }
                
                IsProviderActivated = true;
                result.Result = true;
                result.IsError = false;
                result.Message = "Google Cloud provider activated successfully with configured services initialized.";
            }
            catch (Exception e)
            {
                var cleanup=await DeActivateProviderAsync();
                var cause=cleanup.IsError ? new AggregateException("Google activation and cleanup failed.",e,cleanup.Exception ?? new InvalidOperationException(cleanup.Message)):e;
                OASISErrorHandling.HandleError(ref result, $"Error activating Google Cloud provider: {e.Message}", cause);
            }

            return result;
        }

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var result=new OASISResult<bool>();
            var storage=_storageClient;var bigQuery=_bigQueryClient;
            _storageClient=null;_bigQueryClient=null;_firestoreDb=null;
            IsProviderActivated=false;
            var failures=new List<Exception>();
            try {storage?.Dispose();} catch(Exception cause) {failures.Add(cause);}
            try {bigQuery?.Dispose();} catch(Exception cause) {failures.Add(cause);}
            if(failures.Count>0)
            {
                var cause=failures.Count==1 ? failures[0]:new AggregateException(failures);
                OASISErrorHandling.HandleError(ref result,cause.Message,cause);
            }
            else result.Result=true;
            return Task.FromResult(result);
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        // Real Google Cloud implementation methods
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
            => ReadCanonicalAvatarAsync(id,version);

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
            => ReadCanonicalAvatarSelectorAsync(avatarEmail,true,version);

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => ReadCanonicalAvatarSelectorAsync(avatarUsername,false,version);

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
            => ReadCanonicalAvatarsAsync(version);

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            try
            {
                ArgumentNullException.ThrowIfNull(avatar);
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IAvatar>(activateResult);
                    }
                }

                if (!_enableFirestore)
                {
                    OASISErrorHandling.HandleError(ref result, "Firestore is not enabled");
                    return result;
                }

                var snapshot=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<Avatar>(
                    NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(avatar));
                if(snapshot.Id==Guid.Empty) snapshot.Id=Guid.NewGuid();
                snapshot.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                snapshot.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS]=snapshot.Id.ToString();

                // Save avatar to Firestore with FULL property mapping
                var docRef = _firestoreDb.Collection("avatars").Document(snapshot.Id.ToString());
                var avatarData = new Dictionary<string, object>
                {
                    ["id"] = snapshot.Id.ToString(),
                    ["username"] = snapshot.Username,
                    ["email"] = snapshot.Email,
                    ["isActive"] = snapshot.IsActive,
                    ["data_json"] = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(snapshot)
                };
                
                await ObserveFirestoreMutationAsync(()=>docRef.SetAsync(avatarData),docRef.Path);
                avatar.Id=snapshot.Id;
                avatar.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS]=snapshot.Id.ToString();
                
                result.Result = avatar;
                result.IsSaved = true;
                result.IsError = false;
                result.Message = "Avatar saved successfully to Google Cloud Firestore with full property mapping";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

    }
}
