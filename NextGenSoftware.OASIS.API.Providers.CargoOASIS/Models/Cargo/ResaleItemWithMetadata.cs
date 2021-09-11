namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class ResaleItemWithMetadata
    {
        public TokenMetadata TokenMetadata { get; set; }
        public bool FromVendor { get; set; }
        public string Price { get; set; }
        public string ResaleItemId { get; set; }
        public string SellerAddress { get; set; }
        public string TokenAddress { get; set; }
        public string TokenId { get; set; }
    }
}