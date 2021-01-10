
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Config;
using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public class OASISControllerBase : ControllerBase
    {
        public IOptions<OASISSettings> OASISSettings;

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

        public OASISControllerBase(IOptions<OASISSettings> settings)
        {
            OASISSettings = settings;
            OASISProviderManager.OASISSettings = settings.Value;
        }

        protected IOASISStorage GetAndActivateProvider()
        {
            return OASISProviderManager.GetAndActivateProvider();
        }

        protected IOASISStorage GetAndActivateProvider(ProviderType providerType, bool setGlobally = false)
        {
            return OASISProviderManager.GetAndActivateProvider(providerType, setGlobally);
        }
    }
}
