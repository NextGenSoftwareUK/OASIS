using System.Linq;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetInfo
{
    public class GetNodeInfoResponseDto
    {
        [JsonProperty("server_version")] public string ServerVersion { get; set; }

        [JsonProperty("chain_id")] public string ChainId { get; set; }

        [JsonProperty("head_block_num")] public int HeadBlockNum { get; set; }

        [JsonProperty("head_block_id")] public string HeadBlockId { get; set; }

        [JsonProperty("head_block_time")] public string HeadBlockTime { get; set; }

        [JsonProperty("head_block_producer")] public string HeadBlockProducer { get; set; }

        [JsonProperty("last_irreversible_block_num")]
        public int LastIrreversibleBlockNum { get; set; }

        [JsonProperty("last_irreversible_block_id")]
        public string LastIrreversibleBlockId { get; set; }

        [JsonProperty("virtual_block_cpu_limit")]
        public int VirtualBlockCpuLimit { get; set; }

        [JsonProperty("virtual_block_net_limit")]
        public int VirtualBlockNetLimit { get; set; }

        [JsonProperty("block_cpu_limit")] public int BlockCpuLimit { get; set; }

        [JsonProperty("block_net_limit")] public int BlockNetLimit { get; set; }

        [JsonProperty("server_version_string")]
        public string ServerVersionString { get; set; }

        [JsonProperty("fork_db_head_block_num")]
        public int ForkDbHeadBlockNum { get; set; }

        [JsonProperty("fork_db_head_block_id")]
        public string ForkDbHeadBlockId { get; set; }

        /// <summary>
        ///     Returns true is all properties filled correctly.
        /// </summary>
        /// <returns>True if properties filled correctly, otherwise false</returns>
        public bool IsNodeInfoCorrect()
        {
            return (from propertyInfo
                        in GetType().GetProperties()
                    where propertyInfo.PropertyType == typeof(string)
                    select (string) propertyInfo.GetValue(this))
                .All(propertyValue => !string.IsNullOrEmpty(propertyValue));
        }
    }
}