# Wizard Account Overrides

OASIS has a `Wizard` avatar type (`AvatarType.Wizard = 0`) which is the admin role. Certain API endpoints behave differently when the caller is a Wizard, allowing service accounts to perform operations on behalf of regular users.

## How it works

The `JwtMiddleware` populates `HttpContext.Items["Avatar"]` for any request that includes a valid `Authorization: Bearer` header. The `OASISControllerBase.Avatar` property reads from this, so `Avatar?.AvatarType.Value == AvatarType.Wizard` is available in any controller without an explicit `[Authorize]` attribute.

---

## Endpoint overrides

### `POST /api/Avatar/register`

**Normal behaviour:** Returns the new avatar with auth details hidden (`VerificationToken` stripped).

**Wizard override:** If the caller is a Wizard, `VerificationToken` is included in the response. This allows service accounts to immediately verify a newly registered avatar's email without the user needing to check their inbox.

**Where:** `AvatarManager-Private.cs → AvatarRegistered()`, `AvatarManager.cs → Register/RegisterAsync`, `AvatarController.cs → Register()`

**Use case:** `oasismint` service account registers a buyer's avatar during NFT purchase flow, then immediately calls `verify-email` with the returned token so the avatar is active.

---

### `POST /api/nft/mint-nft`

**Normal behaviour:** `MintedByAvatarId` is always set from the JWT caller's avatar ID, ignoring any value passed in the request body.

**Wizard override:** If the caller is a Wizard and passes a valid `MintedByAvatarId` GUID in the request body, that value is used instead. This attributes the minted NFT to the specified avatar rather than the service account.

**Where:** `NftController.cs → MintNftAsync()`

**Use case:** `oasismint` mints NFTs on behalf of buyers so the NFTs appear under the buyer's avatar in OPORTAL (which queries holons by `NFT.MintedByAvatarId`).

---

## The `oasismint` service account

`oasismint` is a Wizard-type avatar used by the NFT Founders Landing Page to automate the mint-on-purchase flow. Its credentials are stored as environment variables on the Vercel deployment.

The full flow it performs:
1. Authenticate as `oasismint` to get a Wizard JWT
2. Look up the buyer's avatar by email (`GET /api/Avatar/get-by-email/{email}`)
3. If no avatar exists, register one (passing the Wizard JWT — gets `VerificationToken` back), then verify the email
4. Mint the NFT, passing the buyer's `avatarId` as `MintedByAvatarId` so it appears under their account
5. Store an activation record in Redis so the buyer can claim their account via the activation portal

---

## Security note

Wizard accounts can only be created by directly setting `avatarType: 0` in the MongoDB `Avatars` collection. There is no API endpoint that allows self-promotion to Wizard. Knowing this pattern exists is therefore not exploitable without existing database admin access.
