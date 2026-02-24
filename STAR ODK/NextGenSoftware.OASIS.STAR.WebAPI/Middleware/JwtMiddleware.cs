using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Middleware
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
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var id = Guid.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(id, false, false);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    context.Items["Avatar"] = avatarResult.Result;
                    NextGenSoftware.OASIS.API.Core.OASISRequestContext.CurrentAvatarId = id;
                    NextGenSoftware.OASIS.API.Core.OASISRequestContext.CurrentAvatar = avatarResult.Result;
                }
            }
            catch (Exception ex)
            {
                var exceptionResponse = new OASISResult<string>()
                {
                    Message = $"Authorization Failed: JWT Token Is Invalid. Make sure it is set in the Authorization Header for your request or alternatively please re-login and try again.",
                };

                OASISErrorHandling.HandleError(ref exceptionResponse, exceptionResponse.Message, ex.Message);
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(exceptionResponse));
                
                try
                {
                    await context.Response.Body.WriteAsync(body);
                }
                catch
                {
                    // If we can't write to the response body, just continue
                }
            }
        }
    }
}
