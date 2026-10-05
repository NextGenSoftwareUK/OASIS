using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class MintTokenRequestBase : IMintTokenRequestBase
    {
        public Dictionary<string, object> MetaData { get; set; }
        public List<string> Tags { get; set; }
        public string Symbol { get; set; }
        public Guid MintedByAvatarId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string MemoText { get; set; }
        //public EnumValue<ProviderType> ProviderType { get; set; }
    }
}