namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class GetOrderParams
    {
        public string Limit { get; set; }
        public string Page { get; set; }
        public string VendorId { get; set; }
        public string ContractAddress { get; set; }
        public string SellerAddress { get; set; }
        public string BuyerAddress { get; set; }
        public string TokenId { get; set; }
        public string CrateId { get; set; }
    }
}