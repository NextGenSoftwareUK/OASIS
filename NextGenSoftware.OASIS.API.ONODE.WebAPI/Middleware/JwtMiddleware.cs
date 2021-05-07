using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA.Manager;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        //public JwtMiddleware(RequestDelegate next, IOptions<OASISSettings> OASISSettings)
        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
          //  OASISProviderManager.OASISSettings = OASISSettings.Value;
        }

        public async Task Invoke(HttpContext context)
        {
         //   if (!ProviderManager.IgnoreDefaultProviderTypes && ProviderManager.DefaultProviderTypes != null && ProviderManager.CurrentStorageProviderType != (ProviderType)Enum.Parse(typeof(ProviderType), ProviderManager.DefaultProviderTypes[0]))
         //       ProviderManager.SetAndActivateCurrentStorageProvider(ProviderType.Default);

            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
                await AttachAccountToContext(context, token);

            await _next(context);
        }

        //private async Task attachAccountToContext(HttpContext context, DataContext dataContext, string token)
        private async Task AttachAccountToContext(HttpContext context, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(OASISDNAManager.OASISDNA.OASIS.Security.Secret);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var id = Guid.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

                context.Items["Avatar"] = (Core.Holons.Avatar)Program.AvatarManager.LoadAvatar(id);
                AvatarManager.LoggedInAvatar = (IAvatar)context.Items["Avatar"];

            }
            catch (Exception ex)
            {
                
                
                
                throw new Exception("Token Authorization Failed.");
                // account is not attached to context so request won't have access to secure routes
                
                //TODO: Log and handle error
            }
        }
    }
}