namespace NextGenSoftware.OASIS.API.ONODE.Core;

/// <summary>
/// Root facade for ONODE Core API. Exposes manager endpoints for tests and consumers.
/// EndpointName identifies this as ONODE (not a storage/blockchain ProviderType).
/// </summary>
public class ONODE
{
    /// <summary>Identifies this endpoint (ONODE). Not a storage/blockchain provider.</summary>
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

    public class ManagerStub
    {
        public string EndpointName => ONODE.EndpointName;
    }
}
