using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

// The isolated component host combines service assemblies on one loopback origin.
// WEB4 owns the inventory route; WEB5's proxy is deployed on a separate origin.
internal sealed class StorefrontControllerSelection : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        var proxy = feature.Controllers.SingleOrDefault(controller => controller.AsType() ==
            typeof(NextGenSoftware.OASIS.STAR.WebAPI.Controllers.AvatarController));
        if (proxy != null) feature.Controllers.Remove(proxy);
        if (feature.Controllers.Count(controller => controller.AsType() ==
            typeof(NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.AvatarProfileController)) != 1)
            throw new InvalidOperationException("Actual WEB4 inventory controller required.");
    }
}