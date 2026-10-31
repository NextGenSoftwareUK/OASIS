using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS
{
    public partial class GoogleCloudOASIS
    {
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
            => ReadNearbyAsync(geoLat,geoLong,radiusInMeters,()=>LoadAllAvatarsAsync()).GetAwaiter().GetResult();

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
            => ReadNearbyAsync(geoLat,geoLong,radiusInMeters,()=>LoadAllHolonsAsync(HolonType.All,loadChildren:false),Type).GetAwaiter().GetResult();

        /// <summary>Authentication belongs to AvatarManager, not the provider storage interface.</summary>
        [Obsolete("Authenticate through AvatarManager, then use authorized ID-based storage reads.")]
        public Task<OASISResult<IHolon>> LoadHolonAsync(string username, string password, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result=new OASISResult<IHolon>();
            var cause=new NotSupportedException("Credential-based holon reads are not supported by the storage provider. Authenticate through AvatarManager and use authorized ID-based reads.");
            OASISErrorHandling.HandleError(ref result,cause.Message,cause);
            result.ErrorCode="OASIS_CREDENTIAL_STORAGE_READ_UNSUPPORTED";
            return Task.FromResult(result);
        }
    }
}
