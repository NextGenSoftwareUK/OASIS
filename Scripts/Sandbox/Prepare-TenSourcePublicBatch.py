"""Offline extension of the preserved seven-source candidate. Explicit apply-reviewed uses the journalled publisher without deployment."""
from pathlib import Path
import importlib.util
import json
import sys

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

SOURCE_KEYS=("CQL_GCS_PUBLIC_SOURCE_JSON","CQL_ETHEREUM_PUBLIC_SOURCE_JSON",
    "CQL_POLYGON_PUBLIC_SOURCE_JSON","CQL_ARBITRUM_PUBLIC_SOURCE_JSON",
    "CQL_COSMOS_PUBLIC_SOURCE_JSON","CQL_POLKADOT_PUBLIC_SOURCE_JSON",
    "CQL_NEAR_PUBLIC_SOURCE_JSON","CQL_BASE_PUBLIC_SOURCE_JSON",
    "CQL_AVALANCHE_PUBLIC_SOURCE_JSON","CQL_BNB_PUBLIC_SOURCE_JSON")
APPLY_KEYS=SOURCE_KEYS+("PREPARED_STORAGE_ADMISSION_JSON","CQL_HOSTED_QUERY_EXAMPLES_JSON")

def application_keys(before,after,include_optimism=False,include_linea_scroll=False,include_zksync_abstract=False,include_monad=False,include_sei=False):
    if include_linea_scroll and not include_optimism:raise ValueError("Explicit thirteen-source scope requires Optimism")
    if include_zksync_abstract and not (include_optimism and include_linea_scroll):raise ValueError("Explicit fifteen-source scope requires all earlier profiles")
    if include_monad and not (include_optimism and include_linea_scroll and include_zksync_abstract):raise ValueError("Explicit sixteen-source scope requires all earlier profiles")
    if include_sei and not (include_optimism and include_linea_scroll and include_zksync_abstract and include_monad):raise ValueError("Explicit seventeen-source scope requires all earlier profiles")
    source_keys=SOURCE_KEYS+(("CQL_OPTIMISM_PUBLIC_SOURCE_JSON",) if include_optimism else ())
    if include_linea_scroll:source_keys+=("CQL_LINEA_PUBLIC_SOURCE_JSON","CQL_SCROLL_PUBLIC_SOURCE_JSON")
    if include_zksync_abstract:source_keys+=("CQL_ZKSYNC_PUBLIC_SOURCE_JSON","CQL_ABSTRACT_PUBLIC_SOURCE_JSON")
    if include_monad:source_keys+=("CQL_MONAD_PUBLIC_SOURCE_JSON",)
    if include_sei:source_keys+=("CQL_SEI_PUBLIC_SOURCE_JSON",)
    apply_keys=source_keys+APPLY_KEYS[-2:]
    changed={key for key in before.keys()|after.keys() if before.get(key)!=after.get(key)}
    if changed!=set(apply_keys) or any(key not in after for key in before):
        raise ValueError("Exactly the selected source profiles and shared admission/examples may change; no removal")
    if any(key in before for key in source_keys):
        raise ValueError("Existing source enrollment requires reconciliation")
    return apply_keys

def apply(include_optimism=False,include_linea_scroll=False,include_zksync_abstract=False,include_monad=False,include_sei=False):
    prefix="seventeen-source-public" if include_sei else "sixteen-source-public" if include_monad else "fifteen-source-public" if include_zksync_abstract else "thirteen-source-public" if include_linea_scroll else "eleven-source-public" if include_optimism else PREFIX
    configuration=load("Publish-HostedSolidConfiguration")
    before=configuration.protected_read(prefix+"-before-variables.dpapi")
    after=configuration.protected_read(prefix+"-candidate-variables.dpapi")
    # All intent, concurrency, exact-observation and no-replay semantics have
    # one owner in the existing publisher. This command never deploys.
    configuration.apply_values(prefix,application_keys(before,after,include_optimism,include_linea_scroll,include_zksync_abstract,include_monad,include_sei))

