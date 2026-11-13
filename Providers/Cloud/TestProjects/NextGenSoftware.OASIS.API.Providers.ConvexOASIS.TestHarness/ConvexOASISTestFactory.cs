using System;
using NextGenSoftware.OASIS.API.Providers.ConvexOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ConvexOASIS.TestHarness
{
    internal static class ConvexOASISTestFactory
    {
        public static ConvexOASIS Create()
        {
            var deploymentUrl = Environment.GetEnvironmentVariable("CONVEXOASIS_DEPLOYMENTURL");
            var deployKey = Environment.GetEnvironmentVariable("CONVEXOASIS_DEPLOYKEY");
            if (string.IsNullOrWhiteSpace(deploymentUrl) || string.IsNullOrWhiteSpace(deployKey))
                throw new InvalidOperationException("Set CONVEXOASIS_DEPLOYMENTURL and CONVEXOASIS_DEPLOYKEY for the disposable Convex test deployment.");

            return new ConvexOASIS(deploymentUrl, deployKey);
        }
    }
}
