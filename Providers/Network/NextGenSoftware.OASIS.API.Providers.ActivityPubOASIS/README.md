# NextGen Software WEB4 OASIS API ActivityPub OASIS Provider

WEB4 OASIS API ActivityPub OASIS Provider is one of the many WEB2/WEB3 providers that plug in and integtate into the OASIS.

This public-social adapter supports native Mastodon account IDs and public handles, not private OASIS email identity storage. Avatar/detail lookup and exports by email fail explicitly before HTTP; email is never substituted as a public account-search query. Use a private storage provider for email lookups. Generic GUID projection lookup and private record saves remain unsupported,not completed capabilities. Current evidence and remaining social/authenticated/V2 gates: `Docs/WIP/HyperDrive-Providers/ACTIVITYPUB_PROVIDER_IMPLEMENTATION_AUDIT.md`.

Search supports public accounts and statuses,including mixed text groups and AND/OR membership,through native bounded paging. A user token with `read:search` is required for complete offset paging; account search sets `resolve=false`. Accounts reuse the canonical public-profile projection and exclude private credentials/email. Omitted type (`Default`) and `All` are unrestricted; explicit types filter projected roots. Controlled tests and public-read regression pass,while live authenticated search remains pending separately; this is not full provider certification.

`LoadAccountBatchAsync(nativeAccountIds)` and `LoadAccountDetailBatchAsync(nativeAccountIds)` (with synchronous counterparts) read explicit native account IDs using [Mastodon4.3+ account batches](https://docs.joinmastodon.org/methods/accounts/). Both reuse the point-read public profile projection and the status batch reader's bounds, input snapshot, order and complete-response validation. Supply1–40 distinct decimal IDs;40 is local policy. Missing or unconfirmed accounts may be omitted. Profile/detail GUIDs match for the same instance/native ID; email, credentials and generic private fields are excluded. These calls do not enumerate every account or expose OASIS version history. `LoadAllAvatars`, `LoadAllAvatarDetails` and geographic collection methods fail before HTTP because this native endpoint supplies neither complete collections nor a geographic index.

`LoadStatusBatchAsync(nativeStatusIds)` (and synchronous `LoadStatusBatch`) performs one explicit-ID request using [Mastodon4.3+ `GET /api/v1/statuses?id[]=...`](https://docs.joinmastodon.org/methods/statuses/#index). Supply1–40 distinct native decimal status IDs;40 is this adapter's local admission bound, not a claimed server limit. Input is frozen before activation, returned roots follow input order, and inaccessible/missing statuses may be omitted by the native service. Private reads require the appropriate user token. This returns flat projected statuses, not edit history, reply graphs or a complete collection. `LoadAllHolons` now fails before HTTP rather than misusing that endpoint without IDs. Use the existing explicit `LoadPublicTimelinePage` for cursor-based public timeline reads; it is not substituted for complete private storage.

Initial Release targeting OASIS API v2.3.1 (Current LIVE REST version): \
https://api.oasisweb4.one

Full documentation and source code can be found here: \
https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK

Make sure you check out the Overview and Benefits Of Building On The WEB 4 OASIS API sections above! :) Thank you!