def main(include_optimism=False,include_linea_scroll=False,include_zksync_abstract=False,include_monad=False,include_sei=False):
    prefix="seventeen-source-public" if include_sei else "sixteen-source-public" if include_monad else "fifteen-source-public" if include_zksync_abstract else "thirteen-source-public" if include_linea_scroll else "eleven-source-public" if include_optimism else PREFIX
    configuration=load("Publish-HostedSolidConfiguration")
    seven=configuration.protected_read(OLD+"-candidate-variables.dpapi")
    sources={name:json.loads((ROOT/("LocalState/COSMIC/Hosted"+name+"PublicSourceCurrent/source.json")).read_text(encoding="utf-8-sig")) for name in ("Base","Avalanche","Bnb")}
    after=candidate(seven,sources)
    if include_optimism:
        sources["Optimism"]=json.loads((ROOT/"LocalState/COSMIC/HostedOptimismPublicSourceCurrent/source.json").read_text(encoding="utf-8-sig"))
        after=load("Publish-HostedOptimismPublicConfiguration").candidate(after,sources["Optimism"])
    if include_linea_scroll:
        if not include_optimism:raise ValueError("Explicit thirteen-source scope requires Optimism")
        for name in ("Linea","Scroll"):
            sources[name]=json.loads((ROOT/("LocalState/COSMIC/Hosted"+name+"PublicSourceCurrent/source.json")).read_text(encoding="utf-8-sig"))
            after=load("Publish-Hosted"+name+"PublicConfiguration").candidate(after,sources[name])
    if include_zksync_abstract:
        if not (include_optimism and include_linea_scroll):raise ValueError("Explicit fifteen-source scope requires all earlier profiles")
        for name in ("ZkSync","Abstract"):
            sources[name]=json.loads((ROOT/("LocalState/COSMIC/Hosted"+name+"PublicSourceCurrent/source.json")).read_text(encoding="utf-8-sig"))
            after=load("Publish-Hosted"+name+"PublicConfiguration").candidate(after,sources[name])
    if include_monad:
        if not (include_optimism and include_linea_scroll and include_zksync_abstract):raise ValueError("Explicit sixteen-source scope requires all earlier profiles")
        sources["Monad"]=json.loads((ROOT/"LocalState/COSMIC/HostedMonadPublicSourceCurrent/source.json").read_text(encoding="utf-8-sig"))
        after=load("Publish-HostedMonadPublicConfiguration").candidate(after,sources["Monad"])
    if include_sei:
        if not (include_optimism and include_linea_scroll and include_zksync_abstract and include_monad):raise ValueError("Explicit seventeen-source scope requires all earlier profiles")
        sources["Sei"]=json.loads((ROOT/"LocalState/COSMIC/HostedSeiPublicSourceCurrent/source.json").read_text(encoding="utf-8-sig"))
        after=load("Publish-HostedSeiPublicConfiguration").candidate(after,sources["Sei"])
    # Original host snapshot remains authoritative; this offline extension is
    # not a fresh observation and cannot authorize application.
    for suffix,value in (("before",configuration.protected_read(OLD+"-before-variables.dpapi")),("candidate",after)):
        path=configuration.STATE/(prefix+"-"+suffix+"-variables.dpapi")
        if path.exists():
            if configuration.protected_read(path.name)!=value:raise RuntimeError("Preserved candidate differs; reconcile without overwrite")
        else:configuration.write_new(path,configuration.verification.protected_token(configuration.compact(value).encode()))
    review=load("Review-PreparedPublicBatch")
    public=review.public_review(after)
    examples=json.loads(after["CQL_HOSTED_QUERY_EXAMPLES_JSON"])
    for name,provider,key in (("Base","BaseOASIS","CQL_BASE_PUBLIC_SOURCE_JSON"),("Avalanche","AvalancheOASIS","CQL_AVALANCHE_PUBLIC_SOURCE_JSON"),("Bnb","BNBChainOASIS","CQL_BNB_PUBLIC_SOURCE_JSON")):
        public["profiles"][provider]=sources[name]
        public["examples"][provider]=examples[provider]
    if include_optimism:
        public["profiles"]["OptimismOASIS"]=sources["Optimism"]
        public["examples"]["OptimismOASIS"]=examples["OptimismOASIS"]
    if include_linea_scroll:
        for name in ("Linea","Scroll"):
            public["profiles"][name+"OASIS"]=sources[name]
            public["examples"][name+"OASIS"]=examples[name+"OASIS"]
    if include_zksync_abstract:
        for name in ("ZkSync","Abstract"):
            public["profiles"][name+"OASIS"]=sources[name]
            public["examples"][name+"OASIS"]=examples[name+"OASIS"]
    if include_monad:
        public["profiles"]["MonadOASIS"]=sources["Monad"]
        public["examples"]["MonadOASIS"]=examples["MonadOASIS"]
    if include_sei:
        public["profiles"]["SeiOASIS"]=sources["Sei"]
        public["examples"]["SeiOASIS"]=examples["SeiOASIS"]
    path=ROOT/("TestResults/HostedCqlIntegrationCurrent/"+prefix+"-review.json")
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(public,indent=2)+"\n",encoding="utf-8")
    if include_optimism:
        focused={key:{"OptimismOASIS":value["OptimismOASIS"]} for key,value in public.items()}
        (path.parent/"optimism-prepared-review.json").write_text(json.dumps(focused,indent=2)+"\n",encoding="utf-8")
    if include_linea_scroll:
        focused={key:{provider:value[provider] for provider in ("LineaOASIS","ScrollOASIS")} for key,value in public.items()}
        (path.parent/"linea-scroll-prepared-review.json").write_text(json.dumps(focused,indent=2)+"\n",encoding="utf-8")
    if include_zksync_abstract:
        focused={key:{provider:value[provider] for provider in ("ZkSyncOASIS","AbstractOASIS")} for key,value in public.items()}
        (path.parent/"zksync-abstract-prepared-review.json").write_text(json.dumps(focused,indent=2)+"\n",encoding="utf-8")
    if include_monad:
        focused={key:{"MonadOASIS":value["MonadOASIS"]} for key,value in public.items()}
        (path.parent/"monad-prepared-review.json").write_text(json.dumps(focused,indent=2)+"\n",encoding="utf-8")
    if include_sei:
        focused={key:{"SeiOASIS":value["SeiOASIS"]} for key,value in public.items()}
        (path.parent/"sei-prepared-review.json").write_text(json.dumps(focused,indent=2)+"\n",encoding="utf-8")
    print("Offline protected candidate prepared: "+str(len(public["profiles"]))+"profiles; no host observation/application/deployment.")

if __name__=="__main__":
    if not sys.argv[1:]:main()
    elif sys.argv[1:]==["with-optimism"]:main(True)
    elif sys.argv[1:]==["with-linea-scroll"]:main(True,True)
    elif sys.argv[1:]==["with-zksync-abstract"]:main(True,True,True)
    elif sys.argv[1:]==["with-monad"]:main(True,True,True,True)
    elif sys.argv[1:]==["with-sei"]:main(True,True,True,True,True)
    elif sys.argv[1:]==["apply-reviewed"]:apply()
    elif sys.argv[1:]==["apply-reviewed-with-optimism"]:apply(True)
    elif sys.argv[1:]==["apply-reviewed-with-linea-scroll"]:apply(True,True)
    elif sys.argv[1:]==["apply-reviewed-with-zksync-abstract"]:apply(True,True,True)
    elif sys.argv[1:]==["apply-reviewed-with-monad"]:apply(True,True,True,True)
    elif sys.argv[1:]==["apply-reviewed-with-sei"]:apply(True,True,True,True,True)
    else:raise SystemExit("Use no arguments for offline preparation; apply-reviewed only after matching release and publication timing are verified")
