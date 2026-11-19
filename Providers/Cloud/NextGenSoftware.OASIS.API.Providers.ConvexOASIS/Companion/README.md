# Convex OASIS private-record companion

This companion supplies the real indexed query/mutation operations used by ConvexOASIS. The .NET adapter delegates record/query/history/encryption policy to the existing shared key-value base. This deployment stores encrypted values and opaque keys in oasis_records; it does not expose plaintext avatar fields.

## Deployment

Reuse an existing installation of the pinned Convex1.48.0 package. If absent, install once with npm ci and retain it. Use an existing TypeScript5.5+ compiler with tsconfig.json; do not create a fresh environment per deployment. Configure CONVEX_SELF_HOSTED_URL and CONVEX_SELF_HOSTED_ADMIN_KEY for self-hosting, or the appropriate protected deployment credentials for cloud hosting. Never place administrator credentials in browser/public environment variables.

Run the installed Convex CLI from this directory: convex dev --once --typecheck disable --codegen disable, after a separate successful typecheck. The source uses official internalQueryGeneric/internalMutationGeneric bindings and needs no generated server stubs. Internal functions require administrator authorization for direct Functions HTTP API calls. Ordinary anonymous calls are rejected by Convex itself.

## .NET configuration

Existing ConvexOASIS(deploymentUrl, deployKey) and injected-HttpClient constructors require OASIS_CONVEX_STORAGE_KEY: canonical Base64 for a separately persisted32-byte key. Retain that key across restarts, separate from administrator credentials. The explicit resolver overload accepts temporary key copies, which are zeroed after copying. The provider owns only clients it creates or explicitly receives with ownsHttpClient=true; dispose the provider when finished.

The deployment URL must be a root HTTPS origin, or explicit loopback HTTP for local testing. Owned clients disable redirects; supplied clients must not silently replay uncertain writes. Credentials are attached per request, leaving caller default headers unchanged.

## Storage contract

Private functions under oasis/records implement verify, get, put, remove and list. Mutations execute atomically in the native engine; conditional operations compare the observed document-id/revision token in the same mutation. This is per-key atomicity, not a multi-record transaction. Pagination uses an indexed range and explicit complete/cursor fields; no100-record truncation is accepted by the client.

verify refuses existing oasis_avatars, oasis_avatar_details or oasis_holons records. Preserve any legacy data for a reviewed migration; no automatic reset or plaintext fallback is provided. Stop legacy writers before adopting this layout. Default holon deletion stays soft. Archived records remain readable by version, but archived wallet/token records cannot authenticate.

The native baseline covers the common storage suite, revision conflicts, pagination and exact fixture cleanup. Hosted deployment IAM, full relevant interfaces, concurrency/version stress and global HyperDrive routing are separate acceptance gates.
