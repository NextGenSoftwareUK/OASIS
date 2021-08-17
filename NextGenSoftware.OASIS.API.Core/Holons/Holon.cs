using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class Holon : IHolon, INotifyPropertyChanged
    {
        private string _name;
        private string _description;

        public IHolon Original { get; set; }

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

        //TODO: Finish converting all properties so are same as above...

        public bool IsNewHolon { get; set; }
        public bool IsChanged { get; set; }
        public Dictionary<ProviderType, string> ProviderKey { get; set; } = new Dictionary<ProviderType, string>(); //Unique key used by each provider (e.g. hashaddress in hc, accountname for Telos, id in MongoDB etc).        
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; } = new Dictionary<ProviderType, Dictionary<string, string>>(); // Key/Value pair meta data can be stored here, which is unique for that provider.
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>(); // Key/Value pair meta data can be stored here that applies globally across ALL providers.
        public HolonType HolonType { get; set; }
        public Guid ParentOmiverseId { get; set; } //The Omiverse this Holon belongs to.
        public IOmiverse ParentOmiverse { get; set; } //The Omiverse this Holon belongs to.
        public Guid ParentMultiverseId { get; set; } //The Multiverse this Holon belongs to.
        public IMultiverse ParentMultiverse { get; set; } //The Multiverse this Holon belongs to.
        public Guid ParentUniverseId { get; set; } //The Universe this Holon belongs to.
        public IUniverse ParentUniverse { get; set; } //The Universe this Holon belongs to.
        public Guid ParentDimensionId { get; set; } //The Dimension this Holon belongs to.
        public IDimension ParentDimension { get; set; } //The Dimension this Holon belongs to.
        public DimensionLevel DimensionLevel { get; set; } //The dimension this Holon belongs to (a holon can have a different version of itself in each dimension (asscended/evolved versions of itself).
        public SubDimensionLevel SubDimensionLevel { get; set; } //The sub-dimension/plane this Holon belongs to.
        public Guid ParentGalaxyClusterId { get; set; } //The GalaxyCluster this Holon belongs to.
        public IGalaxyCluster ParentGalaxyCluster { get; set; } //The GalaxyCluster this Holon belongs to.
        public Guid ParentGalaxyId { get; set; } //The Galaxy this Holon belongs to.
        public IGalaxy ParentGalaxy { get; set; } //The Galaxy this Holon belongs to.
        public Guid ParentSolarSystemId { get; set; } //The SolarSystem this Holon belongs to.
        public ISolarSystem ParentSolarSystem { get; set; } //The SolarSystem this Holon belongs to.
        public Guid ParentGreatGrandSuperStarId { get; set; } //The GreatGrandSuperStar this Holon belongs to.
        public IGreatGrandSuperStar ParentGreatGrandSuperStar { get; set; } //The GreatGrandSuperStar this Holon belongs to.
        public Guid ParentGrandSuperStarId { get; set; } //The GrandSuperStar this Holon belongs to.
        public IGrandSuperStar ParentGrandSuperStar { get; set; } //The GrandSuperStar this Holon belongs to.
        public Guid ParentSuperStarId { get; set; } //The SuperStar this Holon belongs to.
        public ISuperStar ParentSuperStar { get; set; } //The SuperStar this Holon belongs to.
        public Guid ParentStarId { get; set; } //The Star this Holon belongs to.
        //public ICelestialBody ParentStar { get; set; } //The Star this Holon belongs to.
        public IStar ParentStar { get; set; } //The Star this Holon belongs to.
        public Guid ParentPlanetId { get; set; } //The Planet this Holon belongs to.
        //public ICelestialBody ParentPlanet { get; set; } //The Planet this Holon belongs to.
        public IPlanet ParentPlanet { get; set; } //The Planet this Holon belongs to.
        public Guid ParentMoonId { get; set; } //The Moon this Holon belongs to.
        //public ICelestialBody ParentMoon { get; set; } //The Moon this Holon belongs to.
        public IMoon ParentMoon { get; set; } //The Moon this Holon belongs to.
        //public Guid ParentCelestialBodyId { get; set; } //The CelestialBody (Planet or Moon (OAPP)) this Holon belongs to.
        //public ICelestialBody ParentCelestialBody { get; set; } //The CelestialBody (Planet or Moon (OAPP)) this Holon belongs to.
        public Guid ParentZomeId { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OAPP's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        public IZome ParentZome { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OAPP's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        public Guid ParentHolonId { get; set; }
        public IHolon ParentHolon { get; set; }
        public IEnumerable<IHolon> Children { get; set; }
        public ObservableCollection<IHolon> ChildrenTest { get; set; }
        public Guid CreatedByAvatarId { get; set; }
        public Avatar CreatedByAvatar { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid ModifiedByAvatarId { get; set; }
        public Avatar ModifiedByAvatar { get; set; }
        public DateTime ModifiedDate { get; set; }
        public Guid DeletedByAvatarId { get; set; }
        public Avatar DeletedByAvatar { get; set; }
        public DateTime DeletedDate { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
        public EnumValue<ProviderType> CreatedProviderType { get; set; } // The primary provider that this holon was originally saved with (it can then be auto-replicated to other providers to give maximum redundancy/speed via auto-load balancing etc).
        //public List<INode> Nodes { get; set; } // List of nodes/fields (int, string, bool, etc) that belong to this Holon (STAR ODK auto-generates these when generating dynamic code from DNA Templates passed in).
        public ObservableCollection<INode> Nodes { get; set; }

        /// <summary>
        /// Fired when a property in this class changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public Holon()
        {
            //TODO: Need to check if these are fired when an item in the collection is changed (not just added/removed).
            if (ChildrenTest != null)
                ChildrenTest.CollectionChanged += Children_CollectionChanged;

            Nodes.CollectionChanged += Nodes_CollectionChanged;
        }

        private void Nodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsChanged = true;

            //TOOD: Not sure if we need this? Because ObservableCollection is supposed to raise PropertyChanged events itself.
            NotifyPropertyChanged("Nodes");
        }

        private void Children_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsChanged = true;

            //TOOD: Not sure if we need this? Because ObservableCollection is supposed to raise PropertyChanged events itself.
            NotifyPropertyChanged("Children");
        }

        /// <summary>
        /// Triggers the property changed event for a specific property.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed.</param>
        public void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual bool HasHolonChanged(bool checkChildren = true)
        {
            if (IsChanged)
                return true;

            if (Original != null)
            {
                if (Original.Id != Id)
                    return true;

                if (Original.Name != Name)
                    return true;

                if (Original.Description != Description)
                    return true;

                if (Original.CreatedByAvatar != CreatedByAvatar)
                    return true;

                if (Original.CreatedByAvatarId != CreatedByAvatarId)
                    return true;

                if (Original.CreatedDate != CreatedDate)
                    return true;

                if (Original.ModifiedByAvatar != ModifiedByAvatar)
                    return true;

                if (Original.ModifiedByAvatarId != ModifiedByAvatarId)
                    return true;

                if (Original.ModifiedDate != ModifiedDate)
                    return true;

                if (Original.CreatedProviderType != CreatedProviderType)
                    return true;

                if (Original.DeletedByAvatar != DeletedByAvatar)
                    return true;

                if (Original.DeletedByAvatarId != DeletedByAvatarId)
                    return true;

                if (Original.DeletedDate != DeletedDate)
                    return true;
   
                if (Original.HolonType != HolonType)
                    return true;

                if (Original.IsActive != IsActive)
                    return true;

                if (Original.Nodes.Count != Nodes.Count)
                    return true;

                foreach (INode node in Original.Nodes)
                {
                   // if (node)
                }

                if (Original.MetaData.Keys.Count != MetaData.Keys.Count)
                    return true;

                foreach (string key in Original.MetaData.Keys)
                {
                    if (Original.MetaData[key] != MetaData[key])
                        return true;
                }

                //Will recursively check all children.
                if (checkChildren)
                {
                    if (Original.Children.Count() != Children.Count())
                        return true;

                    List<IHolon> origChildren = Original.Children.ToList();
                    List<IHolon> children = Children.ToList();

                    for (int i = 0; i < children.Count; i++)
                    {
                        if (children[i].Id != origChildren[i].Id)
                            return true;

                        if (children[i].Name != origChildren[i].Name)
                            return true;

                        if (children[i].Description != origChildren[i].Description)
                            return true;

                        //TODO: Add rest of properties here.

                        if (children[i].HasHolonChanged())
                            return true;
                    }
                }
                
            }
            //TODO: Finish this ASAP!

            return Id == Guid.Empty;
        }


        /*
        //https://stackoverflow.com/questions/2363801/what-would-be-the-best-way-to-implement-change-tracking-on-an-object
        //https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.ichangetracking?redirectedfrom=MSDN&view=net-5.0

        protected bool SetProperty<T>(string name, ref T oldValue, T newValue) where T : IComparable<T>
        {
            if (oldValue == null || oldValue.CompareTo(newValue) != 0)
            {
                oldValue = newValue;
               // PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
               // isDirty = true;
                return true;
            }
            return false;
        }
        // For nullable types
        protected void SetProperty<T>(string name, ref Nullable<T> oldValue, Nullable<T> newValue) where T : struct, IComparable<T>
        {
            if (oldValue.HasValue != newValue.HasValue || (newValue.HasValue && oldValue.Value.CompareTo(newValue.Value) != 0))
            {
                oldValue = newValue;
                //PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
            }
        }*/
    }
}
