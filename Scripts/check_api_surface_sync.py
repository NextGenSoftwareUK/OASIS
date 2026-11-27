#!/usr/bin/env python3
"""Fail when a REST route has no matching SDK method, MCP tool or Postman request.

    python3 Scripts/check_api_surface_sync.py                  # check (CI)
    python3 Scripts/check_api_surface_sync.py --report out.md  # also write the done/total report
    python3 Scripts/check_api_surface_sync.py --update-backlog # rewrite the known-gap baseline

SDK checkouts are looked for in --sdk-root (default: $OASIS_SDK_ROOT, else the parent of
this repo). A surface whose sources are not present is reported as SKIPPED, not as passing.

Scripts/api_surface_allowlist.json   routes that intentionally have no SDK method / MCP tool
                                     ("*" globs on "WEBn VERB /path"), plus explicit
                                     route -> MCP tool mappings for in-process tools.
Scripts/api_surface_backlog.json     gaps that existed when the check was added. Not intentional:
                                     it may only shrink.

An MCP tool covers a route when it calls that verb and path through OASISRestClient, is named
web<n>_<controller>_<method>, or is mapped in the allowlist.
"""
import argparse
import fnmatch
import json
import os
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_web4_full_postman as rest  # noqa: E402  (shared controller parser)

ROOT = rest.ROOT
HERE = ROOT / "Scripts"
DOCS = ROOT / "Docs/Devs/API Documentation"
CONTROLLERS = {
    "WEB4": "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers",
    "WEB5": "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Controllers",
    **{f"WEB{n}": f"WEB{n}/NextGenSoftware.OASIS.Web{n}.WebAPI/Controllers" for n in range(6, 11)},
}
PROTOS = {k: v.replace("/Controllers", "/Protos") for k, v in CONTROLLERS.items()}
GRAPHQL = {k: v.replace("/Controllers", "/GraphQL") for k, v in CONTROLLERS.items()}
SDKS = {"WEB4": "OASIS-API-Javascipt-Package-WEB4",  # (sic) the repository name is misspelt
        **{f"WEB{n}": f"OASIS-API-Javascript-Package-WEB{n}" for n in range(5, 11)}}
MCP_TOOLS = ROOT / "WEB6/NextGenSoftware.OASIS.MCP.Tools/Tools"
SURFACES = ("sdk", "mcp", "postman")


def norm(verb, path):
    path = path.split("?")[0]
    path = re.sub(r"\$?\{\{?[^{}]*(?:\([^()]*\))?[^{}]*\}?\}", "{}", path)
    path = re.sub(r"(?<=/):\w+", "{}", path)
    return f"{verb.upper()} /{re.sub(r'/{2,}', '/', path).strip('/').lower()}"


def slug(text):
    return re.sub(r"[^a-z0-9]", "", text.lower())


def rest_routes(api):
    """{normalised key: (verb, path, controller, method)}; None when sources are absent."""
    folder = ROOT / CONTROLLERS[api]
    if not folder.is_dir():
        return None
    rest.CONTROLLERS = folder
    routes = {}
    for name, data in rest.collect().items():
        for verb, template, method, _ in data["routes"]:
            path = rest.full_path(data["prefix"], template, name)
            routes.setdefault(norm(verb, path), (verb, path, name, method))
    return routes


def sdk_routes(api, sdk_root):
    modules = sdk_root / SDKS[api] / "src/modules"
    if not modules.is_dir():
        return None
    call = re.compile(r"makeOperation\(\s*http\s*,\s*[\"']([^\"']*)[\"']\s*,\s*[\"'](\w+)[\"']\s*,\s*[\"']([^\"']*)[\"']")
    found = set()
    for path in modules.rglob("*.js"):
        for base, verb, sub in call.findall(path.read_text(encoding="utf-8")):
            sub = sub[1:] if sub.startswith("~") else (sub if sub.startswith("/") else f"{base}/{sub}")
            found.add(norm(verb, sub))
    return found


def mcp_index():
    """(names of all tools, {api: routes called over REST}).

    The MCP tools live in the private WEB6 submodule, so a snapshot of the index is committed for CI;
    --update-backlog refreshes it from the sources.
    """
    snapshot = HERE / "api_surface_mcp_index.json"
    if not MCP_TOOLS.is_dir():
        if not snapshot.exists():
            return None
        data = json.loads(snapshot.read_text(encoding="utf-8"))
        return set(data["tools"]), {api: set(v) for api, v in data["called"].items()}
    names, called = set(), {}
    call = re.compile(r"OASISRestClient\.(Get|Post|Put|Delete|PostForm|PostFile|GetRedirect)Async\(\s*\$?\"([^\"]*)\"")
    for path in MCP_TOOLS.glob("*.cs"):
        src = rest.strip_comments(path.read_text(encoding="utf-8-sig"))
        for chunk in re.split(r"(?=McpServerTool\(Name)", src)[1:]:
            name = re.match(r"McpServerTool\(Name\s*=\s*\"(\w+)\"", chunk)
            if not name:
                continue
            names.add(name.group(1))
            api = name.group(1).split("_")[0].upper()
            for kind, url in call.findall(chunk):
                verb = {"PostForm": "POST", "PostFile": "POST", "GetRedirect": "GET"}.get(kind, kind)
                url = re.sub(r"^https?://[^/]+", "", url)
                url = re.sub(r"\{(?:Uri\.|Esc|Encode)[^{}]*\}", "{}", url)
                called.setdefault(api, set()).add(norm(verb, url))
    return names, called


