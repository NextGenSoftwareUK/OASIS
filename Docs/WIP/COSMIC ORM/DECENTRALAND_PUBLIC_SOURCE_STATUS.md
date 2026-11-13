# Decentraland public scene source qualification

Updated2November2026. Red: native/CQL/hosted/browser acceptance not yet run.

## Evidence and current contract

Existing provider: Providers/Maps/NextGenSoftware.OASIS.API.Providers.DecentralandOASIS.
Legacy code remains unchanged; SDK/CRUD presence is not query acceptance.

[Official content API](https://docs.decentraland.org/apis/apis/catalyst/content-server)
and [pointer semantics](https://docs.decentraland.org/contributor/content/pointers)
document bounded active-entity lookup by pointers or IDs. Multiple parcel pointers
can resolve to one scene, so three pointers do not prove three distinct entities.

Official examples pinned to decentraland/catalyst-api-specs commit
4d6397c8bd2dd5c2456ba634c3f49906090af013 are retained with SHA256manifest in
LocalState/COSMIC/DecentralandPublicQualificationCurrent/sdk.
About response includes healthy/content/configurations.networkId. Active entity
responses are arrays of manifests with id/type/timestamp/pointers/content/metadata;
scene display.title and scene.parcels are relevant fields. Examples are schema
fixtures, never evidence of current native records or operator availability.

## Planned native gate

1. One bounded GET https://peer.decentraland.org/about, verify mainnetnetwork1
   and healthy content service, without following advertised alternate hosts.
2. One read-only POST /content/entities/active for three separated known parcel
   pointers (0,0; -53,71; -100,-50), under an exclusive qualification intent.
3. Require exact requested-pointer coverage and three distinct sceneentityIDs;
   duplicates/missing/foreign records fail. Preserve IDs for immutable source
   qualification; an active pointer may change on a later deployment.
4. A two-pointer subset must resolve the same records. Preserve original failure
   or denial; do not silently substitute profiles, scenes, hosts or transports.

Only whitelist public scene identity/title/parcel references/timestamp/counts.
Exclude contact details, arbitrary metadata and asset/code bodies. No downloads
of referenced files or execution/rendering of scene code. No ownership/signature,
CID integrity, freshness or consensus claim without independent verification.

No native endpoint request, identity fixture, CQL grant, install, account, node or
paid resource has been created. ClickHouse hosted rollout remains first priority.

## Original endpoint access gate - 2 November 2026

Fiveofflinecontrols pass. FirstanonymousGET https://peer.decentraland.org/about returnedHTTP403/17bytes; SHA2562938e9f1284180959e33ab1718d0793a72ff6e4cdb8108c34dcd14e69446de5c. Mainnetidentity/notready not verified; bothscenePOSTs were notsent. Nativewrites0/assetsdownloaded0. Originalintent/report/compresseddenial/manifest durable DecentralandPublicQualificationCurrent; raw TestResults/DecentralandPublicSourceCurrent/response-1.json. No retry/alternateheaders/transport/host/proxy, account/node/install or CQLgrant. HTTP403alone does not prove allCatalysts unavailable; legitimate permitted access remains a source-specific external gap. No user action requested while other breadth work remains.
