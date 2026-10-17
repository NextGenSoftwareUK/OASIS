using System.Globalization;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Models
{
    /// <summary>
    /// A position in GO Map's Unity world space. GO Map lays tiles on the XZ plane
    /// with Y as altitude, so this maps directly onto a UnityEngine.Vector3 on the
    /// client side without this assembly needing to reference UnityEngine.
    /// </summary>
    public class GOMapWorldPosition
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public GOMapWorldPosition() { }

        public GOMapWorldPosition(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double[] ToArray() => new[] { X, Y, Z };

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2})", X, Y, Z);
        }
    }
}
