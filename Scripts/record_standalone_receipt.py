#!/usr/bin/env python3
"""Record a standalone HoloNET publication so the next global release plans cleanly.

publish-holonet.yml publishes HoloNET outside the global release. The global release's
source-aware NuGet plan (Scripts/nuget_source_graph.py) refuses a published version that has
no reviewed receipt in Scripts/nuget-publication-provenance.json, and re-plans a package as
"changed" when the pinned sources differ from what was published. This script reads the
publication-source-graph.json that the publish run recorded (the immutable truth of what was
built) and, in the OASIS checkout:

  * adds (if absent) the standalone receipt for the HoloNET ORM package at that version,
  * moves HOLOCHAIN_COMMIT, LIBRARIES_COMMIT and HOLONET_ORM_COMMIT in
    Docker/oasis-dependency-versions.env to the published commits.

The HoloNET-ORM gitlink is moved by the workflow (git update-index) using the printed commit.
Idempotent: running it twice with the same graph changes nothing.
"""
import json
import re
import sys
from pathlib import Path

ORM_ID = "NextGenSoftware.Holochain.HoloNET.ORM"
ORM_URL = "https://github.com/NextGenSoftwareUK/HoloNET-ORM"


def main(graph_path, root="."):
    root = Path(root)
    graph = json.loads(Path(graph_path).read_text(encoding="utf-8-sig"))
    repos = {r["path"]: r for r in graph["repositories"]}
    hc, libs = repos["holochain-client-csharp"], repos["NextGenSoftware-Libraries"]
    orm_commit = None
    for link in graph["gitlinks"]:
        m = re.match(r"^\s*([0-9a-f]{40})\s+NextGenSoftware\.Holochain\.HoloNET\.ORM\b", link)
        if m:
            orm_commit = m.group(1)
    if not orm_commit:
        raise SystemExit("No HoloNET.ORM gitlink in the publication source graph")
    versions = {m.group(1) for p in graph["packages"] for m in [re.match(re.escape(ORM_ID) + r"\.(\d+\.\d+\.\d+)\.nupkg$", p["file"])] if m}
    if len(versions) != 1:
        raise SystemExit("Expected exactly one HoloNET.ORM version in the graph, got %s" % sorted(versions))
    version = versions.pop()

    prov_path = root / "Scripts/nuget-publication-provenance.json"
    text = prov_path.read_text(encoding="utf-8-sig")
    data = json.loads(text)
    receipt = {
        "kind": "standalone", "ownerParent": "holochain-client-csharp", "packageId": ORM_ID, "version": version,
        "run": int(graph["run"]), "ownerUrl": ORM_URL, "ownerCommit": orm_commit,
        "dependencies": {
            "holochain-client-csharp": {"url": hc["url"], "commit": hc["commit"]},
            "NextGenSoftware-Libraries": {"url": libs["url"], "commit": libs["commit"]},
        },
    }
    if any(p.get("packageId") == ORM_ID and p.get("version") == version for p in data["packages"]):
        print("receipt already recorded")
    else:
        nl = "\r\n" if "\r\n" in text else "\n"
        block = json.dumps(receipt, indent=2).replace("\n", nl + "    ")
        marker = re.search(r"\n(\s*)\],\s*\n(\s*)\"metadataCorrections\"", text)
        if not marker:
            raise SystemExit("Cannot find the end of the packages list in the provenance file")
        head = text[:marker.start()].rstrip()
        text = head + "," + nl + "    " + block + text[marker.start():]
        json.loads(text)  # must stay valid
        prov_path.write_text(text, encoding="utf-8")

    env_path = root / "Docker/oasis-dependency-versions.env"
    env = env_path.read_text(encoding="utf-8")
    for key, value in (("HOLOCHAIN_COMMIT", hc["commit"]), ("LIBRARIES_COMMIT", libs["commit"]), ("HOLONET_ORM_COMMIT", orm_commit)):
        env, n = re.subn(r"^%s=.*$" % key, "%s=%s" % (key, value), env, flags=re.M)
        if n != 1:
            raise SystemExit("Pin %s not found exactly once" % key)
    env_path.write_text(env, encoding="utf-8")
    print("version=%s" % version)
    print("orm_commit=%s" % orm_commit)


if __name__ == "__main__":
    main(*sys.argv[1:])
