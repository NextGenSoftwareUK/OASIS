
        public OASISResult<HOLON> SaveHOLON(HOLON holon)
        {
            return base.CelestialBodyCore.SaveHolon<HOLON>(holon);
        }

        public async Task<OASISResult<HOLON>> SaveHOLONAsync(HOLON holon)
        {
            return await base.CelestialBodyCore.SaveHolonAsync<HOLON>(holon);
        }