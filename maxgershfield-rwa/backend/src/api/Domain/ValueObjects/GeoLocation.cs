using System.Globalization;

namespace Domain.ValueObjects;

public record GeoLocation(double Latitude, double Longitude)
{
    public override string ToString() => $"{Latitude},{Longitude}";

    public static GeoLocation Parse(string value)
    {
        var parts = value.Split(',');
        return new GeoLocation(
            double.Parse(parts[0], CultureInfo.InvariantCulture),
            double.Parse(parts[1], CultureInfo.InvariantCulture)
        );
    }
}