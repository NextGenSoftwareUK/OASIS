using System;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class SaveFileRequest : OASISRequest
    {
        public byte[] Data { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public string MimeType { get; set; }
        public Guid? AvatarId { get; set; }
        public string Provider { get; set; }
    }
}
