namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Cargo
{
    public class OrderParams
    {
        public string ContractAddress { get; set; }
        public string VendorId { get; set; }
        public string SellerAddress { get; set; }
        public string BuyerAddress { get; set; }
        public string TokenId { get; set; }
        public string CrateId { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Limit { get; set; }
        public string Page { get; set; }
        
        public string AccessJwtToken { get; set; }
    }
}