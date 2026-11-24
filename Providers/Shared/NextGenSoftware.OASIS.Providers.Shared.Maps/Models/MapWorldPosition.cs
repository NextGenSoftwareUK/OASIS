using System.Globalization;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Models
{
    /// <summary>
    /// A position in a client's local world space, in metres east (X), up (Y) and
    /// north (Z) of the map origin - the layout Unity and most 3D engines use.
    /// </summary>
    public sealed class MapWorldPosition
    {
        public MapWorldPosition(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2})", X, Y, Z);
    }
}
