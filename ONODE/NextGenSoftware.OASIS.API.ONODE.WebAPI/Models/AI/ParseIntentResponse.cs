using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.AI
{
    public class ParseIntentResponse
    {
        public string Intent { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }
}

