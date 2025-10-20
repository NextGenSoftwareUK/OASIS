using System;
using System.ComponentModel;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public abstract class AuditBase : IAuditBase, INotifyPropertyChanged
    {
        private IAvatar _createdByAvatar = null;
        private IAvatar _modifiedByAvatar = null;
        private IAvatar _deletedByAvatar = null;

        public Guid CreatedByAvatarId { get; set; }

        public IAvatar CreatedByAvatar
        {
            get
            {
                if (_createdByAvatar == null && CreatedByAvatarId != Guid.Empty)
                {
                    OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(CreatedByAvatarId);

                    if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                        _createdByAvatar = avatarResult.Result;
                }

                return _createdByAvatar;
            }
            set
            {
                if (value != _createdByAvatar)
                {
                    IsChanged = true;
                    NotifyPropertyChanged("CreatedByAvatar");
                }

                _createdByAvatar = value;
            }
        }

        public DateTime CreatedDate { get; set; }
        public Guid ModifiedByAvatarId { get; set; }

        public IAvatar ModifiedByAvatar
        {
            get
            {
                if (_modifiedByAvatar == null && ModifiedByAvatarId != Guid.Empty)
                {
                    OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(ModifiedByAvatarId);

                    if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                        _modifiedByAvatar = avatarResult.Result;
                }

                return _modifiedByAvatar;
            }
            set
            {
                if (value != _modifiedByAvatar)
                {
                    IsChanged = true;
                    NotifyPropertyChanged("ModifiedByAvatar");
                }

                _modifiedByAvatar = value;
            }
        }

        public DateTime ModifiedDate { get; set; }
        public Guid DeletedByAvatarId { get; set; }

        public IAvatar DeletedByAvatar
        {
            get
            {
                if (_deletedByAvatar == null && DeletedByAvatarId != Guid.Empty)
                {
                    OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(DeletedByAvatarId);

                    if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                        _deletedByAvatar = avatarResult.Result;
                }

                return _deletedByAvatar;
            }
            set
            {
                if (value != _deletedByAvatar)
                {
                    IsChanged = true;
                    NotifyPropertyChanged("DeletedByAvatar");
                }

                _deletedByAvatar = value;
            }
        }

        public DateTime DeletedDate { get; set; }
        public int Version { get; set; }
        public Guid VersionId { get; set; }
        public bool IsChanged { get; set; } = false;

        /// <summary>
        /// Triggers the property changed event for a specific property.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed.</param>
        public void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Fired when a property in this class changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}