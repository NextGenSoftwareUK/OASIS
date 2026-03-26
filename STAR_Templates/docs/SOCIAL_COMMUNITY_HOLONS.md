# Social & Community Holons

This document covers three templates that form the community layer of STARNET. They can be used standalone or composed: a `ClanGuildTemplate` OAPP might embed `SocialGraphTemplate` for its chat, and a `GameTemplate` OAPP might embed `CompetitionTournamentTemplate` for its ranked seasons.

---

## SocialGraphTemplate

**STARNET ID:** `05495506-4679-4519-b421-d3edfba56daf`  
**DNA:** `STAR_Templates/star_dna/SocialGraphTemplate.json`  
**Backed by:** SocialController · MessagingController · ChatController · ShareController

### What it does

A decentralised social layer where **every piece of content and every relationship is a holon** — no centralised platform, no proprietary feed algorithm. The OASIS Avatar is the universal identity; content is portable across every OAPP on STARNET.

```
SocialProfileHolon (extends Avatar)
  ├─ FollowHolon[]        (directed graph — who follows whom)
  ├─ BlockHolon[]         (blocked relationships)
  ├─ PostHolon[]          (text, image, video, repost, quote)
  │    └─ ReactionHolon[] (likes and emoji reactions)
  ├─ ConversationHolon[]  (DMs and group chats)
  │    └─ MessageHolon[]  (individual messages)
  ├─ ShareHolon[]         (shares to other Avatars / OAPPs / platforms)
  └─ NotificationHolon[]  (follow, mention, reaction, message alerts)
```

---

### Zome 1 — ProfileZome

#### SocialProfileHolon
Extends the OASIS Avatar with social-specific display fields.

| Field | Purpose |
|---|---|
| `DisplayName` | Social handle (different from Avatar username if desired) |
| `Bio` | Short text bio |
| `AvatarImageUrl` / `CoverImageUrl` | Profile and banner images |
| `Visibility` | `public` / `followers` / `private` — controls who can see posts |
| `FollowerCount` / `FollowingCount` | Denormalised counters (updated on FollowHolon write) |
| `KarmaScore` | Pulled from AvatarDetail — visible reputation signal |

One profile per Avatar per OAPP. Multiple OAPPs can each have their own SocialProfile for the same Avatar (e.g. different personas in different communities).

---

### Zome 2 — ConnectionZome

#### FollowHolon
A directed edge in the social graph: `FollowerAvatarId` follows `FolloweeAvatarId`.

- `IsMutual: true` is set when the follow is reciprocated — this is the "friend" state.
- `NotifyOnPost: true` enables push notifications when the followee posts.
- **No mutual requirement** — asymmetric follows are the default (Twitter-style).

#### BlockHolon
Soft-removes a user from your experience. When `BlockerAvatarId` blocks `BlockedAvatarId`:
- Posts by the blocked user are hidden
- DMs cannot be sent
- FollowHolons between the pair are soft-disabled

---

### Zome 3 — FeedZome

#### PostHolon
The core content unit.

| Field | Notes |
|---|---|
| `PostType` | `text` / `image` / `video` / `link` / `repost` / `quote` |
| `ParentPostId` | Set for replies — builds threaded conversations |
| `OriginalPostId` | Set for reposts — chains back to the source |
| `Visibility` | Per-post visibility (overrides profile default) |
| `LikeCount` / `RepostCount` / `ReplyCount` | Denormalised for fast feed rendering |
| `Tags` | `string[]` — hashtag-style content tagging |
| `IsDeleted` | Soft delete — preserves thread structure |

**Threading model:** `ParentPostId` creates a tree. Walk the tree by querying `WHERE ParentPostId = X`. No depth limit enforced by the holon model.

**Repost chain:** `OriginalPostId` always points to the root post regardless of repost depth — you can always find the source.

#### ReactionHolon
One record per Avatar per post per reaction type. `ReactionType` enum: `like`, `love`, `laugh`, `wow`, `sad`, `angry`, `celebrate`.

To get total reactions: query `WHERE PostId = X` and group by `ReactionType`.

---

### Zome 4 — ChatZome

#### ConversationHolon
Container for a message thread — DM or group.

| Field | Notes |
|---|---|
| `ConversationType` | `direct` (2 participants) or `group` (N participants) |
| `ParticipantIds` | `string[]` of Avatar IDs — add to extend the group |
| `LastMessageAt` | Denormalised for conversation list sorting |
| `IsArchived` | Soft-archive for the creator |

