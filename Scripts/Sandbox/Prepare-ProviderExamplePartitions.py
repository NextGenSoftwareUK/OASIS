"""Prepare/apply provider-partitioned public presets after reviewed partial enrollment.

The rejected combined catalogue and its original journal are never overwritten.
Default prepares only; apply-reviewed requires the exact matching live release.
"""
from pathlib import Path
import importlib.util
import json
import sys
ROOT=Path(__file__).resolve().parents[2]
PREFIX='seventeen-source-example-partitions'
ORIGINAL='seventeen-source-public'
KEY='CQL_HOSTED_QUERY_EXAMPLES_JSON'
SECTION='CQL_HOSTED_QUERY_EXAMPLES__'

def load(name):
    spec=importlib.util.spec_from_file_location(name,Path(__file__).with_name(name+'.py'))
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module

def partition_candidate(partial,complete,providers):
    existing=json.loads(partial[KEY]);examples=json.loads(complete[KEY])
    if len(providers)!=17 or len(set(providers))!=17 or set(examples)-set(existing)!=set(providers):
        raise ValueError('Exact reviewed seventeen-provider addition required')
    if any(examples[name]!=value for name,value in existing.items()):
        raise ValueError('Existing provider examples must remain unchanged')
    after=dict(partial)
    for provider in providers:
        name=SECTION+provider
        if name in partial:raise ValueError('Existing partition requires reconciliation')
        value=json.dumps(examples[provider],separators=(',',':'))
        if len(value)>32768:raise ValueError('Provider partition exceeds hosted variable limit')
        after[name]=value
    return after

def main(apply=False):
    configuration=load('Publish-HostedSolidConfiguration')
    before=configuration.protected_read(ORIGINAL+'-before-variables.dpapi')
    complete=configuration.protected_read(ORIGINAL+'-candidate-variables.dpapi')
    partial=dict(complete);partial[KEY]=before[KEY]
    providers=list(json.loads(complete[KEY]).keys()-json.loads(before[KEY]).keys())
    providers.sort()
    after=partition_candidate(partial,complete,providers)
    if apply:
        batch=load('Prepare-TenSourcePublicBatch')
        batch.require_matching_release(configuration)
        if configuration.protected_read(PREFIX+'-before-variables.dpapi')!=partial or configuration.protected_read(PREFIX+'-candidate-variables.dpapi')!=after:
            raise RuntimeError('Protected reviewed partition candidate changed')
        configuration.apply_values(PREFIX,tuple(SECTION+provider for provider in providers))
        return
    if configuration.variables()!=partial:
        raise RuntimeError('Current settings differ from the exact reviewed18setting state')
    for suffix,value in [('before',partial),('candidate',after)]:
        path=configuration.STATE/(PREFIX+'-'+suffix+'-variables.dpapi')
        if path.exists():
            if configuration.protected_read(path.name)!=value:raise RuntimeError('Preserved candidate differs')
        else:configuration.write_new(path,configuration.verification.protected_token(configuration.compact(value).encode()))
    print(json.dumps({'partitions':len(providers),'maximumPartitionCharacters':max(len(after[SECTION+p]) for p in providers),'hostMutation':False}))

if __name__=='__main__':
    if not sys.argv[1:]:main()
    elif sys.argv[1:]==['apply-reviewed']:main(True)
    else:raise SystemExit('Use no arguments to prepare; apply-reviewed after matching live release')
