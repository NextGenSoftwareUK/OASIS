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

WEB4 admission passed. Local federation and hosted acceptance remain open.
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
