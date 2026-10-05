using System.Web.Http;

namespace NextGenSoftware.OASIS.API.ONODE.WebUI.HTML2
{
    public class Global
    {
        protected void Application_Start(object sender, EventArgs e) 
        {
            WebApiConfig.Register(GlobalConfiguration.Configuration);
        }

    }
}
