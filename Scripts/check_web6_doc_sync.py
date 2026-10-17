#!/usr/bin/env python3
"""
check_web6_doc_sync.py
Scans all WEB6 doc surfaces for stale capability claims.
Run before any WEB6 release or after adding providers/protocols/memory adapters.

Usage:
    python Scripts/check_web6_doc_sync.py
    python Scripts/check_web6_doc_sync.py --fix   # print sed-style suggestions (manual apply)
"""

import os, re, sys, argparse
from pathlib import Path

ROOT = Path(__file__).parent.parent  # C:\Source\OASIS2

# ── Current ground-truth values ───────────────────────────────────────────────
TRUTH = {
    "provider_count": 97,
    "protocol_count": 17,
    "memory_adapters": 7,
    "mcp_tools": 250,
    "rest_endpoints": 56,
    "memory_list": ["Mem0", "Zep", "Letta", "LangMem", "Graphiti", "Qdrant", "Weaviate"],
    "protocols": [
        "MCP", "A2A", "ACP", "ANP", "LangGraph", "OpenAIAgents", "Nostr",
        "LangChain", "AutoGen", "CrewAI", "SemanticKernel",
        "gRPC", "GraphQL", "Kafka", "AMQP", "MQTT", "Webhook",
    ],
}

# ── Files to check ─────────────────────────────────────────────────────────────
FILES = [
    # OASIS2 repo
    ROOT / "Docs/Devs/API Documentation/WEB6/WEB6_REST_API_Reference.md",
    ROOT / "Docs/Devs/API Documentation/WEB6/WEB6-Getting-Started-Guide.md",
    ROOT / "Docs/Devs/API Documentation/WEB6/WEB6_MCP_Tool_Reference.md",
    ROOT / "Docs/Devs/API Documentation/WEB6/WEB6_User_Guide.md",
    ROOT / "Docs/INVESTOR_EVALUATION_GUIDE.md",
    ROOT / "Docs/OASIS_TECHNOLOGY_SUMMARY_AND_USE_CASES.md",
    ROOT / "Docs/OASIS_UNIQUE_SELLING_PROPOSITIONS.md",
    ROOT / "Docs/OASIS-IP-Repository-Report.html",
    ROOT / "Docs/Devs/OASIS_ARCHITECTURE_OVERVIEW.md",
    # OASIS-WEB6 submodule
    ROOT / "WEB6/README.md",
    # Web6Site repo
    Path(r"C:\Source\Web6Site\index.html"),
    Path(r"C:\Source\Web6Site\api.html"),
    Path(r"C:\Source\Web6Site\holonic-braid-whitepaper.html"),
    Path(r"C:\Source\Web6Site\whitepaper-pdf.html"),
    Path(r"C:\Source\Web6Site\pricing.html"),
    # OASISLearnWebsite
    Path(r"C:\Source\OASISLearnWebsite\tutorials\15-web6-ai-intro.html"),
]

# ── Stale patterns to detect ───────────────────────────────────────────────────
STALE_PROVIDER_COUNTS = [
    r"\b(6|10|12|14|15|16|17|18|19|20|25|30|40|50|60|70|80)\+\s*(?:AI\s*)?providers?\b",
    r"providers?[^.]*?\b(6|10|12|14|15|16|17|18|19|20|25|30|40|50|60|70|80)\+\b",
    r"\b(6|10|12|14|15|16|17|18|19|20|25|30|40|50|60|70|80)\+\s*(?:AI\s*)?model\s+providers?\b",
]

STALE_PROTOCOL_COUNTS = [
    r"\b([1-9]|1[0-6])\s+orchestrator\s+(?:adapter|protocol)s?\b",
    r"\b([1-9]|1[0-6])\+?\s+protocol\s+adapter",
]

STALE_MEMORY_PROVIDERS = [
    # Redis Vector is gone, replaced by Qdrant/Weaviate
    r"Redis\s+Vector\s+Memory",
    r"Redis\s+Vector",
]

MISSING_MEMORY_ADAPTERS = ["Qdrant", "Weaviate"]
MISSING_PROTOCOLS = ["LangGraph", "OpenAI", "Nostr"]  # "OpenAI" matches both "OpenAI Agents SDK" and "OpenAIAgents"


def check_file(path: Path) -> list[str]:
    issues = []
    if not path.exists():
        issues.append(f"  FILE MISSING: {path}")
        return issues
    try:
        text = path.read_text(encoding="utf-8", errors="replace")
    except Exception as e:
        issues.append(f"  READ ERROR: {e}")
        return issues

    lines = text.splitlines()

    def flag(lineno, msg):
        snippet = lines[lineno - 1][:120].strip()
        issues.append(f"  line {lineno:4d}: {msg}\n           → {snippet}")

    # Check for stale provider counts
    for i, line in enumerate(lines, 1):
        for pat in STALE_PROVIDER_COUNTS:
            if re.search(pat, line, re.IGNORECASE):
                # Allow if it also contains 97 (already correct)
                if "97" not in line:
                    # Exclude COSMIC ORM / WEB4 storage provider counts (not AI providers)
                    if re.search(r"COSMIC\s*ORM|storage\s+provider|blockchain|OASIS\s+provider|WEB4|pill", line, re.IGNORECASE):
                        continue
                    flag(i, f"STALE AI PROVIDER COUNT (expected 97) — matched: {pat!r}")

        for pat in STALE_PROTOCOL_COUNTS:
            if re.search(pat, line, re.IGNORECASE):
                if "17" not in line:
                    flag(i, f"STALE PROTOCOL COUNT (expected 17) — matched: {pat!r}")

        for pat in STALE_MEMORY_PROVIDERS:
            if re.search(pat, line, re.IGNORECASE):
                flag(i, f"STALE MEMORY PROVIDER (Redis Vector removed) — matched: {pat!r}")

    # Check for missing new memory adapters in files that list memory providers
    if re.search(r"Mem0|memory\s+provider|external\s+memory", text, re.IGNORECASE):
        for adapter in MISSING_MEMORY_ADAPTERS:
            if adapter not in text:
                issues.append(f"  MISSING MEMORY ADAPTER: '{adapter}' not mentioned")

    # Check for missing new protocols in files that list orchestrator protocols
    if re.search(r"orchestrat|MCP.*A2A|protocol\s+adapter", text, re.IGNORECASE):
        for proto in MISSING_PROTOCOLS:
            if proto not in text:
                issues.append(f"  MISSING PROTOCOL: '{proto}' not mentioned")

    return issues


def main():
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    parser = argparse.ArgumentParser(description="Check WEB6 doc sync")
    parser.add_argument("--fix", action="store_true", help="Show fix suggestions")
    args = parser.parse_args()

    print("WEB6 Doc Sync Checker")
    print(f"Ground truth: {TRUTH['provider_count']} providers · "
          f"{TRUTH['protocol_count']} protocols · "
          f"{TRUTH['memory_adapters']} memory adapters\n")

    total_issues = 0
    for path in FILES:
        issues = check_file(path)
        rel = str(path).replace(str(ROOT), "").replace(r"C:\Source", "")
        if issues:
            print(f"❌ {rel}")
            for issue in issues:
                print(issue)
            print()
            total_issues += len(issues)
        else:
            print(f"✅ {rel}")

    print(f"\n{'='*60}")
    if total_issues == 0:
        print("All docs in sync. ✅")
    else:
        print(f"{total_issues} issue(s) found. Update the flagged files.")
        sys.exit(1)


if __name__ == "__main__":
    main()
