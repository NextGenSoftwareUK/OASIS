#!/usr/bin/env python3
"""Generate a Postman collection covering every WEB4 OASIS API route.

Reads the ONODE WebAPI controllers (partial classes merged, commented-out code
ignored), resolves [Route] prefixes, [controller] tokens and "~/" absolute
routes, and writes one folder per controller. No credentials are embedded:
requests use {{web4BaseUrl}} and {{bearerToken}} collection variables.

    python3 Scripts/generate_web4_full_postman.py            # write the file
    python3 Scripts/generate_web4_full_postman.py --check    # exit 1 if stale
"""
import json
import re
import sys
from collections import OrderedDict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONTROLLERS = ROOT / "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers"
TARGET = ROOT / "Docs/Devs/API Documentation/WEB4 OASIS API/WEB4-Full-API.postman_collection.json"

HTTP = re.compile(r"[\[,]\s*Http(Get|Post|Put|Delete|Patch)\s*(?:\(\s*(?:template\s*:\s*)?@?\"([^\"]*)\"[^)]*\))?\s*(?=[\],])")
ROUTE = re.compile(r"[\[,]\s*Route\s*\(\s*@?\"([^\"]*)\"\s*\)\s*(?=[\],])")
CLASS = re.compile(r"\bclass\s+(\w+?)Controller\b")
METHOD_NAME = re.compile(r"\b(?:public|protected|internal)\s+(?:virtual\s+|override\s+|static\s+|async\s+)*[\w<>\[\],.? ]+?\s+(\w+)\s*\(")
ANON = re.compile(r"[\[,]\s*AllowAnonymous\s*(?=[\],])")
PARAM = re.compile(r"\{(\w+)(?::[^}]*)?\??\}")


def strip_comments(src: str) -> str:
    """Remove // and /* */ comments but keep string literals intact."""
    out, i, n = [], 0, len(src)
    while i < n:
        c = src[i]
        if c == '"' or (c == '@' and i + 1 < n and src[i + 1] == '"'):
            verbatim = c == '@'
            j = i + (2 if verbatim else 1)
            while j < n:
                if verbatim and src[j] == '"' and j + 1 < n and src[j + 1] == '"':
                    j += 2
                    continue
                if not verbatim and src[j] == '\\':
                    j += 2
                    continue
                if src[j] == '"':
                    break
                j += 1
            out.append(src[i:j + 1])
            i = j + 1
        elif src.startswith("//", i):
            j = src.find("\n", i)
            i = n if j < 0 else j
        elif src.startswith("/*", i):
            j = src.find("*/", i + 2)
            i = n if j < 0 else j + 2
        else:
            out.append(c)
            i += 1
    return "".join(out)


CONSTANT_ROOTS = ("ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI", "OASIS Architecture/NextGenSoftware.OASIS.API.Core/Services")
CONCAT = re.compile(r'"~"\s*\+\s*(\w+)\.(\w+)')


def resolve_constants(src: str) -> str:
    """Expand [HttpPost("~" + Routes.Name)] to the literal route; fail rather than emit "~".

    The constants live in the private API Core submodule, so their values are recorded in
    api_surface_route_constants.json. Wherever the source is checked out, the record must match it.
    """
    recorded = json.loads((ROOT / "Scripts/api_surface_route_constants.json").read_text(encoding="utf-8"))

    def literal(match):
        owner, member = match.groups()
        name = f"{owner}.{member}"
        value = recorded.get(name)
        for root in CONSTANT_ROOTS:
            for path in (ROOT / root).rglob("*.cs"):
                text = path.read_text(encoding="utf-8-sig", errors="replace")
                found = re.search(rf'class {owner}[^{{]*\{{[^}}]*?const string {member}\s*=\s*"([^"]*)"', text)
                if found and found.group(1) != value:
                    raise SystemExit(f"record {name} = {found.group(1)!r} in Scripts/api_surface_route_constants.json")
        if value is None:
            raise SystemExit(f"add route constant {name} to Scripts/api_surface_route_constants.json")
        return f'"~{value}"'
    return CONCAT.sub(literal, src)


