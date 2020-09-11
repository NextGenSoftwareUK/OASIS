using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(NextGenSoftware.OASIS.API.ONODE.WebAPI._4._71.Startup))]

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI._4._71
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
