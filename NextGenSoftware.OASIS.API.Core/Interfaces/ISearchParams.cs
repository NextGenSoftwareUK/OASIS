
namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface ISearchParams 
    {
        string SearchQuery { get; set; }
        bool SearchAllProviders { get; set; }
        bool SearchAvatarsOnly { get; set; }    
    }
}
