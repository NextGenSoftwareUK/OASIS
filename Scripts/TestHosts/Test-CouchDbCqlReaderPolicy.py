"""Exercise the CQL reader role on the job-owned CouchDB service."""
import base64
import json
import os
import secrets
import urllib.error
import urllib.request


def main():
    if os.environ.get("GITHUB_ACTIONS") != "true":
        raise RuntimeError("This test requires the disposable CI CouchDB container.")
    base = "http://127.0.0.1:5984"
    suffix = secrets.token_hex(8)
    database = "/cql_policy_" + suffix
    username = "cql_reader_" + suffix
    role = "oasis_cql_reader"
    password = secrets.token_urlsafe(24)
    admin = ("admin", "password")
    reader = (username, password)

    def request(method, path, credential, expected, body=None):
        token = base64.b64encode(":".join(credential).encode()).decode()
        query = urllib.request.Request(
            base + path, method=method,
            data=None if body is None else json.dumps(body).encode(),
            headers={"Authorization": "Basic " + token, "Content-Type": "application/json"},
        )
        try:
            response = urllib.request.urlopen(query, timeout=10)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            if response.status not in expected:
                raise RuntimeError(f"CouchDB {method} {path} returned {response.status}; expected {expected}.")
            return json.load(response)

    request("PUT", "/_users", admin, (201, 412))
    request("PUT", "/_users/org.couchdb.user:" + username, admin, (201,),
            {"name": username, "password": password, "roles": [role], "type": "user"})
    request("PUT", database, admin, (201,))
    request("PUT", database + "/_security", admin, (200,),
            {"admins": {"names": ["admin"], "roles": []}, "members": {"names": [], "roles": [role]}})
    request("PUT", database + "/_design/cql_reader", admin, (201,),
            {"validate_doc_update": 'function (n, o, u) { if (u.roles.indexOf("oasis_cql_reader") !== -1) '
                                    'throw {forbidden: "The CQL demo role is read-only."}; }'})
    request("PUT", database + "/teaching-record", admin, (201,), {"kind": "synthetic"})
    original = request("GET", database + "/teaching-record", reader, (200,))
    revision = original["_rev"]
    request("PUT", database + "/new-record", reader, (403,), {"kind": "synthetic"})
    request("PUT", database + "/teaching-record", reader, (403,), {"_rev": revision, "kind": "changed"})
    request("DELETE", database + "/teaching-record?rev=" + revision, reader, (403,))
    if request("GET", database + "/teaching-record", reader, (200,)) != original:
        raise RuntimeError("The query role changed the original teaching record.")
    print("CouchDB query role read the fixture and was denied create, update and delete.")


if __name__ == "__main__":
    main()
