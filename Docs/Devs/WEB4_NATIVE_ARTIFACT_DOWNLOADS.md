# WEB4 native artifact download contract

The Waku and NATS stages in `Docker/Dockerfile.web4` download one immutable
official GitHub release asset ID each. Archive SHA256 and native version checks
are mandatory before the final image receives either executable. A transport
failure never selects a different URL, release, binary or checksum.

These build-only GETs use curl's documented transient-transfer retry policy:
two retries, at most three attempts, with the default one-second then two-second
backoff. Each attempt has a90second transfer limit; the retry-start window is
180seconds. An attempt already started can finish after that window, so180seconds
is not an overall wall-clock deadline. The enclosing cloud qualification job is
also bounded. Curl honors applicable Retry-After within its retry policy.

The standard transient classification covers timeouts and temporary HTTP errors
such as408,429,500,502,503 and504; exact supported statuses follow the installed
curl version. Do not enable `--retry-all-errors`, connection-refused retries or
insecure TLS. Authentication and permission failures must stop without another
attempt. This policy applies only to the two immutable build downloads; it does
not permit replaying CQL operations, business writes, model inference or unknown
save outcomes. Native startup-health polling has its own explicit contract.

`--disable` is the first curl argument so ambient curlrc files cannot alter the
request contract. `--no-progress-meter` keeps build logs compact while preserving
HTTP errors and retry warnings. Public asset requests send no authorization.
Do not silence final errors, remove checksum checks or accept partial files.
Some curl versions report an HTTP error while returning zero when retries are
enabled; the mandatory checksum gate must still reject the missing/error bytes.
The complete download/checksum/version pipeline determines build success.

`Scripts/TestHosts/test_web4_asset_download_policy.py` executes the actual Docker
curl arguments against an owned loopback server with synthetic bytes. It checks
temporary-error recovery, persistent-failure limits, no401/403 retry, and
checksum rejection after HTTP200 with corrupt bytes. The corresponding mock
digest substitutes only for the synthetic fixture; production pins stay intact.
Temporary files and the server are removed after each case. Actual runtime-stage
cloud builds independently verify official bytes, checksums and native versions.

See the [curl retry documentation](https://curl.se/docs/manpage.html#--retry),
[GitHub binary asset API](https://docs.github.com/en/rest/releases/assets#get-a-release-asset)
and [agent root-cause policy](AGENT_Root_Cause_No_Fallbacks.md).
