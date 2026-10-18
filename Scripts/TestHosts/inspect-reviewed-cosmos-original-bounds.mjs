import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createHash} from 'node:crypto';
import {verifyCosmosPreparedIdentity} from '../../Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/SdkBridge/transaction-results.mjs';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const file=path.join(root,'TestResults/HyperDriveCosmos/reviewed-final-fee-current.json');
const digest=value=>createHash('sha256').update(value).digest('hex').toUpperCase();
const bytes=fs.readFileSync(file);
const originalHash='945C7A5777E6D662BF14CD31BA83B19FF0CDB8871893074027055F4160DC2693';
if(digest(bytes)!==originalHash)throw new Error('Exact reviewed original report changed; inspect before accepting another fixture.');
const report=JSON.parse(bytes);
if(report.operation.Id!=='2a8d018c-2c3e-a6b9-c0ae-7c6a81057d99'||report.review.LastHeight!==9396||report.operation.ProviderAttempts.length!==3)
 throw new Error('Reviewed operation/height/original inventory mismatch');
const originals=report.operation.ProviderAttempts.map(attempt=>{
 const prepared=attempt.NativePreparation;
 const identity=verifyCosmosPreparedIdentity({transactionHash:attempt.ProviderRequestId,chainId:prepared.ChainId,sender:prepared.Sender,
  signedTransactionBase64:prepared.SignedTransactionBase64,signDocBase64:prepared.SignDocBase64});
 if(identity.timeoutHeight!=='9396'||identity.hasSignedTip!==false)throw new Error('Original signed bounds/no-tip evidence mismatch');
 return{hash:identity.transactionHash,timeoutHeight:identity.timeoutHeight,hasSignedTip:identity.hasSignedTip};
});
if(digest(fs.readFileSync(file))!==originalHash)throw new Error('Original evidence changed during inspection');
const result={originalReportSha256:originalHash,originals,readOnly:true,nativeSubmissions:0};
fs.writeFileSync(path.join(root,'TestResults/HyperDriveCosmos/reviewed-original-bounds-current.json'),JSON.stringify(result,null,2));
console.log(JSON.stringify(result));
