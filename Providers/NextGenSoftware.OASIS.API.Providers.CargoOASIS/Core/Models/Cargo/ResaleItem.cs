namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class ResaleItem
    {
        public string SellerAddress { get; set; }
        public string TokenAddress { get; set; }
        public string TokenId { get; set; }
        public string ResaleItemId { get; set; }
        public string Price { get; set; }
        public string FromVendor { get; set; }
        public TokenMetadata Metadata { get; set; }
    }
}