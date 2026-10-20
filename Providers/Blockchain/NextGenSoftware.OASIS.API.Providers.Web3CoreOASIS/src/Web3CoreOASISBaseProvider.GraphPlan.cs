using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
public partial class Web3CoreOASISBaseProvider
{
    private sealed record PlannedWeb3Holon(PlannedHolonWrite Write,StoredRecordPublicationSnapshot<IHolon> Publication)
    {
        public IHolon Source=>Write.Source;
        public Holon Snapshot=>Write.Snapshot;
    }

    private static (List<IHolon> Roots,List<PlannedWeb3Holon> Nodes) PlanWeb3Graph(IEnumerable<IHolon> input,bool children,bool recursive,int depth,int initialDepth)
    {
        var publications=new Dictionary<IHolon,StoredRecordPublicationSnapshot<IHolon>>(ReferenceEqualityComparer.Instance);
        var plan=HolonGraphWritePlan.Create(input,children,recursive,depth,initialDepth,
            source=>publications.Add(source,StoredRecordPublicationSnapshot<IHolon>.Capture(source)));
        return (plan.Roots,plan.Nodes.Select(node=>new PlannedWeb3Holon(node,publications[node.Source])).ToList());
    }

}
