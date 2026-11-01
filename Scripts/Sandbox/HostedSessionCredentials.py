"""One protected credential bundle per owned HTTP acceptance session.

Refresh is an explicit journalled rotation. Unknown outcomes never resend a
credential or create another avatar. No provider/database writes occur here.
"""
import base64
import hashlib
import json
import os
import time
from http.cookies import SimpleCookie


def refresh_cookie(headers):
    found = []
    for header in headers:
        cookie = SimpleCookie()
        cookie.load(header)
        if "refreshToken" in cookie:
            value = cookie["refreshToken"]
            if (not value.value or not value["httponly"] or not value["secure"]
                    or value["path"] != "/" or value["samesite"].lower() != "lax"):
                raise RuntimeError("A scoped HTTPS HttpOnly refresh cookie is required.")
            found.append("refreshToken=" + value.coded_value)
    if len(found) != 1:
        raise RuntimeError("Exactly one refresh cookie is required; preserve the original issuance outcome.")
    return found[0]


def claims(token):
    return json.loads(base64.urlsafe_b64decode(token.split(".")[1] + "==="))


def bundle(owner, token, cookies):
    parsed = claims(token)
    if parsed.get("sub") != owner or parsed.get("exp", 0) <= time.time():
        raise RuntimeError("Issued access token must match the acknowledged owner and remain unexpired.")
    return {"schema": 1, "ownerId": owner, "jwtToken": token, "cookieHeader": refresh_cookie(cookies)}


def validate(value, owner):
    if (set(value) != {"schema", "ownerId", "jwtToken", "cookieHeader"} or value["schema"] != 1
            or value["ownerId"] != owner or claims(value["jwtToken"]).get("sub") != owner
            or not value["cookieHeader"].startswith("refreshToken=")
            or any(char in value["cookieHeader"] for char in "\r\n;")):
        raise RuntimeError("The original typed session credential bundle differs.")
    return value


def no_pending_rotation(state, label):
    for intent in state.glob(label + "-refresh-*.intent.json"):
        if not intent.with_name(intent.name.replace(".intent.json", ".acknowledged.json")).exists():
            raise RuntimeError("An original refresh rotation requires reconciliation; do not repeat it.")


def rotate(state, label, original, transport, protect, write_new):
    """Commit access/refresh credentials together; response and old bundle remain sealed."""
    validate(original, original["ownerId"])
    no_pending_rotation(state, label)
    digest = hashlib.sha256(original["jwtToken"].encode()).hexdigest()
    prefix = label + "-refresh-" + digest[:16]
    intent = state / (prefix + ".intent.json")
    if intent.exists():
        raise RuntimeError("Original rotation already has an outcome; reconcile rather than replay.")
    write_new(intent, json.dumps({"ownerId": original["ownerId"], "route": "avatar/refresh-token",
                                 "originalJwtSha256": digest}).encode())

    def capture(status, raw, cookies):
        write_new(state / (prefix + ".response.dpapi"), protect(json.dumps({"status": status,
            "bodyBase64": base64.b64encode(raw).decode(), "cookies": cookies}).encode()))
        captured.append((status, cookies))

    captured = []
    result = transport("avatar/refresh-token", body={}, cookie_header=original["cookieHeader"], capture_auth=capture)
    if len(captured) != 1 or captured[0][0] != 200:
        raise RuntimeError("Refresh acknowledgement missing; preserve original intent.")
    avatar = result["result"]
    if result.get("isError") or avatar.get("id") != original["ownerId"]:
        raise RuntimeError("A refresh cannot replace the acknowledged avatar.")
    updated = bundle(original["ownerId"], avatar["jwtToken"], captured[0][1])
    if updated["cookieHeader"] == original["cookieHeader"]:
        raise RuntimeError("Refresh credential was not rotated.")
    write_new(state / (prefix + ".before.dpapi"), protect(json.dumps(original).encode()))
    ready = state / (prefix + ".ready.dpapi")
    write_new(ready, protect(json.dumps(updated).encode()))
    # One atomic file replacement is the local access/refresh ownership boundary.
    os.replace(ready, state / (label + ".session.dpapi"))
    write_new(state / (prefix + ".acknowledged.json"), json.dumps({"ownerId": updated["ownerId"],
        "originalJwtSha256": digest, "newJwtSha256": hashlib.sha256(updated["jwtToken"].encode()).hexdigest(),
        "credentialsProtected": True, "accountReplaced": False}).encode())
    return updated
