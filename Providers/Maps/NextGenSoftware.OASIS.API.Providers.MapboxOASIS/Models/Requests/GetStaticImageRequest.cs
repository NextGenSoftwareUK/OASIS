namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Requests
{
    public class GetStaticImageRequest
    {
        public string Username { get; set; }
        public string StyleId { get; set; }
        public string Overlay { get; set; }
        public decimal Zoom { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public decimal Bearing { get; set; }
        public decimal Pitch { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}