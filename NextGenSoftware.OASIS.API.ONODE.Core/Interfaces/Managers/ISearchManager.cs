using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface ISearchManager : IOASISManager
    {
        Task<ISearchResults> SearchAsync(ISearchParams searchParams, ProviderType provider = ProviderType.Default);
    }
}