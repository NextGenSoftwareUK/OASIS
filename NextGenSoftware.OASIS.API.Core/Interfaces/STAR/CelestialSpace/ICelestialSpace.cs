using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ICelestialSpace : ICelestialHolon
    {
        //Task<OASISResult<ICelestialSpace>> SaveAsync<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialSpace, new();
        //OASISResult<ICelestialSpace> Save<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialSpace, new();
        Task<OASISResult<ICelestialSpace>> SaveAsync(bool saveChildren = true, bool recursive = true, bool continueOnError = true);
        OASISResult<ICelestialSpace> Save(bool saveChildren = true, bool recursive = true, bool continueOnError = true);
    }
}