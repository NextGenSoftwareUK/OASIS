# Decentraland Genesis City scene CQL MVP

The read profile resolves three reviewed Genesis City parcel pointers
through the Decentraland Foundation's production Catalyst scene API.
It validates that each current scene explicitly includes the selected
parcel and its declared base parcel. A stable CQL ID identifies the
parcel, while the active scene entity CID and deployment timestamp can
change when a scene is replaced.

The projection includes the parcel, base, public title, active entity
CID, deployment timestamp and parcel count. Scene assets and individual
profile data stay outside the projection.

| Exact parcel | Scene observed 6 November 2026 |
| --- | --- |
| `0,0` | Genesis Plaza |
| `-53,71` | METATOKYO2 |
| `-100,-50` | Road at -100,-50 |

[Decentraland pointer contract](https://docs.decentraland.org/contributor/content/pointers) ·
[Decentraland entity manifest contract](https://docs.decentraland.org/contributor/content/entities).

At Core `c1354d87da2add7720f9c40d4e502195c01a3f41`, native CQL
returned exact 3/2 rows from production Catalyst. Both rejection
controls passed for an absent requested parcel or absent base parcel.
Three checks passed with zero skips. The 5,848-byte TRX SHA-256 is
`f25f800b3680763f12460f2d2a33f635c7afbeb0bcbfd76239ea6732afd41bdf`.
WEB4 admission, local federation and hosted acceptance remain open.
