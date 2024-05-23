
        public OASISResult<HOLON> LoadHOLON(Guid id)
        {
            return base.CelestialBodyCore.GlobalHolonData.LoadHolon<HOLON>(id);
        }

        public async Task<OASISResult<HOLON>> LoadHOLONAsync(Guid id)
        {
            return await base.CelestialBodyCore.GlobalHolonData.LoadHolonAsync<HOLON>(id);
        }

        public OASISResult<HOLON> LoadHOLON(ProviderType providerType, string providerKey)
        {
            return base.CelestialBodyCore.GlobalHolonData.LoadHolon<HOLON>(providerType, providerKey);
        }

        public async Task<OASISResult<HOLON>> LoadHOLONAsync(ProviderType providerType, string providerKey)
        {
            return await base.CelestialBodyCore.GlobalHolonData.LoadHolonAsync<HOLON>(providerType, providerKey);
        }