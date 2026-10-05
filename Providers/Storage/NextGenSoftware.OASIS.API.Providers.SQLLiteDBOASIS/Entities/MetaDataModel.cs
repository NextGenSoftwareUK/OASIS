using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("MetaData")]
    public class MetaDataModel
    {
        [Required, Key]
        public string Property { get; set; }
        public object Value { get; set; }
        public string OwnerId{ set; get; }

        public MetaDataModel(){}
        public MetaDataModel(string Id, object value){
            this.Property = Id;
            this.Value = value;
        }
    }

}