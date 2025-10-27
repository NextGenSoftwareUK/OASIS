using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    public class STARControllerBase : ControllerBase
    {
        public IAvatar Avatar
        {
            get
            {
                if (HttpContext.Items.ContainsKey("Avatar") && HttpContext.Items["Avatar"] != null)
                    return (IAvatar)HttpContext.Items["Avatar"];

                return null;
            }
            set
            {
                HttpContext.Items["Avatar"] = value;
            }
        }

        public Guid AvatarId
        {
            get
            {
                return Avatar != null ? Avatar.Id : Guid.Empty;
            }
        }

        public STARControllerBase()
        {
        }
    }
}

