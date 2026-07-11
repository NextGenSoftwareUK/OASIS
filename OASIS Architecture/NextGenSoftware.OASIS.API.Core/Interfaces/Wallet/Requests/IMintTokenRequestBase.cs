using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests
{
    public interface IMintTokenRequestBase
    {
        public Dictionary<string, string> MetaData { get; set; }
        public List<string> Tags { get; set; }
        public string Symbol { get; set; }
        public Guid MintedByAvatarId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string MemoText { get; set; }
        public decimal Amount { get; set; }
        //public string ToWalletAddress { get; set; }
        //public EnumValue<ProviderType> ProviderType { get; set; }
    }
}