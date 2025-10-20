using System;
using System.ComponentModel;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

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
    }
}
