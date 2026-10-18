using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        private static Func<IHolon,IHolon> CreateHolonProjector(IEnumerable<IHolon> records,bool loadChildren,
            bool recursive,int maxChildDepth,int initialDepth,bool childrenFromProvider)
        {
            return NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphProjection.Create(
                records,loadChildren,recursive,maxChildDepth,initialDepth,childrenFromProvider,
                source=>JsonConvert.DeserializeObject<Core.Holons.Holon>(JsonConvert.SerializeObject(source),new StoredHolonConverter()));
        }
    }
}
