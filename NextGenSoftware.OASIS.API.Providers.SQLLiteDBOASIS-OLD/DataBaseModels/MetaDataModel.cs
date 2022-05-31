using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels{

    [Table("MetaData")]
    public class MetaDataModel
    {
        [Required, Key]
        public string Property { get; set; }
        public string Value { get; set; }
        public string OwnerId{ set; get; }

        public MetaDataModel(){}
        public MetaDataModel(string Id, String value){
            this.Property = Id;
            this.Value = value;
        }
    }

}