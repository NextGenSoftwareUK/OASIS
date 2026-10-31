"""Offline extension of the preserved seven-source candidate. Never applies variables."""
from pathlib import Path
import importlib.util
import json

ROOT=Path(__file__).resolve().parents[2]
PREFIX="ten-source-public"
OLD="google-ethereum-public-polygon-arbitrum-cosmos-polkadot-near"

def load(name):
    spec=importlib.util.spec_from_file_location(name,Path(__file__).with_name(name+".py"))
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module

def candidate(seven,sources):
    after=seven
    for name in ("Base","Avalanche","Bnb"):
        after=load("Publish-Hosted"+name+"PublicConfiguration").candidate(after,sources[name])
    return after

def main():
    configuration=load("Publish-HostedSolidConfiguration")
    seven=configuration.protected_read(OLD+"-candidate-variables.dpapi")
    sources={name:json.loads((ROOT/("LocalState/COSMIC/Hosted"+name+"PublicSourceCurrent/source.json")).read_text(encoding="utf-8-sig")) for name in ("Base","Avalanche","Bnb")}
    after=candidate(seven,sources)
    # Original host snapshot remains authoritative; this offline extension is
    # not a fresh observation and cannot authorize application.
    for suffix,value in (("before",configuration.protected_read(OLD+"-before-variables.dpapi")),("candidate",after)):
        path=configuration.STATE/(PREFIX+"-"+suffix+"-variables.dpapi")
        if path.exists():
            if configuration.protected_read(path.name)!=value:raise RuntimeError("Preserved candidate differs; reconcile without overwrite")
        else:configuration.write_new(path,configuration.verification.protected_token(configuration.compact(value).encode()))
    review=load("Review-PreparedPublicBatch")
    public=review.public_review(after)
    examples=json.loads(after["CQL_HOSTED_QUERY_EXAMPLES_JSON"])
    for name,provider,key in (("Base","BaseOASIS","CQL_BASE_PUBLIC_SOURCE_JSON"),("Avalanche","AvalancheOASIS","CQL_AVALANCHE_PUBLIC_SOURCE_JSON"),("Bnb","BNBChainOASIS","CQL_BNB_PUBLIC_SOURCE_JSON")):
        public["profiles"][provider]=sources[name]
        public["examples"][provider]=examples[provider]
    path=ROOT/"TestResults/HostedCqlIntegrationCurrent/ten-source-public-review.json"
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(public,indent=2)+"\n",encoding="utf-8")
    print("Offline protected ten-source candidate and public-only40preset review prepared; no host observation/application/deployment.")

if __name__=="__main__":main()
