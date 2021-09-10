using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels{

    public abstract class ProviderKeyAbstract
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int KeyId{ set; get; }

        public string Value{ set; get; }
        public string AvatarId{ set; get; }

        protected ProviderKeyAbstract(){}
        protected ProviderKeyAbstract(KeyValuePair<ProviderType,string> key){

            this.KeyId=(int)key.Key;
            this.Value=key.Value;
        }

        public abstract ProviderKeyAbstract GetProviderKey();
    }

}