using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Interfaces
{
    public interface IHolonRepository
    {
        //Task<List<HolonEntity>> GetHolons();
        //Task<HolonEntity> GetHolonById(Guid holonId);
        //Task<HolonEntity> CreateHolon(HolonEntity request);
        //Task<HolonEntity> UpdateHolon(HolonEntity request);
        //Task<bool> DeleteHolonById(Guid holonId);


        List<HolonEntity> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);

        Task<List<HolonEntity>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);

        List<HolonEntity> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);

        Task<List<HolonEntity>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);

        List<HolonEntity> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);

        Task<List<HolonEntity>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);

        List<HolonEntity> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);

        Task<List<HolonEntity>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);

        List<HolonEntity> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);

        Task<List<HolonEntity>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);

        HolonEntity SaveHolon(HolonEntity holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);

        Task<HolonEntity> SaveHolonAsync(HolonEntity holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);

        HolonEntity SaveHolons(IEnumerable<HolonEntity> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true);

        Task<HolonEntity> SaveHolonsAsync(IEnumerable<HolonEntity> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true);

        bool DeleteHolon(Guid id, bool softDelete = true);

        Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true);

        bool DeleteHolon(string providerKey, bool softDelete = true);

        Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true);
        List<HolonEntity> GetHolonsNearMe(HolonType Type);
    }
}
