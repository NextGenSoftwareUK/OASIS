using System.Collections.Generic;

namespace EOSNewYork.EOSCore.Serialization
{
    public class InternalServiceError
    {
        public uint code { get; set; }
        public string message { get; set; }
        public Error error { get; set; }
    }
    public class Error
    {
        public uint code { get; set; }
        public string name { get; set; }
        public string what { get; set; }
        public List<ErrorDetail> details { get; set; }
    }
    public class ErrorDetail
    {
        public string message { get; set; }
        public string file { get; set; }
        public uint line_num { get; set; }
        public string method { get; set; }
    }
}