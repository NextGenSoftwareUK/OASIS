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
        [Obsolete("Authenticate through AvatarManager, then use authorized ID-based storage reads.")]
        public OASISResult<IHolon> LoadHolon(string username, string password, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(username, password, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();
        }


        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            var failures=new List<Exception>();
            try {
                var sourceGuards=new Dictionary<IHolon,NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordPublicationSnapshot<Holon>>(ReferenceEqualityComparer.Instance);
                var plan=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphWritePlan.Create(holons,saveChildren,recursive,maxChildDepth,curentChildDepth,
                    source=>sourceGuards.Add(source,NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordPublicationSnapshot<Holon>.Capture(source)));
                var failedParents=new HashSet<Guid>();
                foreach(var node in plan.Nodes) {
                    var publication=sourceGuards[node.Source];publication.ValidateUnchanged();
                    if(failedParents.Contains(node.Snapshot.ParentHolonId)) {
                        failedParents.Add(node.Snapshot.Id);
                        failures.Add(new InvalidOperationException("Firestore descendant skipped because its parent failed."));
                        continue;
                    }
                    var written=await SavePlannedHolonAsync(node);
                    if(written.IsError || written.Result==null || !written.IsSaved) {
                        failedParents.Add(node.Snapshot.Id);
                        if(!result.IsError) result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,IEnumerable<IHolon>>(written);
                        result.IsError=true;
                        failures.Add(written.Exception ?? new InvalidDataException(written.Message ?? "Firestore item returned no acknowledged save."));
                        if(!continueOnError || NativeMutationReconciliation.IsRequired(written.Exception)) break;
                    }
                    else {
                        saved.Add(written.Result);
                        publication.ValidateUnchanged();
                        node.Source.Id=node.Snapshot.Id;node.Source.ParentHolonId=node.Snapshot.ParentHolonId;
                        node.Source.Version=written.Result.Version;node.Source.VersionId=written.Result.VersionId;node.Source.PreviousVersionId=written.Result.PreviousVersionId;
                        if(node.Source is NextGenSoftware.OASIS.API.Core.Holons.Holon caller && written.Result is NextGenSoftware.OASIS.API.Core.Holons.Holon committed)caller.ChildRoleManifestJson=committed.ChildRoleManifestJson;
                        node.Source.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                        node.Source.ProviderUniqueStorageKey[Core.Enums.ProviderType.GoogleCloudOASIS]=node.Snapshot.Id.ToString();
                        var revision=CosmicConcurrency.GetProviderRevisionId(written.Result,Core.Enums.ProviderType.GoogleCloudOASIS);
                        if(revision.HasValue)CosmicConcurrency.SetProviderRevisionId(node.Source,Core.Enums.ProviderType.GoogleCloudOASIS,revision.Value);
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
            result.SavedCount=saved.Select(row=>row.Id).Concat(KnownFirestoreAcknowledgements(result.Exception)).Distinct().Count();
            if(result.IsError)result.ErrorCount=Math.Max(1,result.ErrorCount);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
            => DeleteCanonicalHolonAsync(id);

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
            => DeleteCanonicalHolonAsync(providerKey);

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
            => DeleteCanonicalHolonAsync(providerKey).GetAwaiter().GetResult();


    }
}
