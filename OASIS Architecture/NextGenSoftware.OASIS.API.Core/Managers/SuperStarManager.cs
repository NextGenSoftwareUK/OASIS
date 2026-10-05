
//namespace NextGenSoftware.OASIS.API.Core
//{
//    public class SuperStarManager : OASISManager
//    {
//        public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

//        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
//        public SuperStarManager(IOASISStorageProvider OASISStorageProvider) : base(OASISStorageProvider)
//        {

//        }
//        public bool NativeCodeGenesis(ICelestialBody celestialBody, ProviderType provider = ProviderType.Default)
//        {
//            return ((IOASISSuperStar)ProviderManager.SetAndActivateCurrentStorageProvider(provider)).NativeCodeGenesis(celestialBody);
//        }

//        /*
//        public Task<IHolon> LoadHolonAsync(Guid id, HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolonAsync(id, type);
//        }

//        public IHolon LoadHolon(string providerKey, HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolon(providerKey, type);
//        }

//        public Task<IHolon> LoadHolonAsync(string providerKey, HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolonAsync(providerKey, type);
//        }


//        public IEnumerable<IHolon> LoadHolons(Guid id, HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolons(id, type);
//        }

//        public Task<IEnumerable<IHolon>> LoadHolonsAsync(Guid id, HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolonsAsync(id, type);
//        }

//        public IEnumerable<IHolon> LoadHolons(string providerKey, HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolons(providerKey, type);
//        }

//        public Task<IEnumerable<IHolon>> LoadHolonsAsync(string providerKey, HolonType type = HolonType.Holon, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolonsAsync(providerKey, type);
//        }

//        public IHolon SaveHolon(IHolon holon, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).SaveHolon(PrepareHolonForSaving(holon));
//        }

//        public Task<IHolon> SaveHolonAsync(IHolon holon, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).SaveHolonAsync(PrepareHolonForSaving(holon));
//        }

//        public IEnumerable<IHolon> SaveHolons(IEnumerable<IHolon> holons, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).SaveHolons(PrepareHolonsForSaving(holons));
//        }

//        public Task<IEnumerable<IHolon>> SaveHolonsAsync(IEnumerable<IHolon> holons, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).SaveHolonsAsync(PrepareHolonsForSaving(holons));
//        }

//        public bool DeleteHolon(Guid id, bool softDelete = true, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).DeleteHolon(id, softDelete);
//        }

//        public Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).DeleteHolonAsync(id, softDelete);
//        }

//        public bool DeleteHolon(string providerKey, bool softDelete = true, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).DeleteHolon(providerKey, softDelete);
//        }

//        public Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true, ProviderType provider = ProviderType.Default)
//        {
//            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).DeleteHolonAsync(providerKey, softDelete);
//        }

//        private IHolon PrepareHolonForSaving(IHolon holon)
//        {
//            // TODO: I think it's best to include audit stuff here so the providers do not need to worry about it?
//            // Providers could always override this behaviour if they choose...
//            if (holon.Id != Guid.Empty)
//            {
//                holon.ModifiedDate = DateTime.Now;

//                if (AvatarManager.LoggedInAvatar != null)
//                    holon.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id;
//            }
//            else
//            {
//                holon.IsActive = true;
//                holon.CreatedDate = DateTime.Now;

//                if (AvatarManager.LoggedInAvatar != null)
//                    holon.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id;
//            }

//            return holon;
//        }

//        private IEnumerable<IHolon> PrepareHolonsForSaving(IEnumerable<IHolon> holons)
//        {
//            List<IHolon> holonsToReturn = new List<IHolon>();

//            foreach (IHolon holon in holons)
//                holonsToReturn.Add(PrepareHolonForSaving(holon));

//            return holonsToReturn;
//        }*/
//    }
//}
