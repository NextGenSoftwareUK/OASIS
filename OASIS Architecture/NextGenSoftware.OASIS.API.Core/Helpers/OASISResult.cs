//using System;
//using System.Collections.Generic;
//using System.Text.Json.Serialization;

//namespace NextGenSoftware.OASIS.API.Core.Helpers
//{
//    public class OASISResult<T>
//    // where T : IHolon
//    {
//        private bool _isError = false;
//        private bool _isWarning = false;
//        private string _message = "";

//        //public List<OASISResult<T2>> InnerResults { get; set; } = new List<OASISResult<T2>>();

//        //TODO: Finish implementing later... :)
//        public int ResultsCount { get; set; }
//        //public int ResultsCount
//        //{
//        //    get
//        //    {
//        //        if (typeof(T) == typeof(IEnumerable<T>))
//        //        {
//        //            return T.Count;
//        //        }

//        //        IEnumerable<T> list = T as IEnumerable<T>;


//        //    }
//        //}

//        public int ErrorCount { get; set; }
//        public int WarningCount { get; set; }
//        public int SavedCount { get; set; }
//        public int LoadedCount { get; set; }
//        public bool HasAnyHolonsChanged { get; set; }

//        public List<string> InnerMessages = new List<string>();
//        public List<string> StackTraces = new List<string>();

//        [JsonIgnore]
//        public Exception Exception { get; set; }

//        public Dictionary<string, string> MetaData = new Dictionary<string, string>();

//        public bool IsError
//        {
//            get
//            {
//                return _isError;
//            }

//            set
//            {
//                _isError = value;

//                //if (value && ErrorCount == 0)
//                //    ErrorCount = 1;

//                //if (ErrorHandling.ThrowExceptionsOnErrors && !string.IsNullOrEmpty(Message))
//                //    throw new Exception(Message);
//            }
//        }

//        public bool IsWarning
//        {
//            get
//            {
//                return _isWarning;
//            }

//            set
//            {
//                _isWarning = value;

//                //if (ErrorHandling.ThrowExceptionsOnWarnings && !string.IsNullOrEmpty(Message))
//                //    throw new Exception(Message);
//            }
//        }

//        public bool IsSaved { get; set; }
//        public bool IsLoaded { get; set; }

//        public string Message
//        {
//            get
//            {
//                return _message;
//            }
//            set
//            {
//                _message = value;

//                //if (ErrorHandling.ThrowExceptionsOnErrors && IsError)
//                //    throw new Exception(Message);
//            }
//        }

//        public string DetailedMessage { get; set; }

//        public T Result { get; set; }

//        public OASISResult()
//        {

//        }

//        public OASISResult(T value)
//        {
//            Result = value;
//        }
//    }
//}