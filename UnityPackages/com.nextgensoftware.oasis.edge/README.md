# OGEngineClient for Unity

OGEngineClient is the reusable game and application client for OASIS. This Edge-enabled distribution contains the
same shared client API plus the lightweight OASIS Edge Runtime, HyperDrive synchronization, ONET runtime, durable
SQLite state and the platform libraries required by Unity IL2CPP builds.

## Supported platforms

- Android: runtime, SQLite and Android Keystore secure-session storage.
- iOS: runtime, SQLite and Keychain secure-session storage.
- Windows Editor and Standalone: runtime, SQLite and Windows Credential Manager secure-session storage.
- Linux and macOS: runtime and SQLite are packaged, but protected offline-session storage is not yet provided.
  Use remote-only operation on those platforms until a protected store is qualified.

## Install and start

Install the generated package through Unity Package Manager, then read `Documentation~/index.md`. A small status
listener is available from Package Manager under **Samples > Quick Start**.

The host application supplies its own ONODE URL and authentication token at runtime. No endpoint, account,
credential, API key or signing key is embedded in this package. Offline operation requires a signed offline grant
issued by the application's hosted ONODE.

## Distribution profiles

This package is the Edge-enabled profile and includes local persistence and synchronization. The same OGEngineClient
source also produces a Remote-Only profile for applications that deliberately require the smallest footprint and do
not need offline support.

See `THIRD PARTY NOTICES.md` for bundled component licences and `CHANGELOG.md` for release history.
