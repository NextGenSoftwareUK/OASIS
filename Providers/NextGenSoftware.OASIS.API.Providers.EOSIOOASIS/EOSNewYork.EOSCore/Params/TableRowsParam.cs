
namespace EOSNewYork.EOSCore.Params
{
    public class TableRowsParam
    {
        public string scope { get; set; }
        public string code { get; set; }
        public string table { get; set; }        
        public string json { get; set; }        
        public string lower_bound { get; set; }        
        public string upper_bound { get; set; }        
        public int limit { get; set; }        
        public int index_position { get; set; }
        public string key_type { get; set; }
    }

    public class TableRowsParamIntBounds
    {
        public string scope { get; set; }
        public string code { get; set; }
        public string table { get; set; }
        public string json { get; set; }
        public int lower_bound { get; set; }
        public int upper_bound { get; set; }
        public int limit { get; set; }
        public int index_position { get; set; }
        public string key_type { get; set; }
    }
}
