using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IInstalledGeoNFT : IInstalledSTARNETHolon
    {
        //public IOASISGeoSpatialNFT GeoNFT { get; set; } //TODO: Not sure if we need this?
        public NFTType NFTType { get; set; }
        public Guid GeoNFTId { get; set; }
    }
}