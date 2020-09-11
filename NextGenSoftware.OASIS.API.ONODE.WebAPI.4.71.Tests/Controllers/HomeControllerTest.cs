using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.ONODE.WebAPI._4._71;
using NextGenSoftware.OASIS.API.ONODE.WebAPI._4._71.Controllers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI._4._71.Tests.Controllers
{
    [TestClass]
    public class HomeControllerTest
    {
        [TestMethod]
        public void Index()
        {
            // Arrange
            HomeController controller = new HomeController();

            // Act
            ViewResult result = controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Home Page", result.ViewBag.Title);
        }
    }
}
