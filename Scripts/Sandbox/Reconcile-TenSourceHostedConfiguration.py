"""Observe current hosted settings without applying the staged candidate."""
import argparse
import importlib.util
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]

def compare(before,current,after):
    changed={key for key in before.keys()|current.keys() if before.get(key)!=current.get(key)}
    planned={key for key in before.keys()|after.keys() if before.get(key)!=after.get(key)}
    return {"matchesPreservedSnapshot":not changed,"changedKeyNames":sorted(changed),
        "plannedKeyNames":sorted(planned),"overlappingChangedKeyNames":sorted(changed&planned),
        "plannedValueCount":len(planned),"hostMutation":False,"deploymentTriggered":False}

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--scope",choices=("ten","eleven","thirteen","fifteen","sixteen"),default="ten")
    options=parser.parse_args()
    prefix=options.scope+"-source-public"
    spec=importlib.util.spec_from_file_location("configuration",Path(__file__).with_name("Publish-HostedSolidConfiguration.py"))
    config=importlib.util.module_from_spec(spec);spec.loader.exec_module(config)
    before=config.protected_read(prefix+"-before-variables.dpapi")
    after=config.protected_read(prefix+"-candidate-variables.dpapi")
    current=config.variables()
    report=compare(before,current,after)
    output=ROOT/("TestResults/HostedCqlIntegrationCurrent/"+options.scope+"-source-host-reconciliation.json")
    output.write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report))

if __name__=="__main__":main()
