using NextGenSoftware.OASIS.API.Core.Enums;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS
{
    public class HolonResume
    {
        public Guid Id { get; set; }
        public string login { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
        public Guid ParentHolonId { get; set; }
        public HolonType HolonType { get; set; }
    }
}