#!/usr/bin/env python3
"""Keeps each WEB4-WEB10 API version identical everywhere it is written down.

Source of truth: the WEBnAPIVersion constants in OASISBootLoader.cs (WEB4 is OASISAPIVersion, WEB5 is STARAPIVersion). For each
API the version must match its release-history entry, its Postman collection file name and title, and the OPORTAL-JS portal
links. WEB4 is also tied to OASISRuntimeVersion (the OASIS Runtime ships with the WEB4 API). The Swagger titles, version endpoint and
startup log read the constants at runtime, so they need no edit.

  py -3 Scripts/api_version_sync.py --check                 fail if anything disagrees with the constants
  py -3 Scripts/api_version_sync.py --sync                  propagate the constants to the runtime tie, release docs, Postman, portal
  py -3 Scripts/api_version_sync.py --apply WEB5 3.3.0      set a constant (and history stub if missing), then --sync

The sibling OPORTAL-JS repo (../OPORTAL-JS, or OASIS_OPORTAL_DIR) holds the Postman collections and portal pages; if it is absent
that part is reported and skipped. Release-history files inside submodules that are not checked out are reported and skipped.
See Docs/Devs/API_VERSIONING.md.
"""
import argparse, os, re, sys
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BOOT = ROOT / "OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.cs"
RELEASE_DOC = ROOT / "Docs/Devs/GLOBAL_RELEASE_AUTOMATION.md"
OPORTAL = Path(os.environ.get("OASIS_OPORTAL_DIR", ROOT.parent / "OPORTAL-JS"))
PORTAL_PAGES = ["assets/components/modals/dev-portal.html", "source/main/index.html", "source/dist/index.html"]
POSTMAN_DIR = "postman"

# name -> (constant, [release history files], Postman file prefix)
APIS = {
    "WEB4": ("OASISAPIVersion", ["ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS API RELEASE HISTORY.md"], "OASIS WEB4 API v"),
    "WEB5": ("STARAPIVersion", ["STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/STAR API RELEASE HISTORY.md", "Docs/API/WEB5-STAR-API-RELEASE-HISTORY.md"], "STAR WEB5 API v"),
    "WEB6": ("WEB6APIVersion", ["WEB6/NextGenSoftware.OASIS.Web6.WebAPI/WEB6 API RELEASE HISTORY.md", "Docs/API/WEB6-AI-API-RELEASE-HISTORY.md"], "OASIS WEB6 API v"),
    "WEB7": ("WEB7APIVersion", ["WEB7/NextGenSoftware.OASIS.Web7.WebAPI/WEB7 API RELEASE HISTORY.md"], "OASIS WEB7 API v"),
    "WEB8": ("WEB8APIVersion", ["WEB8/NextGenSoftware.OASIS.Web8.WebAPI/WEB8 API RELEASE HISTORY.md"], "OASIS WEB8 API v"),
    "WEB9": ("WEB9APIVersion", ["WEB9/NextGenSoftware.OASIS.Web9.WebAPI/WEB9 API RELEASE HISTORY.md"], "OASIS WEB9 API v"),
    "WEB10": ("WEB10APIVersion", ["WEB10/NextGenSoftware.OASIS.Web10.WebAPI/WEB10 API RELEASE HISTORY.md"], "OASIS WEB10 API v"),
}
VER = r"\d+\.\d+\.\d+"
notes = []


def read(p): return Path(p).read_bytes().decode("utf-8")
def write(p, t): Path(p).write_bytes(t.encode("utf-8"))  # written exactly as built, so line endings are preserved
def const_re(name): return re.compile(rf"(public static string {name} \{{ get; set; \}} = \")({VER})(\")")


def versions():
    boot = read(BOOT)
    out = {}
    for api, (const, _, _) in APIS.items():
        m = const_re(const).search(boot)
        if not m: sys.exit(f"{const} not found in OASISBootLoader.cs")
        out[api] = m.group(2)
    return out


def runtime_version(): return const_re("OASISRuntimeVersion").search(read(BOOT)).group(2)
def portal_ok(): return OPORTAL.is_dir()


def docs_sentence(v):
    rest = [v[a] for a in ("WEB7", "WEB8", "WEB9", "WEB10")]
    tail = f"WEB7-WEB10 remain {rest[0]}" if len(set(rest)) == 1 else "WEB7-WEB10 are " + ", ".join(rest)
    return f"WEB4, WEB5 and WEB6 are {v['WEB4']}, {v['WEB5']} and {v['WEB6']} respectively; {tail}"


DOC_RE = re.compile(rf"WEB4, WEB5 and WEB6 are {VER}, {VER} and {VER} respectively; WEB7-WEB10 (?:remain {VER}|are {VER}(?:, {VER})*)")


