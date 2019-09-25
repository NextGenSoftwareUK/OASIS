
namespace NextGenSoftware.OASIS.API.Core
{
    public class OASISProvider : IOASISProvider
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public ProviderCat Category { get; set; }

        public ProviderType Type { get; set; }
        public bool Activated { get; set; }

        virtual public void ActivateProvider()
        {
            Activated = true;
        }

        virtual public void DeActivateProvider()
        {
            Activated = false;
        }
    }
}
