"""Prepare/verify Eclipse after the acknowledged Akash enrollment.

No mutations, deployment or native requests. Original candidates are immutable;
the shared mutation owner can later apply the separately journalled continuation.
"""
from pathlib import Path
import hashlib
import importlib.util
import json
import sys

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("eclipse_publisher", Path(__file__).with_name("Publish-HostedEclipsePublicConfiguration.py"))
eclipse = importlib.util.module_from_spec(spec)
spec.loader.exec_module(eclipse)
akash = eclipse.module("Publish-HostedAkashPublicConfiguration")
configuration = eclipse.configuration
PREFIX = "eclipse-public-after-akash"
INPUTS = ["eclipse-public-before-variables.dpapi", "eclipse-public-candidate-variables.dpapi",
          "akash-public-before-variables.dpapi", "akash-public-candidate-variables.dpapi"]


def require(condition, message):
    if not condition:
        raise ValueError(message)


def reconcile(original_before, original_after, akash_before, akash_after, current,
              eclipse_source, akash_source, acknowledgements):
    require(original_before == akash_before, "Original baselines differ; unrelated changes need explicit reconciliation.")
    require(eclipse.candidate(original_before, eclipse_source) == original_after,
            "Original Eclipse candidate/source differs.")
    require(akash.candidate(akash_before, akash_source) == akash_after,
            "Acknowledged Akash change differs from its reviewed candidate.")
    require(current == akash_after, "Unaudited live baseline change; no continuation permitted.")
    require(len(acknowledgements) == 3 and {a.get("key") for a in acknowledgements} == set(akash.KEYS),
            "Three original Akash mutation acknowledgements required.")
    for ack in acknowledgements:
        require(ack.get("verified") is True and ack.get("deploymentTriggered") is False and
                ack.get("sha256") == hashlib.sha256(akash_after[ack["key"]].encode()).hexdigest(),
                "Akash mutation acknowledgement differs.")

    result = eclipse.candidate(current, eclipse_source)
    # Source owner/catalogue and the original tested four presets are unchanged;
    # only admission is recomposed with the accepted Akash enrollment retained.
    for key in (eclipse.KEYS[0], eclipse.KEYS[2]):
        require(result[key] == original_after[key], "Original Eclipse source/presets changed.")
    return result


def inputs():
    source = eclipse.qualified_source()
    akash_source = akash.qualified_source()
    values = [configuration.protected_read(name) for name in INPUTS]
    acks = [json.loads((configuration.STATE / ("akash-public-" + key + ".acknowledged.json")).read_bytes())
            for key in akash.KEYS]
    accepted = json.loads((ROOT / "LocalState/COSMIC/HostedQueryAcceptance-AkashOASIS/cql-http-acceptance.acknowledged.json").read_bytes())
    require(accepted.get("provider") == "AkashOASIS" and accepted.get("pendingSaves") == 0 and
            accepted.get("saved") == 2 and accepted.get("repeatNewSaves") == 0 and
            accepted.get("nativeCounts") == [3, 2] and accepted.get("federationCounts") == [5, 4] and
            accepted.get("replicas") == {"SQLLiteDBOASIS": 2, "Neo4jOASIS": 2} and
            accepted.get("sourceSha256") == hashlib.sha256((ROOT / "LocalState/COSMIC/HostedAkashPublicSourceCurrent/source.json").read_bytes()).hexdigest(),
            "Original accepted Akash cohort/source acknowledgement required.")
    current = configuration.variables()
    after = reconcile(*values, current, source, akash_source, acks)
    lineage = {"schema": 1, "prefix": PREFIX,
               "originalInputs": {name: hashlib.sha256((configuration.STATE / name).read_bytes()).hexdigest() for name in INPUTS},
               "eclipseSourceSha256": hashlib.sha256((ROOT / "LocalState/COSMIC/HostedEclipsePublicSourceCurrent/source.json").read_bytes()).hexdigest(),
               "akashAcceptanceSha256": hashlib.sha256((ROOT / "LocalState/COSMIC/HostedQueryAcceptance-AkashOASIS/cql-http-acceptance.acknowledged.json").read_bytes()).hexdigest(),
               "changedKeys": eclipse.KEYS, "sourceOwnerReplaced": False, "originalsOverwritten": False}
    return source, current, after, lineage


def prepare():
    source, before, after, lineage = inputs()
    for suffix in ("before-variables.dpapi", "candidate-variables.dpapi", "lineage.json"):
        require(not (configuration.STATE / (PREFIX + "-" + suffix)).exists(),
                "Reconciliation already exists; verify it instead of preparing another.")
    configuration.write_new(configuration.STATE / (PREFIX + "-lineage.json"),
                            (json.dumps(lineage, indent=2) + "\n").encode())
    configuration.write_new(configuration.STATE / (PREFIX + "-before-variables.dpapi"),
                            configuration.verification.protected_token(configuration.compact(before).encode()))
    configuration.write_new(configuration.STATE / (PREFIX + "-candidate-variables.dpapi"),
                            configuration.verification.protected_token(configuration.compact(after).encode()))
    review = {"changedKeys": eclipse.KEYS, "profiles": {"EclipseOASIS": source},
              "examples": {"EclipseOASIS": json.loads(after[eclipse.KEYS[2]])},
              "dnaUnchanged": before["OASIS_DNA_JSON"] == after["OASIS_DNA_JSON"],
              "originalSourceAndPresetsUnchanged": True, "acceptedAkashPreserved": True, "cloudSpend": 0}
    configuration.write_new(ROOT / "TestResults/EclipsePublicSourceCurrent/reconciled-review.json",
                            (json.dumps(review, indent=2) + "\n").encode())
    print("Eclipse continuation prepared: three keys; original source/presets and accepted Akash preserved. No mutation.")


def verify():
    _, before, after, lineage = inputs()
    require(configuration.protected_read(PREFIX + "-before-variables.dpapi") == before and
            configuration.protected_read(PREFIX + "-candidate-variables.dpapi") == after and
            json.loads((configuration.STATE / (PREFIX + "-lineage.json")).read_bytes()) == lineage,
            "Prepared continuation or original evidence changed; no apply permitted.")
    print("Prepared Eclipse continuation and original lineage match live baseline; no mutation.")


if __name__ == "__main__":
    if sys.argv[1:] == ["prepare"]:
        prepare()
    elif sys.argv[1:] == ["verify"]:
        verify()
    else:
        raise SystemExit("Use prepare once or verify; this tool never applies settings.")
