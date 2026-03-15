namespace NextGenSoftware.OASIS.STARAPI.Client.Tests;

/// <summary>Single place for default test credentials and API URLs when running against real APIs (integration tests and test harness). Override with env vars STARAPI_USERNAME, STARAPI_PASSWORD, etc.</summary>
public static class StarApiTestDefaults
{
    public const string Username = "dellams";
    public const string Password = "test!";
    public const string Web5BaseUrl = "http://localhost:5556";
    public const string Web4BaseUrl = "http://localhost:5555";
}
