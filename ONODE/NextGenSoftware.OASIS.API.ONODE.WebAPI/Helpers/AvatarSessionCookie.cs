using System;
using Microsoft.AspNetCore.Http;
using BootLoader = NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

/// <summary>One refresh-cookie contract for authentication, rotation and demo sessions.</summary>
public static class AvatarSessionCookie
{
    public static void Append(HttpContext context, string token)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("A successfully issued refresh token is required.", nameof(token));
        var days = BootLoader.OASISDNA?.OASIS?.Security?.RefreshTokenExpirationDays ?? 7;
        context.Response.Cookies.Append("refreshToken", token,
            CreateOptions(context.Request.IsHttps, days, DateTime.UtcNow));
    }

    public static void Clear(HttpContext context)
        => context.Response.Cookies.Delete("refreshToken", new CookieOptions {
            HttpOnly = true, Secure = context.Request.IsHttps, SameSite = SameSiteMode.Lax, Path = "/" });

    internal static CookieOptions CreateOptions(bool isHttps, int refreshDays, DateTime nowUtc) => new()
    {
        HttpOnly = true,
        Secure = isHttps,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = nowUtc.AddDays(refreshDays > 0 ? refreshDays : 7)
    };
}
