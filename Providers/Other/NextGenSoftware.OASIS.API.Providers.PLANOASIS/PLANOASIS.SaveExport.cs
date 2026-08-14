using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    public partial class PLANOASIS
    {
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var playersUrl = $"{_apiBaseUrl}/players/nearby?lat={geoLat}&lng={geoLong}&radius={radiusInMeters}";
                var response = _httpClient.GetAsync(playersUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var playersData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                    var avatars = new List<IAvatar>();
                    if (playersData?.ContainsKey("players") == true && playersData["players"] is JsonElement playersArray)
                    {
                        foreach (var item in playersArray.EnumerateArray())
                        {
                            var avatar = new Avatar
                            {
                                Id = Guid.Parse(item.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                Username = item.GetProperty("username").GetString() ?? "",
                                Email = item.TryGetProperty("email", out var email) ? email.GetString() : ""
                            };
                            avatars.Add(avatar);
                        }
                    }

                    result.Result = avatars;
                    result.IsError = false;
                    result.Message = $"Found {avatars.Count} avatars nearby from PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN GetAvatarsNearMe failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsUrl = $"{_apiBaseUrl}/holons/nearby?type={Type}";
                var response = _httpClient.GetAsync(holonsUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var holonsData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                    var holons = new List<IHolon>();
                    if (holonsData?.ContainsKey("holons") == true && holonsData["holons"] is JsonElement holonsArray)
                    {
                        foreach (var item in holonsArray.EnumerateArray())
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(item.GetProperty("id").GetString() ?? Guid.Empty.ToString()),
                                Name = item.GetProperty("name").GetString() ?? "",
                                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                                HolonType = Type
                            };
                            holons.Add(holon);
                        }
                    }

                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Found {holons.Count} holons nearby from PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN GetHolonsNearMe failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from PLAN: {ex.Message}", ex);
            }
            return result;
        }


        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
        {
            try
            {
                if (string.IsNullOrEmpty(outputFolder))
                    return false;

                string planFolder = Path.Combine(outputFolder, "PLAN");
                if (!Directory.Exists(planFolder))
                    Directory.CreateDirectory(planFolder);

                if (!string.IsNullOrEmpty(nativeSource))
                {
                    File.WriteAllText(Path.Combine(planFolder, "plan.json"), nativeSource);
                    return true;
                }

                if (celestialBody == null)
                    return true;

                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"name\": \"{celestialBody.Name ?? "OAPP"}\",");
                sb.AppendLine($"  \"description\": \"{celestialBody.Description ?? ""}\",");
                sb.AppendLine("  \"holons\": [");

                var zomes = celestialBody.CelestialBodyCore?.Zomes;
                bool firstHolon = true;
                if (zomes != null)
                {
                    foreach (var zome in zomes)
                    {
                        if (zome?.Children == null) continue;

                        foreach (var holon in zome.Children)
                        {
                            if (holon == null || string.IsNullOrWhiteSpace(holon.Name)) continue;

                            if (!firstHolon) sb.AppendLine(",");
                            firstHolon = false;

                            sb.AppendLine("    {");
                            sb.AppendLine($"      \"id\": \"{holon.Id}\",");
                            sb.AppendLine($"      \"name\": \"{holon.Name}\",");
                            sb.AppendLine($"      \"description\": \"{holon.Description ?? ""}\"");
                            sb.AppendLine();
                            sb.Append("    }");
                        }
                    }
                }

                sb.AppendLine();
                sb.AppendLine("  ]");
                sb.AppendLine("}");

                File.WriteAllText(Path.Combine(planFolder, "plan.json"), sb.ToString());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public async Task<OASISResult<string>> SendTransactionAsync(IWalletTransactionRequest transaction)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var transactionUrl = $"{_apiBaseUrl}/transactions/send";
                var transactionData = new
                {
                    fromAddress = transaction.FromWalletAddress,
                    toAddress = transaction.ToWalletAddress,
                    amount = transaction.Amount,
                    token = "PLAN", // Default token for PLAN
                    memo = transaction.MemoText ?? ""
                };

                var content = new StringContent(JsonSerializer.Serialize(transactionData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(transactionUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                    
                    result.Result = txData?.ContainsKey("transactionHash") == true ? txData["transactionHash"].ToString() : "";
                    result.IsError = false;
                    result.Message = "Transaction sent successfully to PLAN network";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN transaction failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransaction(IWalletTransactionRequest transaction)
        {
            return SendTransactionAsync(transaction).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var fromAvatar = await LoadAvatarAsync(fromAvatarId);
                var toAvatar = await LoadAvatarAsync(toAvatarId);

                if (fromAvatar.IsError || fromAvatar.Result == null || toAvatar.IsError || toAvatar.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to load avatar(s) for transaction");
                    return result;
                }

                var walletResult = await WalletManager.Instance.LoadProviderWalletsForAvatarByIdAsync(fromAvatarId, providerTypeToLoadFrom: ProviderType.Value);
                if (walletResult.IsError || walletResult.Result == null || !walletResult.Result.ContainsKey(ProviderType.Value) || walletResult.Result[ProviderType.Value] == null || !walletResult.Result[ProviderType.Value].Any())
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet for sender avatar");
                    return result;
                }

                var fromWallet = walletResult.Result[ProviderType.Value].FirstOrDefault();
                var toWallet = toAvatar.Result.ProviderWallets?.ContainsKey(ProviderType.Value) == true && toAvatar.Result.ProviderWallets[ProviderType.Value]?.Any() == true 
                    ? toAvatar.Result.ProviderWallets[ProviderType.Value].FirstOrDefault() 
                    : null;

                var transactionRequest = new WalletTransactionRequest
                {
                    FromWalletAddress = fromWallet?.WalletAddress ?? "",
                    ToWalletAddress = toWallet?.WalletAddress ?? "",
                    Amount = amount
                };

                return await SendTransactionAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by ID to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var fromAvatar = await LoadAvatarAsync(fromAvatarId);
                var toAvatar = await LoadAvatarAsync(toAvatarId);

                if (fromAvatar.IsError || fromAvatar.Result == null || toAvatar.IsError || toAvatar.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to load avatar(s) for transaction");
                    return result;
                }

                var walletResult = await WalletManager.Instance.LoadProviderWalletsForAvatarByIdAsync(fromAvatarId, providerTypeToLoadFrom: ProviderType.Value);
                if (walletResult.IsError || walletResult.Result == null || !walletResult.Result.ContainsKey(ProviderType.Value) || walletResult.Result[ProviderType.Value] == null || !walletResult.Result[ProviderType.Value].Any())
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet for sender avatar");
                    return result;
                }

                var fromWallet = walletResult.Result[ProviderType.Value].FirstOrDefault();
                var toWallet = toAvatar.Result.ProviderWallets?.ContainsKey(ProviderType.Value) == true && toAvatar.Result.ProviderWallets[ProviderType.Value]?.Any() == true 
                    ? toAvatar.Result.ProviderWallets[ProviderType.Value].FirstOrDefault() 
                    : null;

                var transactionRequest = new WalletTransactionRequest
                {
                    FromWalletAddress = fromWallet?.WalletAddress ?? "",
                    ToWalletAddress = toWallet?.WalletAddress ?? "",
                    Amount = amount,
                    MemoText = token?.ToString() ?? ""
                };

                return await SendTransactionAsync(transactionRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by ID with token to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                var fromAvatar = await LoadAvatarByUsernameAsync(fromAvatarUsername);
                var toAvatar = await LoadAvatarByUsernameAsync(toAvatarUsername);

                if (fromAvatar.IsError || fromAvatar.Result == null || toAvatar.IsError || toAvatar.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to load avatar(s) by username for transaction");
                    return result;
                }

                return await SendTransactionByIdAsync(fromAvatar.Result.Id, toAvatar.Result.Id, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by username to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<string>();
            try
            {
                var fromAvatar = await LoadAvatarByUsernameAsync(fromAvatarUsername);
                var toAvatar = await LoadAvatarByUsernameAsync(toAvatarUsername);

                if (fromAvatar.IsError || fromAvatar.Result == null || toAvatar.IsError || toAvatar.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to load avatar(s) by username for transaction");
                    return result;
                }

                return await SendTransactionByIdAsync(fromAvatar.Result.Id, toAvatar.Result.Id, amount, token);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by username with token to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                var fromAvatar = await LoadAvatarByEmailAsync(fromAvatarEmail);
                var toAvatar = await LoadAvatarByEmailAsync(toAvatarEmail);

                if (fromAvatar.IsError || fromAvatar.Result == null || toAvatar.IsError || toAvatar.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to load avatar(s) by email for transaction");
                    return result;
                }

                return await SendTransactionByIdAsync(fromAvatar.Result.Id, toAvatar.Result.Id, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by email to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<string>();
            try
            {
                var fromAvatar = await LoadAvatarByEmailAsync(fromAvatarEmail);
                var toAvatar = await LoadAvatarByEmailAsync(toAvatarEmail);

                if (fromAvatar.IsError || fromAvatar.Result == null || toAvatar.IsError || toAvatar.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to load avatar(s) by email for transaction");
                    return result;
                }

                return await SendTransactionByIdAsync(fromAvatar.Result.Id, toAvatar.Result.Id, amount, token);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction by email with token to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        public async Task<OASISResult<string>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
        }

        public OASISResult<string> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }



        public async Task<OASISResult<bool>> SendNFTAsync(IWalletTransactionRequest transaction)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                var nftUrl = $"{_apiBaseUrl}/nft/transfer";
                var nftData = new
                {
                    fromAddress = transaction.FromWalletAddress ?? "",
                    toAddress = transaction.ToWalletAddress ?? "",
                    nftTokenId = "",
                    nftContractAddress = ""
                };

                var content = new StringContent(JsonSerializer.Serialize(nftData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(nftUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "NFT sent successfully to PLAN network";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"PLAN NFT transfer failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT to PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SendNFT(IWalletTransactionRequest transaction)
        {
            return SendNFTAsync(transaction).Result;
        }



        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            return LoadProviderWalletsForAvatarByIdAsync(id).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar to get provider wallets
                var avatarResult = await LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var providerWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                if (avatarResult.Result?.ProviderWallets != null)
                {
                    foreach (var group in avatarResult.Result.ProviderWallets.GroupBy(w => w.Key))
                    {
                        providerWallets[group.Key] = group.SelectMany(g => g.Value).ToList();
                    }
                }

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {id} from PLAN";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar from PLAN: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByIdAsync(id, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate PLAN provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar and update provider wallets
                var avatarResult = await LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var avatar = avatarResult.Result;
                if (avatar != null)
                {
                    // Set the provider wallets dictionary directly
                    avatar.ProviderWallets = providerWallets;

                    // Save updated avatar
                    var saveResult = await SaveAvatarAsync(avatar);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {saveResult.Message}");
                        return result;
                    }

                    // Count total wallets
                    var allWallets = new List<IProviderWallet>();
                    foreach (var kvp in providerWallets)
                    {
                        allWallets.AddRange(kvp.Value);
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {id} to PLAN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar to PLAN: {ex.Message}", ex);
            }
            return result;
        }



        /// <summary>
        /// Parse PLAN response to Avatar object
        /// </summary>
        private Avatar ParsePLANToAvatar(string planJson)
        {
            try
            {
                // Deserialize the complete Avatar object from PLAN JSON
                var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(planJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                
                return avatar;
            }
            catch (Exception)
            {
                // If JSON deserialization fails, try to extract basic info
                return CreateAvatarFromPLAN(planJson);
            }
        }

        /// <summary>
        /// Create Avatar from PLAN response when JSON deserialization fails
        /// </summary>
        private Avatar CreateAvatarFromPLAN(string planJson)
        {
            try
            {
                // Extract basic information from PLAN JSON response
                var planId = ExtractPLANProperty(planJson, "id") ?? ExtractPLANProperty(planJson, "account") ?? "plan_unknown";
                var avatar = new Avatar
                {
                    Id = CreateDeterministicGuid($"{ProviderType.Value}:{planId}"),
                    Username = planId,
                    Email = ExtractPLANProperty(planJson, "email") ?? "user@plan.example",
                    FirstName = ExtractPLANProperty(planJson, "first_name"),
                    LastName = ExtractPLANProperty(planJson, "last_name"),
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                
                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Extract property value from PLAN JSON response
        /// </summary>
        private string ExtractPLANProperty(string planJson, string propertyName)
        {
            try
            {
                // Simple regex-based extraction for PLAN properties
                var pattern = $"\"{propertyName}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(planJson, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Convert Avatar to PLAN format
        /// </summary>
        private string ConvertAvatarToPLAN(IAvatar avatar)
        {
            try
            {
                // Serialize Avatar to JSON with PLAN structure
                var planData = new
                {
                    id = avatar.Username,
                    email = avatar.Email,
                    first_name = avatar.FirstName,
                    last_name = avatar.LastName,
                    created = avatar.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = avatar.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(planData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(avatar, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        /// <summary>
        /// Convert Holon to PLAN format
        /// </summary>
        private string ConvertHolonToPLAN(IHolon holon)
        {
            try
            {
                // Serialize Holon to JSON with PLAN structure
                var planData = new
                {
                    id = holon.Id.ToString(),
                    type = holon.HolonType.ToString(),
                    name = holon.Name,
                    description = holon.Description,
                    created = holon.CreatedDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    modified = holon.ModifiedDate.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                return System.Text.Json.JsonSerializer.Serialize(planData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch (Exception)
            {
                // Fallback to basic JSON serialization
                return System.Text.Json.JsonSerializer.Serialize(holon, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
        }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }

    }
}
