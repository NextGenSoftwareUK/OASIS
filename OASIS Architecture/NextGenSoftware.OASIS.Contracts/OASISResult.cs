using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.Common
{
    public class OASISResult<T>
    {
        private bool _isError;
        private bool _isWarning;
        private string _message = string.Empty;

        public int ResultsCount { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int SavedCount { get; set; }
        public int LoadedCount { get; set; }
        public int DeletedCount { get; set; }
        public bool HasAnyHolonsChanged { get; set; }
        public List<string> InnerMessages { get; set; } = new List<string>();
        public List<string> StackTraces { get; set; } = new List<string>();
        [JsonIgnore, System.Text.Json.Serialization.JsonIgnore] public Exception Exception { get; set; }
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public string ErrorCode { get; set; }
        public bool IsError { get => _isError; set => _isError = value; }
        public bool IsWarning { get => _isWarning; set => _isWarning = value; }
        public bool IsSaved { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsDeleted { get; set; }
        public string Message { get => _message; set => _message = value; }
        public string DetailedMessage { get; set; }
        public T Result { get; set; }

        public OASISResult() { }
        public OASISResult(T value) { Result = value; }
    }
}
