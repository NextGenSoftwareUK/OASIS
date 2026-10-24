using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS;

public partial class BigQueryOASIS
{
    private async Task<OASISResult<IHolon>> SaveSelectedGraphAsync(IHolon source,bool children,bool recursive,int maximum)
    {
        if(source==null)
        {
            var invalid=new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref invalid,"BigQuery holon source must not be null.",new ArgumentNullException(nameof(source)));
            return invalid;
        }
        var graph=await WriteGraphAsync(new[]{source},children,recursive,maximum,0);
        var result=Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(graph);
        if(!graph.IsError)result.Result=source;
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> WriteGraphAsync(IEnumerable<IHolon> sources,bool children,bool recursive,int maximum,int initial)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();var acknowledged=new List<string>();var published=new List<IHolon>();
        try
        {
            ArgumentNullException.ThrowIfNull(sources);ValidateGraphDepth(maximum,initial);
            var roots=sources.ToArray();
            if(roots.Length==0){result.Result=Array.Empty<IHolon>();result.IsSaved=true;return result;}
            var plan=HolonGraphWritePlan.Create(roots,children,recursive,maximum,initial);
            var publications=new Dictionary<IHolon,StoredRecordPublicationSnapshot<Holon>>(ReferenceEqualityComparer.Instance);
            foreach(var node in plan.Nodes)publications.Add(node.Source,StoredRecordPublicationSnapshot<Holon>.Capture(node.Source));
            foreach(var node in plan.Nodes)
            {
                var publication=publications[node.Source];publication.ValidateUnchanged();
                var id=node.Snapshot.Id;string? original=null;
                var observed=UniqueRecord(await ReadRowsAsync<Holon>($"SELECT doc FROM {Table(CollHolons)} WHERE id = @id LIMIT 2",new[]{StringParameter("id",id.ToString())},(_,raw)=>original=raw));
                if(observed!=null&&observed.Id!=id)throw new InvalidDataException("BigQuery graph native identity differs from its canonical record.");
                if(observed?.IsDeleted==true&&!node.Snapshot.IsDeleted)throw new InvalidOperationException("A deleted BigQuery record cannot be implicitly resurrected by graph save.");
                node.Snapshot.ChildRoleManifestJson=HolonChildRolePersistence.Resolve(node,HolonChildRoleWriteKind.SourceSave,observed!=null,observed?.ChildRoleManifestJson);
                node.Snapshot.ProviderUniqueStorageKey??=new();node.Snapshot.ProviderUniqueStorageKey[Core.Enums.ProviderType.BigQueryOASIS]=id.ToString();
                publication.ValidateUnchanged();BigQueryWriteAcknowledgement? receipt=null;
                try
                {
                    receipt=await UpsertDocAsync(CollHolons,id.ToString(),node.Snapshot,checkOriginal:true,expectedDoc:original);
                    acknowledged.Add(id.ToString());
                    publication.ValidateUnchanged();
                    node.Source.Id=id;node.Source.ParentHolonId=node.Snapshot.ParentHolonId;
                    node.Source.ProviderUniqueStorageKey??=new();node.Source.ProviderUniqueStorageKey[Core.Enums.ProviderType.BigQueryOASIS]=id.ToString();
                    if(node.Source is Holon concrete)concrete.ChildRoleManifestJson=node.Snapshot.ChildRoleManifestJson;
                    published.Add(node.Source);
                }
                catch(Exception cause)
                {
                    if(receipt!=null)throw new BigQueryRecordReconciliationException(id,_projectId,receipt.JobId,receipt.Location,true,cause);
                    throw;
                }
            }
            result.Result=plan.Roots;result.IsSaved=true;
        }
        catch(Exception cause)
        {
            result.Result=published;
            if(acknowledged.Count>0&&!(cause is BigQueryRecordReconciliationException native&&acknowledged.Count==1&&native.WriteAcknowledged&&native.RecordId.ToString()==acknowledged[0]))
                cause=new KeyValuePartialMutationException(Core.Enums.ProviderType.BigQueryOASIS,acknowledged,cause);
            OASISErrorHandling.HandleError(ref result,"BigQuery graph write failed; reconcile original jobs and acknowledged records before replay.",cause);
        }
        result.SavedCount=acknowledged.Count;
        return result;
    }
}
