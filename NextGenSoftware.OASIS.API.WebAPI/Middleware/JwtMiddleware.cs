using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly OASISSettings _OASISSettings;

        public JwtMiddleware(RequestDelegate next, IOptions<OASISSettings> OASISSettings)
        {
            _next = next;
            _OASISSettings = OASISSettings.Value;
        }

        //public async Task Invoke(HttpContext context, DataContext dataContext)
        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
                await attachAccountToContext(context, token);
            //await attachAccountToContext(context, dataContext, token);

            await _next(context);
        }

        //private async Task attachAccountToContext(HttpContext context, DataContext dataContext, string token)
        private async Task attachAccountToContext(HttpContext context, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_OASISSettings.Secret);
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

                //TODO: Check this still works now it's a Guid instead of an int...
                var id = Guid.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
                // var id = Guid.Parse(jwtToken.Claims.First(x => x.Type == "Guid").Value);

                // attach account to context on successful jwt validation
                //context.Items["Account"] = await dataContext.Accounts.FindAsync(accountId);

                //TODO: Change to async version when it is fixed...
                //context.Items["Avatar"] = await Program.AvatarManager.LoadAvatarAsync(id);

                OASISProviderManager.OASISSettings = _OASISSettings;
                OASISProviderManager.GetAndActivateProvider();

                context.Items["Avatar"] = Program.AvatarManager.LoadAvatar(id);
            }
            catch (Exception ex)
            {
                // do nothing if jwt validation fails
                // account is not attached to context so request won't have access to secure routes
                
                //TODO: Log and handle error
            }
        }
    }
}