using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        private static Func<IHolon,IHolon> CreateHolonProjector(IEnumerable<IHolon> records,bool loadChildren,
            bool recursive,int maxChildDepth,int initialDepth,bool childrenFromProvider,bool rootsAreChildren=false)
        {
            return NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphProjection.Create(
                records,loadChildren,recursive,maxChildDepth,initialDepth,childrenFromProvider,
                source=>JsonConvert.DeserializeObject<Core.Holons.Holon>(JsonConvert.SerializeObject(source),new StoredHolonConverter()),rootsAreChildren:rootsAreChildren);
        }

        private static OASISResult<IEnumerable<IHolon>> ProjectParentSnapshot(OASISResult<IEnumerable<IHolon>> snapshot,Guid parent,
            HolonType type,bool children,bool recursive,int maximum,int initial)
        {
            if(snapshot.IsError)return snapshot;
            try{
                var rows=snapshot.Result?.ToList()??throw new InvalidDataException("Ethereum parent snapshot has no record collection.");
                // LoadAll already hydrated independent descendants from its frozen native block.
                // Reproject those captured memberships, not a second block read or historical-root index.
                var project=CreateHolonProjector(rows,children,recursive,maximum,initial,false,rootsAreChildren:true);
                snapshot.Result=rows.Where(row=>row.ParentHolonId==parent&&(type==HolonType.All||row.HolonType==type)).Select(project).ToList();
            }catch(Exception cause){snapshot.Result=null;snapshot.IsLoaded=false;OASISErrorHandling.HandleError(ref snapshot,"Ethereum parent projection failed.",cause);}
            return snapshot;
        }
    }
}
