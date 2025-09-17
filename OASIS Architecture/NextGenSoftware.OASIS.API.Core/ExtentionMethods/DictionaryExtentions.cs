
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.ExtentionMethods
{
    public static class DictionaryExtentions
    {
        static HolonManager _holonManager = new HolonManager(ProviderManager.Instance.CurrentStorageProvider);

        public static void AddFormat<TKey>(this Dictionary<TKey, string> dictionary, TKey key, string formatString, params object[] argList)
        {
            dictionary.Add(key, string.Format(formatString, argList));
        }

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
        {
            TValue ret;

            if (!dictionary.TryGetValue(key, out ret))
            {
                ret = new TValue();
                dictionary[key] = ret;
            }

            return ret;
        }

        public static TValue Load<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
        {
            TValue ret;
            IHolon holon = null;

            if (!dictionary.TryGetValue(key, out ret))
            {
                ret = new TValue();
                dictionary[key] = ret;

                holon = ret as IHolon;

                if (holon != null)
                {
                    OASISResult<IHolon> holonResult = _holonManager.LoadHolon(holon.Id);

                    if (!holonResult.IsError && holonResult.Result != null)
                        dictionary[key] = (TValue)holon;
                    
                }
            }

            //return ret;
            return (TValue)holon; 
        }
    }
}
