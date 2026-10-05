using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Models
{
    public class PublishRequest
    {
        public string SourcePath { get; set; } = "";
        public string LaunchTarget { get; set; } = "";
        public string PublishPath { get; set; } = "";
        public bool Edit { get; set; } = false;
        public bool RegisterOnSTARNET { get; set; } = true;
        public bool GenerateBinary { get; set; } = true;
        public bool UploadToCloud { get; set; } = false;
    }

    public class SearchRequest
    {
        public string SearchTerm { get; set; } = string.Empty;
        public Dictionary<string, object> Filters { get; set; } = new Dictionary<string, object>();
    }

}
