using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS
{
    public partial class CosmosBlockChainOASIS
    {
        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                var query = new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordLocationQuery(geoLat, geoLong, radiusInMeters);
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, IEnumerable<IAvatar>>(activateResult);
                    }
                }

                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError) return avatarsResult;
                var nearby = query.Select(avatarsResult.Result, row => row.IsActive && !row.IsDeleted);

                response.Result = nearby;
                response.IsError = false;
                response.IsLoaded = true;
                response.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error getting avatars near me from Cosmos: {ex.Message}", ex);
            }

            return response;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                var query = new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordLocationQuery(geoLat, geoLong, radiusInMeters);
                if (!Enum.IsDefined(typeof(HolonType), Type)) throw new ArgumentOutOfRangeException(nameof(Type));
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, IEnumerable<IHolon>>(activateResult);
                    }
                }

                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError) return holonsResult;
                var nearby = query.Select(holonsResult.Result,
                    row => row.IsActive && !row.IsDeleted && (Type == HolonType.All || row.HolonType == Type));

                response.Result = nearby;
                response.IsError = false;
                response.IsLoaded = true;
                response.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near me from Cosmos: {ex.Message}", ex);
            }

            return response;
        }
    }
}
