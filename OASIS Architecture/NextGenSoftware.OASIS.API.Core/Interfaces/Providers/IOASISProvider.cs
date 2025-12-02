using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISProvider
    {
        string ProviderName { get; set; }
        string ProviderDescription { get; set; }
        
        //TODO: Phase this out
        EnumValue<ProviderCategory> ProviderCategory { get; set; } //= new EnumValue<ProviderCategory>(Enums.ProviderCategory.SmartContract);
        
        public List<EnumValue<ProviderCategory>> ProviderCategories { get; set; }
        EnumValue<ProviderType> ProviderType { get; set; }
        //bool Activated { get; private set; } //TODO: Use this when upgrade to C# 8.0 (.NET Core 3.0).
        bool IsProviderActivated { get; set; }
        OASISResult<bool> ActivateProvider();
        Task<OASISResult<bool>> ActivateProviderAsync();
        OASISResult<bool> DeActivateProvider();
        Task<OASISResult<bool>> DeActivateProviderAsync();
    }
}