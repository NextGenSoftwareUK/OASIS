using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels{

    [Table("MetaData")]
    public class MetaDataModel
    {
        [Required, Key]
        public string PropertyId{ set; get; }
        public string Value{ set; get; }
        public string ParentId{ set; get; }

        public MetaDataModel(){}
        public MetaDataModel(string Id, String value){

            this.PropertyId=Id;
            this.Value=value;
        }

        public MetaDataModel GetMetaData()
        {
            MetaDataModel item=new MetaDataModel();

            item.PropertyId=this.PropertyId;
            item.Value=this.Value;
            item.ParentId = this.ParentId;

            return(item);
        }
    }

}