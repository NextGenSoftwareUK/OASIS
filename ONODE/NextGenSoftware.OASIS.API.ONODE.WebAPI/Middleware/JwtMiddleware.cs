using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware
{
    public class JwtMiddleware
    {
        public const string AuthenticationErrorItemKey = "OASIS.AuthenticationError";
        private readonly RequestDelegate _next;
        
        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (token != null)
                await AttachAccountToContext(context, token);
            await _next(context);
        }

        private async Task AttachAccountToContext(HttpContext context, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Security.SecretKey);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = "OASIS",
                    ValidateAudience = true,
                    ValidAudience = "OASIS",
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var id = Guid.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

                OASISResult<IAvatar> avatarResult = await Program.AvatarManager.LoadAvatarAsync(id);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    context.Items["Avatar"] = avatarResult.Result;
                    OASISRequestContext.CurrentAvatarId = id;
                    OASISRequestContext.CurrentAvatar = avatarResult.Result;
                }
            }
            catch (Exception ex)
            {
                // Authentication policy belongs to the endpoint/filter. Mutating the response here and
                // continuing the pipeline left a stale 401 status even when an endpoint subsequently
                // authenticated with another explicitly supported credential (for example an Edge grant).
                context.Items[AuthenticationErrorItemKey] = ex.Message;
            }
        }
    }
}
