using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Objects;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Objects
{
    public class PurchaseOlandRequest : IPurchaseOlandRequest
    {
        public List<Guid> OlandIds { get; set; }
        public Guid AvatarId { get; set; }
        public string AvatarUsername { get; set; }
        public string Tiles { get; set; }
        public string WalletAddress { get; set; }
        public ProviderType ProviderType { get; set; }
    }
}
