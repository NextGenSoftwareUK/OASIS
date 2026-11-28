#!/usr/bin/env python3
"""One M04 XML increment provider batch; caller owns the adjacent producer/consumer build lease."""
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
 'FullyQualifiedName~NativeXmlSequenceGenerationAllocatesUniqueBusinessIdentifiers|FullyQualifiedName~NativeXmlInheritanceUsesPolymorphicProviderMappings',
 '--results-directory',str(RESULTS),'--logger','trx;LogFileName=xml-mapping-native.trx'],cwd=ROOT,check=True)
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
c=ET.parse(RESULTS/'xml-mapping-native.trx').find('.//t:Counters',ns)
assert c is not None and c.attrib['total']=='6' and c.attrib['passed']=='6' and c.attrib['failed']=='0'
groups={"xmlNativeSequencePolicy","crossContextUniqueIdentifiers","nativeReservationSeparateFromEntityAcknowledgement",
 "generatedLogicalKeyLookup","readonlyAndCancellationBeforeAllocation","fluentNativeBlockAllocation",
 "syncZeroKeyRefusedExplicitly","terminalHiLoBlockDoesNotWrap"}
for store in ['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS']:
 r=json.loads((RESULTS/(store+'-numeric-identifiers-native.json')).read_text(encoding='utf-8-sig'))
 assert r['provider']==store and r['checks']==8 and r['ownedCleanupIds']==6 and r['successfulHolonWrites']==5
 assert r['nativeProviderVerified'] is True and r['exactCleanupVerified'] is True
 assert set(r['evidence'])==groups and all(r['evidence'].values())
 assert len(r['queryElapsedMilliseconds'])==13
groups={"modelAdmissionRefusesInvalidHierarchies","discriminatorStoredNatively",
 "rootQueryMaterializesConcreteTypes","midLevelQueryReturnsSubtypesNatively",
 "leafQueriesExact","restrictionSurvivesFilterBypass","undeclaredSubtypeRefused",
 "derivedUpdateKeepsTypeAndDiscriminator"}
for store in ['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS']:
 r=json.loads((RESULTS/(store+'-xml-inheritance-native.json')).read_text(encoding='utf-8-sig'))
 assert r['provider']==store and r['checks']==8 and r['ownedCleanupIds']==4 and r['successfulHolonWrites']==2
 assert r['nativeProviderVerified'] is True and r['exactCleanupVerified'] is True
 assert set(r['evidence'])==groups and all(r['evidence'].values())
 assert len(r['queryElapsedMilliseconds'])==12
subprocess.run(['dotnet','build','ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI',*BOUNDS],cwd=ROOT,check=True)
def git(*args):
 return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
source=git('rev-parse','HEAD')
assert source==os.environ['GITHUB_SHA']
proof={'source':source,'core':git('-C','OASIS Architecture/NextGenSoftware.OASIS.API.Core','rev-parse','HEAD'),
 'nativePassed':6,'nativeNumericIdentifiersVerified':True,'xmlInheritanceVerified':True,'fullNHibernateParity':False,'actualWeb4Compiled':True,'ownedFixturesAbsent':True,
 'sharedDatabaseCalls':0,'deployed':False}
(RESULTS/'source.json').write_text(json.dumps(proof,indent=2)+'\n',encoding='utf-8')
