#!/usr/bin/env python3
"""Held native transaction owner and changed owned-write regression; caller owns the build lease."""
import json,os,subprocess,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
RESULTS=ROOT/'TestResults'/'Neo4jCurrent'
RESULTS.mkdir(parents=True,exist_ok=True)
os.environ['OASIS_CQL_MONGO_ENTITY_PROJECTION_ROOT']=str(ROOT)
BOUNDS=['-m:2','-nr:false','-p:UseSharedCompilation=false']
subprocess.run(['dotnet','test','Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests',*BOUNDS,
 '--filter','FullyQualifiedName~NativeHeldTransactionsKeepProvisionalWritesAndReadsInsideOneOwner|FullyQualifiedName~NativeHeldOwnerPreservesExistingMixedUnitOfWork|FullyQualifiedName~NativeHeldOwnedPreparationEnforcesPermissionsAndDefersPublication',
 '--results-directory',str(RESULTS),'--logger','trx;LogFileName=held-transaction-native.trx'],cwd=ROOT,check=True)
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
c=ET.parse(RESULTS/'held-transaction-native.trx').find('.//t:Counters',ns)
assert c is not None and c.attrib['total']=='9' and c.attrib['passed']=='9' and c.attrib['failed']=='0'
for store in ['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS']:
 r=json.loads((RESULTS/(store+'-held-transaction-native.json')).read_text(encoding='utf-8-sig'))
 assert r['provider']==store and r['checks']==11 and r['ownedCleanupIds']==2 and r['successfulHolonWrites']==3
 assert r['nativeProviderVerified'] and r['exactCleanupVerified'] and len(r['queryElapsedMilliseconds'])==21
 assert set(r['evidence'])=={'privateAuthorityReadsEnlistedWithoutGrant','nativeUniqueClaimsShareTransaction','heldNativeOwner','provisionalWriteWithoutSavedAcknowledgement','readOwnUncommittedWrites',
  'outsideReadCannotSeeProvisionalWrites','sequentialNativeRevisionUpdate','immutableProvisionalSnapshots',
  'acknowledgedCommitAndRollback','capturedCallerPolicy','cancelBeforeCommitDoesNotDispatch'} and all(r['evidence'].values())
 old=json.loads((RESULTS/(store+'-held-owner-mixed-uow-native.json')).read_text(encoding='utf-8-sig'))
 assert old['provider']==store and old['nativeProviderVerified'] and old['exactCleanupVerified']
 assert old['checks']==26 and old['successfulHolonWrites']==4 and all(old['evidence'].values())
 r=json.loads((RESULTS/(store+'-held-owned-preparation-native.json')).read_text(encoding='utf-8-sig'))
 assert r['provider']==store and r['checks']==6 and r['ownedCleanupIds']==2 and r['successfulHolonWrites']==1
 assert r['nativeProviderVerified'] and r['exactCleanupVerified'] and len(r['queryElapsedMilliseconds'])==10
 assert set(r['evidence'])=={'sharedOwnedPreparation','generatedIdentityDeferred','provisionalPublicationRefused',
  'acknowledgedTrackedPublication','writePermissionEnforced','rollbackLeavesCallerUnpublished'} and all(r['evidence'].values())
subprocess.run(['dotnet','build','ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI',*BOUNDS],cwd=ROOT,check=True)
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
source=git('rev-parse','HEAD');assert source==os.environ['GITHUB_SHA']
(RESULTS/'source.json').write_text(json.dumps({'source':source,'core':git('-C','OASIS Architecture/NextGenSoftware.OASIS.API.Core','rev-parse','HEAD'),
 'heldNativeProviders':['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS'],'heldProviderDone':3,'heldProviderTotal':3,'nativePassed':9,'ownedPreparationNativeVerified':True,'privateAuthorityReadsEnlisted':True,'provisionalKernelOnly':True,
 'publicSessionOwnedWriteEnlistmentComplete':False,'fullO03Complete':False,'actualWeb4Compiled':True,
 'ownedFixturesAbsent':True,'sharedDatabaseCalls':0,'deployed':False},indent=2)+'\n',encoding='utf-8')
