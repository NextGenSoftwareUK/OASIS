using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Security
{
    /// <summary>
    /// Applies <see cref="ApiSurfaceAuthorization"/> to every root Query/Mutation field. Nested fields are covered by
    /// their root field; introspection fields stay public.
    /// </summary>
    public sealed class OASISGraphQLAuthorizationMiddleware
    {
        private readonly FieldDelegate _next;

        public OASISGraphQLAuthorizationMiddleware(FieldDelegate next) => _next = next;

        public async ValueTask InvokeAsync(IMiddlewareContext context)
        {
            var fieldName = context.Selection.Field.Name;
            if (context.Path.Parent.IsRoot && !fieldName.StartsWith("__") &&
                !ApiSurfaceAuthorization.PublicGraphQLRootFields.Contains(fieldName))
            {
                var decision = ApiSurfaceAuthorization.RequireWizard(
                    context.Services.GetRequiredService<IHttpContextAccessor>().HttpContext);
                if (decision != ApiSurfaceAuthorization.Decision.Allowed)
                {
                    context.ReportError(ErrorBuilder.New()
                        .SetMessage(ApiSurfaceAuthorization.Message(decision))
                        .SetCode(decision == ApiSurfaceAuthorization.Decision.Unauthenticated ? "AUTH_NOT_AUTHENTICATED" : "AUTH_NOT_AUTHORIZED")
                        .SetPath(context.Path)
                        .Build());
                    context.Result = null;
                    return;
                }
            }

            await _next(context);
        }
    }
}
