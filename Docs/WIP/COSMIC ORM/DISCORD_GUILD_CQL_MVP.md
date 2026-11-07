# Discord public guild CQL MVP

The read profile uses Discord's official v10 Get Invite route for three
published developer community invites. It matches the expected invite
code and guild ID before exposing the guild ID and name. The projected
holon identity uses the stable guild ID and teaching source owner. Invite
codes, member counts, inviter and channel data stay out of the projection.
The HTTP transport uses the fixed Discord origin, reads only the reviewed
keys, and observes Discord's rate-limit headers without replaying a query.

The three reviewed guilds are Python `267624335836053506`, Godot Engine
`1235157165589794909` and Godot Café `212250894228652034`.
The source uses their public invitation metadata and performs no guild
membership or message operation.

At Core `ddd996a55d41c2cd38711b08effc305ff5fd83a3`, the official
service returned exact native CQL 3/2. The wrong-guild and bounded
projection controls passed. Three passed with zero skips. The
5,740-byte TRX SHA-256 is
`99ea3edd1ac8492d436a689219469d87f94c15c767797e8957995b0d24cc4df1`.

WEB4 admission and local federation passed. Hosted acceptance remains open.
[Discord Get Invite](https://docs.discord.com/developers/resources/invite#get-invite) ·
[Discord rate limits](https://docs.discord.com/developers/topics/rate-limits) ·
[Python Discord's published invite](https://www.pythondiscord.com/timeline/) ·
[Godot community links](https://godotengine.org/community/).

## WEB4 admission

Set `CQL_DISCORD_PUBLIC_GUILD_QUERY_SOURCE_OWNER` to the reviewed canonical teaching owner GUID.
The provider admits the fixed teaching catalogue through an independent
read-only permission and tariff. A reviewed settings descriptor
contains the single `Actor` field. The protected settings and HTTP
verifier derive the three exact logical IDs from that owner and the
native catalogue.

The WEB4 producer and tests passed seven Discord/Livepeer admission
checks and seven Polygon gateway regression checks at Core
`cec62ced64ddb5b913f595e4ffc901e95f4c86dd`. The 26,040-byte TRX
SHA-256 is `a4b9c0d872d508be606b816d04b1afa601e91865f3e42eee910245bf955b4346`.
The settings and query evidence suites passed 39 and 21 checks.
Development activation follows LocalFile.

## Local WEB4 and Mongo federation

The two loopback HTTP cases passed at Core
`cec62ced64ddb5b913f595e4ffc901e95f4c86dd`. Each returned native 3/2,
paired 5/4, foreign-private 0 and foreign-public 3. Each acknowledged
two version-one private Mongo saves, checked exact row identities and
recorded nine receipts from the controlled admission layer. Both
owned databases were verified absent after cleanup and the reused
Mongo process shut down cleanly. The 4,679-byte TRX SHA-256 is
`b31cc2e3f8e197ad069c50cd6c4293bbb40b5642a114c70363bcf6b859013d41`.

## Follow-on scoped release candidate

Parent 8645794bbac7c7f8b28c51180db5afa60e586721 and Core
4585be145c13e3c3d43cb9a5b205475f126faa8f are published on
work/cosmic-public-three-release-candidate. The batch contains Discord,
Livepeer and Pinata on top of the World ID candidate. Provider sources
match the tested task branch, including Livepeer's native contract reader.
Manifest/gitlink validation passed. [CI 42332826329](https://github.com/NextGenSoftwareUK/OASIS/actions/runs/42332826329)
is the image gate. Promotion and hosted settings remain open.

Eight shared batch settings and twelve presets are prepared under
LocalState/COSMIC/HostedSourceRolloutCurrent with prefix public-three.
The protected baseline is the intended World ID candidate, so it is
explicitly future state until World ID deployment and settings finish.
Before applying, the guarded publisher must verify the actual live
variables equal this baseline. No hosted values were written.

The immutable image gate passed for exact parent 8645794bb. The candidate
tag resolves to sha256:9186270c613dd1fb90a71f1edbb894d336f39ed5e1527efb5f523ea7c076722f.
Finish the World ID settings deployment and its provider acceptance
before advancing Development to this batch. Recheck remote ancestry and
the live protected baseline at promotion time.

Development advanced to the exact image-qualified candidate 8645794bb.
The live settings matched the intended World ID baseline before promotion.
Railway code job 052ece00-c14b-4aa7-abeb-f89c43cf0c51 is queued. Observe
this exact job before using the guarded publisher for the three-provider
public-three batch. Settings have not been applied.

Code deployment 052ece00 succeeded. The guarded publisher applied and
read back all eight batch settings. Same-source settings job
0ab0d7c9-45b7-4fcc-b1ea-c79c9d6c0eae is building at parent 8645794bb.
Observe the exact job before live readiness and provider acceptance.
