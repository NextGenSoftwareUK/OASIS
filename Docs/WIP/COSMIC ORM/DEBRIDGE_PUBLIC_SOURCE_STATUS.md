# deBridge public source qualification

4 November 2026. The existing DLN stats API bounded discovery returned HTTP
200 with three distinct canonical order IDs. Three independent exact order
reads returned HTTP 200 and matching order IDs. The first two define the
future CQL subset. No quote, wallet, or transaction operation was performed.

Next bind the retained catalogue to a bounded CQL profile using the existing
native client. Validate exact identity and preserve native integer precision.
This qualifies indexed order reads only. CQL and WEB4 enrollment remain open.

Raw evidence is in TestResults/DeBridgePublicSourceCurrent. Discovery SHA256 8ED7FD3DBE2426E8FE344B12BB883D91086B322E1AEE30133EE39F143046E89B.

[deBridge explorer contract](https://debridge.com/support/what-is-deexplorer/)

## Native CQL verified

The bounded profile reuses the existing DLN exact-order client. It rejects
wrong IDs and invalid amounts, preserves native give amounts as decimal
strings, and reports a failed page without partial rows. Five controlled
cases and one live native test passed, zero skips, with exact 3/2 selections.
Native TRX SHA256 F2E0823C5BF92B45C3DBE08BA6662ACC9166D535F6A93429D6994F2817F8D08F.
Next: opt-in WEB4 admission, four preset plans, and local federation/privacy.

## WEB4 local HTTP verified

Opt-in admission and four preset plans passed. The isolated WEB4 HTTP run
returned native 3/2, paired 5/4, foreign-private 0/public 3 and nine metering
receipts. Both test snapshots were removed with the isolated database. The
retained Mongo replica stopped. The hosted publisher accepted both receipts.
Local HTTP TRX SHA256 75B2CD5A95E82CF8B8E28C309C6FC60070939D0FB263337051A102F8FD59C33E.
Hosted settings, replicas and browser acceptance remain open.
