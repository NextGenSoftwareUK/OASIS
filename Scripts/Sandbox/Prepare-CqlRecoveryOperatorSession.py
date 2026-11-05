"""Interactive existing-account sign-in. Password stays off command lines, files and chat."""
from pathlib import Path
import getpass
import base64
import hashlib
import importlib.util
import json
import os
import sys
import urllib.error
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / "LocalState/COSMIC/HostedSourceRecoveryOperatorCurrent"


def write_new(path, data):
    with path.open("xb") as stream:
        stream.write(data)
        stream.flush()
        os.fsync(stream.fileno())


def operator_authority(query, token):
    # Invalid host identity guarantees no native activation. A Wizard reaches
    # the explicit host fence; an ordinary avatar receives403 before that fence.
    body = {"operationId": str(uuid.uuid4()), "expectedGeneration": 0,
            "hostInstanceId": str(uuid.UUID(int=0))}
    request = urllib.request.Request(query.BASE + "admin/cql/sources/NostrOASIS/recover",
        method="POST", data=json.dumps(body).encode(), headers={"Content-Type": "application/json",
            "Origin": query.BROWSER_ORIGIN, "Authorization": "Bearer " + token})
    try:
        response = urllib.request.build_opener(query.NoRedirect()).open(request, timeout=45)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        raw = response.read(8193)
        if len(raw) > 8192:
            raise RuntimeError("Bounded authority response required.")
        data = query.verification.unwrap(json.loads(raw))
        return {"status": response.status, "errorCode": data.get("errorCode"),
                "responseSha256": hashlib.sha256(raw).hexdigest()}


def prepare(state, username, password, query, authority=operator_authority):
    if not username.strip() or not password:
        raise ValueError("Existing account credentials required.")
    state.mkdir(parents=True, exist_ok=True)
    if any(state.iterdir()):
        raise RuntimeError("Original sign-in state exists; reconcile without replacing it.")
    operation = str(uuid.uuid4())
    write_new(state / "operator.intent.json", json.dumps({"route": "avatar/authenticate",
        "operationId": operation, "existingAccountOnly": True}).encode())
    captured = []

    def capture(status, raw, cookies):
        write_new(state / "operator.auth-response.dpapi", query.verification.protected_token(
            json.dumps({"status": status, "bodyBase64": base64.b64encode(raw).decode(),
                        "cookies": cookies}).encode()))
        captured.append((status, cookies))

    requested_at = query.session_credentials.time.time()
    result = query.call("avatar/authenticate", body={"username": username, "password": password},
                        operation=operation, capture_auth=capture)
    avatar = result.get("result") or {}
    owner = str(uuid.UUID(avatar["id"]))
    if len(captured) != 1 or captured[0][0] != 200:
        raise RuntimeError("Complete sign-in issuance acknowledgement required.")
    credential = query.session_credentials.bundle(owner, avatar["jwtToken"], captured[0][1], requested_at=requested_at)
    role = authority(query, credential["jwtToken"])
    write_new(state / "operator.authority-observed.json", json.dumps(role).encode())
    if role["status"] != 409 or role["errorCode"] != "CQL_SOURCE_HOST_INSTANCE_CONFLICT":
        raise RuntimeError("The existing account did not prove operator authority;no role changes performed.")
    write_new(state / "operator.session.dpapi", query.verification.protected_token(json.dumps(credential).encode()))
    write_new(state / "operator.acknowledged.json", json.dumps({"id": owner,
        "label": "operator", "operationId": operation, "credentialsSchema": 1,
        "wizardAuthorityVerified": True, "existingAccountOnly": True}).encode())


def main():
    if os.name != "nt" or not sys.stdin.isatty():
        raise RuntimeError("Run in your own interactive Windows terminal;do not supply passwords as arguments.")
    spec = importlib.util.spec_from_file_location("query", Path(__file__).with_name("Verify-HostedQuerySource.py"))
    query = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(query)
    username = input("Existing development Wizard username: ")
    password = getpass.getpass("Password (hidden;never saved): ")
    try:
        prepare(STATE, username, password, query)
    finally:
        password = None
    print("Existing Wizard session protected locally. Tell the agent: operator sign-in complete.")


if __name__ == "__main__":
    try:
        main()
    except Exception:
        # Raw auth responses/exceptions can contain credentials. Keep them sealed;
        # never dump a traceback/body or retry issuance after an unknown result.
        print("Sign-in incomplete. Preserve the original local state and ask the agent to reconcile;no automatic retry.", file=sys.stderr)
        sys.exit(1)
