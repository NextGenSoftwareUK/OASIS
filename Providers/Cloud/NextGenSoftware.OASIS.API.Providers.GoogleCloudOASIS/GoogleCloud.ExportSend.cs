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
        public OASISResult<IHolon> LoadHolon(string username, string password, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(username, password, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonByEmailAsync(string email, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Google Cloud provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (!_enableFirestore)
                {
                    OASISErrorHandling.HandleError(ref result, "Firestore is not enabled");
                    return result;
                }

                // Load holon from Firestore by email
                var docRef = _firestoreDb.Collection("holons").WhereEqualTo("email", email).Limit(1);
                var snapshot = await docRef.GetSnapshotAsync();
                
                if (snapshot.Count > 0)
                {
                    var doc = snapshot.Documents.First();
                    var holonData = doc.ConvertTo<Dictionary<string, object>>();
                    var holon = DecodeHolon(holonData,doc.Id);
                    
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully from Google Cloud Firestore by email with full property mapping";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in Google Cloud Firestore by email");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Google Cloud by email: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IHolon> LoadHolonByEmail(string email, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonByEmailAsync(email, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            var failures=new List<Exception>();
            try {
                var plan=PlanFirestoreGraph(holons,saveChildren,recursive,maxChildDepth,curentChildDepth);
                var failedParents=new HashSet<Guid>();
                foreach(var node in plan.Nodes) {
                    if(failedParents.Contains(node.Snapshot.ParentHolonId)) {
                        failedParents.Add(node.Snapshot.Id);
                        failures.Add(new InvalidOperationException("Firestore descendant skipped because its parent failed."));
                        continue;
                    }
                    var written=await SaveHolonAsync(node.Snapshot,false,recursive,maxChildDepth,continueOnError,saveChildrenOnProvider);
                    if(written.IsError || written.Result==null) {
                        failedParents.Add(node.Snapshot.Id);
                        if(!result.IsError) result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,IEnumerable<IHolon>>(written);
                        result.IsError=true;
                        failures.Add(written.Exception ?? new InvalidOperationException(written.Message ?? "Firestore item returned no acknowledgement."));
                        if(!continueOnError || NativeMutationReconciliation.IsRequired(written.Exception)) break;
                    }
                    else {
                        saved.Add(written.Result);
                        node.Source.Id=node.Snapshot.Id;node.Source.ParentHolonId=node.Snapshot.ParentHolonId;
                        node.Source.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                        node.Source.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS]=node.Snapshot.Id.ToString();
                    }
                }
                result.Result=failures.Count==0 ? plan.Roots:saved;result.IsSaved=failures.Count==0;
                if(failures.Count>0) {
                    result.IsError=true;
                    var cause=failures.Count==1 ? failures[0]:new AggregateException(failures);
                    result.Exception=saved.Count>0 ? new GoogleFirestorePartialMutationException(saved.Select(row=>row.Id),cause):cause;
                }
            }
            catch(Exception cause) {
                result.Exception=saved.Count>0 ? new GoogleFirestorePartialMutationException(saved.Select(row=>row.Id),cause):cause;
                result.IsSaved=false;OASISErrorHandling.HandleError(ref result,cause.Message);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Google Cloud provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (!_enableFirestore)
                {
                    OASISErrorHandling.HandleError(ref result, "Firestore is not enabled");
                    return result;
                }

                var docRef = _firestoreDb.Collection("holons").Document(id.ToString());
                var snapshot = await docRef.GetSnapshotAsync();
                if (!snapshot.Exists)
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon '{id}' was not found in Google Cloud Firestore");
                    return result;
                }

                var loadResult = await LoadHolonAsync(id);
                if (loadResult.IsError || loadResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, loadResult.Message ?? $"Holon '{id}' could not be loaded before deletion");
                    return result;
                }

                await docRef.DeleteAsync();
                result.Result = loadResult.Result;
                result.IsError = false;
                result.Message = "Holon deleted successfully from Google Cloud Firestore";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Google Cloud: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Google Cloud provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (!_enableFirestore)
                {
                    OASISErrorHandling.HandleError(ref result, "Firestore is not enabled");
                    return result;
                }

                var query = _firestoreDb.Collection("holons").WhereEqualTo("providerKey", providerKey);
                var snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Count > 0)
                {
                    var doc = snapshot.Documents.First();
                    var docRef = doc.Reference;
                    
                    if (true) // Soft delete by default (OASIS standard)
                    {
                        // Soft delete - mark as deleted
                        await docRef.UpdateAsync("IsDeleted", true);
                        await docRef.UpdateAsync("DeletedDate", Timestamp.FromDateTime(DateTime.UtcNow));
                        await docRef.UpdateAsync("DeletedByAvatarId", AvatarManager.LoggedInAvatar?.Id ?? Guid.Empty);
                    }
                    else
                    {
                        // Hard delete - remove document
                        await docRef.DeleteAsync();
                    }
                    
                    result.Result = null; // Return null for deleted holon
                    result.IsError = false;
                    result.Message = "Holon soft deleted successfully from Google Cloud Firestore by provider key";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in Google Cloud Firestore by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Google Cloud by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var deleteResult = DeleteHolonAsync(providerKey).Result;
                result.IsError = deleteResult.IsError;
                result.Message = deleteResult.Message;
                result.Result = null; // Return null for deleted holon
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<bool>> DeleteHolonAsync(string username, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Google Cloud provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (!_enableFirestore)
                {
                    OASISErrorHandling.HandleError(ref result, "Firestore is not enabled");
                    return result;
                }

                var query = _firestoreDb.Collection("holons").WhereEqualTo("username", username);
                var snapshot = await query.GetSnapshotAsync();
                
                if (snapshot.Count > 0)
                {
                    var doc = snapshot.Documents.First();
                    var docRef = doc.Reference;
                    
                    if (true) // Soft delete by default (OASIS standard)
                    {
                        // Soft delete - mark as deleted
                        await docRef.UpdateAsync("IsDeleted", true);
                        await docRef.UpdateAsync("DeletedDate", Timestamp.FromDateTime(DateTime.UtcNow));
                        await docRef.UpdateAsync("DeletedByAvatarId", AvatarManager.LoggedInAvatar?.Id ?? Guid.Empty);
                    }
                    else
                    {
                        // Hard delete - remove document
                        await docRef.DeleteAsync();
                    }
                    
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Holon {(softDelete ? "soft" : "hard")} deleted successfully from Google Cloud Firestore by username";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found in Google Cloud Firestore by username");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Google Cloud by username: {ex.Message}", ex);
            }
            return result;
        }

    }
}
