
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core
{
    public abstract class OASISProvider : IOASISProvider
    {
        public string ProviderName { get; set; }
        public string ProviderDescription { get; set; }

        public ProviderCategory ProviderCategory { get; set; }

        public ProviderType ProviderType { get; set; }
        public bool ProviderActivated { get; set; }

        virtual public void ActivateProvider()
        {
            ProviderActivated = true;
        }

        virtual public void DeActivateProvider()
        {
            ProviderActivated = false;
        }
    }
}
