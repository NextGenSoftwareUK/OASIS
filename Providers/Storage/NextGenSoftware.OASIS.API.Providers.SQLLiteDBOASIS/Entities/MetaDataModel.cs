using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("MetaData")]
    public class MetaDataModel
    {
        [Required, Key]
        public string Property { get; set; }
        // SQLite persists metadata as text; callers already consume provider metadata
        // through its string representation. Keeping this as object makes EF Core reject
        // the entire provider model before the first operation.
        public string Value { get; set; }
        public string OwnerId{ set; get; }

        public MetaDataModel(){}
        public MetaDataModel(string Id, object value){
            this.Property = Id;
            this.Value = value?.ToString();
        }
    }

}
