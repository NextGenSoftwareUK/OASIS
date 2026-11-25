# NianticLightshipOASIS

Niantic Lightship map provider. **Unavailable:** Niantic sunset the Lightship Maps SDK and shut its map endpoints down in October 2025, and Lightship ARDK/VPS is a Unity client SDK with no server-side map API.

Every map operation returns `false` and sets `LastError` to `NianticLightshipOASIS.UnavailableReason`; nothing reports success for work that is not done. For a working real-world map provider use MapLibre, Mapbox, Google Maps, HERE or GO Map.

Part of the [OASIS HOT-Swappable Provider Architecture](https://oasisomniverse.one).

## Usage

Register in your `OASIS_DNA.json`:

```json
{
  "DefaultProviders": ["NianticLightshipOASIS"]
}
```

## Links

- [OASIS Website](https://oasisomniverse.one)
- [GitHub](https://github.com/NextGenSoftwareUK/OASIS)
- [NuGet](https://www.nuget.org/packages/NextGenSoftware.OASIS.API.Providers.NianticLightshipOASIS/)
