# Free place source setup

Geoapify is recommended for reliable nearby-place queries while retaining OSM identities and attribution. Its free plan currently provides3000 credits/day, allows commercial production use with attribution and needs no payment card. Official source: https://www.geoapify.com/pricing/ . Do not enable paid upgrades.

1. Reuse an existing Geoapify project if available; otherwise create a free project at https://myprojects.geoapify.com/ .
2. Put the API key in the existing Railway development WEB4 service secret variable `GEOAPIFY_API_KEY`. Do not paste the key into chat, source or WIP logs.
3. Tell the agent only that the secret is configured, or identify the existing secure project/environment containing the key. The agent owns wiring, caps, tests and deployment.

The integration must enforce its own free-only request/credit ceiling and provider attribution, and stop at the cap rather than silently upgrading or moving requests to a paid source. Public Overpass returned502 capacity from the deployed server on2 November2026; this provider availability failure remains agent-owned and is not a user account chore.
