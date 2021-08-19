using Enums;

namespace Models.Requests
{
    public class GetVectorTileRequest
    {
        public string TilesetId { get; set; }
        public decimal Zoom { get; set; }
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public VectorTileFormat Format { get; set; }
    }
}