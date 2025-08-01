using System.Net;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
{
    public class OASISHttpResponseMessage<T> : HttpResponseMessage
    {
        public bool ShowDetailedSettings { get; set; }
        public OASISResult<T> Result { get; set; }
        public string OASISVersion
        {
            get
            {
                return OASISBootLoader.OASISBootLoader.OASISRuntimeVersion;
                //switch (OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.OASISVersion.ToUpper())
                //{
                //    case "LIVE":
                //        return string.Concat(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.CurrentLiveVersion, " ", OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.OASISVersion.ToUpper());

                //    case "STAGING":
                //        return string.Concat(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.CurrentStagingVersion, " ", OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.OASISVersion.ToUpper());

                //    default:
                //        return string.Concat(OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.CurrentLiveVersion, " ", OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.OASISVersion.ToUpper());
                //}
            }
        }

        public bool? AutoLoadBalanceEnabled
        {
            get
            {
                return ProviderManager.Instance.IsAutoLoadBalanceEnabled;

                //if (_autoLoadBalanceMode == AutoLoadBalanceMode.NotSet || _autoLoadBalanceMode == AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA)
                //    return OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoLoadBalanceEnabled;
                //else
                //    return _autoLoadBalanceMode == AutoLoadBalanceMode.True;
            }
        }

        public bool? AutoFailOverEnabled
        {
            get
            {
                return ProviderManager.Instance.IsAutoFailOverEnabled;

                //if (_autoFailOverMode == AutoFailOverMode.NotSet || _autoFailOverMode == AutoFailOverMode.UseGlobalDefaultInOASISDNA)
                //    return OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoFailOverEnabled;
                //else
                //    return _autoFailOverMode == AutoFailOverMode.True;
            }
        }

        public bool? AutoReplicationEnabled
        {
            get
            {
                return ProviderManager.Instance.IsAutoReplicationEnabled;

                //if (_autoReplicationMode == AutoReplicationMode.NotSet || _autoReplicationMode == AutoReplicationMode.UseGlobalDefaultInOASISDNA)
                //    return OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.AutoReplicationEnabled;
                //else
                //    return _autoReplicationMode == AutoReplicationMode.True;
            }
        }

        public string AutoLoadBalanceProviders
        {
            get
            {
                if (ShowDetailedSettings)
                    return ProviderManager.Instance.GetProviderAutoLoadBalanceListAsString();
                else
                    return null;
            }
        }

        public string AutoFailOverProviders
        {
            get
            {
                if (ShowDetailedSettings)
                    return ProviderManager.Instance.GetProviderAutoFailOverListAsString();
                else
                    return null;
            }
        }

        public string AutoReplicationProviders
        {
            get
            {
                if (ShowDetailedSettings)
                    return ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString();
                else
                    return null;
            }
        }

        public string CurrentOASISProvider
        {
            get
            {
                return ProviderManager.Instance.CurrentStorageProviderType.Name;
            }
        }

        public OASISHttpResponseMessage(OASISResult<T> result, bool showDetailedSettings = false) : base()
        {
            ShowDetailedSettings = showDetailedSettings;
            Result = result;
        }

        public OASISHttpResponseMessage(bool showDetailedSettings = false) : base()
        {
            ShowDetailedSettings = showDetailedSettings;
        }

        public OASISHttpResponseMessage(HttpStatusCode statusCode, bool showDetailedSettings = false) : base(statusCode)
        {
            ShowDetailedSettings = showDetailedSettings;
        }

        public OASISHttpResponseMessage(OASISResult<T> result, HttpStatusCode statusCode, bool showDetailedSettings = false) : base(statusCode)
        {
            ShowDetailedSettings = showDetailedSettings;
            Result = result;
        }
    }
}