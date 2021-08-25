using Models.Cargo;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class Token
    {
        public string TokenUrl { get; set; }
        public string Owner { get; set; }
        public TokenMetadata Metadata { get; set; }
        public bool SupportsBatchMint { get; set; }
        public string TokenId { get; set; }
        public string Imprint { get; set; }

        public string Contract { get; set; }
        public string StakedAmount { get; set; }
        public TokenDetail Detail { get; set; }
    }
}