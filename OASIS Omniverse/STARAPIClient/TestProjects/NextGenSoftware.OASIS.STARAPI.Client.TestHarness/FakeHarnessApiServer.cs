using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace NextGenSoftware.OASIS.STARAPI.Client.TestHarness;

internal sealed class FakeHarnessApiServer : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loopTask;
    private readonly List<InventoryItemRecord> _inventory = [];
    private readonly object _sync = new();
    private readonly ConcurrentDictionary<string, int> _routeHits = new(StringComparer.OrdinalIgnoreCase);
    private readonly Guid _avatarId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public FakeHarnessApiServer()
    {
        var port = GetFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        _listener.Prefixes.Add(BaseUrl + "/");
        _listener.Start();
        _loopTask = Task.Run(ProcessLoopAsync);
    }

    public string BaseUrl { get; }

    public ValueTask DisposeAsync()
    {
        _cts.Cancel();
        try
        {
            _listener.Stop();
            _listener.Close();
            _loopTask.GetAwaiter().GetResult();
        }
        catch
        {
        }
        finally
        {
            _cts.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private async Task ProcessLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch
            {
                if (_cts.IsCancellationRequested)
                    return;
            }

            if (context is not null)
                _ = Task.Run(() => HandleRequestAsync(context));
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var method = request.HttpMethod.ToUpperInvariant();
        var path = NormalizePath(request.Url?.AbsolutePath ?? "/");
        RegisterHit(method, path);

        try
        {
            if (method == "POST" && path == "/api/avatar/authenticate")
            {
                await WriteJsonAsync(response, 200, new
                {
                    IsError = false,
                    Result = new
                    {
                        Id = _avatarId,
                        JwtToken = "jwt-token",
                        RefreshToken = "refresh-token"
                    }
                }).ConfigureAwait(false);
                return;
            }

            if (method == "GET" && path == "/api/avatar/current")
            {
                await WriteJsonAsync(response, 200, new
                {
                    IsError = false,
                    Result = new
                    {
                        Id = _avatarId,
                        Username = "harness_user",
                        Email = "harness@example.com",
                        FirstName = "Harness",
                        LastName = "User"
                    }
                }).ConfigureAwait(false);
                return;
            }

            if (method == "GET" && path == "/api/avatar/inventory")
            {
                List<object> snapshot;
                lock (_sync)
                {
                    snapshot = _inventory.Select(x => (object)new
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Description = x.Description,
                        MetaData = new Dictionary<string, string>
                        {
                            ["GameSource"] = x.GameSource,
                            ["ItemType"] = x.ItemType
                        }
                    }).ToList();
                }

                await WriteJsonAsync(response, 200, new { IsError = false, Result = snapshot }).ConfigureAwait(false);
                return;
            }

            if (method == "POST" && path == "/api/inventoryitems")
            {
                var body = await ReadBodyAsync(request).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var id = Guid.NewGuid();
                var name = GetProperty(root, "Name")?.GetString() ?? "Unnamed";
                var description = GetProperty(root, "Description")?.GetString() ?? string.Empty;
                var gameSource = GetProperty(GetProperty(root, "MetaData"), "GameSource")?.GetString() ?? "Unknown";
                var itemType = GetProperty(GetProperty(root, "MetaData"), "ItemType")?.GetString() ?? "KeyItem";

                lock (_sync)
                    _inventory.Add(new InventoryItemRecord(id, name, description, gameSource, itemType));

                await WriteJsonAsync(response, 200, new
                {
                    IsError = false,
                    Result = new
                    {
                        Id = id,
                        Name = name,
                        Description = description,
                        MetaData = new Dictionary<string, string>
                        {
                            ["GameSource"] = gameSource,
                            ["ItemType"] = itemType
                        }
                    }
                }).ConfigureAwait(false);
                return;
            }

            if (method == "DELETE" && path.StartsWith("/api/avatar/inventory/", StringComparison.OrdinalIgnoreCase))
            {
                var idText = path["/api/avatar/inventory/".Length..];
                Guid.TryParse(idText, out var id);
                lock (_sync)
                    _inventory.RemoveAll(x => x.Id == id);

                await WriteJsonAsync(response, 200, new { IsError = false, Result = true }).ConfigureAwait(false);
                return;
            }

            if (method == "POST" && path.StartsWith("/api/quests/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/start", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(response, 200, new { IsError = false, Result = true }).ConfigureAwait(false);
                return;
            }

            if (method == "POST" && path.StartsWith("/api/quests/", StringComparison.OrdinalIgnoreCase) && path.Contains("/objectives/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/complete", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(response, 200, new { IsError = false, Result = true }).ConfigureAwait(false);
                return;
            }

            if (method == "POST" && path.StartsWith("/api/quests/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/complete", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(response, 200, new { IsError = false, Result = true }).ConfigureAwait(false);
                return;
            }

            if (method == "POST" && path == "/api/missions")
            {
                await WriteJsonAsync(response, 200, new { IsError = false, Result = true }).ConfigureAwait(false);
                return;
            }

            if (method == "GET" && path == "/api/quests/by-status/InProgress")
            {
                await WriteJsonAsync(response, 200, new
                {
                    IsError = false,
                    Result = new[]
                    {
                        new
                        {
                            Id = "quest-harness",
                            Name = "Harness Quest",
                            Description = "Quest from harness mock",
                            Status = "InProgress",
                            Objectives = new[] { new { Description = "Obj", GameSource = "Harness", ItemRequired = "Key", IsCompleted = false } }
                        }
                    }
                }).ConfigureAwait(false);
                return;
            }

            if (method == "POST" && path == "/api/nft/mint-nft")
            {
                await WriteJsonAsync(response, 200, new { IsError = false, Result = new { Id = "nft-harness-001" } }).ConfigureAwait(false);
                return;
            }

            if (method == "POST" && path.StartsWith("/api/nfts/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/activate", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(response, 200, new { IsError = false, Result = true }).ConfigureAwait(false);
                return;
            }

            if (method == "GET" && path == "/api/nfts/load-all-for-avatar")
            {
                await WriteJsonAsync(response, 200, new
                {
                    IsError = false,
                    Result = new[]
                    {
                        new { Id = "nft-harness-001", Name = "Harness NFT", Description = "Harness Mock NFT", Type = "Boss", MetaData = new { GameSource = "Harness" } }
                    }
                }).ConfigureAwait(false);
                return;
            }

            await WriteJsonAsync(response, 404, new { IsError = true, Message = $"Route not found: {method} {path}", ErrorCode = "-5" }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await WriteJsonAsync(response, 500, new { IsError = true, Message = ex.Message, ErrorCode = "-3" }).ConfigureAwait(false);
        }
        finally
        {
            response.Close();
        }
    }

    private void RegisterHit(string method, string path)
    {
        var key = $"{method} {path}";
        _routeHits.AddOrUpdate(key, 1, (_, existing) => existing + 1);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        var value = path.Trim();
        if (!value.StartsWith('/'))
            value = "/" + value;

        return value.Length > 1 ? value.TrimEnd('/') : value;
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<string> ReadBodyAsync(HttpListenerRequest request)
    {
        if (!request.HasEntityBody || request.InputStream is null)
            return "{}";

        using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    private static async Task WriteJsonAsync(HttpListenerResponse response, int statusCode, object payload)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
    }

    private static JsonElement? GetProperty(JsonElement? element, string name)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in element.Value.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        }

        return null;
    }

    private readonly record struct InventoryItemRecord(Guid Id, string Name, string Description, string GameSource, string ItemType);
}

