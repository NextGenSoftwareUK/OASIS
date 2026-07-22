using Grpc.Core;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GrpcServices
{
    internal static class GrpcHelpers
    {
        /// <summary>
        /// Extracts the AvatarId from the gRPC request metadata (x-avatar-id header or bearer JWT).
        /// Returns Guid.Empty if not found.
        /// </summary>
        public static Guid GetAvatarId(ServerCallContext context)
        {
            var avatarIdHeader = context.RequestHeaders.GetValue("x-avatar-id");
            if (!string.IsNullOrWhiteSpace(avatarIdHeader) && Guid.TryParse(avatarIdHeader, out var avatarId))
                return avatarId;

            var auth = context.RequestHeaders.GetValue("authorization");
            if (!string.IsNullOrWhiteSpace(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = auth["Bearer ".Length..].Trim();
                return ParseAvatarIdFromJwt(token);
            }

            return Guid.Empty;
        }

        private static Guid ParseAvatarIdFromJwt(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length < 2) return Guid.Empty;

                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var bytes = Convert.FromBase64String(payload);
                using var doc = System.Text.Json.JsonDocument.Parse(bytes);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if ((string.Equals(prop.Name, "id", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(prop.Name, "avatarId", StringComparison.OrdinalIgnoreCase)) &&
                        prop.Value.ValueKind == System.Text.Json.JsonValueKind.String &&
                        Guid.TryParse(prop.Value.GetString(), out var parsed))
                        return parsed;
                }
            }
            catch { }
            return Guid.Empty;
        }
    }
}
