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
 'FullyQualifiedName~NativeSetCountsIgnorePublicationCapsAndKeepSemanticWindows|FullyQualifiedName~NativeRecursiveSetsPreserveNodeWindowsAndInputProofs|FullyQualifiedName~NativeProjectedTupleSetsCompareEveryVisibleColumn|FullyQualifiedName~NativeRecursiveSelectedSetsUseVisibleTupleEquality',
 '--results-directory',str(RESULTS),'--logger','trx;LogFileName=recursive-selected-set-native.trx'],cwd=ROOT,check=True)
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
c=ET.parse(RESULTS/'recursive-selected-set-native.trx').find('.//t:Counters',ns)
assert c is not None and c.attrib['total']=='12' and c.attrib['passed']=='12' and c.attrib['failed']=='0'
groups={'allFourSetKinds','duplicateCountIndependentOfPublicationCap','semanticWindowsRetained',
 'postWindowPredicatesRetained','noEntityMaterialization','foreignCallerIsolated','inputCompletenessBeforeEmptyOutput','cancellationPreserved'}
for store in ['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS']:
 r=json.loads((RESULTS/(store+'-set-publication-count-native.json')).read_text(encoding='utf-8-sig'))
 assert r['provider']==store and r['checks']==8 and r['ownedCleanupIds']==7 and r['successfulHolonWrites']==1
 assert r['nativeProviderVerified'] is True and r['exactCleanupVerified'] is True
 assert set(r['evidence'])==groups and all(r['evidence'].values()) and len(r['queryElapsedMilliseconds'])==12
for store in ['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS']:
 tree=json.loads((RESULTS/(store+'-recursive-entity-set-native.json')).read_text(encoding='utf-8-sig'))
 assert tree['provider']==store and tree['checks']==14 and tree['ownedCleanupIds']==4 and tree['successfulHolonWrites']==1
 assert tree['nativeProviderVerified'] is True and tree['exactCleanupVerified'] is True and len(tree['queryElapsedMilliseconds'])==16
 assert set(tree['evidence'])=={'registeredLinqLateSelection','registeredLinqLeftTree','registeredLinqRightTree','registeredLinqNativeCount','sharedActorAuthorityModelAdmission','boundedTreeAndExplicitEnrollment','nestedConcatOccurrenceOrder',
  'perNodeWindowsPreserved','allFourNativeIdentityOperators','countIndependentOfPublication','rightNestedInput',
  'deepSourceAndChildOutputBoundsBeforeEmpty','foreignPolicyPreserved','cancellationPreserved'} and all(tree['evidence'].values())
for store in ['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS']:
 flat=json.loads((RESULTS/(store+'-projected-tuple-set-native.json')).read_text(encoding='utf-8-sig'))
 assert flat['provider']==store and flat['checks']==4 and flat['successfulHolonWrites']==4
 assert flat['nativeProviderVerified'] and flat['exactCleanupVerified'] and flat['allFourSetOperationsVerified'] and flat['visibleTupleEqualityVerified']
 assert flat['concatMultiplicityVerified'] and flat['nullTupleCellVerified'] and flat['foreignRowsHiddenVerified']
for store in ['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS']:
 selected=json.loads((RESULTS/(store+'-recursive-selected-set-native.json')).read_text(encoding='utf-8-sig'))
 assert selected['provider']==store and selected['checks']==14 and selected['ownedCleanupIds']==4 and selected['successfulHolonWrites']==1
 assert selected['nativeProviderVerified'] and selected['exactCleanupVerified'] and len(selected['queryElapsedMilliseconds'])==17
 assert set(selected['evidence'])=={'nativeCountIndependentOfPublication','nativeCardinalityWithoutConstructors','foreignNativeCount','registeredLinqLeftTree','registeredLinqRightTree','explicitTreeAdmission','concatOccurrences','visibleTupleUnion','visibleTupleIntersectExcept','nodeWindows','rightNested','foreignIsolation','childBoundBeforeEmpty','cancellation'} and all(selected['evidence'].values())
subprocess.run(['dotnet','build','ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI',*BOUNDS],cwd=ROOT,check=True)
def git(*args):
 return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
source=git('rev-parse','HEAD')
assert source==os.environ['GITHUB_SHA']
proof={'source':source,'core':git('-C','OASIS Architecture/NextGenSoftware.OASIS.API.Core','rev-parse','HEAD'),
 'nativePassed':12,'nativeSetPublicationCountsVerified':True,'nativeRecursiveEntitySetProviders':['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS'],'nativeRecursiveSelectedSetProviders':['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS'],'nativeSelectedSetCountVerified':True,'fullSetParentsComplete':False,'actualWeb4Compiled':True,'ownedFixturesAbsent':True,
 'sharedDatabaseCalls':0,'deployed':False}
(RESULTS/'source.json').write_text(json.dumps(proof,indent=2)+'\n',encoding='utf-8')
