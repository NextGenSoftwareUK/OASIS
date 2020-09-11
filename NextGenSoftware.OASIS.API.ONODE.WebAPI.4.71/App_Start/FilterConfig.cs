using System.Web;
using System.Web.Mvc;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI._4._71
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
