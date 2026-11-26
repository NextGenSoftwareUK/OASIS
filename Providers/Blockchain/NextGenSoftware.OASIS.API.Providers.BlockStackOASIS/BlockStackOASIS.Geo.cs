using System;
using System.Collections.Generic;
using System.Globalization;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    public partial class BlockStackOASIS
    {
        /// <summary>Avatars whose stored Latitude/Longitude metadata lie within the radius (coordinates are micro-degrees).</summary>
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            var all = LoadAllAvatars();
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading BlockStack avatars: {all.Message}", all.Exception);
                return result;
            }
            var nearby = new List<IAvatar>();
            foreach (var avatar in all.Result)
                if (Within(avatar?.MetaData, geoLat, geoLong, radiusInMeters)) nearby.Add(avatar);
            result.Result = nearby;
            result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            return result;
        }

        /// <summary>Holons whose stored Latitude/Longitude metadata lie within the radius (coordinates are micro-degrees).</summary>
        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = LoadAllHolons(Type);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading BlockStack holons: {all.Message}", all.Exception);
                return result;
            }
            var nearby = new List<IHolon>();
            foreach (var holon in all.Result)
                if (Within(holon?.MetaData, geoLat, geoLong, radiusInMeters)) nearby.Add(holon);
            result.Result = nearby;
            result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            return result;
        }

        private static bool Within(IDictionary<string, object> metaData, long geoLat, long geoLong, int radiusInMeters)
            => metaData != null
               && metaData.TryGetValue("Latitude", out var latValue)
               && metaData.TryGetValue("Longitude", out var lngValue)
               && double.TryParse(latValue?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
               && double.TryParse(lngValue?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lng)
               && GeoHelper.CalculateDistance(geoLat / 1e6d, geoLong / 1e6d, lat, lng) <= radiusInMeters;
    }
}
