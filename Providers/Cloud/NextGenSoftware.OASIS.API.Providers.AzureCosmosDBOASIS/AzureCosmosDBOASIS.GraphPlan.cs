using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;
public partial class AzureCosmosDBOASIS
{
    private static (List<IHolon> Roots,List<PlannedHolonWrite> Nodes) PlanCosmosGraph(IEnumerable<IHolon> input,bool children,bool recursive,int depth,int initialDepth,Action<IHolon> validateSource)
        => HolonGraphWritePlan.Create(input,children,recursive,depth,initialDepth,validateSource);

}
