using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using System.Text.Json;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{
    public class STARControllerBase : ControllerBase
    {
        public IAvatar Avatar
        {
            get
            {
                if (HttpContext.Items.ContainsKey("Avatar") && HttpContext.Items["Avatar"] != null)
                    return (IAvatar)HttpContext.Items["Avatar"];

                return null;
            }
            set
            {
                HttpContext.Items["Avatar"] = value;
            }
        }

        public Guid AvatarId
        {
            get
            {
                if (Avatar != null && Avatar.Id != Guid.Empty)
                    return Avatar.Id;

                return ResolveAvatarIdFromBearerToken();
            }
        }

        public STARControllerBase()
        {
        }

        private Guid ResolveAvatarIdFromBearerToken()
        {
            try
            {
                if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValue))
                    return Guid.Empty;

                var authHeader = authHeaderValue.ToString();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return Guid.Empty;

                var token = authHeader["Bearer ".Length..].Trim();
                if (string.IsNullOrWhiteSpace(token))
                    return Guid.Empty;

                var parts = token.Split('.');
                if (parts.Length < 2)
                    return Guid.Empty;

                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var bytes = Convert.FromBase64String(payload);
                using var doc = JsonDocument.Parse(bytes);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return Guid.Empty;

                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    if ((string.Equals(property.Name, "id", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(property.Name, "avatarId", StringComparison.OrdinalIgnoreCase)) &&
                        property.Value.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(property.Value.GetString(), out var parsed))
                    {
                        return parsed;
                    }
                }
            }
            catch
            {
            }

            return Guid.Empty;
        }
    }
}

