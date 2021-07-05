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

namespace EOSNewYork.EOSCore
{
    public class HistoryAPI : BaseAPI
    {
        public HistoryAPI(){}

        public HistoryAPI(string host) : base(host) {}
        
        public async Task<Actions> GetActionsAsync(int pos, int offset, string accountName)
        {
            return await new EOS_Object<Actions>(HOST).GetObjectsFromAPIAsync(new ActionsParam { pos = pos, offset = offset, account_name = accountName });
        }
        public Actions GetActions(int pos, int offset, string accountName)
        {
            return GetActionsAsync(pos, offset, accountName).Result;
        }
        public async Task<TransactionResult> GetTransactionAsync(string id, uint? blockNumHint)
        {
            return await new EOS_Object<TransactionResult>(HOST).GetObjectsFromAPIAsync(new TransactionResultParam { id = id, block_num_hint = blockNumHint });
        }
        public TransactionResult GetTransaction(string id, uint? blockNumHint)
        {
            return GetTransactionAsync(id, blockNumHint).Result;
        }
    }
}
