using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Neo4jOgm.Attribute;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels{

    [NeoNodeEntity("ProviderPublicKey", "ProviderPublicKey")]

    
    public class ProviderPublicKeyModel : ProviderKeyAbstract
    {
        [NeoNodeId]
        public long? Id { get; set; }

        public ProviderPublicKeyModel():base(){}
        public ProviderPublicKeyModel(KeyValuePair<ProviderType,string> key) : base(key){}

        public override ProviderKeyAbstract GetProviderKey()
        {
            ProviderKeyAbstract item=new ProviderPrivateKeyModel();

            item.KeyId=this.KeyId;
            item.Value=this.Value;

            return(item);
        }
    }

}