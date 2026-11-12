# Infura capability and CQL status

8 November 2026. The higher-priority live demos and their open gates remain in
[CURRENT_STATUS](CURRENT_STATUS.md). Infura is an independent remaining network
provider: current [MetaMask Connect guidance](https://docs.metamask.io/metamask-connect/)
uses Infura for supported EVM networks and relay access. This supports prioritizing
it among remaining RPC services; it is not a global popularity measurement.

## Retired storage contract

[Infura's official IPFS notice](https://support.metamask.io/develop/ipfs) says
uploads/pinning stopped 3 August and all access ended 15 August 2026. The old
provider still constructed that HTTP client, advertised storage and attempted
add/cat/list/unpin, with instructions to fix credentials on failure. This is an
upstream capability retirement, not a transient endpoint/authentication failure.

The provider now advertises network/Blockchain capability, removes the retired
IPFS client and returns `INFURA_IPFS_RETIRED` for its legacy canonical holon
operations, retaining public signatures. No substitute provider, automatic
migration, CID deletion, remote retry or write. Existing identifiers/data remain
intact; a retained copy can be explicitly placed on a supported IPFS provider.
Three actual-provider controls cover advertised capabilities, typed read/write
failures and unchanged retained CID/input. Exact cloud compile/test is accepted:
[42535999021](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42535999021),
job128900305746, frozen source122ae35d343d112e21fac550a151114b955e0005/Coree62ec44.
Artifact13639481220 (1638bytes), SHA256
`d3a707b762aa37fb0e4deb03ab6dce3f32de130675b7a36d63018452062cbe24`
verified in memory: exact3/3 tests executed/passed, zero skipped. WEB4's
historical descriptive catalogue now states the same scope. Only description
text changed there; no enrollment, CQL API or route change. No live deployment.

## Current RPC and remaining demo gates

The existing Ethereum JSON-RPC code is separate from retired storage. It still
has unverified legacy behavior, including an assumed demo URL and missing-value
projection. It is not a qualified native CQL reader. Current [Infura setup](https://docs.infura.io/get-started/infura/)
requires an authorized key on the official Ethereum endpoint; no anonymous/demo
key request was made. [Current key policy](https://docs.infura.io/dashboard/how-to/secure-an-api/api-key/)
requires that key in the request path. For a secret-required key, use the exact
[official HTTPS Basic authentication example](https://docs.infura.io/dashboard/how-to/secure-an-api/api-key-secret/#call-apis-using-an-api-key-secret)
and keep both values in private server configuration. No new paid account/hosting or credential values retained.

The scoped Infura reader reuses the bounded EVM engine for chain/genesis checks
and finalized canonical account balances. It requires a server API key and secret
and uses Infura Ethereum mainnet. Cloud run 42536290104 compiled the reader with
the retirement controls.

WEB4 source 78607a087 and Core 64a691b add opt-in registration and query tariff
eligibility. The configuration names are CQL_INFURA_QUERY_SOURCE_OWNER,
CQL_INFURA_API_KEY and CQL_INFURA_API_SECRET. Use protected server settings for
the credentials. The fixed teaching set contains the zero address, burn address
and Ethereum deposit contract. The first registration test failed while populating its configuration fixture.
The fixture now uses an in-memory configuration source. Corrected run
[42536679933](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42536679933)
passed all five tests, including registration, tariff and credential
destination and error redaction. Artifact 13640160084 was verified in memory
against SHA256 `d7424619ac791576f0e2503251b92ba646a7d20d47afbb9dca148add7d51f458`.
The shared owner list tracks access for native verification.

Remaining gates include inspecting that run, verifying credential handling and
native reads with authorized access, qualifying the exact WEB4 package and paired
query flow, then live and browser acceptance. The independent IPFS demo covers
IPFS separately from the retired Infura service.

The hosted verifier now accepts an actor-only Infura catalogue and derives the
three fixed account IDs and exact address filters. Credentials stay in protected
server settings. All 40 local hosted-configuration tests passed using temporary
fixtures. Native Infura and hosted acceptance still require authorized access.

The C# mapper assertions and hosted presets agree for all three Infura IDs.
Cloud run 42539832668 passed five tests at 6df08d93f. Its digest-verified artifact
13639999278 includes the passing registration and ID assertions. The added
Python regression raised the shared configuration suite to 47. Corrected run
42540103480 passed all 47 with zero skips and zero outbound HTTP calls.

The full-image qualifier can require the Infura reader, its WEB4 registration
and the packaged project dependency. Eight local verifier tests and the cloud
control run 42540357920 passed. The exact teaching image for `da74323d2e22d21fbcc2c953010a000421f68ce9` published
successfully in build 42540376773. Digest `sha256:a66123e51d83ea18d290d7028764b6a9de6ccec4433518b46f0575294c50b05c`
passed [package and startup qualification](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42543310651).
Artifact 13642158093 matched SHA256 `fb8c80a0b25911bee0420f6f01858064f4e6316edd82a21b0f3600f6678eaa5f`. Nine assemblies,
Infura, TRON and Urbit readers, helper/model checks, API and Docker health,
native metadata, exit 0 and owned resource cleanup passed. Native Infura
access and live acceptance remain separate gates.

The opt-in native harness compiled at aa6c835e4 and the five controlled tests
passed in [42543704050](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42543704050).
Artifact 13642457448 matched SHA256 `8885a7a330d5e52e1f93f336ff81827ff42e742ecbec266e4e0e254963f4182b`.
The workflow's authenticated_native input enables one native test after the
authorized key and secret are available as protected Actions settings. It uses
the registered reader to verify three finalized accounts and a two-account
subset, with the exact preset IDs. The native case awaits authorized access.

12 November UTC. Checked the three scoped Infura CQL settings by name only. Neither the recovery laptop nor WEB4 Development has any of them. The existing protected access action remains open. Prepared local Core candidate 9e4ee4a0 on the verified Urbit Core candidate 7ea7639c. It adds only the explicit Infura read tariff and preserves Urbit and TRON. WEB4 reconciliation and exact qualification remain open. No hosted request, tag or Development push.

12 November UTC. Candidate 3c5559c601 carries the existing Ethereum reader, its protected configuration, the explicit Core read tariff and the reviewed retirement behavior for the discontinued Infura IPFS transport. It preserves the Urbit candidate and uses matching Core gitlink and Railway manifest pin 9e4ee4a0. [Controlled CI 43163893924](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43163893924) is queued. No authenticated Infura request or Development push.

12 November UTC. Exact candidate 3c5559c601/Core 9e4ee4a0 passed all five controlled Infura cases in [CI 43163893924](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43163893924). These cover explicit reader configuration, credential transport and sanitization, and retirement behavior that preserves retained CID input. The authenticated case was disabled and hosted access remains open. Retain the CI refs for the next integration batch after Urbit.

12 November UTC. Exact current-Core candidate 2420a01cb9 passed all five controls in CI 43165191767. [Exact commit-only WEB4 image CI 43166337078](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/43166337078) is pending behind the earlier image build. Authenticated qualification and hosted enrollment remain open.
