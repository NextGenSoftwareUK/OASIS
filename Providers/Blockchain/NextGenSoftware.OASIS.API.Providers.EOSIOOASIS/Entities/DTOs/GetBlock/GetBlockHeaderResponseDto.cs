using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetBlock
{
    public class GetBlockHeaderResponseDto
    {
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("producer")]
        public string Producer { get; set; }

        [JsonProperty("confirmed")]
        public int Confirmed { get; set; }

        [JsonProperty("previous")]
        public string Previous { get; set; }

        [JsonProperty("transaction_mroot")]
        public string TransactionMroot { get; set; }

        [JsonProperty("action_mroot")]
        public string ActionMroot { get; set; }

        [JsonProperty("schedule_version")]
        public int ScheduleVersion { get; set; }

        [JsonProperty("new_producers")]
        public GetBlockNewProducersResponseDto NewProducers { get; set; }

        [JsonProperty("header_extensions")]
        public List<int> HeaderExtensions { get; set; }

        [JsonProperty("new_protocol_features")]
        public List<GetBlockNewProtocolFeatureResponseDto> NewProtocolFeatures { get; set; }

        [JsonProperty("producer_signature")]
        public string ProducerSignature { get; set; }

        [JsonProperty("transactions")]
        public List<GetBlockTransactionResponseDto> Transactions { get; set; }

        [JsonProperty("block_extensions")]
        public List<int> BlockExtensions { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("block_num")]
        public int BlockNum { get; set; }

        [JsonProperty("ref_block_prefix")]
        public int RefBlockPrefix { get; set; }
    }
}