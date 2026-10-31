# TigrisOASIS

Tigris - globally distributed object storage with S3-compatible API.

The configured bucket must already exist. Activation performs a read-only bucket
metadata request and propagates service/authorization errors; it never provisions
a bucket. Only an explicit `NoSuchKey` response is treated as an absent object.

Part of the [OASIS HOT-Swappable Provider Architecture](https://oasisomniverse.one).

## Usage

Register in your `OASIS_DNA.json`:

```json
{
  "DefaultProviders": ["TigrisOASIS"]
}
```

## Links

- [OASIS Website](https://oasisomniverse.one)
- [GitHub](https://github.com/NextGenSoftwareUK/OASIS)
- [NuGet](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.TigrisOASIS/)
