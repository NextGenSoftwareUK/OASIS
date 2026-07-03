using System;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// Base controller for WEB6 endpoints. Resolves the calling OASIS avatar id (optional) from the
    /// AvatarId header/query so completions and dispatches can be grounded with WEB4 identity/karma.
    /// </summary>
    public abstract class Web6ControllerBase : ControllerBase
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

        protected OASISDNA OASISDNA => OASISBootLoader.OASISBootLoader.OASISDNA; //TODO: check if this is the best way to get the OASISDNA instance in a controller.
    }
}
