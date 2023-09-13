
namespace NextGenSoftware.OASIS.API.Core.Objects.Search
{
    public class SearchTextGroup : SearchGroupBase, ISearchTextGroup
    {
        public string SearchQuery { get; set; }
        //public bool SearchAllProviders { get; set; }
        public bool SearchIds { get; set; } //Will search in all Guids for the given SearchQuery.
        public bool SearchProviderKeys { get; set; } //Will search in all ProviderKeys for the given SearchQuery.
    }
}