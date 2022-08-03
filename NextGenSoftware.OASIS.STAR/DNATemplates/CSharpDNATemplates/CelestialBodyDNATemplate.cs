using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates
{
    public class CelestialBodyDNATemplate : CELESTIALBODY, ICELESTIALBODY
    {
        //public CelestialBodyDNATemplate(string providerKey, ProviderType providerType, bool autoLoad = true) : base(providerKey, providerType, autoLoad)
        //{

        //}

        //public CelestialBodyDNATemplate(Guid id, bool autoLoad = true) : base(id, autoLoad)
        //{

        //}

        public CelestialBodyDNATemplate() : base(new Guid("ID"))
        {

        }

        public async Task<OASISResult<HOLON>> LoadHOLONAsync(Guid id)
        {
            return await base.CelestialBodyCore.LoadHolonAsync<HOLON>(id);
        }

        public OASISResult<HOLON> LoadHOLON(Guid id)
        {
            return base.CelestialBodyCore.LoadHolon<HOLON>(id);
        }

        //public async Task<OASISResult<T>> LoadHOLONAsync<T>(Guid id) where T : IHolon
        //{
        //    return await base.CelestialBodyCore.LoadHolonAsync<T>(id);
        //}

        //public OASISResult<T> LoadHOLON<T>(Guid id) where T : IHolon
        //{
        //    return base.CelestialBodyCore.LoadHolon<T>(id);
        //}

        //public async Task<OASISResult<IHolon>> LoadHOLONAsync(Guid id)
        //{
        //    return await base.CelestialBodyCore.LoadHolonAsync(id);
        //}

        //public OASISResult<IHolon> LoadHOLON(Guid id)
        //{
        //    return base.CelestialBodyCore.LoadHolon(id);
        //}

        public async Task<OASISResult<HOLON>> LoadHOLONAsync(ProviderType providerType, string providerKey)
        {
            return await base.CelestialBodyCore.LoadHolonAsync<HOLON>(providerType, providerKey);
        }

        public OASISResult<HOLON> LoadHOLON(ProviderType providerType, string providerKey)
        {
            return base.CelestialBodyCore.LoadHolon<HOLON>(providerType, providerKey);
        }

        //public async Task<OASISResult<T>> LoadHOLONAsync<T>(ProviderType providerType, string providerKey) where T : IHolon
        //{
        //    return await base.CelestialBodyCore.LoadHolonAsync<T>(providerKey, providerType);
        //}

        //public OASISResult<T> LoadHOLON(ProviderType providerType, string providerKey)
        //{
        //    return base.CelestialBodyCore.LoadHolon<T>(providerKey, providerType);
        //}

        //public async Task<OASISResult<IHolon>> LoadHOLONAsync(ProviderType providerType, string providerKey)
        //{
        //    return await base.CelestialBodyCore.LoadHolonAsync(providerKey, providerType);
        //}

        //public OASISResult<IHolon> LoadHOLON(ProviderType providerType, string providerKey)
        //{
        //    return base.CelestialBodyCore.LoadHolon(providerKey, providerType);
        //}

        public async Task<OASISResult<HOLON>> SaveHOLONAsync(HOLON holon)
        {
            return await base.CelestialBodyCore.SaveHolonAsync<HOLON>(holon);
        }

        public OASISResult<HOLON> SaveHOLON(HOLON holon)
        {
            return base.CelestialBodyCore.SaveHolon<HOLON>(holon);
        }

        //public async Task<OASISResult<T>> SaveHOLONAsync<T>(T holon) where T : IHolon
        //{
        //    return await base.CelestialBodyCore.SaveHolonAsync<T>(holon);
        //}

        //public OASISResult<T> SaveHOLON<T>(HOLON holon) where T : IHolon
        //{
        //    return base.CelestialBodyCore.SaveHolon<HOLON>(holon);
        //}

        //public async Task<OASISResult<IHolon>> SaveHOLONAsync(IHolon holon)
        //{
        //    return await base.CelestialBodyCore.SaveHolonAsync(holon);
        //}

        //public OASISResult<IHolon> SaveHOLON(IHolon holon)
        //{
        //    return base.CelestialBodyCore.SaveHolon(holon);
        //}
    }
}