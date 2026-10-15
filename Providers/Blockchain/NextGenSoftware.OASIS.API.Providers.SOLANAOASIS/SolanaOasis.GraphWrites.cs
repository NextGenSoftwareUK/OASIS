using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
    private sealed record PlannedSolanaHolon(IHolon Source,SolanaHolonDto Record,string PriorSignature);

    private static (List<IHolon> Roots,List<PlannedSolanaHolon> Nodes) PlanSolanaGraph(IEnumerable<IHolon> input,
        bool saveChildren,bool recursive,int maxDepth,int initialDepth)
    {
        ArgumentNullException.ThrowIfNull(input);
        if(maxDepth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
        var roots=input.ToList();
        var references=new HashSet<IHolon>(ReferenceEqualityComparer.Instance);
        var identities=new HashSet<Guid>();
        var nodes=new List<PlannedSolanaHolon>();
        void Visit(IHolon source,int depth,Guid? parent)
        {
            if(source == null || !references.Add(source)) throw new ArgumentException("Selected Solana graph has a null/repeated node or cycle.");
            string prior=null;
            if(source.ProviderUniqueStorageKey?.TryGetValue(Core.Enums.ProviderType.SolanaOASIS,out prior)==true && string.IsNullOrWhiteSpace(prior))
                throw new ArgumentException("Existing Solana storage signature must be nonblank.");
            if(source.Id==Guid.Empty && prior != null) throw new ArgumentException("An existing Solana storage reference requires its actual identity.");
            var id=source.Id==Guid.Empty ? Guid.NewGuid():source.Id;
            if(!identities.Add(id)) throw new ArgumentException("Selected Solana graph contains duplicate identities.");
            if(source.Version<0) throw new ArgumentException("Solana record version must be nonnegative.");
            if(parent.HasValue && source.ParentHolonId!=Guid.Empty && source.ParentHolonId!=parent.Value)
                throw new ArgumentException("Selected Solana child has a conflicting parent identity.");
            var record=JsonConvert.DeserializeObject<SolanaHolonDto>(JsonConvert.SerializeObject(source.GetSolanaHolonDto()));
            record.Id=id; record.ParentHolonId=parent ?? source.ParentHolonId;
            nodes.Add(new PlannedSolanaHolon(source,record,prior));
            if(saveChildren && (maxDepth==0 || depth<maxDepth) && (recursive || depth==initialDepth))
                foreach(var child in source.Children ?? Enumerable.Empty<IHolon>()) Visit(child,depth+1,id);
        }
        foreach(var root in roots) Visit(root,initialDepth,null);
        return (roots,nodes);
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> SavePlannedSolanaGraphAsync(IEnumerable<IHolon> input,
        bool saveChildren,bool recursive,int maxDepth,int initialDepth)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        var committed=new List<string>();
        try
        {
            var plan=PlanSolanaGraph(input,saveChildren,recursive,maxDepth,initialDepth);
            if(plan.Nodes.Count==0) {result.Result=plan.Roots;result.IsSaved=true;return result;}
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) throw new InvalidOperationException(activation.Message,activation.Exception);
            }
            // Resolve every selected existing reference before the first mutation.
            foreach(var node in plan.Nodes.Where(node=>node.PriorSignature != null))
            {
                var stored=await _solanaRepository.GetAsync<SolanaHolonDto>(node.PriorSignature);
                if(stored == null || stored.Id!=node.Record.Id) throw new InvalidOperationException("Solana holon storage reference does not match the requested identity.");
                if(stored.Version<0 || stored.Version==int.MaxValue) throw new ArgumentException("Stored Solana version cannot advance.");
                node.Record.Version=stored.Version;
            }
            // Publish stable identity assignments only after the whole selected graph validates.
            foreach(var node in plan.Nodes) {node.Source.Id=node.Record.Id;node.Source.ParentHolonId=node.Record.ParentHolonId;}
            foreach(var node in plan.Nodes)
            {
                var signature=node.PriorSignature == null ? await _solanaRepository.CreateAsync(node.Record):await _solanaRepository.UpdateAsync(node.Record);
                if(string.IsNullOrWhiteSpace(signature)) throw new InvalidOperationException("Solana storage returned no committed signature.");
                committed.Add(signature);
                node.Source.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                node.Source.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS]=signature;
                node.Source.Version=node.Record.Version; node.Source.PreviousVersionId=node.Record.PreviousVersionId;
            }
            result.Result=plan.Roots; result.IsSaved=true;
        }
        catch(Exception cause)
        {
            var evidence=committed.Count>0 ? new SolanaPartialMutationException(committed,cause):cause;
            OASISErrorHandling.HandleError(ref result,"Solana selected graph save failed: "+cause.Message,evidence);
        }
        return result;
    }
}
