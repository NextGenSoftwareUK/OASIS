using System.Security.Cryptography;

namespace NextGenSoftware.OASIS.ONODE.Service.Services;

public class TokenService
{
    private string _token = "";

    public string Token => _token;

    public void Initialise(string tokenPath)
    {
        var dir = Path.GetDirectoryName(tokenPath)!;
        Directory.CreateDirectory(dir);

        if (File.Exists(tokenPath))
        {
            _token = File.ReadAllText(tokenPath).Trim();
            if (!string.IsNullOrWhiteSpace(_token)) return;
        }

        _token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        File.WriteAllText(tokenPath, _token);
        // restrict permissions on non-Windows
        if (!OperatingSystem.IsWindows())
        {
            try { File.SetUnixFileMode(tokenPath, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
            catch { /* best effort */ }
        }
    }

    public bool Validate(string? bearerToken) =>
        !string.IsNullOrEmpty(bearerToken) &&
        bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) &&
        bearerToken["Bearer ".Length..].Trim() == _token;
}
