#!/usr/bin/env python3
"""Native set publication-count batch; caller owns the adjacent producer/consumer build lease."""
import json
import os
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[2]
RESULTS=ROOT/'TestResults'/'Neo4jCurrent'
RESULTS.mkdir(parents=True,exist_ok=True)
os.environ['OASIS_CQL_MONGO_ENTITY_PROJECTION_ROOT']=str(ROOT)
PROJECT='Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests'
BOUNDS=['-m:2','-nr:false','-p:UseSharedCompilation=false']
subprocess.run(['dotnet','test',PROJECT,*BOUNDS,'--filter',
 'FullyQualifiedName~NativeSetCountsIgnorePublicationCapsAndKeepSemanticWindows|FullyQualifiedName~NativeMongoRecursiveSetsPreserveNodeWindowsAndInputProofs',
 '--results-directory',str(RESULTS),'--logger','trx;LogFileName=recursive-entity-set-native.trx'],cwd=ROOT,check=True)
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
c=ET.parse(RESULTS/'recursive-entity-set-native.trx').find('.//t:Counters',ns)
assert c is not None and c.attrib['total']=='4' and c.attrib['passed']=='4' and c.attrib['failed']=='0'
groups={'allFourSetKinds','duplicateCountIndependentOfPublicationCap','semanticWindowsRetained',
 'postWindowPredicatesRetained','noEntityMaterialization','foreignCallerIsolated','inputCompletenessBeforeEmptyOutput','cancellationPreserved'}
for store in ['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS']:
 r=json.loads((RESULTS/(store+'-set-publication-count-native.json')).read_text(encoding='utf-8-sig'))
 assert r['provider']==store and r['checks']==8 and r['ownedCleanupIds']==7 and r['successfulHolonWrites']==1
 assert r['nativeProviderVerified'] is True and r['exactCleanupVerified'] is True
 assert set(r['evidence'])==groups and all(r['evidence'].values()) and len(r['queryElapsedMilliseconds'])==12
tree=json.loads((RESULTS/'MongoDBOASIS-recursive-entity-set-native.json').read_text(encoding='utf-8-sig'))
assert tree['provider']=='MongoDBOASIS' and tree['checks']==10 and tree['ownedCleanupIds']==4 and tree['successfulHolonWrites']==1
assert tree['nativeProviderVerified'] is True and tree['exactCleanupVerified'] is True and len(tree['queryElapsedMilliseconds'])==12
assert set(tree['evidence'])=={'sharedActorAuthorityModelAdmission','boundedTreeAndExplicitEnrollment','nestedConcatOccurrenceOrder',
 'perNodeWindowsPreserved','allFourNativeIdentityOperators','countIndependentOfPublication','rightNestedInput',
 'deepSourceAndChildOutputBoundsBeforeEmpty','foreignPolicyPreserved','cancellationPreserved'} and all(tree['evidence'].values())
subprocess.run(['dotnet','build','ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI',*BOUNDS],cwd=ROOT,check=True)
def git(*args):
 return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
source=git('rev-parse','HEAD')
assert source==os.environ['GITHUB_SHA']
proof={'source':source,'core':git('-C','OASIS Architecture/NextGenSoftware.OASIS.API.Core','rev-parse','HEAD'),
 'nativePassed':4,'nativeSetPublicationCountsVerified':True,'nativeRecursiveEntitySetProviders':['MongoDBOASIS'],'fullSetParentsComplete':False,'actualWeb4Compiled':True,'ownedFixturesAbsent':True,
 'sharedDatabaseCalls':0,'deployed':False}
(RESULTS/'source.json').write_text(json.dumps(proof,indent=2)+'\n',encoding='utf-8')
