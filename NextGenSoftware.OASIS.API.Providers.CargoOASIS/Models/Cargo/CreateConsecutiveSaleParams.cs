namespace Models.Cargo
{
    public class CreateConsecutiveSaleParams
    {
        public string CrateId { get; set; }
        public string ContractAddress { get; set; }
        public string PricePerItem { get; set; }
        public string FormTokenId { get; set; }
        public string ToTokenId { get; set; }
    }
}