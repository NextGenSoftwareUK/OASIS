namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Requests
{
    public class GetStaticTileRequest
    {
        public string Username { get; set; }
        public string StyleId { get; set; }
        public decimal TileSize { get; set; }
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal Z { get; set; }
    }
}