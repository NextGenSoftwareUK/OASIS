using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces
{
    public interface IOlandRepository
    {
        Task<Oland> AddOlandAsync(Oland oland);
        Oland AddOland(Oland oland);

        Task<Oland> UpdateOlandAsync(Oland oland);
        Oland UpdateOland(Oland oland);

        Task<bool> DeleteOlandAsync(int id, bool safeDelete); 
        bool DeleteOland(int id, bool safeDelete);

        Task<Oland> GetOlandAsync(int id);
        Oland GetOland(int id);

        Task<IEnumerable<Oland>> GetAllOlandsAsync();
        IEnumerable<Oland> GetAllOlands();
    }
}