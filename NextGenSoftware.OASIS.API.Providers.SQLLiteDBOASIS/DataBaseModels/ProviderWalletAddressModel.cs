using System;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.DataBaseModels{

    [Table("ProviderWalletAddress")]
    public class ProviderWalletAddressModel : ProviderKeyAbstract
    {
        public ProviderWalletAddressModel():base(){}
        public ProviderWalletAddressModel(ProviderType Id, String value) : base(Id, value){}

        public override ProviderKeyAbstract GetProviderKey()
        {
            ProviderKeyAbstract item=new ProviderPrivateKeyModel();

            item.KeyId=this.KeyId;
            item.Value=this.Value;

            return(item);
        }
    }

}