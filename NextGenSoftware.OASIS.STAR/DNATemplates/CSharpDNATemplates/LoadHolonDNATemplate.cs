
        public OASISResult<HOLON> LoadHOLON(Guid id)
        {
            return base.CelestialBodyCore.LoadHolon<HOLON>(id);
        }

        public async Task<OASISResult<HOLON>> LoadHOLONAsync(Guid id)
        {
            return await base.CelestialBodyCore.LoadHolonAsync<HOLON>(id);
        }

        public OASISResult<HOLON> LoadHOLON(ProviderType providerType, string providerKey)
        {
            return base.CelestialBodyCore.LoadHolon<HOLON>(providerType, providerKey);
        }

        public async Task<OASISResult<HOLON>> LoadHOLONAsync(ProviderType providerType, string providerKey)
        {
            return await base.CelestialBodyCore.LoadHolonAsync<HOLON>(providerType, providerKey);
        }