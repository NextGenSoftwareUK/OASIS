using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels{

    public abstract class ProviderKeyAbstract
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ProviderType KeyId{ set; get; }

        public string Value{ set; get; }
        public string ParentId{ set; get; }

        protected ProviderKeyAbstract(){}
        protected ProviderKeyAbstract(ProviderType Id, String value){

            this.KeyId=Id;
            this.Value=value;
        }

        public abstract ProviderKeyAbstract GetProviderKey();
    }

}