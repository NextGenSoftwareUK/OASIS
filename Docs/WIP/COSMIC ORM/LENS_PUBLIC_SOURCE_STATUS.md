# Lens public-account demo qualification

Updated 2 November 2026. Discovery only; not native-qualified, implemented or
admitted to CQL. Provider/hosted/browser totals unchanged.

## Verified interface distinction

The [official SDK account actions](https://github.com/lens-protocol/lens-sdk/blob/main/packages/client/src/actions/account.ts)
define fetchAccount, fetchAccounts and fetchAccountsBulk on AnyClient. A session
adds account-specific operations; public read support in source still needs
native verification. The [SDK README](https://github.com/lens-protocol/lens-sdk)
identifies its work as Lens Protocol v3.

Existing LensOASIS comments call api.lens.xyz/graphql a v2 profile API, whereas
the current SDK uses account requests. LensV2OASIS separately targets
api-v2.lens.dev. Do not submit legacy profile operations to a current endpoint,
change legacy code or count v3 accounts as proof of LensV2 support.

Official website API/account documentation could not be fetched by the web
tool. A GitHub directory request returned429; no repeat, proxy or bypass was
attempted. This is documentation-tool evidence, not a native API failure.
Search also returned scholarly/patent Lens.org APIs; these are unrelated and
excluded from the provider design.

## Exact next step

Inspect the official SDK's mainnet environment, GraphQL account request types
and minimal account response selection after rate limiting permits access.
Pin the inspected revision and confirm public-read limits/terms. Then write
a first bounded qualification with an exclusive intent, strict GraphQL error/
identity/shape validation, explicit response/time bounds and no retries.
Choose several real account keys from a bounded native discovery before a
CQL source is admitted. Do not install/clone the SDK or create wallets/accounts.
No native request, source owner, config, fixture, permissions, tariffs, hosting
settings or sandbox card exists for this scope yet. No David action is needed.
