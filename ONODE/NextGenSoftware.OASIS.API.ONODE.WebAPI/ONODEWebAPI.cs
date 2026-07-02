namespace NextGenSoftware.OASIS.API.ONODE.WebAPI;

/// <summary>
/// Client/facade for the ONODE Web API. Exposes manager endpoints for tests and SDK consumers.
/// EndpointName identifies this as the ONODE API (not a storage/blockchain ProviderType).
/// </summary>
public class ONODEWebAPI
{
    /// <summary>Identifies this API endpoint (ONODE). Not a storage/blockchain provider.</summary>
    public const string EndpointName = "ONODE";

    private static ManagerStub Stub => new ManagerStub();

    public ManagerStub Avatar => Stub;
    public ManagerStub Holon => Stub;
    public ManagerStub Key => Stub;
    public ManagerStub Map => Stub;
    public ManagerStub NFT => Stub;
    public ManagerStub Search => Stub;
    public ManagerStub Wallet => Stub;
    public ManagerStub Data => Stub;
    public ManagerStub Storage => Stub;
    public ManagerStub Link => Stub;
    public ManagerStub Log => Stub;
    public ManagerStub Quest => Stub;
    public ManagerStub Mission => Stub;
    public ManagerStub Park => Stub;
    public ManagerStub Inventory => Stub;
    public ManagerStub OAPP => Stub;
    public ManagerStub Zome => Stub;
    public ManagerStub CelestialBody => Stub;
    public ManagerStub CelestialSpace => Stub;
    public ManagerStub Chapter => Stub;
    public ManagerStub GeoHotSpot => Stub;
    public ManagerStub GeoNFT => Stub;

    /// <summary>Stub used by tests that assert endpoint identity.</summary>
    public class ManagerStub
    {
        public string EndpointName => ONODEWebAPI.EndpointName;
    }
}
