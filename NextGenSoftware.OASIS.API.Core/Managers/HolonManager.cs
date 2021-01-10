using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public class HolonManager : OASISManager
    {
       // public List<IOASISStorage> OASISStorageProviders { get; set; }
        
        public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public HolonManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
        {

        }
        public IHolon LoadHolon(Guid id, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolon(id);
        }
     
        public Task<IHolon> LoadHolonAsync(Guid id, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolonAsync(id);
        }

        public IHolon LoadHolon(string providerKey, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolon(providerKey);
        }

        public Task<IHolon> LoadHolonAsync(string providerKey, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolonAsync(providerKey);
        }


        public IEnumerable<IHolon> LoadHolons(Guid id, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolons(id);
        }

        public Task<IEnumerable<IHolon>> LoadHolonsAsync(Guid id, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolonsAsync(id);
        }

        public IEnumerable<IHolon> LoadHolons(string providerKey, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolons(providerKey);
        }

        public Task<IEnumerable<IHolon>> LoadHolonsAsync(string providerKey, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).LoadHolonsAsync(providerKey);
        }

        public IHolon SaveHolon(IHolon holon, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).SaveHolon(PrepareHolonForSaving(holon));
        }

        public Task<IHolon> SaveHolonAsync(IHolon holon, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).SaveHolonAsync(PrepareHolonForSaving(holon));
        }

        public IEnumerable<IHolon> SaveHolons(IEnumerable<IHolon> holons, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).SaveHolons(PrepareHolonsForSaving(holons));
        }

        public Task<IEnumerable<IHolon>> SaveHolonAsync(IEnumerable<IHolon> holons, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).SaveHolonsAsync(PrepareHolonsForSaving(holons));
        }

        public bool DeleteHolon(Guid id, bool softDelete = true, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).DeleteHolon(id, softDelete);
        }

        public Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).DeleteHolonAsync(id, softDelete);
        }

        public bool DeleteHolon(string providerKey, bool softDelete = true, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).DeleteHolon(providerKey, softDelete);
        }

        public Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true, ProviderType provider = ProviderType.Default)
        {
            return ProviderManager.SetAndActivateCurrentStorageProvider(provider).DeleteHolonAsync(providerKey, softDelete);
        }

        private IHolon PrepareHolonForSaving(IHolon holon)
        {
            // TODO: I think it's best to include audit stuff here so the providers do not need to worry about it?
            // Providers could always override this behaviour if they choose...
            if (holon.Id != Guid.Empty)
            {
                holon.ModifiedDate = DateTime.Now;

                if (AvatarManager.LoggedInAvatar != null)
                    holon.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id;
            }
            else
            {
                holon.IsActive = true;
                holon.CreatedDate = DateTime.Now;

                if (AvatarManager.LoggedInAvatar != null)
                    holon.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id;
            }

            return holon;
        }

        private IEnumerable<IHolon> PrepareHolonsForSaving(IEnumerable<IHolon> holons)
        {
            List<IHolon> holonsToReturn = new List<IHolon>();

            foreach (IHolon holon in holons)
                holonsToReturn.Add(PrepareHolonForSaving(holon));

            return holonsToReturn;
        }
    }
}
