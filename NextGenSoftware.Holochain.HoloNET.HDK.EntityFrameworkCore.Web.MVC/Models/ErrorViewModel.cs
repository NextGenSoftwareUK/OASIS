using System;

namespace NextGenSoftware.Holochain.HoloNET.HDK.EntityFrameworkCore.Web.MVC.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
