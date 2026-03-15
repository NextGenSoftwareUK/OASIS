using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.AllProviders.IntegrationTests
{
    [TestClass]
    public class AllProvidersIntegrationTests
    {
        [TestMethod]
        public async Task AllProviders_SmokeTest_ShouldRunWithoutCrashing()
        {
            // Arrange
            var harnessType = Type.GetType("NextGenSoftware.OASIS.API.Providers.AllProviders.TestHarness.AllProvidersTestHarness, NextGenSoftware.OASIS.API.Providers.AllProviders.TestHarness");
            
            if (harnessType == null)
            {
                Assert.Inconclusive("AllProvidersTestHarness not found. Ensure the TestHarness project is built.");
                return;
            }

            var mainMethod = harnessType.GetMethod("Main", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            
            if (mainMethod == null)
            {
                Assert.Inconclusive("AllProvidersTestHarness.Main method not found.");
                return;
            }

            // Act
            try
            {
                var args = new object[] { new string[0] };
                var task = (Task)mainMethod.Invoke(null, args);
                await task;
            }
            catch (Exception ex)
            {
                // Assert
                Assert.Fail($"AllProviders smoke test failed: {ex.Message}");
            }
        }
    }
}


