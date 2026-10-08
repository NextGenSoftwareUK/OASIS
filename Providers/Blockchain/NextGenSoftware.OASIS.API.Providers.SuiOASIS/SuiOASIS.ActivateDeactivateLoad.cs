using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS
{
    public partial class SuiOASIS
    {










        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (radiusInMeters < 0 || geoLat < -90000000 || geoLat > 90000000
                    || geoLong < -180000000 || geoLong > 180000000)
                    throw new ArgumentOutOfRangeException(nameof(radiusInMeters), "Valid coordinates and nonnegative radius are required.");
                result = LoadAllAvatarsAsync().GetAwaiter().GetResult();
                if (result.IsError) return result;
                result.Result = result.Result.Where(item => item.MetaData != null
                    && item.MetaData.TryGetValue("Latitude", out var lat)
                    && item.MetaData.TryGetValue("Longitude", out var lon)
                    && GeoHelper.CalculateDistance(geoLat / 1000000d, geoLong / 1000000d,
                        Convert.ToDouble(lat, System.Globalization.CultureInfo.InvariantCulture),
                        Convert.ToDouble(lon, System.Globalization.CultureInfo.InvariantCulture)) <= radiusInMeters).ToList();
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType holonType)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (radiusInMeters < 0 || geoLat < -90000000 || geoLat > 90000000
                    || geoLong < -180000000 || geoLong > 180000000)
                    throw new ArgumentOutOfRangeException(nameof(radiusInMeters), "Valid coordinates and nonnegative radius are required.");
                result = LoadAllHolonsAsync(holonType).GetAwaiter().GetResult();
                if (result.IsError) return result;
                result.Result = result.Result.Where(item => item.MetaData != null
                    && item.MetaData.TryGetValue("Latitude", out var lat)
                    && item.MetaData.TryGetValue("Longitude", out var lon)
                    && GeoHelper.CalculateDistance(geoLat / 1000000d, geoLong / 1000000d,
                        Convert.ToDouble(lat, System.Globalization.CultureInfo.InvariantCulture),
                        Convert.ToDouble(lon, System.Globalization.CultureInfo.InvariantCulture)) <= radiusInMeters).ToList();
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            return result;
        }













        /// <summary>
        /// Parse Sui blockchain response to Avatar object
        /// </summary>


        /// <summary>
        /// Create Avatar from Sui response when JSON deserialization fails
        /// </summary>

        /// <summary>
        /// Extract property value from Sui JSON response
        /// </summary>

        /// <summary>
        /// Convert Avatar to Sui blockchain format
        /// </summary>

        /// <summary>
        /// Convert Holon to Sui blockchain format
        /// </summary>



    }
}
