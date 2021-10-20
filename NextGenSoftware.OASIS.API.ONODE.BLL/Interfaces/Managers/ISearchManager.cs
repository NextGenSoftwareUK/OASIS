using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Interfaces
{
    public interface ISearchManager : IOASISManager
    {
        Task<ISearchResults> SearchAsync(ISearchParams searchParams, ProviderType provider = ProviderType.Default);
    }
}