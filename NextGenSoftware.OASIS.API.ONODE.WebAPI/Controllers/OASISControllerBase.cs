using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public class OASISControllerBase : ControllerBase
    {
       // public IOptions<OASISSettings> OASISSettings;

        public IAvatar Avatar
        {
            get
            {
                if (HttpContext.Items.ContainsKey("Avatar") && HttpContext.Items["Avatar"] != null)
                    return (IAvatar)HttpContext.Items["Avatar"];

                //if (HttpContext.Session.GetString("Avatar") != null)
                //    return JsonSerializer.Deserialize<IAvatar>(HttpContext.Session.GetString("Avatar"));

                return null;
            }
            set
            {
                HttpContext.Items["Avatar"] = value;
                //HttpContext.Session.SetString("Avatar", JsonSerializer.Serialize(value));
            }
        }

        //public OASISControllerBase(IOptions<OASISSettings> settings)
        public OASISControllerBase()
        {
            //OASISSettings = settings;
           // OASISProviderManager.OASISSettings = settings.Value;
        }

        //TODO: REMOVE ASAP, NOT USED ANYMORE
        //public OASISControllerBase(IOptions<OASISDNA> settings)
        //{
        //    //OASISSettings = settings;
        //    // OASISProviderManager.OASISSettings = settings.Value;
        //}

        protected IOASISStorageProvider GetAndActivateDefaultProvider()
        {
            OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

            //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
            if (result.IsError)
                ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISDNAManager.GetAndActivateDefaultProvider(). Error details: ", result.Message), true, false, true);

            return result.Result;
        }

        protected IOASISStorageProvider GetAndActivateProvider(ProviderType providerType, bool setGlobally = false)
        {
            // TODO: Everywhere I have had to just return the .Result object of OASISResult we need to check for errors and handle correcty.
            // Or maybe better in this case and in the Managers (AvatarManager/HolonManager) just change the return types to OASISResult<T> and pass it up to show any errors/messages to UI if needed...
            // But in meantime if you need to show any errors just throw an exception (ONLY TEMP!)
            return OASISBootLoader.OASISBootLoader.GetAndActivateProvider(providerType, null, false, setGlobally).Result;
        }

        protected IOASISStorageProvider GetAndActivateProvider(ProviderType providerType, string customConnectionString = null, bool forceRegister = false, bool setGlobally = false)
        {
            // TODO: Everywhere I have had to just return the .Result object of OASISResult we need to check for errors and handle correcty.
            // Or maybe better in this case and in the Managers (AvatarManager/HolonManager) just change the return types to OASISResult<T> and pass it up to show any errors/messages to UI if needed...
            // But in meantime if you need to show any errors just throw an exception (ONLY TEMP!)
            return OASISBootLoader.OASISBootLoader.GetAndActivateProvider(providerType, customConnectionString, forceRegister, setGlobally).Result;
        }
    }
}
