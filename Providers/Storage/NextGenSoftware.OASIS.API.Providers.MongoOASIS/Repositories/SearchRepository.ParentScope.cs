using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class SearchRepository
{
    private async Task<HashSet<Guid>> ReadSearchParentScopeAsync(Guid parent,bool recursive,bool synchronous)
    {
        // Project only current nondeleted links, across types: a matching descendant may
        // be reached through an ancestor excluded by the eventual group's type predicate.
        var query=_dbContext.Holon.Find(HolonRepository.BuildHolonTypeFilter(HolonType.All))
            .Project(row=>new {Id=row.HolonId,Parent=row.ParentHolonId});
        var links=synchronous ? query.ToList():await query.ToListAsync().ConfigureAwait(false);
        var children=links.ToLookup(row=>row.Parent);
        var visited=new HashSet<Guid> {parent};var scope=new HashSet<Guid>();
        var pending=new Queue<Guid>();pending.Enqueue(parent);
        while(pending.Count>0)
            foreach(var child in children[pending.Dequeue()])
            {
                if(child.Id==Guid.Empty || !visited.Add(child.Id))
                    throw new InvalidDataException("Mongo search parent scope contains an invalid, duplicate or cyclic identity.");
                scope.Add(child.Id);
                if(recursive) pending.Enqueue(child.Id);
            }
        return scope;
    }
}
