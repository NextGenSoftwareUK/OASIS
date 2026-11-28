#!/usr/bin/env python3
"""Canonical XML Version and registered custom scalars through real native acknowledged writes; caller owns the build lease."""
import json,os,subprocess,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
RESULTS=ROOT/'TestResults'/'Neo4jCurrent'
RESULTS.mkdir(parents=True,exist_ok=True)
os.environ['OASIS_CQL_MONGO_ENTITY_PROJECTION_ROOT']=str(ROOT)
BOUNDS=['-m:2','-nr:false','-p:UseSharedCompilation=false']
subprocess.run(['dotnet','test','Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests',*BOUNDS,
 '--filter','FullyQualifiedName~NativeXmlVersionMappingRejectsStaleWrites|FullyQualifiedName~XmlVersionDeclarationsPreserveCanonicalConcurrencyAndAtomicImport|FullyQualifiedName~NativeXmlUserTypesRoundtripFilterProjectAndTrack|FullyQualifiedName~XmlUserTypeRegistryIsExplicitAndAtomic|FullyQualifiedName~NativeOrdinaryXmlUserTypesShareFluentNativeMapping',
 '--results-directory',str(RESULTS),'--logger','trx;LogFileName=xml-version-native.trx'],cwd=ROOT,check=True)
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
c=ET.parse(RESULTS/'xml-version-native.trx').find('.//t:Counters',ns)
assert c is not None and c.attrib['total']=='11' and c.attrib['passed']=='11' and c.attrib['failed']=='0'
for store in ['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS']:
 r=json.loads((RESULTS/(store+'-xml-version-native.json')).read_text(encoding='utf-8-sig'))
 assert r['provider']==store and r['checks']==5 and r['ownedCleanupIds']==1 and r['successfulHolonWrites']==2
 assert r['nativeProviderVerified'] and r['exactCleanupVerified'] and len(r['queryElapsedMilliseconds'])==6
 assert set(r['evidence'])=={'xmlCanonicalVersionAck','nativeVersionIncrement','nativeStaleWriteRejected','winnerPreserved','callerPolicyPreserved'} and all(r['evidence'].values())
for store in ['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS']:
 r=json.loads((RESULTS/(store+'-xml-user-type-native.json')).read_text(encoding='utf-8-sig'))
 assert r['provider']==store and r['checks']==8 and r['ownedCleanupIds']==1 and r['successfulHolonWrites']==2
 assert r['nativeProviderVerified'] and r['exactCleanupVerified'] and len(r['queryElapsedMilliseconds'])==8
 assert set(r['evidence'])=={'convertedStorageRepresentation','typedConversionRoundtrip','nativeConvertedFilterAndOrder','nativeConvertedProjectionAndMembership','convertedSetModelPreserved','convertedCallerIsolation','convertedMutableUpdateAcknowledged','immutableConverterModelAndFilterBypass'} and all(r['evidence'].values())
for store in ['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS']:
 r=json.loads((RESULTS/(store+'-poco-xml-conversion-native.json')).read_text(encoding='utf-8-sig'))
 assert r['provider']==store and r['checks']==9 and r['ownedCleanupIds']==1 and r['successfulHolonWrites']==2
 assert r['nativeProviderVerified'] and r['exactCleanupVerified'] and len(r['queryElapsedMilliseconds'])==9
 assert set(r['evidence'])=={'fluentAndXmlNativeModel','providerStorageRepresentation','convertedNativeFilterOrderProjection','typedValueRoundtrip','convertedTrackedUpdate','callerVisibility','modelDomainConstraints','atomicRegistryImport','frozenConverters'} and all(r['evidence'].values())
subprocess.run(['dotnet','build','ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI',*BOUNDS],cwd=ROOT,check=True)
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT,text=True).strip()
source=git('rev-parse','HEAD');assert source==os.environ['GITHUB_SHA']
(RESULTS/'source.json').write_text(json.dumps({'source':source,'core':git('-C','OASIS Architecture/NextGenSoftware.OASIS.API.Core','rev-parse','HEAD'),
 'nativePassed':9,'ordinaryClassXmlConversionsVerified':True,'admissionControlsPassed':2,'registeredXmlUserTypesVerified':True,'canonicalXmlVersionVerified':True,'nativeStaleWritesRejected':True,
 'fullNhXmlComplete':False,'actualWeb4Compiled':True,'ownedFixturesAbsent':True,'sharedDatabaseCalls':0,'deployed':False},indent=2)+'\n',encoding='utf-8')
