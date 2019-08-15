using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.FrontEnd.Web.Models;

namespace NextGenSoftware.OASIS.API.FrontEnd.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