#### MessageHolon
One record per message sent.

| Field | Notes |
|---|---|
| `MessageType` | `text` / `image` / `file` / `voice` / `system` |
| `ReadByIds` | `string[]` of Avatar IDs who have read the message |
| `IsDeleted` | Soft delete — message body is cleared, record preserved |

**Read receipts** are derived: if `AvatarId` is in `ReadByIds`, the message is read. Unread count = `WHERE ConversationId = X AND AvatarId NOT IN ReadByIds`.

---

### Zome 5 — ShareZome

#### ShareHolon
Any holon can be shared — not just posts. `TargetHolonType` identifies what was shared (Post, Quest, NFT, PropertyHolon, etc.). `Platform` enables cross-platform sharing signals (shared to Twitter, Telegram, etc.).

#### NotificationHolon
Real-time signal layer. `NotificationType` covers: `follow`, `mention`, `reaction`, `reply`, `repost`, `message`, `system`. `IsRead: false` drives the unread badge count.

---

---

## SubscriptionMembershipTemplate

**STARNET ID:** `dc2562b4-95af-4063-85ec-0d4ee797b717`  
**DNA:** `STAR_Templates/star_dna/SubscriptionMembershipTemplate.json`  
**Backed by:** SubscriptionController · KarmaController · NftController · WalletController

### What it does

Tier-based gated access where the **gate condition can be anything**: pay monthly, hold a token, own an NFT, or just accumulate enough Karma. Membership is an NFT — transferable, verifiable on-chain.

```
MembershipTierHolon  (the rule)
  ├─ BenefitHolon[]         (what the tier unlocks)
  │    └─ AccessGrantHolon[] (issued to active members)
  ├─ MembershipHolon        (an Avatar's active subscription)
  └─ SubscriptionPaymentHolon[] (payment history)
```

---

### Zome 1 — TierZome

#### MembershipTierHolon
Defines a tier and its **access rules**. Multiple gate types can be combined:

| Gate type | Field | Example |
|---|---|---|
| Payment | `MonthlyPrice` / `AnnualPrice` | £9.99/month |
| Karma | `MinKarmaScore` | Require ≥ 500 Karma |
| Token | `RequiredTokenId` + `RequiredTokenBalance` | Hold ≥ 100 OASIS tokens |
| NFT | `RequiredNftCollectionId` | Own any NFT from collection X |

`MaxMembers` enables scarcity — once the cap is reached, the tier is closed to new members.

---

### Zome 2 — MemberZome

#### MembershipHolon
One record per Avatar per tier (active + history).

| Field | Notes |
|---|---|
| `BillingCycle` | `monthly` / `annual` / `lifetime` / `token-gated` / `karma-gated` |
| `MembershipNftId` | The NFT representing this membership — transferable |
| `AutoRenew` | Triggers payment on expiry |
| `Status` | `active` / `paused` / `cancelled` / `expired` / `pendingPayment` |

The `MembershipNftId` is the proof token. Transferring the NFT to another Avatar transfers the membership. The receiving Avatar's wallet is checked against tier requirements on any access attempt.

---

### Zome 3 — BenefitZome

#### BenefitHolon
Defines what a tier actually unlocks. `BenefitType` covers a wide range:

| Type | What it enables |
|---|---|
| `contentAccess` | Unlocks gated content (`AccessScopeId` identifies the content) |
| `discountPct` | Percentage discount at checkout |
| `nftDrop` | Triggers an NFT mint on membership activation |
| `earlyAccess` | Time-shifted access to new features/content |
| `karmaBoost` | Multiplier on Karma earned in this OAPP |
| `physicalPerk` | Physical-world benefit (tracked via description) |
| `apiAccess` | API rate limit tier increase |

#### AccessGrantHolon
The enforcement record — created when a benefit is granted to a member. `IsRevoked: true` on membership cancellation. Checking access: `WHERE AvatarId = X AND BenefitId = Y AND IsRevoked = false AND ExpiresAt > now`.

---

### Zome 4 — PaymentZome

#### SubscriptionPaymentHolon
One record per billing event. `PaymentMethod` is flexible: `crypto`, `fiat`, `karma` (Karma as currency), `token`, `nft` (NFT burn as payment). `TxHash` is set for on-chain payments. `PeriodStart` / `PeriodEnd` define what billing period was paid.

---

---

## CompetitionTournamentTemplate

