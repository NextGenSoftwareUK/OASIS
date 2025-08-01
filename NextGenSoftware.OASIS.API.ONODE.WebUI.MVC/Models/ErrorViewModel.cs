using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebUI.MVC.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
