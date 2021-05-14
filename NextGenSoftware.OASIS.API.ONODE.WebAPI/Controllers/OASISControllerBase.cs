using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA.Manager;
using System;

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

        protected IOASISStorage GetAndActivateDefaultProvider()
        {
            OASISResult<IOASISStorage> result = OASISDNAManager.GetAndActivateDefaultProvider();

            //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
            if (result.IsError)
                ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISDNAManager.GetAndActivateDefaultProvider(). Error details: ", result.Message), true, false, true);

            return result.Result;
        }

        protected IOASISStorage GetAndActivateProvider(ProviderType providerType, bool setGlobally = false)
        {
            return OASISDNAManager.GetAndActivateProvider(providerType, null, false, setGlobally);
        }

        protected IOASISStorage GetAndActivateProvider(ProviderType providerType, string customConnectionString = null, bool forceRegister = false, bool setGlobally = false)
        {
            return OASISDNAManager.GetAndActivateProvider(providerType, customConnectionString, forceRegister, setGlobally);
        }
    }
}
