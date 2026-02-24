using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

[TestClass]
public class OasisWebApiControllerUnitTests
{
    [TestMethod]
    public void AllControllers_ExposeHttpActions()
    {
        var assembly = Assembly.Load("NextGenSoftware.OASIS.API.ONODE.WebAPI");
        var controllerTypes = assembly.GetTypes()
            .Where(t => t.IsClass &&
                        !t.IsAbstract &&
                        typeof(ControllerBase).IsAssignableFrom(t) &&
                        t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToList();

        Assert.IsTrue(controllerTypes.Count > 0);

        foreach (var controller in controllerTypes)
        {
            var actions = controller
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes()
                    .Any(a => a.GetType().Name.Contains("Http", StringComparison.Ordinal)))
                .ToList();

            Assert.IsTrue(actions.Count > 0, $"{controller.FullName} has no HTTP action methods.");
        }
    }
}


