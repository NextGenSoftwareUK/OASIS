using System;
using Microsoft.AspNetCore.Mvc;

namespace NextGenSoftware.OASIS.Web8.WebAPI.Controllers
{
    /// <summary>Base controller for WEB8 endpoints. Resolves the calling OASIS avatar id from the AvatarId header/query.</summary>
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
    }
}
