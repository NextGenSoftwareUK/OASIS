using NextGenSoftware.OASIS.API.Core.Enums;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Objects
{
    public interface IPurchaseOlandRequest
    {
        Guid AvatarId { get; set; }
        string AvatarUsername { get; set; }
        List<Guid> OlandIds { get; set; }
        ProviderType ProviderType { get; set; }
        string Tiles { get; set; }
        string WalletAddress { get; set; }
    }
}