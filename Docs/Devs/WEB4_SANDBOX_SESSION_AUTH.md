# WEB4 browser and demo session authentication

Normal authentication and `POST /api/sandbox/session` issue the same refresh-cookie contract: `HttpOnly`, `Secure` on HTTPS, `SameSite=Lax`, host-only and path `/`. Expiry follows the configured refresh-token policy. Demo session publication requires both an access token and a generated refresh credential; incomplete authentication returns an error without publishing an avatar or cookie.

The hosted WEB4 page at `https://web4.oasisomniverse.one` uses credentialed, explicit-origin CORS. Other origins retain the public bearer-token API policy; they do not receive a cookie credential grant. Responses vary by Origin and both policies expose `X-OASIS-Operation-Id`. There is no all-origin credentialed policy. Local/native clients can continue using the ordinary bearer API and explicitly authorized refresh-token request contract.

The page keeps its bearer in memory. Before an API request approaches access-token expiry, it renews through `POST /api/avatar/refresh-token` with the existing HttpOnly cookie. Login, rotation and logout share one asynchronous owner, so overlapping widgets coalesce into one rotation. The avatar ID must stay identical. A failed or uncertain rotation stops the original request; the client does not retry the refresh or automatically create another demo avatar.

Renewal preserves stable avatar, environment and session-generation identity. A new access-token string does not clear CQL results or preparation state. A response from a former avatar or environment is discarded. Original provider requests, especially writes with retained operation keys, are never replayed after an authentication response.

Logout uses the normal revoke-token endpoint and clears the same cookie scope. A failed server logout is reported while the local session is cleared. Switching environments requires explicit authentication in the selected environment.

For owned HTTP verification, a versioned DPAPI bundle holds access and encoded refresh-cookie credentials together. One journalled rotation captures the response, checks the same owner and atomically installs the new bundle. Unknown outcomes require reconciliation of the original intent. Older JWT-only fixture cohorts keep their existing identity and cannot silently acquire a replacement owner.

Live rollout evidence and outstanding browser verification are recorded in [session status](../WIP/COSMIC%20ORM/SESSION_RENEWAL_STATUS.md). This protocol does not claim browser certification from unit or HTTP tests.

Railway terminates TLS before forwarding requests. Startup processes one forwarded hop before authentication or redirects and explicitly clears the default known-network/proxy lists, matching the deployment’s existing edge-trust policy. An empty C# collection initializer does not clear those defaults. This policy requires an edge-protected service; it is not permission to expose the application listener directly to untrusted clients. A regression exercises a non-loopback proxy and verifies that the resulting refresh cookie is Secure.
