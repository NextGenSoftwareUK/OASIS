namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS;

public partial class AptosOASIS
{
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