def collect():
    controllers = OrderedDict()
    for path in sorted(CONTROLLERS.rglob("*.cs")):
        src = resolve_constants(strip_comments(path.read_text(encoding="utf-8-sig", errors="replace")))
        cls = CLASS.search(src)
        if not cls:
            continue
        name = cls.group(1)
        head = src[:cls.start()]
        prefixes = ROUTE.findall(head[-600:]) or [""]
        prefix = prefixes[-1].replace("[controller]", name)
        class_anon = bool(ANON.search(head[-600:]))
        body = src[cls.end():]
        entry = controllers.setdefault(name, {"prefix": prefix, "routes": []})
        if prefix and not entry["prefix"]:
            entry["prefix"] = prefix
        # Walk attribute blocks: each Http* attribute applies to the next method.
        for m in HTTP.finditer(body):
            verb, template = m.group(1).upper(), m.group(2)
            after = body[m.end():m.end() + 1500]
            method = METHOD_NAME.search(after)
            attrs_before = body[max(0, m.start() - 400):m.start()]
            attrs_after = after[:method.start()] if method else ""
            route_attr = ROUTE.search(attrs_after) or None
            if template is None and route_attr:
                template = route_attr.group(1)
            anon = class_anon or bool(ANON.search(attrs_after)) or bool(ANON.search(attrs_before.split("}")[-1]))
            entry["routes"].append((verb, template or "", method.group(1) if method else verb.title(), anon))
    return controllers


def full_path(prefix: str, template: str, name: str) -> str:
    template = template.replace("[controller]", name).replace("[action]", "")
    if template.startswith("~/"):
        path = template[1:]
    elif template.startswith("/"):
        path = template
    else:
        base = prefix.replace("[controller]", name).strip("/")
        path = "/" + "/".join(p for p in (base, template.strip("/")) if p)
    return re.sub(r"/{2,}", "/", path)


def pm_url(path: str):
    raw = "{{web4BaseUrl}}" + PARAM.sub(lambda m: "{{" + m.group(1) + "}}", path)
    return raw


# Example bodies for routes whose request is not obvious; every other body is left as {}.
EXAMPLE_BODIES = {
    "/api/places/observe": {"id":"{{$guid}}","placeId":"{{placeId}}","latitude":0,"longitude":0,"observedUtc":"{{$isoTimestamp}}","sourceApp":"Our World"},
    "/api/data/query-cql/sql": {"query": "SELECT * FROM MongoDBOASIS WHERE Version >= @minimum LIMIT 5",
                                "parameters": {"minimum": 1}},
    "/api/data/query-cql/ask": {"question": "Show five records from MongoDB", "execute": False},
}


def build():
    folders = []
    total = 0
    for name, data in collect().items():
        seen, items = set(), []
        for verb, template, method, anon in data["routes"]:
            path = full_path(data["prefix"], template, name)
            key = (verb, path.lower())
            if key in seen:
                continue
            seen.add(key)
            headers = [{"key": "Content-Type", "value": "application/json"}]
            if not anon:
                headers.append({"key": "Authorization", "value": "Bearer {{bearerToken}}"})
            if verb in ("POST", "PUT", "PATCH", "DELETE"):
                headers.append({"key": "Idempotency-Key", "value": "{{$guid}}"})
            request = {"method": verb, "header": headers, "url": pm_url(path)}
            if verb in ("POST", "PUT", "PATCH"):
                request["body"] = {"mode": "raw", "raw": json.dumps(EXAMPLE_BODIES[path], indent=2) if path in EXAMPLE_BODIES else "{}", "options": {"raw": {"language": "json"}}}
            label = re.sub(r"(?<!^)(?=[A-Z])", " ", method).replace("Async", "").strip()
            items.append({"name": f"{label}  [{verb} {path}]", "request": request})
        if items:
            items.sort(key=lambda i: i["request"]["url"])
            folders.append({"name": re.sub(r"(?<!^)(?=[A-Z][a-z])", " ", name), "item": items})
            total += len(items)
    folders.sort(key=lambda f: f["name"].lower())
    return {
        "info": {
            "name": "WEB4 OASIS API (all routes)",
            "description": (
                f"Every WEB4 OASIS API route ({total} requests), generated from the ONODE WebAPI controllers by "
                "Scripts/generate_web4_full_postman.py. Set web4BaseUrl (production https://api.web4.oasisomniverse.one, "
                "dev https://dev.api.web4.oasisomniverse.one) and bearerToken (from POST /api/avatar/authenticate). "
                "Most request bodies are left as {}; see the controller or Swagger for each model. Do not run money, crypto "
                "or minting requests against production."),
            "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
        },
        "variable": [
            {"key": "web4BaseUrl", "value": "https://dev.api.web4.oasisomniverse.one"},
            {"key": "bearerToken", "value": ""},
        ],
        "item": folders,
    }, total


def main():
    collection, total = build()
    text = json.dumps(collection, indent=2, ensure_ascii=False) + "\n"
    if "--check" in sys.argv:
        current = TARGET.read_text(encoding="utf-8") if TARGET.exists() else ""
        if current != text:
            print(f"{TARGET.name} is stale; run Scripts/generate_web4_full_postman.py")
            sys.exit(1)
        print(f"{TARGET.name} is current ({total} requests)")
        return
    TARGET.write_text(text, encoding="utf-8", newline="\n")
    print(f"wrote {TARGET.relative_to(ROOT)} ({total} requests, {len(collection['item'])} folders)")


if __name__ == "__main__":
    main()