**STARNET ID:** `a64f5fda-1f71-4ef2-88fc-6d490f562633`  
**DNA:** `STAR_Templates/star_dna/CompetitionTournamentTemplate.json`  
**Backed by:** CompetitionController · EggsController · GiftsController · KarmaController · NftController

### What it does

Structured competitions with brackets, rounds, match results, leaderboards, and prize distribution — all Avatar-native. Works standalone or embedded inside a `GameTemplate` (seasonal ranked play) or `ClanGuildTemplate` (inter-clan tournaments).

```
TournamentHolon
  ├─ ParticipantHolon[]    (registered Avatars)
  ├─ RoundHolon[]
  │    └─ MatchHolon[]     (results + proof holons)
  ├─ LeaderboardEntryHolon[] (live rankings)
  ├─ PrizeHolon[]          (prize pool per placement)
  │    └─ RewardClaimHolon[] (claimed prizes)
  └─ EggChallengeHolon[]   (embedded timed challenges)
```

---

### Zome 1 — TournamentZome

#### TournamentHolon
Defines the competition.

| Field | Notes |
|---|---|
| `Format` | `singleElimination` / `doubleElimination` / `roundRobin` / `swiss` / `ladder` / `openChallenge` |
| `MaxParticipants` | Bracket size cap |
| `MinKarmaToEnter` | Karma gate — keeps quality floors high |
| `EntryFee` | Optional paid entry (creates prize pool contribution) |
| `GameOAPPId` | Links to a GameTemplate OAPP if this is in-game |

#### ParticipantHolon
One record per registered Avatar.

| Field | Notes |
|---|---|
| `Seed` | Seeding position (set by organiser before bracket generation) |
| `TeamName` | Optional — for team-based formats |
| `Status` | `registered` → `active` → `eliminated` / `winner` |

---

### Zome 2 — BracketZome

#### RoundHolon
Groups all matches at the same stage. `RoundNumber` drives the display order (1 = first round, N = final). `Status: completed` triggers automatic bracket advancement.

#### MatchHolon
One match between two participants.

| Field | Notes |
|---|---|
| `Score1` / `Score2` | Numeric scores (game-specific interpretation) |
| `WinnerParticipantId` | Set on completion — drives bracket advancement |
| `ProofHolonId` | Link to a screenshot, replay file, or on-chain proof |
| `Status` | `scheduled` / `live` / `completed` / `disputed` / `walkover` |

**Dispute resolution:** `Status: disputed` freezes the match. An admin Avatar resolves it, setting `WinnerParticipantId` and reverting to `completed`.

---

### Zome 3 — LeaderboardZome

#### LeaderboardEntryHolon
Live ranking row. `TournamentId` is null for global leaderboards. `Score`, `Wins`, `Losses`, `XPEarned`, `KarmaEarned` are updated after each match. `AsOfDate` enables historical snapshots.

---

### Zome 4 — RewardZome

#### PrizeHolon
Defines what each placement wins. `PrizeType` covers: `tokenAmount`, `nft`, `karma`, `gift`, `badge`, `physical`.

- **`tokenAmount`**: `TokenAmount` + `Currency` — paid from prize pool on-chain
- **`nft`**: `NftCollectionId` — mint from a specific collection
- **`karma`**: `KarmaAmount` — added directly to Avatar's Karma score
- **`gift`**: Physical item tracked via `Description` + fulfilment flow
- **`badge`**: Permanent achievement NFT

#### RewardClaimHolon
The redemption record. `Status: pending` → `claimed` → `delivered`. `TxHash` set when on-chain distribution occurs.

#### EggChallengeHolon
An embedded timed challenge — from the EggsController. `ChallengeType` covers: `speedrun`, `highScore`, `survival`, `puzzle`, `creative`. Can exist within a tournament (semifinal egg challenge) or standalone (daily community challenge). `MaxAttempts` limits re-tries. `RewardHolonId` points to the prize for completion.

---

### Composition Example: Clan War Tournament

```
ClanGuildTemplate OAPP
  └─ embeds CompetitionTournamentTemplate
       └─ TournamentHolon (Format: roundRobin, MaxParticipants: 8 clans)
            └─ ParticipantHolon[] (one per clan, TeamName = clan name)
            └─ RoundHolon[] (round-robin weeks)
                 └─ MatchHolon[] (clan vs clan, Score = match points)
            └─ LeaderboardEntryHolon[] (live clan table)
            └─ PrizeHolon (1st: NFT trophy + Karma boost, 2nd: token award)
                 └─ RewardClaimHolon (winning clan Avatar claims prizes)
```
