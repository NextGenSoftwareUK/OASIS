using System;
using System.Collections.Generic;
using System.ComponentModel;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public abstract class HolonBase : AuditBase, IHolonBase, INotifyPropertyChanged
    {
        private string _name;
        private string _description;

        public HolonBase(Guid id)
        {
            Id = id;
        }

        //public HolonBase(string providerKey, ProviderType providerType)
        //{
        //    if (providerType == ProviderType.Default)
        //        providerType = ProviderManager.Instance.CurrentStorageProviderType.Value;

        //    this.ProviderUniqueStorageKey[providerType] = providerKey;
        //}


        public HolonBase(HolonType holonType)
        {
            HolonType = holonType;
        }

        public HolonBase()
        {

        }

        public Guid Id { get; set; } //Unique id within the OASIS.
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (value != _name)
                {
                    IsChanged = true;
                    NotifyPropertyChanged("Name");
                }

                _name = value;
            }
        }
        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
                if (value != _description)
                {
                    IsChanged = true;
                    NotifyPropertyChanged("Description");
                }

                _description = value;
            }
        }

        public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>(); // Key/Value pair meta data can be stored here that applies globally across ALL providers.
        
        public HolonType HolonType { get; set; }
        public bool IsActive { get; set; }




        //TODO: TEMP MOVED FROM HOLON TILL REFACTOR CODEBASE.
        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>(); //Unique key used by each provider (e.g. hashaddress in hc, accountname for Telos, id in MongoDB etc).        
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; } = new Dictionary<ProviderType, Dictionary<string, string>>(); // Key/Value pair meta data can be stored here, which is unique for that provider.
        public string CustomKey { get; set; } //A custom key that can be used to load the holon by (other than Id or ProviderKey).
        public bool IsNewHolon { get; set; } //TODO: Want to remove this ASAP!
        public bool IsSaving { get; set; }

        public Guid PreviousVersionId { get; set; }
        public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>();

        public EnumValue<ProviderType> CreatedProviderType { get; set; } // The primary provider that this holon was originally saved with (it can then be auto-replicated to other providers to give maximum redundancy/speed via auto-load balancing etc).
        public EnumValue<ProviderType> InstanceSavedOnProviderType { get; set; }
        public EnumValue<OASISType> CreatedOASISType { get; set; }

        public IHolon Original { get; set; }
    }
}
