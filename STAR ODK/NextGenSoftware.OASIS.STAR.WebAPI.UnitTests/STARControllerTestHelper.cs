using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests
{
    /// <summary>
    /// Shared helper that wires up a minimal HTTP context on STAR WebAPI controllers
    /// so controller action methods that read User, HttpContext, or route data work in tests.
    /// </summary>
    public static class STARControllerTestHelper
    {
        public static void SetUpControllerContext(ControllerBase controller)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-avatar-id"),
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.Email, "test@oasisomniverse.one")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal };
            httpContext.Request.Headers["Authorization"] = "Bearer test-token";

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }
    }
}
