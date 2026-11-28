#!/usr/bin/env python3
"""One O07 provider batch; caller owns the adjacent producer/consumer build lease."""
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
 'FullyQualifiedName~NativeExecutionStrategyPreservesIntentAndNeverReplaysUnknownOutcome',
 '--results-directory',str(RESULTS),'--logger','trx;LogFileName=execution-strategy-native.trx'],cwd=ROOT,check=True)
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
c=ET.parse(RESULTS/'execution-strategy-native.trx').find('.//t:Counters',ns)
assert c is not None and c.attrib['total']=='3' and c.attrib['passed']=='3' and c.attrib['failed']=='0'
groups={'nativeSourceBoundCreate','stableIdempotentReplay','boundedConfirmedAbortRetry',
 'unknownCommitReceiptNoReplay','absentReceiptNoReplay','callerSnapshotPreserved',
 'optimisticConflictNotRetried','cancellationBound','physicalSourceBinding','receiptActorIsolation'}
for store in ['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS']:
 r=json.loads((RESULTS/(store+'-execution-strategy-native.json')).read_text(encoding='utf-8-sig'))
 assert r['provider']==store and r['checks']==10 and r['ownedCleanupIds']==5 and r['successfulHolonWrites']==3
 assert r['nativeProviderVerified'] is True and r['exactCleanupVerified'] is True
 assert set(r['evidence'])==groups and all(r['evidence'].values())
 assert len(r['queryElapsedMilliseconds'])==10
subprocess.run(['dotnet','build','ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI',*BOUNDS],cwd=ROOT,check=True)
def git(*args):
 return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
source=git('rev-parse','HEAD')
assert source==os.environ['GITHUB_SHA']
proof={'source':source,'core':git('-C','OASIS Architecture/NextGenSoftware.OASIS.API.Core','rev-parse','HEAD'),
 'nativePassed':3,'executionStrategyVerified':True,'actualWeb4Compiled':True,'ownedFixturesAbsent':True,
 'sharedDatabaseCalls':0,'deployed':False,'faultBoundary':'Client transport only; actual storage and receipts use shipping native providers.'}
(RESULTS/'source.json').write_text(json.dumps(proof,indent=2)+'\n',encoding='utf-8')
