using Grpc.Core;
using NextGenSoftware.OASIS.STAR.WebAPI.Grpc;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GrpcServices
{
    public class AvatarGrpcService : AvatarService.AvatarServiceBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _web4BaseUrl;

        public AvatarGrpcService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _web4BaseUrl = (configuration["Web4OasisApiBaseUrl"] ??
                           configuration["OASIS:Web4OasisApiBaseUrl"] ??
                           Environment.GetEnvironmentVariable("WEB4_OASIS_API_BASE_URL") ??
                           string.Empty).TrimEnd('/');
        }

        public override async Task<AuthenticateResponse> Authenticate(AuthenticateRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_web4BaseUrl))
                    return new AuthenticateResponse { IsError = true, Message = "WEB4 OASIS API base URL is not configured." };

                var body = JsonSerializer.Serialize(new { username = request.Username, password = request.Password });
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_web4BaseUrl}/api/avatar/authenticate", content);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new AuthenticateResponse { IsError = true, Message = $"Authentication failed: {json}" };

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var result = TryGetElement(root, "result");

                return new AuthenticateResponse
                {
                    IsError = false,
                    JwtToken = TryGetString(result ?? root, "jwtToken") ?? string.Empty,
                    RefreshToken = TryGetString(result ?? root, "refreshToken") ?? string.Empty,
                    AvatarId = TryGetString(result ?? root, "id") ?? string.Empty,
                    Username = TryGetString(result ?? root, "username") ?? string.Empty,
                    Email = TryGetString(result ?? root, "email") ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                return new AuthenticateResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<AvatarResponse> GetCurrentAvatar(EmptyRequest request, ServerCallContext context)
        {
            try
            {
                ForwardAuthHeader(context);
                var response = await _httpClient.GetAsync($"{_web4BaseUrl}/api/avatar/get-logged-in-avatar-with-xp");
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new AvatarResponse { IsError = true, Message = $"Failed to get avatar: {json}" };

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var result = TryGetElement(root, "result") ?? root;

                return new AvatarResponse
                {
                    IsError = false,
                    Id = TryGetString(result, "id") ?? string.Empty,
                    Username = TryGetString(result, "username") ?? string.Empty,
                    Email = TryGetString(result, "email") ?? string.Empty,
                    FullName = TryGetString(result, "fullName") ?? string.Empty,
                    AvatarType = TryGetString(result, "avatarType") ?? string.Empty,
                    IsActive = TryGetBool(result, "isActive"),
                    IsVerified = TryGetBool(result, "isVerified")
                };
            }
            catch (Exception ex)
            {
                return new AvatarResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<BoolResponse> SetActiveQuest(SetActiveQuestRequest request, ServerCallContext context)
        {
            try
            {
                ForwardAuthHeader(context);
                var body = JsonSerializer.Serialize(new
                {
                    activeQuestId = string.IsNullOrWhiteSpace(request.ActiveQuestId) ? (Guid?)null : Guid.Parse(request.ActiveQuestId),
                    activeObjectiveId = string.IsNullOrWhiteSpace(request.ActiveObjectiveId) ? (Guid?)null : Guid.Parse(request.ActiveObjectiveId)
                });
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_web4BaseUrl}/api/avatar/set-active-quest", content);
                var json = await response.Content.ReadAsStringAsync();
                return new BoolResponse { IsError = !response.IsSuccessStatusCode, Message = json, Result = response.IsSuccessStatusCode };
            }
            catch (Exception ex)
            {
                return new BoolResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<InventoryListResponse> GetInventory(EmptyRequest request, ServerCallContext context)
        {
            try
            {
                ForwardAuthHeader(context);
                var response = await _httpClient.GetAsync($"{_web4BaseUrl}/api/avatar/inventory");
                var json = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    return new InventoryListResponse { IsError = true, Message = json };
                return new InventoryListResponse { IsError = false, Message = "OK" };
            }
            catch (Exception ex)
            {
                return new InventoryListResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<InventoryItemResponse> AddItemToInventory(InventoryItemMessage request, ServerCallContext context)
        {
            try
            {
                ForwardAuthHeader(context);
                var body = JsonSerializer.Serialize(new { id = request.Id, name = request.Name, description = request.Description, quantity = request.Quantity });
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_web4BaseUrl}/api/avatar/inventory", content);
                var json = await response.Content.ReadAsStringAsync();
                return new InventoryItemResponse { IsError = !response.IsSuccessStatusCode, Message = json };
            }
            catch (Exception ex)
            {
                return new InventoryItemResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<BoolResponse> RemoveItemFromInventory(IdRequest request, ServerCallContext context)
        {
            try
            {
                ForwardAuthHeader(context);
                var response = await _httpClient.DeleteAsync($"{_web4BaseUrl}/api/avatar/inventory/{request.Id}");
                return new BoolResponse { IsError = !response.IsSuccessStatusCode, Result = response.IsSuccessStatusCode };
            }
            catch (Exception ex)
            {
                return new BoolResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<BoolResponse> HasItem(IdRequest request, ServerCallContext context)
        {
            try
            {
                ForwardAuthHeader(context);
                var response = await _httpClient.GetAsync($"{_web4BaseUrl}/api/avatar/inventory/{request.Id}/has");
                return new BoolResponse { IsError = !response.IsSuccessStatusCode, Result = response.IsSuccessStatusCode };
            }
            catch (Exception ex)
            {
                return new BoolResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<BoolResponse> HasItemByName(NameRequest request, ServerCallContext context)
        {
            try
            {
                ForwardAuthHeader(context);
                var response = await _httpClient.GetAsync($"{_web4BaseUrl}/api/avatar/inventory/has-by-name?itemName={Uri.EscapeDataString(request.Name)}");
                return new BoolResponse { IsError = !response.IsSuccessStatusCode, Result = response.IsSuccessStatusCode };
            }
            catch (Exception ex)
            {
                return new BoolResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<InventoryListResponse> SearchInventory(SearchRequest request, ServerCallContext context)
        {
            try
            {
                ForwardAuthHeader(context);
                var response = await _httpClient.GetAsync($"{_web4BaseUrl}/api/avatar/inventory/search?searchTerm={Uri.EscapeDataString(request.SearchTerm)}");
                var json = await response.Content.ReadAsStringAsync();
                return new InventoryListResponse { IsError = !response.IsSuccessStatusCode, Message = json };
            }
            catch (Exception ex)
            {
                return new InventoryListResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<InventoryItemResponse> GetInventoryItem(IdRequest request, ServerCallContext context)
        {
            try
            {
                ForwardAuthHeader(context);
                var response = await _httpClient.GetAsync($"{_web4BaseUrl}/api/avatar/inventory/{request.Id}");
                var json = await response.Content.ReadAsStringAsync();
                return new InventoryItemResponse { IsError = !response.IsSuccessStatusCode, Message = json };
            }
            catch (Exception ex)
            {
                return new InventoryItemResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<GenericResponse> AddXp(AddXpRequest request, ServerCallContext context)
        {
            try
            {
                ForwardAuthHeader(context);
                var body = JsonSerializer.Serialize(new { amount = request.Amount });
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_web4BaseUrl}/api/avatar/add-xp", content);
                var json = await response.Content.ReadAsStringAsync();
                return new GenericResponse { IsError = !response.IsSuccessStatusCode, Message = json };
            }
            catch (Exception ex)
            {
                return new GenericResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<BoolResponse> SendItemToAvatar(SendItemRequest request, ServerCallContext context)
        {
            try
            {
                ForwardAuthHeader(context);
                var body = JsonSerializer.Serialize(new { target = request.Target, itemName = request.ItemName, itemId = request.ItemId, quantity = request.Quantity });
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_web4BaseUrl}/api/avatar/inventory/send-to-avatar", content);
                return new BoolResponse { IsError = !response.IsSuccessStatusCode, Result = response.IsSuccessStatusCode };
            }
            catch (Exception ex)
            {
                return new BoolResponse { IsError = true, Message = ex.Message };
            }
        }

        public override async Task<BoolResponse> SendItemToClan(SendItemRequest request, ServerCallContext context)
        {
            try
            {
                ForwardAuthHeader(context);
                var body = JsonSerializer.Serialize(new { target = request.Target, itemName = request.ItemName, itemId = request.ItemId, quantity = request.Quantity });
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_web4BaseUrl}/api/avatar/inventory/send-to-clan", content);
                return new BoolResponse { IsError = !response.IsSuccessStatusCode, Result = response.IsSuccessStatusCode };
            }
            catch (Exception ex)
            {
                return new BoolResponse { IsError = true, Message = ex.Message };
            }
        }

        private void ForwardAuthHeader(ServerCallContext context)
        {
            var auth = context.RequestHeaders.GetValue("authorization");
            if (!string.IsNullOrWhiteSpace(auth))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", auth);
            }
        }

        private static JsonElement? TryGetElement(JsonElement root, string key)
        {
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out var el) && el.ValueKind != JsonValueKind.Null)
                return el;
            return null;
        }

        private static string? TryGetString(JsonElement el, string key)
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            return null;
        }

        private static bool TryGetBool(JsonElement el, string key)
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.True)
                return true;
            return false;
        }
    }
}