def postman_routes(api):
    if api == "WEB5":
        files = [ROOT / "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/STAR_API.postman_collection.json"]
    else:
        files = [p for p in DOCS.rglob("*postman_collection.json")
                 if p.name.upper().startswith(api) or p.parent.name.upper().split()[0] == api]
        files += list((ROOT / api).rglob("*postman_collection.json")) if (ROOT / api).is_dir() else []
    files = [f for f in files if f.is_file()]
    found = set()

    def walk(node):
        for item in node.get("item", []):
            walk(item)
        request = node.get("request")
        if isinstance(request, dict):
            url = request.get("url", "")
            raw = url if isinstance(url, str) else url.get("raw", "")
            raw = re.sub(r"^(\{\{[^}]*\}\}|https?://[^/]+)", "", raw)
            found.add(norm(request.get("method", "GET"), raw))

    for f in files:
        walk(json.loads(f.read_text(encoding="utf-8-sig")))
    return found


def load(name, default):
    path = HERE / name
    return json.loads(path.read_text(encoding="utf-8")) if path.exists() else default


def count(folder, pattern, glob):
    folder = ROOT / folder
    if not folder.is_dir():
        return 0
    return sum(len(re.findall(pattern, rest.strip_comments(p.read_text(encoding="utf-8-sig", errors="replace")), re.M))
               for p in folder.rglob(glob))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--sdk-root", default=os.environ.get("OASIS_SDK_ROOT"))
    parser.add_argument("--report")
    parser.add_argument("--update-backlog", action="store_true")
    parser.add_argument("--require-all", action="store_true", help="fail if any surface source is missing")
    args = parser.parse_args()
    sdk_root = Path(args.sdk_root) if args.sdk_root else ROOT.parent
    allow = load("api_surface_allowlist.json", {})
    backlog = load("api_surface_backlog.json", {})
    mcp = mcp_index()
    new_backlog, failures, skipped, rows, gaps = {}, [], [], [], []

    for api in CONTROLLERS:
        routes = rest_routes(api)
        if routes is None:
            skipped.append(f"{api} controllers")
            new_backlog.update({k: v for k, v in backlog.items() if k.startswith(api + " ")})
            continue
        sources = {"sdk": sdk_routes(api, sdk_root), "postman": postman_routes(api), "mcp": mcp}
        row = [api, str(len(routes))]
        for surface in SURFACES:
            key = f"{api} {surface}"
            if sources[surface] is None:
                skipped.append(key)
                new_backlog[key] = backlog.get(key, [])
                row.append("skipped")
                continue
            rules = allow.get("intentional", {}).get(surface, {})
            wanted, missing = 0, []
            for norm_key, (verb, path, controller, method) in sorted(routes.items()):
                label = f"{api} {verb} {path}"
                if any(fnmatch.fnmatchcase(label.lower(), p.lower()) for p in rules):
                    continue
                wanted += 1
                if surface == "mcp":
                    names, called = mcp
                    tool = allow.get("mcpToolMap", {}).get(label)
                    method_slug = re.sub(r"async$", "", method.lower())
                    covered = (norm_key in called.get(api, ())
                               or tool in names
                               or any(slug(n) == slug(api + controller + method_slug) for n in names))
                else:
                    covered = norm_key in sources[surface]
                if not covered:
                    missing.append(f"{verb} {path}")
            row.append(f"{wanted - len(missing)}/{wanted}")
            new_backlog[key] = missing
            known = set(backlog.get(key, []))
            failures += [f"{key}: {m}" for m in missing if m not in known]
            gaps += [f"{key}: {m}" for m in missing]
        rpcs = count(PROTOS[api], r"^\s*rpc\s+\w+", "*.proto")
        resolvers = count(GRAPHQL[api], r"^\s*public\s+(?:async\s+)?(?!class|record|sealed|static class)[\w<>\[\]?, ]+?\s+\w+\s*\(", "*.cs")
        row += [str(rpcs), str(resolvers)]
        rows.append(row)

    header = ["API", "REST routes", "SDK", "MCP", "Postman", "gRPC rpcs", "GraphQL methods"]
    table = "\n".join("| " + " | ".join(r) + " |" for r in [header, ["---"] * len(header)] + rows)
    print(table)
    if skipped:
        print("\nSKIPPED (sources not checked out): " + ", ".join(skipped))
    if args.report:
        Path(args.report).write_text(
            "# API surface sync report\n\nGenerated by `Scripts/check_api_surface_sync.py`. Cells are covered/total.\n"
            "gRPC and GraphQL are inventory counts only (they are not mapped route by route).\n\n"
            + table + "\n\n## Gaps\n\n" + "\n".join(f"- {g}" for g in gaps) + "\n",
            encoding="utf-8", newline="\n")
    if args.update_backlog:
        if MCP_TOOLS.is_dir():
            names, called = mcp
            (HERE / "api_surface_mcp_index.json").write_text(
                json.dumps({"tools": sorted(names), "called": {a: sorted(v) for a, v in sorted(called.items())}}, indent=1)
                + "\n", encoding="utf-8", newline="\n")
        (HERE / "api_surface_backlog.json").write_text(
            json.dumps({k: v for k, v in sorted(new_backlog.items()) if v}, indent=1) + "\n",
            encoding="utf-8", newline="\n")
        return
    if failures:
        print(f"\n{len(failures)} REST route(s) have no matching surface:\n  " + "\n  ".join(failures))
        print("\nAdd the SDK method / MCP tool / Postman request, or add the route to "
              "Scripts/api_surface_allowlist.json with a reason.")
        sys.exit(1)
    if args.require_all and skipped:
        sys.exit(1)
    print(f"\nNo new gaps ({len(gaps)} known gaps remain in the backlog).")


if __name__ == "__main__":
    main()
