using NextGenSoftware.OASIS.API.Core.Enums;
using System;

namespace NextGenSoftware.OASIS.API.ONode.Core.Objects
{
    public class PurchaseOlandRequest
    {
        public Guid OlandId { get; set; }
        public Guid AvatarId { get; set; }
        public string AvatarUsername { get; set; }
        public string Tiles { get; set; }
        public string WalletAddress { get; set; }
       // public string CargoSaleId { get; set; }
        public ProviderType ProviderType { get; set; }
    }
}
