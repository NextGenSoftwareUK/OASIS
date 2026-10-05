
using NextGenSoftware.OASIS.API.Core.Enums;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IDownloadedNFT : IDownloadedSTARNETHolon
    {
        public NFTType NFTType { get; set; }
        public Guid OASISNFTId { get; set; }
    }
}