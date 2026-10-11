# CivicOASIS

Civic Pass verification, read on-chain from the Identity.com Gateway Protocol GatewayToken contract (0xF65b6396dF6B7e2D8a6270E3AB6c7BB08BAEF22E on Polygon and Ethereum).

- `VerifyPassAsync(address, network)`: calls `verifyToken(address,uint256)`, so the pass must be active and unexpired.
- `GetPassesAsync(address, network)`: lists all tokens with their state, expiration and bitmask.

Gatekeeper networks: 4 = CAPTCHA, 10 = uniqueness. RPC failures are returned as errors, never as "no pass".

Part of the [OASIS HOT-Swappable Provider Architecture](https://oasisomniverse.one).

## Usage

Register in your `OASIS_DNA.json`:

```json
{
  "DefaultProviders": ["CivicOASIS"]
}
```

## Links

- [OASIS Website](https://oasisomniverse.one)
- [GitHub](https://github.com/NextGenSoftwareUK/OASIS)
- [NuGet](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.CivicOASIS/)
