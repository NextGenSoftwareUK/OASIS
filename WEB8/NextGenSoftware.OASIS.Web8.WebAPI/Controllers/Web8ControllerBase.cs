using System;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Web8.WebAPI.Attributes;

namespace NextGenSoftware.OASIS.Web8.WebAPI.Controllers
{
    /// <summary>Base controller for WEB8 endpoints. Resolves the calling OASIS avatar id from the AvatarId header/query.</summary>
    [Authorize]
    public abstract class Web8ControllerBase : ControllerBase
    {
        protected Guid AvatarId
        {
            get
            {
                string raw = Request.Headers["AvatarId"].ToString();

                if (string.IsNullOrEmpty(raw))
                    raw = Request.Query["avatarId"].ToString();

                return Guid.TryParse(raw, out Guid id) ? id : Guid.Empty;
            }
        }

        protected IAvatar Avatar => HttpContext.Items["Avatar"] as IAvatar;
    }
}
