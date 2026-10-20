using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

public partial class IPFSOASIS
{
    private async Task<OASISResult<IEnumerable<IHolon>>> SaveSelectedGraphAsync(IEnumerable<IHolon> input,
        bool children,bool recursive,int maximumDepth,int initialDepth,bool continueOnError)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        List<PlannedHolonWrite> nodes;
        var publications=new Dictionary<IHolon,StoredRecordPublicationSnapshot<Holon>>(ReferenceEqualityComparer.Instance);
        List<IHolon> roots;
        try
        {
            ArgumentNullException.ThrowIfNull(input);
            if(maximumDepth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(maximumDepth));
            roots=input.ToList();
            nodes=roots.Count==0?new List<PlannedHolonWrite>():HolonGraphWritePlan.Create(roots,children,recursive,maximumDepth,initialDepth,
                source=>{if(source.Id==Guid.Empty)throw new ArgumentException("Selected IPFS records require nonempty identities.");}).Nodes;
            foreach(var node in nodes)publications.Add(node.Source,StoredRecordPublicationSnapshot<Holon>.Capture(node.Source));
        }
        catch(Exception cause)
        {
            result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
            OASISErrorHandling.HandleError(ref result,"IPFS graph planning failed: "+cause.Message,cause);
            return result;
        }

        var committed=new List<string>();var failed=new HashSet<Guid>();var failures=new List<Exception>();
        foreach(var node in nodes)
        {
            if(failed.Contains(node.Snapshot.ParentHolonId)) {failed.Add(node.Snapshot.Id);continue;}
            try
            {
                publications[node.Source].ValidateUnchanged();
                await SaveHolonRecordAsync(node.Snapshot,node,publications[node.Source].ValidateUnchanged);
                var cid=node.Snapshot.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS];committed.Add(cid);
                publications[node.Source].ValidateUnchanged();
                node.Source.Id=node.Snapshot.Id;node.Source.ParentHolonId=node.Snapshot.ParentHolonId;
                node.Source.ProviderUniqueStorageKey=new Dictionary<Core.Enums.ProviderType,string>(node.Snapshot.ProviderUniqueStorageKey);
                node.Source.PreviousVersionProviderUniqueStorageKey=new Dictionary<Core.Enums.ProviderType,string>(node.Snapshot.PreviousVersionProviderUniqueStorageKey);
                if(node.Source is Holon caller)caller.ChildRoleManifestJson=node.Snapshot.ChildRoleManifestJson;
            }
            catch(Exception cause)
            {
                failed.Add(node.Snapshot.Id);failures.Add(cause);
                if(cause is IPFSRecordPublicationException publication) committed.AddRange(publication.AcknowledgedRecordCids);
                if(!continueOnError || NativeMutationReconciliation.IsRequired(cause)) break;
            }
        }
        if(failures.Count>0)
        {
            Exception cause=failures.Count==1 ? failures[0]:new AggregateException(failures);
            if(committed.Count>0) cause=new IPFSRecordPublicationException(committed.Distinct(StringComparer.Ordinal),cause);
            OASISErrorHandling.HandleError(ref result,"IPFS selected graph save failed: "+cause.Message,cause);
            return result;
        }
        result.Result=roots;result.IsSaved=true;return result;
    }
}
