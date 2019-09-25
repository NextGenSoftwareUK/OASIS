
namespace NextGenSoftware.OASIS.API.Core
{
    public interface IOASISProvider
    {
        string Name { get; set; }
        string Description { get; set; }
        ProviderCat Category { get; set; }
        ProviderType Type { get; set; }
        //bool Activated { get; private set; } //TODO: Use this when upgrade to C# 8.0 (.NET Core 3.0).
        bool Activated { get; set; }
        //virtual void ActivateProvider();
        //virtual void DeActivateProvider();
        void ActivateProvider();
        void DeActivateProvider();


    }
}