def title_re(api):
    n = api[3:]
    return re.compile(rf"((?:WEB{n} API|WEB{n} OASIS API|OASIS WEB{n} API|STAR WEB{n} API) v)({VER})")


def problems():
    v, out = versions(), []
    if runtime_version() != v["WEB4"]: out.append(f"OASISRuntimeVersion is {runtime_version()}, but the WEB4 API is {v['WEB4']}")
    for api, (const, hist, prefix) in APIS.items():
        for h in hist:
            if not (ROOT / h).is_file(): notes.append(f"{h} not present (submodule not checked out?); not checked"); continue
            if f"## {v[api]} (" not in read(ROOT / h): out.append(f"{h} has no '## {v[api]} (' entry for {api}")
    m = DOC_RE.search(read(RELEASE_DOC))
    if not m or m.group(0) != docs_sentence(v): out.append("GLOBAL_RELEASE_AUTOMATION.md lists different API versions")
    if not portal_ok():
        notes.append(f"{OPORTAL} not found; Postman collections and portal links not checked")
        return out
    files = list((OPORTAL / POSTMAN_DIR).glob("*.postman_collection.json"))
    for api, (const, _, prefix) in APIS.items():
        want = f"{prefix}{v[api]}.postman_collection.json"
        mine = [f.name for f in files if f.name.startswith(prefix)]
        if want not in mine: out.append(f"missing Postman collection {want}")
        out += [f"stale Postman file {n}" for n in mine if n != want]
        for page in PORTAL_PAGES:
            text = read(OPORTAL / page)
            if f"{POSTMAN_DIR}/{prefix}" in text and f"{POSTMAN_DIR}/{want}" not in text: out.append(f"{page} links a {api} collection other than {want}")
            if any(x.group(2) != v[api] for x in title_re(api).finditer(text)): out.append(f"{page} shows a different {api} version label")
    return out


def sync():
    v = versions()
    write(BOOT, const_re("OASISRuntimeVersion").sub(rf"\g<1>{v['WEB4']}\g<3>", read(BOOT)))
    write(RELEASE_DOC, DOC_RE.sub(docs_sentence(v), read(RELEASE_DOC)))
    if not portal_ok(): return
    for api, (const, _, prefix) in APIS.items():
        want = OPORTAL / POSTMAN_DIR / f"{prefix}{v[api]}.postman_collection.json"
        for f in (OPORTAL / POSTMAN_DIR).glob(f"{prefix}*.postman_collection.json"):
            if f != want:
                if want.exists(): sys.exit(f"both {f.name} and {want.name} exist; remove the stale one")
                write(want, read(f)); f.unlink()
        if want.exists(): write(want, title_re(api).sub(lambda m: m.group(1) + v[api], read(want)))
        for page in PORTAL_PAGES:
            p = OPORTAL / page
            t = title_re(api).sub(lambda m: m.group(1) + v[api], read(p))
            t = re.sub(rf"{re.escape(POSTMAN_DIR)}/{re.escape(prefix)}{VER}\.postman_collection\.json", f"{POSTMAN_DIR}/{prefix}{v[api]}.postman_collection.json", t)
            write(p, t)


def apply(api, version, notes):
    const, hist, _ = APIS[api]
    write(BOOT, const_re(const).sub(rf"\g<1>{version}\g<3>", read(BOOT)))
    for h in hist:
        p = ROOT / h
        if not p.is_file(): sys.exit(f"{h} not present; check out the submodule before applying {api}")
        text = read(p)
        if f"## {version} (" not in text:
            eol = "\r\n" if "\r\n" in text else "\n"
            write(p, text.rstrip() + f"{eol}{eol}{'-' * 124}{eol}## {version} ({date.today():%d/%m/%y}){eol}{eol}- {notes}{eol}")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--check", action="store_true")
    g.add_argument("--sync", action="store_true")
    g.add_argument("--apply", nargs=2, metavar=("WEBn", "X.Y.Z"))
    ap.add_argument("--notes", default="TODO: describe this release.", help="release-history text for --apply")
    a = ap.parse_args()
    if a.apply:
        api, ver = a.apply[0].upper(), a.apply[1]
        if api not in APIS or not re.fullmatch(VER, ver): sys.exit("usage: --apply WEB4..WEB10 X.Y.Z")
        if tuple(map(int, ver.split("."))) <= tuple(map(int, versions()[api].split("."))): sys.exit(f"{api} is already {versions()[api]}; the new version must be higher")
        apply(api, ver, a.notes)
    if a.sync or a.apply: sync()
    bad = problems()
    for n in dict.fromkeys(notes): print("note:", n)
    for b in bad: print("OUT OF SYNC:", b)
    print(", ".join(f"{k} {x}" for k, x in versions().items()), "- all in sync" if not bad else "- fix the above")
    sys.exit(1 if bad else 0)
