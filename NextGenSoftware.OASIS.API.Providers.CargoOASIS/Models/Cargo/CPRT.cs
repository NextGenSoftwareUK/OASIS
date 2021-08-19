namespace Models.Cargo
{
    public class CPRT
    {
        public string ContractAddress { get; set; }
        public string ToTokenId { get; set; }
        public string Commission { get; set; }
        public string AmountToPurchase { get; set; }
        public string SellerAddress { get; set; }
        public string SaleId { get; set; }
        public string Nonce { get; set; }
        public string CrateId { get; set; }
        public string Signature { get; set; }
    }
}