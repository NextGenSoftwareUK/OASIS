using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Nearby query: request coordinates in microdegrees, canonical metadata in degrees, radius in metres.</summary>
public sealed class StoredRecordLocationQuery
{
    private readonly double _latitude,_longitude;
    private readonly int _radius;
    public StoredRecordLocationQuery(long latitude,long longitude,int radius)
    {
        if(latitude < -90000000 || latitude > 90000000) throw new ArgumentOutOfRangeException(nameof(latitude));
        if(longitude < -180000000 || longitude > 180000000) throw new ArgumentOutOfRangeException(nameof(longitude));
        if(radius<0) throw new ArgumentOutOfRangeException(nameof(radius));
        _latitude=latitude/1e6d;_longitude=CanonicalLongitude(_latitude,longitude/1e6d);_radius=radius;
    }

    public List<T> Select<T>(IEnumerable<T> records,Func<T,bool> include=null) where T:IHolonBase
    {
        if(records==null) throw new InvalidDataException("Nearby source collection is missing.");
        var selected=new List<T>();
        foreach(var record in records)
        {
            if(record==null) throw new InvalidDataException("Nearby source contains a null record.");
            if(include!=null && !include(record)) continue;
            var metadata=record.MetaData;
            if(metadata==null) continue;
            var hasLatitude=metadata.TryGetValue("Latitude",out var lat);
            var hasLongitude=metadata.TryGetValue("Longitude",out var lon);
            if(!hasLatitude && !hasLongitude) continue;
            if(!hasLatitude || !hasLongitude
                || !double.TryParse(Convert.ToString(lat,CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out var latitude)
                || !double.TryParse(Convert.ToString(lon,CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out var longitude)
                || !double.IsFinite(latitude) || !double.IsFinite(longitude) || latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
                throw new InvalidDataException("Stored record requires valid Latitude/Longitude degree metadata.");
            var distance=GeoHelper.CalculateDistance(_latitude,_longitude,latitude,CanonicalLongitude(latitude,longitude));
            if(!double.IsFinite(distance)) throw new ArithmeticException("Nearby distance calculation did not return a finite result.");
            if(distance<=_radius) selected.Add(record);
        }
        return selected;
    }

    private static double CanonicalLongitude(double latitude,double longitude)
        => Math.Abs(latitude)==90 ? 0:longitude==180 ? -180:longitude;
}
