#!/usr/bin/env python3
"""Keeps the WEB4 version identical everywhere (runtime, API, Swagger, release history, docs, Postman, portal links).

  py -3 Scripts/web4_version_sync.py --check             fail if any place disagrees with OASISAPIVersion
  py -3 Scripts/web4_version_sync.py --apply 5.4.0       set every place to 5.4.0 (release history gets a stub entry)

Source of truth: OASISAPIVersion in OASISBootLoader.cs. OASISRuntimeVersion must equal it. The Swagger title and the Settings
version endpoint read it at runtime, so there is nothing else to edit for them. The sibling OPORTAL-JS repo (../OPORTAL-JS, or
OASIS_OPORTAL_DIR) holds the Postman collection and the portal links; if it is absent that part is skipped and reported.
See Docs/Devs/WEB4_VERSIONING.md.
"""
import argparse, os, re, sys
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BOOT = ROOT / "OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.cs"
HISTORY = ROOT / "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS API RELEASE HISTORY.md"
RELEASE_TEST = ROOT / "Scripts/Test-OASISGlobalRelease.ps1"
RELEASE_DOC = ROOT / "Docs/Devs/GLOBAL_RELEASE_AUTOMATION.md"
OPORTAL = Path(os.environ.get("OASIS_OPORTAL_DIR", ROOT.parent / "OPORTAL-JS"))
PORTAL_PAGES = ["assets/components/modals/dev-portal.html", "source/main/index.html", "source/dist/index.html"]
API_RE = re.compile(r'(OASISAPIVersion \{ get; set; \} = ")(\d+\.\d+\.\d+)(")')
RUN_RE = re.compile(r'(OASISRuntimeVersion \{ get; set; \} = ")(\d+\.\d+\.\d+)(")')
DOC_RE = re.compile(r"(WEB4, WEB5 and WEB6 are )(\d+\.\d+\.\d+)(, )")
POSTMAN = "OASIS WEB4 API v{}.postman_collection.json"


def read(p): return p.read_bytes().decode("utf-8")
def write(p, t): p.write_bytes(t.encode("utf-8"))  # keep line endings exactly as written below


def current():
    m = API_RE.search(read(BOOT))
    if not m: sys.exit("OASISAPIVersion not found in OASISBootLoader.cs")
    return m.group(2)


def problems(v):
    out = []
    boot = read(BOOT)
    if RUN_RE.search(boot).group(2) != v: out.append(f"OASISRuntimeVersion is {RUN_RE.search(boot).group(2)}, expected {v}")
    if f"## {v} (" not in read(HISTORY): out.append(f"'{HISTORY.name}' has no '## {v} (' entry")
    if f"OASISAPIVersion {{ get; set; }} = \"{v}\"" not in read(RELEASE_TEST): out.append("Test-OASISGlobalRelease.ps1 expects a different OASISAPIVersion")
    if f"'{v}')" not in read(RELEASE_TEST): out.append("Test-OASISGlobalRelease.ps1 does not require the history entry")
    m = DOC_RE.search(read(RELEASE_DOC))
    if not m or m.group(2) != v: out.append("GLOBAL_RELEASE_AUTOMATION.md lists a different WEB4 version")
    if not OPORTAL.is_dir():
        print(f"note: {OPORTAL} not found; Postman and portal links not checked")
    else:
        if not (OPORTAL / "postman" / POSTMAN.format(v)).is_file(): out.append(f"missing {OPORTAL / 'postman' / POSTMAN.format(v)}")
        for other in (OPORTAL / "postman").glob("OASIS WEB4 API v*.postman_collection.json"):
            if other.name != POSTMAN.format(v): out.append(f"stale Postman file {other.name}")
        for page in PORTAL_PAGES:
            if POSTMAN.format(v) not in read(OPORTAL / page): out.append(f"{page} does not link {POSTMAN.format(v)}")
    return out


def apply(v):
    old = current()
    boot = API_RE.sub(rf"\g<1>{v}\g<3>", read(BOOT)); boot = RUN_RE.sub(rf"\g<1>{v}\g<3>", boot); write(BOOT, boot)
    t = read(RELEASE_TEST).replace(f'OASISAPIVersion {{ get; set; }} = "{old}"', f'OASISAPIVersion {{ get; set; }} = "{v}"')
    t = re.sub(r"('4\.5\.1',)'\d+\.\d+\.\d+'\)", rf"\g<1>'{v}')", t); write(RELEASE_TEST, t)
    write(RELEASE_DOC, DOC_RE.sub(rf"\g<1>{v}\g<3>", read(RELEASE_DOC)))
    h = read(HISTORY)
    if f"## {v} (" not in h:
        eol = "\r\n" if "\r\n" in h else "\n"
        rule = "-" * 124
        write(HISTORY, h.rstrip() + f"{eol}{eol}{rule}{eol}## {v} ({date.today():%d/%m/%y}){eol}{eol}- TODO: describe this release.{eol}")
    if OPORTAL.is_dir():
        src = OPORTAL / "postman" / POSTMAN.format(old)
        dst = OPORTAL / "postman" / POSTMAN.format(v)
        if src.is_file() and not dst.is_file():
            write(dst, read(src).replace(f"OASIS WEB4 API v{old}", f"OASIS WEB4 API v{v}").replace(f"WEB4 OASIS API v{old}", f"WEB4 OASIS API v{v}")); src.unlink()
        for page in PORTAL_PAGES:
            p = OPORTAL / page; write(p, read(p).replace(f"API v{old}", f"API v{v}"))


if __name__ == "__main__":
    ap = argparse.ArgumentParser(); g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--check", action="store_true"); g.add_argument("--apply", metavar="X.Y.Z")
    a = ap.parse_args()
    if a.apply:
        if not re.fullmatch(r"\d+\.\d+\.\d+", a.apply): sys.exit("version must be X.Y.Z")
        apply(a.apply)
    bad = problems(current())
    for b in bad: print("OUT OF SYNC:", b)
    print("WEB4 version", current(), "- all in sync" if not bad else "- fix the above"); sys.exit(1 if bad else 0)
