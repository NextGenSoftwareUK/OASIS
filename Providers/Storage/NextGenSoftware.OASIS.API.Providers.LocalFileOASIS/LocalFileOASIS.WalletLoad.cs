//using System.Text.Json;
//using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS
{
    public partial class LocalFileOASIS
    {

        //public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWallets()
        //{
        //    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

        //    try
        //    {
        //        string json = File.ReadAllText(_filePath);
        //        result.Result = JsonSerializer.Deserialize<Dictionary<ProviderType, List<IProviderWallet>>>(json);
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsAsync()
        //{
        //    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

        //    try
        //    {
        //        using FileStream openStream = File.OpenRead(_filePath);
        //        result.Result = await JsonSerializer.DeserializeAsync<Dictionary<ProviderType, List<IProviderWallet>>>(openStream);
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsAsync method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
        //    }

        //    return result;
        //}

        //public OASISResult<bool> SaveProviderWallets(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();

        //    try
        //    {
        //        string jsonString = JsonSerializer.Serialize(providerWallets);
        //        File.WriteAllText(_filePath, jsonString);
        //        result.Result = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in SaveProviderWalletsAsync method in LocalFileOASIS Provider saving wallets. Reason: {ex.Message}", ex);
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<bool>> SaveProviderWalletsAsync(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();

        //    try
        //    {
        //        using FileStream createStream = File.Create(_filePath);
        //        await JsonSerializer.SerializeAsync(createStream, providerWallets);
        //        await createStream.DisposeAsync();
        //        result.Result = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in SaveProviderWalletsAsync method in LocalFileOASIS Provider saving wallets. Reason: {ex.Message}", ex);
        //    }

        //    return result;
        //}

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = 
                new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>(new Dictionary<ProviderType, List<IProviderWallet>>());

            try
            {
                Dictionary<ProviderType, List<ProviderWallet>> wallets = new Dictionary<ProviderType, List<ProviderWallet>>();

                if (File.Exists(GetWalletFilePath(id)))
                {
                    string json = File.ReadAllText(GetWalletFilePath(id));
                    //wallets = JsonSerializer.Deserialize<Dictionary<ProviderType, List<ProviderWallet>>>(json);
                    wallets = JsonConvert.DeserializeObject<Dictionary<ProviderType, List<ProviderWallet>>>(json);

                    if (wallets != null)
                    {
                        foreach (ProviderType providerType in wallets.Keys)
                        {
                            foreach (ProviderWallet wallet in wallets[providerType])
                            {
                                if (!result.Result.ContainsKey(providerType))
                                    result.Result[providerType] = new List<IProviderWallet>();

                                result.Result[providerType].Add(wallet);
                            }
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: Error deserializing data.");
                }
                //else
                //    OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: Error wallets json file not found: {GetWalletFilePath(id)}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

        private string GetWalletFilePath(Guid id)
        {
            return string.Concat(_filePath, "wallets_", id.ToString(), ".json");
        }

        //public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        //{
        //    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = 
        //        new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>(new Dictionary<ProviderType, List<IProviderWallet>>());

        //    try
        //    {
        //        if (File.Exists(GetWalletFilePath(id)))
        //        {
        //            Dictionary<ProviderType, List<ProviderWallet>> wallets = new Dictionary<ProviderType, List<ProviderWallet>>();
        //            using FileStream openStream = File.OpenRead(GetWalletFilePath(id));
        //            //wallets = await JsonSerializer.DeserializeAsync<Dictionary<ProviderType, List<ProviderWallet>>>(openStream);
        //            wallets = await JsonConvert.DeserializeObject<Dictionary<ProviderType, List<ProviderWallet>>>(openStream);



        //            if (wallets != null)
        //            {
        //                foreach (ProviderType providerType in wallets.Keys)
        //                {
        //                    foreach (ProviderWallet wallet in wallets[providerType])
        //                    {
        //                        if (!result.Result.ContainsKey(providerType))
        //                            result.Result[providerType] = new List<IProviderWallet>();

        //                        result.Result[providerType].Add(wallet);
        //                    }
        //                }
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForAvatarByIdAsync method in LocalFileOASIS Provider loading wallets. Reason: Error deserializing data.");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: Error wallets json file not found: {GetWalletFilePath(id)}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsAsync method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
        //    }

        //    return result;
        //}

        /*
        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsername(string username)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            try
            {
                string json = File.ReadAllText(_filePath);
                result.Result = JsonSerializer.Deserialize<Dictionary<ProviderType, List<IProviderWallet>>>(json);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameAsync(string username)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            try
            {
                using FileStream openStream = File.OpenRead(_filePath);
                result.Result = await JsonSerializer.DeserializeAsync<Dictionary<ProviderType, List<IProviderWallet>>>(openStream);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsAsync method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByEmail(string email)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            try
            {
                string json = File.ReadAllText(_filePath);
                result.Result = JsonSerializer.Deserialize<Dictionary<ProviderType, List<IProviderWallet>>>(json);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailAsync(string email)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            try
            {
                using FileStream openStream = File.OpenRead(_filePath);
                result.Result = await JsonSerializer.DeserializeAsync<Dictionary<ProviderType, List<IProviderWallet>>>(openStream);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsAsync method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }*/
    }
}
