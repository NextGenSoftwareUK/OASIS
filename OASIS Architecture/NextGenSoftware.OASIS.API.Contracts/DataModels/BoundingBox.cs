using System.Globalization;

namespace NextGenSoftware.OASIS.API.Contracts.Interfaces
{
    /// <summary>
    /// Bounding box data model for geographic areas
    /// </summary>
    public class BoundingBox
    {
        public Geolocation MinPoint { get; set; }
        public Geolocation MaxPoint { get; set; }

        public override string ToString()
        {
            return $"{MinPoint.ToString()},{MaxPoint.ToString()}";
        }
    }
}
