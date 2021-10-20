using NextGenSoftware.OASIS.API.Core.Interfaces;
using OASIS_Onion.Model.Interfaces;
using OASIS_Onion.Model.Models;
using System.Threading.Tasks;

namespace OASIS_Onion.Repository.Interface
{
    public interface ISearchDataRepository : IEntityRepository<SearchData>
    {
        Task<ISearchResults> SearchAsync(ISearchParams searchTerm);
    }
}