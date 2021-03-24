
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISProvider
    {
        string ProviderName { get; set; }
        string ProviderDescription { get; set; }
        EnumValue<ProviderCategory> ProviderCategory { get; set; }
        EnumValue<ProviderType> ProviderType { get; set; }
        //bool Activated { get; private set; } //TODO: Use this when upgrade to C# 8.0 (.NET Core 3.0).
        bool ProviderActivated { get; }
        //virtual void ActivateProvider();
        //virtual void DeActivateProvider();
        void ActivateProvider();
        void DeActivateProvider();
    }
}
