using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.Config;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware
{
    public class OASISMiddleware
    {
        private readonly RequestDelegate _next;

        public OASISMiddleware(RequestDelegate next, IOptions<OASISSettings> OASISSettings)
        {
            _next = next;
            OASISProviderManager.OASISSettings = OASISSettings.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            //TODO: Try and make this more efficient, currently if they override provider in REST call and do not set Global flag to true then even if the next call is the same, it will switch back to default provider below and then have to switch back again to the override provider specified in the REST call...
            //if (!ProviderManager.IgnoreDefaultProviderTypes && ProviderManager.DefaultProviderTypes != null && ProviderManager.CurrentStorageProviderType != (ProviderType)Enum.Parse(typeof(ProviderType), ProviderManager.DefaultProviderTypes[0]))
            //      ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default);

            await _next(context);
        }
    }
}