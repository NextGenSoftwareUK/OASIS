using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Security
{
    /// <summary>
    /// Authorization policy for the GraphQL and gRPC surfaces, whose resolvers perform no caller or ownership
    /// checks of their own. Every operation requires a Wizard avatar except the explicit public allowlist below
    /// (the anonymous login/registration flow). To open an operation to ordinary avatars, give it an ownership
    /// check in its resolver first, then list it here.
    /// </summary>
    public static class ApiSurfaceAuthorization
    {
        public static readonly IReadOnlySet<string> PublicGraphQLRootFields = new HashSet<string>(StringComparer.Ordinal)
        {
            "register", "authenticate", "verifyEmail", "forgotPassword", "resetPassword"
        };

        public static readonly IReadOnlySet<string> PublicGrpcMethods = new HashSet<string>(StringComparer.Ordinal)
        {
            "/oasis.web4.AvatarService/Register",
            "/oasis.web4.AvatarService/Authenticate"
        };

        public enum Decision { Allowed, Unauthenticated, Forbidden }

        public static Decision RequireWizard(HttpContext? httpContext)
        {
            if (httpContext?.Items["Avatar"] is not IAvatar avatar) return Decision.Unauthenticated;
            return avatar.AvatarType?.Value == AvatarType.Wizard ? Decision.Allowed : Decision.Forbidden;
        }

        public static string Message(Decision decision) => decision == Decision.Unauthenticated
            ? "Unauthorized. Send a valid JWT in the Authorization: Bearer header (authenticate first)."
            : "Forbidden. This operation requires a Wizard avatar.";
    }
}
