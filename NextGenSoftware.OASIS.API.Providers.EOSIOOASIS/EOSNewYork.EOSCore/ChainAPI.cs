using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cryptography.ECDSA;
using EOSNewYork.EOSCore.Params;
using EOSNewYork.EOSCore.Response.API;
using EOSNewYork.EOSCore.Serialization;
using EOSNewYork.EOSCore.Utilities;
using Action = EOSNewYork.EOSCore.Params.Action;
using EOSNewYork.EOSCore.Lib;
using NLog;

namespace EOSNewYork.EOSCore
{
    public class ChainAPI : BaseAPI
    {
        static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private int delaySec = 30;
        public ChainAPI(){}

        public ChainAPI(string host) : base(host) {}

        public ChainAPI(string host, int delaySec) : base(host)
        {
            this.delaySec = delaySec;
        }

        public async Task<AbiBinToJson> GetAbiBinToJsonAsync(string code, string action, string binargs)
        {
            return await new EOS_Object<AbiBinToJson>(HOST).GetObjectsFromAPIAsync(new AbiBinToJsonParam { code = code, action = action, binargs = binargs });
        }
        public AbiBinToJson GetAbiBinToJson(string code, string action, string binargs)
        {
            return GetAbiBinToJsonAsync(code, action, binargs).Result;
        }        
        public async Task<AbiJsonToBin> GetAbiJsonToBinAsync(string code, string action, object args)
        {
            return await new EOS_Object<AbiJsonToBin>(HOST).GetObjectsFromAPIAsync(new AbiJsonToBinParam { code = code, action = action, args = args });
        }
        public AbiJsonToBin GetAbiJsonToBin(string code, string action, object args)
        {
            return GetAbiJsonToBinAsync(code, action, args).Result;
        }        
        public async Task<Account> GetAccountAsync(string accountName)
        {
            return await new EOS_Object<Account>(HOST).GetObjectsFromAPIAsync(new AccountParam { account_name = accountName });
        }
        public Account GetAccount(string accountName)
        {
            return GetAccountAsync(accountName).Result;
        }        
        public async Task<Block> GetBlockAsync(string blockNumOrId)
        {
            return await new EOS_Object<Block>(HOST).GetObjectsFromAPIAsync(new BlockParam { block_num_or_id = blockNumOrId });
        }
        public Block GetBlock(string blockNumOrId)
        {
            return GetBlockAsync(blockNumOrId).Result;
        }
        public async Task<Abi> GetAbiAsync(string accountName)
        {
            return await new EOS_Object<Abi>(HOST).GetObjectsFromAPIAsync(new AccountParam { account_name = accountName });
        }
        public Abi GetAbi(string accountName)
        {
            return GetAbiAsync(accountName).Result;
        }
        public async Task<Code> GetCodeAsync(string accountName, bool codeAsWasm)
        {
            return await new EOS_Object<Code>(HOST).GetObjectsFromAPIAsync(new CodeParam { account_name = accountName, code_as_wasm = codeAsWasm });
        }
        public Code GetCode(string accountName, bool codeAsWasm)
        {
            return GetCodeAsync(accountName, codeAsWasm).Result;
        }
        public async Task<RawCodeAndAbi> GetRawCodeAndAbiAsync(string accountName)
        {
            return await new EOS_Object<RawCodeAndAbi>(HOST).GetObjectsFromAPIAsync(new AccountParam { account_name = accountName });
        }
        public RawCodeAndAbi GetRawCodeAndAbi(string accountName)
        {
            return GetRawCodeAndAbiAsync(accountName).Result;
        }
        public async Task<CurrencyBalance> GetCurrencyBalanceAsync(string account, string code, string symbol)
        {
            return await new EOS_StringArray<CurrencyBalance>(HOST).GetObjectsFromAPIAsync(new CurrencyBalanceParam { account = account, code = code, symbol = symbol });
        }
        public CurrencyBalance GetCurrencyBalance(string account, string code, string symbol)
        {
            return GetCurrencyBalanceAsync(account, code, symbol).Result;
        }
        public async Task<TableRows> GetTableRowsAsync(string scope, string code, string table, string json, int lowerBound, int upperBound, int limit)
        {
            return await new EOS_Object<TableRows>(HOST).GetObjectsFromAPIAsync(new TableRowsParam { scope = scope, code = code, table = table, json = json, lower_bound = lowerBound, upper_bound = upperBound, limit = limit });
        }
        public TableRows GetTableRows(string scope, string code, string table, string json, int lowerBound, int upperBound, int limit)
        {
            return GetTableRowsAsync(scope, code, table, json, lowerBound, upperBound, limit).Result;
        }
        public async Task<Info> GetInfoAsync()
        {
            return await new EOS_Object<Info>(HOST).GetObjectsFromAPIAsync();
        }
        public Info GetInfo()
        {
            return GetInfoAsync().Result;
        }
        public async Task<ProducerSchedule> GetProducerScheduleAsync()
        {
            return await new EOS_Object<ProducerSchedule>(HOST).GetObjectsFromAPIAsync();
        }
        public ProducerSchedule GetProducerSchedule()
        {
            return GetProducerScheduleAsync().Result;
        }
        public async Task<PushTransaction> PushTransactionAsync(Action[] actions, List<string> privateKeysInWIF)
        {
            logger.Debug("GetInfoAsync");           
            //get info
            var info = await GetInfoAsync();

            logger.Debug("GetBlockAsync");
            //get head block
            var block = await GetBlockAsync(info.head_block_id);

            //prepare transaction object
            var transaction = new EOSNewYork.EOSCore.Params.Transaction {
                actions = actions,
                ref_block_num = (ushort)(block.block_num & 0xffff),
                ref_block_prefix = block.ref_block_prefix,
                expiration = new TimePointSec(block.timestamp_datetime.AddSeconds(delaySec))
            };
            
            //pack the transaction
            var packedTransaction = new PackingSerializer().Serialize<EOSNewYork.EOSCore.Params.Transaction>(transaction);
            
            //get chain id
            var chainId = Hex.HexToBytes(info.chain_id);
            
            //combine chainId, packed transaction and 32 empty bytes
            var message = new byte[chainId.Length + packedTransaction.Length + 32];
            Array.Copy(chainId, message, chainId.Length);
            Array.Copy(packedTransaction, 0, message, chainId.Length, packedTransaction.Length);
            
            //calculate message hash
            var messageHash = Sha256Manager.GetHash(message);

            //get private keys in WIF format
            List<byte[]> privateKeys = new List<byte[]>();
            for(int i = 0; i < privateKeysInWIF.Count; i++)
            {
                privateKeys.Add(WifUtility.DecodePrivateWif(privateKeysInWIF[i]));
            }
            
            //get signatures for each private key by signing message hash with private key
            string[] signatures = new string[privateKeys.Count];
            for(int i = 0; i< privateKeys.Count; i++)
            {
                signatures[i] = WifUtility.EncodeSignature(Secp256K1Manager.SignCompressedCompact(messageHash, privateKeys[i]));
            }

            logger.Debug("push transaction - GetObjectsFromAPIAsync");
            //push transaction
            return await new EOS_Object<PushTransaction>(HOST).GetObjectsFromAPIAsync(new PushTransactionParam { packed_trx = Hex.ToString(packedTransaction), signatures = signatures, packed_context_free_data = string.Empty, compression = "none" });
        }

        public PushTransaction PushTransaction(Action[] actions, List<string> privateKeysInWIF)
        {
            return PushTransactionAsync(actions, privateKeysInWIF).Result;
        }
    }
}
