using System.Net;
using System.Net.Http;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
{
    public class OASISHttpResponseMessage<T> : HttpResponseMessage
    {
        private ProviderRoutingPolicy _routingPolicy;
        internal OASISHttpResponseMessage<T> BindRoutingPolicy(ProviderRoutingPolicy policy)
        {
            _routingPolicy = policy ?? throw new System.ArgumentNullException(nameof(policy));
            return this;
        }
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
                return _routingPolicy?.AutoLoadBalancingEnabled ?? ProviderManager.Instance.IsAutoLoadBalanceEnabled;

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
                return _routingPolicy?.AutoFailoverEnabled ?? ProviderManager.Instance.IsAutoFailOverEnabled;

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
                return _routingPolicy?.AutoReplicationEnabled ?? ProviderManager.Instance.IsAutoReplicationEnabled;

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
                    return _routingPolicy == null ? ProviderManager.Instance.GetProviderAutoLoadBalanceListAsString() :
                        string.Join(",", _routingPolicy.LoadBalancingProviders.Select(value => value.ToString()));
                else
                    return null;
            }
        }

        public string AutoFailOverProviders
        {
            get
            {
                if (ShowDetailedSettings)
                    return _routingPolicy == null ? ProviderManager.Instance.GetProviderAutoFailOverListAsString() :
                        string.Join(",", _routingPolicy.FailoverProviders.Select(value => value.ToString()));
                else
                    return null;
            }
        }

        public string AutoReplicationProviders
        {
            get
            {
                if (ShowDetailedSettings)
                    return _routingPolicy == null ? ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString() :
                        string.Join(",", _routingPolicy.ReplicationProviders.Select(value => value.ToString()));
                else
                    return null;
            }
        }

        public string CurrentOASISProvider
        {
            get
            {
                return _routingPolicy?.PreferredProvider.ToString() ?? ProviderManager.Instance.CurrentStorageProviderType.Name;
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
