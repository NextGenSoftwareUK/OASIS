using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace OASIS.ProviderTests.Fixtures;

// Shared native acceptance fixture: one canonical chain crosses the API's
// absolute query-depth boundary. No alternate graph representation is used.
public static class CqlDescendantAcceptance
{
    public static void Configure(Holon root)
    {
        root.Children.Clear();
        var parent=root;
        for(var depth=1;depth<=33;depth++)
        {
            var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=parent.Id,
                Name=depth==32?"boundary at32":depth==33?"boundary beyond32":"boundary level "+depth};
            parent.Children.Add(child);
            parent=child;
        }
    }

    public static async Task VerifyAsync(ICosmicQueryStorageProvider adapter,Holon root)
    {
        foreach(var item in new[]{
            (Depth:31,Nodes:31,Name:"boundary at32",Limit:false,Matches:0),
            (Depth:32,Nodes:32,Name:"boundary at32",Limit:false,Matches:1),
            (Depth:32,Nodes:32,Name:"boundary beyond32",Limit:false,Matches:0),
            (Depth:32,Nodes:31,Name:"boundary at32",Limit:true,Matches:0)})
        {
            var id=root.Id;var name=item.Name;
            var query=CosmicQuery<Holon>.Create().Where(owner=>owner.Id==id)
                .WhereAnyDescendant(child=>child.Name==name,
                    CosmicQueryTraversalOptions.Create(item.Depth,item.Nodes).Result).Take(1);
            if(query.IsError)throw new InvalidDataException(query.Message);
            var plan=CosmicQueryPlanner.Plan(query.Result,adapter.QueryProviderType,
                new[]{new CosmicQueryTarget(adapter.QueryProviderType,adapter.QueryCapabilities)});
            if(plan.IsError)throw new InvalidDataException(plan.Message);
            var read=await adapter.ExecuteCosmicQueryAsync(plan.Result,100,default);
            if(item.Limit)
            {
                if(read.ErrorCode!="CQL_QUERY_LIMIT" || read.Result!=null || read.IsLoaded)
                    throw new InvalidDataException("Depth32 scope must reject a31-node budget without publication: "+read.Message,read.Exception);
            }
            else if(read.IsError || read.Result.Count!=item.Matches || read.Result.Any(row=>row.Children.Any()))
                throw new InvalidDataException("Native depth boundary or leaf output differs from the admitted scope: "+read.Message,read.Exception);
        }
    }

    public static async Task VerifyMovedSubtreeAsync(ICosmicQueryStorageProvider adapter,Guid oldRoot,Guid nextRoot,
        bool declaredEmpty,string descendantName)
    {
        var ids=new[]{oldRoot,nextRoot};
        var query=CosmicQuery<Holon>.Create().Where(root=>ids.Contains(root.Id))
            .WhereAnyDescendant(child=>child.Name==descendantName,CosmicQueryTraversalOptions.Create(2,3).Result)
            .OrderBy(root=>root.Name).Take(2);
        if(query.IsError)throw new InvalidDataException(query.Message);
        var plan=CosmicQueryPlanner.Plan(query.Result,adapter.QueryProviderType,
            new[]{new CosmicQueryTarget(adapter.QueryProviderType,adapter.QueryCapabilities)});
        if(plan.IsError)throw new InvalidDataException(plan.Message);
        var read=await adapter.ExecuteCosmicQueryAsync(plan.Result,100,default);
        if(read.IsError || !read.Result.Select(row=>row.Id).SequenceEqual(declaredEmpty?Array.Empty<Guid>():new[]{nextRoot}))
            throw new InvalidDataException("Moved subtree must leave its old path and require new-owner membership: "+read.Message,read.Exception);
    }
}
