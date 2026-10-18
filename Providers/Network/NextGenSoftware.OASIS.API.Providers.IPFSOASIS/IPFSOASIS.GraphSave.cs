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
        var nodes=new List<(IHolon Source,IHolon Stored,Guid? Parent)>();
        List<IHolon> roots;
        try
        {
            ArgumentNullException.ThrowIfNull(input);
            if(maximumDepth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(maximumDepth));
            roots=input.ToList();var references=new HashSet<IHolon>(ReferenceEqualityComparer.Instance);var ids=new HashSet<Guid>();
            void Visit(IHolon source,int depth,Guid? parent)
            {
                if(source==null || !references.Add(source)) throw new ArgumentException("Selected IPFS graph contains null/repeated nodes or a cycle.");
                var id=source.Id;
                if(id==Guid.Empty) throw new ArgumentException("Selected IPFS records require nonempty identities.");
                if(!ids.Add(id)) throw new ArgumentException("Selected IPFS graph has duplicate identities.");
                if(parent.HasValue && source.ParentHolonId!=Guid.Empty && source.ParentHolonId!=parent.Value)
                    throw new ArgumentException("Selected IPFS child has a conflicting parent identity.");
                // Reuse the canonical codec: navigation members are excluded, not copied into parent blobs.
                var stored=OasisJson.Deserialize<Holon>(OasisJson.Serialize<IHolon>(source));
                stored.Id=id;stored.ParentHolonId=parent ?? source.ParentHolonId;
                nodes.Add((source,stored,parent));
                if(children && (maximumDepth==0 || depth<maximumDepth) && (recursive || depth==initialDepth))
                    foreach(var child in source.Children ?? Enumerable.Empty<IHolon>()) Visit(child,depth+1,id);
            }
            foreach(var root in roots) Visit(root,initialDepth,null);
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
            if(node.Parent.HasValue && failed.Contains(node.Parent.Value)) {failed.Add(node.Stored.Id);continue;}
            try
            {
                await SaveHolonToFile(node.Stored);
                var cid=node.Stored.ProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS];committed.Add(cid);
                node.Source.Id=node.Stored.Id;node.Source.ParentHolonId=node.Stored.ParentHolonId;
                node.Source.ProviderUniqueStorageKey=new Dictionary<Core.Enums.ProviderType,string>(node.Stored.ProviderUniqueStorageKey);
                node.Source.PreviousVersionProviderUniqueStorageKey=new Dictionary<Core.Enums.ProviderType,string>(node.Stored.PreviousVersionProviderUniqueStorageKey);
            }
            catch(Exception cause)
            {
                failed.Add(node.Stored.Id);failures.Add(cause);
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
