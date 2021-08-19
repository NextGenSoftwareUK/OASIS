namespace Models
{
    public class Coordinate
    {
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        public override string ToString()
        {
            return $"{Longitude},{Latitude}";
        }
    }
}