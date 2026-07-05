using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (token != null)
                await AttachAvatarToContext(context, token);
            await _next(context);
        }

        private async Task AttachAvatarToContext(HttpContext context, string token)
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
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var id = Guid.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

                // Always mark the JWT as authenticated so AI endpoints work even when the storage
                // provider (avatar DB) is not configured on this deployment. The avatar ID from the
                // JWT claims is sufficient for routing/karma attribution; a full avatar load is best-effort.
                context.Items["AvatarId"] = id;
                OASISRequestContext.CurrentAvatarId = id;

                try
                {
                    OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(id, false, false);
                    if (!avatarResult.IsError && avatarResult.Result != null)
                    {
                        context.Items["Avatar"] = avatarResult.Result;
                        OASISRequestContext.CurrentAvatar = avatarResult.Result;
                    }
                }
                catch
                {
                    // Avatar DB not available on this deployment — JWT is still valid, proceed without full avatar.
                }
            }
            catch (Exception ex)
            {
                var exceptionResponse = new OASISResult<string>
                {
                    Message = "Authorization Failed: JWT Token Is Invalid. Please re-login and try again."
                };
                OASISErrorHandling.HandleError(ref exceptionResponse, exceptionResponse.Message, ex.Message);
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(exceptionResponse));
                try { await context.Response.Body.WriteAsync(body); } catch { }
                return;
            }
        }
    }
}
