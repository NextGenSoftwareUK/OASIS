using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetBlock
{
    public class GetBlockHeaderStateResponseDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("block_num")]
        public int BlockNum { get; set; }

        [JsonProperty("header")]
        public GetBlockHeaderResponseDto Header { get; set; }

        [JsonProperty("dpos_proposed_irreversible_blocknum")]
        public string DposProposedIrreversibleBlocknum { get; set; }

        [JsonProperty("dpos_irreversible_blocknum")]
        public string DposIrreversibleBlocknum { get; set; }

        [JsonProperty("bft_irreversible_blocknum")]
        public string BftIrreversibleBlocknum { get; set; }

        [JsonProperty("pending_schedule_lib_num")]
        public string PendingScheduleLibNum { get; set; }

        [JsonProperty("pending_schedule_hash")]
        public string PendingScheduleHash { get; set; }

        [JsonProperty("pending_schedule")]
        public GetBlockPendingScheduleResponseDto PendingSchedule { get; set; }

        [JsonProperty("active_schedule")]
        public GetBlockActiveScheduleResponseDto ActiveSchedule { get; set; }

        [JsonProperty("blockroot_merkle")]
        public GetBlockBlockRootMerkleResponseDto BlockrootMerkle { get; set; }

        [JsonProperty("producer_to_last_produced")]
        public List<List<string>> ProducerToLastProduced { get; set; }

        [JsonProperty("producer_to_last_implied_irb")]
        public List<List<string>> ProducerToLastImpliedIrb { get; set; }

        [JsonProperty("block_signing_key")]
        public string BlockSigningKey { get; set; }

        [JsonProperty("confirm_count")]
        public List<string> ConfirmCount { get; set; }

        [JsonProperty("confirmations")]
        public List<object> Confirmations { get; set; }
    }
}