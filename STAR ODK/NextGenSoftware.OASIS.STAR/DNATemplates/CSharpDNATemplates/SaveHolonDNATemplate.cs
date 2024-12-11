
        public OASISResult<HOLON> SaveHOLON(HOLON holon)
        {
            return base.CelestialBodyCore.GlobalHolonData.SaveHolon<HOLON>(holon);
        }

        public async Task<OASISResult<HOLON>> SaveHOLONAsync(HOLON holon)
        {
            return await base.CelestialBodyCore.GlobalHolonData.SaveHolonAsync<HOLON>(holon);
        }