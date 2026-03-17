using System;
using System.Text.Json;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    //public class STARNETHolon<T> : Holon, ISTARNETHolon where T : ISTARNETDNA //TODO: Implement this properly so that it can be used with any type of STARNETDNA.
    public class STARNETHolon : Holon, ISTARNETHolon
    {
        private string _STARNETDNAJSONName = "STARNETDNAJSON";
        private ISTARNETDNA _STARNETDNA;

        public STARNETHolon()
        {
            this.HolonType = HolonType.STARNETHolon;
        }

        public STARNETHolon(HolonType holonType)
        {
            this.HolonType = holonType;
        }

        public STARNETHolon(string STARNETDNAJSONName = "STARNETDNAJSON")
        {
            this.HolonType = HolonType.STARNETHolon;
            _STARNETDNAJSONName = STARNETDNAJSONName;
        }

        public string STARNETHolonDNAJSONName
        {
            get
            {
                return _STARNETDNAJSONName;
            }
            set
            {
                _STARNETDNAJSONName = value;
            }
        }

        public virtual ISTARNETDNA STARNETDNA
        {
            get
            {
                if (_STARNETDNA == null && MetaData != null && !string.IsNullOrEmpty(_STARNETDNAJSONName))
                {
                    if (MetaData.TryGetValue(_STARNETDNAJSONName, out var dnaJsonObj) && dnaJsonObj != null)
                    {
                        string jsonString = null;
                        if (dnaJsonObj is string s && !string.IsNullOrWhiteSpace(s))
                            jsonString = s;
                        else if (dnaJsonObj is JToken jToken)
                            jsonString = jToken.ToString();
                        else
                            jsonString = dnaJsonObj.ToString();

                        if (!string.IsNullOrWhiteSpace(jsonString))
                        {
                            try
                            {
                                _STARNETDNA = JsonConvert.DeserializeObject<STARNETDNA>(jsonString);
                            }
                            catch (Newtonsoft.Json.JsonException)
                            {
                                _STARNETDNA = null;
                            }
                        }
                    }
                }

                if (_STARNETDNA == null)
                {
                    _STARNETDNA = new STARNETDNA
                    {
                        Version = "1.0.0"
                    };
                    if (Id != Guid.Empty)
                        _STARNETDNA.Id = Id;
                    if (!string.IsNullOrEmpty(Name))
                        _STARNETDNA.Name = Name;
                    if (!string.IsNullOrEmpty(Description))
                        _STARNETDNA.Description = Description;
                    if (CreatedByAvatarId != Guid.Empty)
                        _STARNETDNA.CreatedByAvatarId = CreatedByAvatarId;
                    if (ModifiedByAvatarId != Guid.Empty)
                        _STARNETDNA.ModifiedByAvatarId = ModifiedByAvatarId;
                    _STARNETDNA.CreatedOn = CreatedDate;
                    _STARNETDNA.ModifiedOn = ModifiedDate;
                    MetaData ??= new Dictionary<string, object>();
                    MetaData[_STARNETDNAJSONName] = JsonConvert.SerializeObject(_STARNETDNA);
                }

                return _STARNETDNA;
            }
            set
            {
                _STARNETDNA = value;
                MetaData ??= new Dictionary<string, object>();
                //MetaData[_STARNETDNAJSONName] = JsonSerializer.Serialize(value);
                MetaData[_STARNETDNAJSONName] = JsonConvert.SerializeObject(value);
            }
        }

        [CustomOASISProperty()]
        public virtual byte[] PublishedSTARNETHolon { get; set; }


        //[CustomOASISProperty]
        //public IList<ISTARNETDependency> RuntimesMetaData { get; set; } = new List<ISTARNETDependency>();

        //[CustomOASISProperty]
        //public IList<ISTARNETDependency> LibrariesMetaData { get; set; } = new List<ISTARNETDependency>();

        //[CustomOASISProperty]
        //public IList<ISTARNETDependency> OAPPTemplatesMetaData { get; set; } = new List<ISTARNETDependency>();


        //public Guid Id { get; set; }
        //public bool IsPublic { get; set; } //TODO: Need to implement ASAP! :)
        //public string Name { get; set; }
        //public string Description { get; set; }
        //public object STARNETHolonType { get; set; }
        //public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>();
        //public Guid CreatedByAvatarId { get; set; }
        //public string CreatedByAvatarUsername { get; set; }
        //public DateTime CreatedOn { get; set; }
        //public string SourcePath { get; set; }
        //public string PublishedPath { get; set; }
        //public Guid PublishedByAvatarId { get; set; }
        //public string PublishedByAvatarUsername { get; set; }
        //public DateTime PublishedOn { get; set; }
        //public string LaunchTarget { get; set; }
        //public Guid ModifiedByAvatarId { get; set; }
        //public string ModifiedByAvatarUsername { get; set; }
        //public DateTime ModifiedOn { get; set; }
        //public bool PublishedOnSTARNET { get; set; }
        //public bool PublishedToCloud { get; set; }
        //public bool PublishedToPinata { get; set; }
        //public string PinataIPFSHash { get; set; }
        //public ProviderType PublishedProviderType { get; set; }
        //public long FileSize { get; set; }
        //public string Version { get; set; }
        //public string OASISRuntimeVersion { get; set; }
        //public string OASISAPIVersion { get; set; }
        //public string COSMICVersion { get; set; }
        //public string STARRuntimeVersion { get; set; }
        //public string STARODKVersion { get; set; }
        //public string STARAPIVersion { get; set; }
        //public string STARNETVersion { get; set; }
        //public string DotNetVersion { get; set; }
        //public int VersionSequence { get; set; }
        //public int Downloads { get; set; }
        //public int Installs { get; set; }
        //public int TotalDownloads { get; set; }
        //public int TotalInstalls { get; set; }
        //public int NumberOfVersions { get; set; }
    }
}