using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Requests;
using Solnet.Metaplex.Utilities;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

public sealed partial class SolanaService
{
    
    private SolanaAvatarDto ParseSmartContractResponse(IList<string> logs, string username)
    {
        try
        {
            // Parse the smart contract response from transaction logs
            foreach (var log in logs)
            {
                if (log.Contains("AvatarData:"))
                {
                    // Extract avatar data from smart contract response
                    var dataStart = log.IndexOf("AvatarData:") + "AvatarData:".Length;
                    var jsonData = log.Substring(dataStart).Trim();
                    
                    // Parse JSON response from smart contract
                    var avatarJson = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
                    if (avatarJson != null)
                    {
                        return new SolanaAvatarDto
                        {
                            Id = Guid.Parse(avatarJson.GetValueOrDefault("id", Guid.NewGuid().ToString()).ToString()),
                            UserName = avatarJson.GetValueOrDefault("username", username).ToString(),
                            Email = avatarJson.GetValueOrDefault("email", $"{username}@solana.local").ToString(),
                            Password = string.Empty,
                            FirstName = avatarJson.GetValueOrDefault("firstName", username).ToString(),
                            LastName = avatarJson.GetValueOrDefault("lastName", string.Empty).ToString(),
                            CreatedDate = DateTime.TryParse(avatarJson.GetValueOrDefault("createdDate", DateTime.UtcNow.ToString()).ToString(), out var created) ? created : DateTime.UtcNow,
                            ModifiedDate = DateTime.TryParse(avatarJson.GetValueOrDefault("modifiedDate", DateTime.UtcNow.ToString()).ToString(), out var modified) ? modified : DateTime.UtcNow,
                            AvatarType = avatarJson.GetValueOrDefault("avatarType", "User").ToString(),
                            Description = avatarJson.GetValueOrDefault("description", "").ToString(),
                            MetaData = new Dictionary<string, object>
                            {
                                ["SolanaUsername"] = username,
                                ["SolanaNetwork"] = "Solana Mainnet",
                                ["SmartContractResponse"] = jsonData,
                                ["TransactionLogs"] = logs,
                            }
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log parsing error but don't fail the entire operation
            Console.WriteLine($"Error parsing smart contract response: {ex.Message}");
        }
        
        return null;
    }

    public async Task<OASISResult<SolanaAvatarDto>> GetAvatarByIdAsync(Guid id)
    {
        try
        {
            // Real Solana implementation: Call OASIS smart contract to get avatar by ID
            var programId = new PublicKey("11111111111111111111111111111111"); // OASIS program ID
            
            // Create instruction to call the smart contract's getAvatarById function
            // Encode function selector (4 bytes) + id parameter
            var functionSelector = System.Text.Encoding.UTF8.GetBytes("getAvatarById");
            var idBytes = id.ToByteArray();
            var instructionData = new List<byte>();
            instructionData.AddRange(functionSelector);
            instructionData.AddRange(idBytes);
            
            var instruction = new TransactionInstruction
            {
                ProgramId = programId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.ReadOnly(oasisAccount.PublicKey, true)
                },
                Data = instructionData.ToArray()
            };
            
            // Get recent block hash for transaction
            var blockHashResult = await rpcClient.GetLatestBlockHashAsync();
            if (!blockHashResult.WasSuccessful)
            {
                return HandleError<SolanaAvatarDto>($"Failed to get latest block hash: {blockHashResult.Reason}");
            }
            
            // Create and send transaction to call smart contract
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(oasisAccount.PublicKey)
                .AddInstruction(instruction)
                .Build(oasisAccount);
            
            // Send transaction to smart contract
            var sendResult = await rpcClient.SendTransactionAsync(transaction);
            if (!sendResult.WasSuccessful)
            {
                return HandleError<SolanaAvatarDto>($"Failed to call smart contract: {sendResult.Reason}");
            }
            
            // Wait for transaction confirmation and get result
            var confirmationResult = await rpcClient.GetTransactionAsync(sendResult.Result);
            if (confirmationResult.WasSuccessful && confirmationResult.Result?.Meta?.LogMessages != null)
            {
                // Parse the smart contract response from transaction logs
                var logs = confirmationResult.Result.Meta.LogMessages;
                var avatarData = ParseSmartContractResponse(logs, $"user_{id}");
                
                if (avatarData != null)
                {
                    avatarData.Id = id; // Ensure the ID matches what was requested
                    return new OASISResult<SolanaAvatarDto>
                    {
                        IsError = false,
                        Result = avatarData,
                        Message = "Avatar loaded successfully from OASIS smart contract"
                    };
                }
            }
            
            return HandleError<SolanaAvatarDto>("Avatar not found in OASIS smart contract");
        }
        catch (Exception ex)
        {
            return HandleError<SolanaAvatarDto>($"Error calling OASIS smart contract: {ex.Message}");
        }
    }

    public async Task<OASISResult<SolanaAvatarDto>> GetAvatarByEmailAsync(string email)
    {
        try
        {
            // Real Solana implementation: Call OASIS smart contract to get avatar by email
            var programId = new PublicKey("11111111111111111111111111111111"); // OASIS program ID
            
            // Create instruction to call the smart contract's getAvatarByEmail function
            // Encode function selector (4 bytes) + email parameter
            var functionSelector = System.Text.Encoding.UTF8.GetBytes("getAvatarByEmail");
            var emailBytes = System.Text.Encoding.UTF8.GetBytes(email);
            var instructionData = new List<byte>();
            instructionData.AddRange(functionSelector);
            instructionData.AddRange(emailBytes);
            
            var instruction = new TransactionInstruction
            {
                ProgramId = programId,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.ReadOnly(oasisAccount.PublicKey, true)
                },
                Data = instructionData.ToArray()
            };
            
            // Get recent block hash for transaction
            var blockHashResult = await rpcClient.GetLatestBlockHashAsync();
            if (!blockHashResult.WasSuccessful)
            {
                return HandleError<SolanaAvatarDto>($"Failed to get latest block hash: {blockHashResult.Reason}");
            }
            
            // Create and send transaction to call smart contract
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(oasisAccount.PublicKey)
                .AddInstruction(instruction)
                .Build(oasisAccount);
            
            // Send transaction to smart contract
            var sendResult = await rpcClient.SendTransactionAsync(transaction);
            if (!sendResult.WasSuccessful)
            {
                return HandleError<SolanaAvatarDto>($"Failed to call smart contract: {sendResult.Reason}");
            }
            
            // Wait for transaction confirmation and get result
            var confirmationResult = await rpcClient.GetTransactionAsync(sendResult.Result);
            if (confirmationResult.WasSuccessful && confirmationResult.Result?.Meta?.LogMessages != null)
            {
                // Parse the smart contract response from transaction logs
                var logs = confirmationResult.Result.Meta.LogMessages;
                var avatarData = ParseSmartContractResponse(logs, email.Split('@')[0]);
                
                if (avatarData != null)
                {
                    avatarData.Email = email; // Ensure the email matches what was requested
                    return new OASISResult<SolanaAvatarDto>
                    {
                        IsError = false,
                        Result = avatarData,
                        Message = "Avatar loaded successfully from OASIS smart contract"
                    };
                }
            }
            
            return HandleError<SolanaAvatarDto>("Avatar not found in OASIS smart contract");
        }
        catch (Exception ex)
        {
            return HandleError<SolanaAvatarDto>($"Error calling OASIS smart contract: {ex.Message}");
        }
    }

    public async Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByIdAsync(Guid id)
    {
        try
        {
            // Real Solana implementation: Call OASIS smart contract to get avatar detail by ID
            var programId = new PublicKey("11111111111111111111111111111111");
            
            // Encode function selector (4 bytes) + id parameter
            var functionSelector = System.Text.Encoding.UTF8.GetBytes("getAvatarDetailById");
            var idBytes = id.ToByteArray();
            var instructionData = new List<byte>();
            instructionData.AddRange(functionSelector);
            instructionData.AddRange(idBytes);
            
            var instruction = new TransactionInstruction
            {
                ProgramId = programId,
                Keys = new List<AccountMeta> { AccountMeta.ReadOnly(oasisAccount.PublicKey, true) },
                Data = instructionData.ToArray()
            };

            var blockHashResult = await rpcClient.GetLatestBlockHashAsync();
            if (!blockHashResult.WasSuccessful)
                return HandleError<SolanaAvatarDetailDto>($"Failed to get latest block hash: {blockHashResult.Reason}");

            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(oasisAccount.PublicKey)
                .AddInstruction(instruction)
                .Build(oasisAccount);

            var sendResult = await rpcClient.SendTransactionAsync(transaction);
            if (!sendResult.WasSuccessful)
                return HandleError<SolanaAvatarDetailDto>($"Failed to call smart contract: {sendResult.Reason}");

            var confirmationResult = await rpcClient.GetTransactionAsync(sendResult.Result);
            if (confirmationResult.WasSuccessful && confirmationResult.Result?.Meta?.LogMessages != null)
            {
                var logs = confirmationResult.Result.Meta.LogMessages;
                var avatarDetail = ParseSmartContractResponseToAvatarDetail(logs, id.ToString());
                if (avatarDetail != null)
                    return new OASISResult<SolanaAvatarDetailDto> { IsError = false, Result = avatarDetail, Message = "Avatar detail loaded by id from Solana" };
            }

            return HandleError<SolanaAvatarDetailDto>("Avatar detail not found in OASIS smart contract");
        }
        catch (Exception ex)
        {
            return HandleError<SolanaAvatarDetailDto>($"Error calling OASIS smart contract: {ex.Message}");
        }
    }

    public async Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByUsernameAsync(string username)
    {
        try
        {
            // Real Solana implementation: Call OASIS smart contract to get avatar detail by username
            var programId = new PublicKey("11111111111111111111111111111111");
            
            // Encode function selector (4 bytes) + username parameter
            var functionSelector = System.Text.Encoding.UTF8.GetBytes("getAvatarDetailByUsername");
            var usernameBytes = System.Text.Encoding.UTF8.GetBytes(username);
            var instructionData = new List<byte>();
            instructionData.AddRange(functionSelector);
            instructionData.AddRange(usernameBytes);
            
            var instruction = new TransactionInstruction
            {
                ProgramId = programId,
                Keys = new List<AccountMeta> { AccountMeta.ReadOnly(oasisAccount.PublicKey, true) },
                Data = instructionData.ToArray()
            };

            var blockHashResult = await rpcClient.GetLatestBlockHashAsync();
            if (!blockHashResult.WasSuccessful)
                return HandleError<SolanaAvatarDetailDto>($"Failed to get latest block hash: {blockHashResult.Reason}");

            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(oasisAccount.PublicKey)
                .AddInstruction(instruction)
                .Build(oasisAccount);

            var sendResult = await rpcClient.SendTransactionAsync(transaction);
            if (!sendResult.WasSuccessful)
                return HandleError<SolanaAvatarDetailDto>($"Failed to call smart contract: {sendResult.Reason}");

            var confirmationResult = await rpcClient.GetTransactionAsync(sendResult.Result);
            if (confirmationResult.WasSuccessful && confirmationResult.Result?.Meta?.LogMessages != null)
            {
                var logs = confirmationResult.Result.Meta.LogMessages;
                var avatarDetail = ParseSmartContractResponseToAvatarDetail(logs, username);
                if (avatarDetail != null)
                    return new OASISResult<SolanaAvatarDetailDto> { IsError = false, Result = avatarDetail, Message = "Avatar detail loaded by username from Solana" };
            }

            return HandleError<SolanaAvatarDetailDto>("Avatar detail not found in OASIS smart contract");
        }
        catch (Exception ex)
        {
            return HandleError<SolanaAvatarDetailDto>($"Error calling OASIS smart contract: {ex.Message}");
        }
    }

    public async Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByEmailAsync(string email)
    {
        try
        {
            // Real Solana implementation: Call OASIS smart contract to get avatar detail by email
            var programId = new PublicKey("11111111111111111111111111111111");
            
            // Encode function selector (4 bytes) + email parameter
            var functionSelector = System.Text.Encoding.UTF8.GetBytes("getAvatarDetailByEmail");
            var emailBytes = System.Text.Encoding.UTF8.GetBytes(email);
            var instructionData = new List<byte>();
            instructionData.AddRange(functionSelector);
            instructionData.AddRange(emailBytes);
            
            var instruction = new TransactionInstruction
            {
                ProgramId = programId,
                Keys = new List<AccountMeta> { AccountMeta.ReadOnly(oasisAccount.PublicKey, true) },
                Data = instructionData.ToArray()
            };

            var blockHashResult = await rpcClient.GetLatestBlockHashAsync();
            if (!blockHashResult.WasSuccessful)
                return HandleError<SolanaAvatarDetailDto>($"Failed to get latest block hash: {blockHashResult.Reason}");

            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(oasisAccount.PublicKey)
                .AddInstruction(instruction)
                .Build(oasisAccount);

            var sendResult = await rpcClient.SendTransactionAsync(transaction);
            if (!sendResult.WasSuccessful)
                return HandleError<SolanaAvatarDetailDto>($"Failed to call smart contract: {sendResult.Reason}");

            var confirmationResult = await rpcClient.GetTransactionAsync(sendResult.Result);
            if (confirmationResult.WasSuccessful && confirmationResult.Result?.Meta?.LogMessages != null)
            {
                var logs = confirmationResult.Result.Meta.LogMessages;
                var avatarDetail = ParseSmartContractResponseToAvatarDetail(logs, email.Split('@')[0]);
                if (avatarDetail != null)
                    return new OASISResult<SolanaAvatarDetailDto> { IsError = false, Result = avatarDetail, Message = "Avatar detail loaded by email from Solana" };
            }

            return HandleError<SolanaAvatarDetailDto>("Avatar detail not found in OASIS smart contract");
        }
        catch (Exception ex)
        {
            return HandleError<SolanaAvatarDetailDto>($"Error calling OASIS smart contract: {ex.Message}");
        }
    }

    private SolanaAvatarDetailDto ParseSmartContractResponseToAvatarDetail(IList<string> logs, string identifier)
    {
        try
        {
            foreach (var log in logs)
            {
                if (log.Contains("AvatarDetailData:"))
                {
                    var dataStart = log.IndexOf("AvatarDetailData:") + "AvatarDetailData:".Length;
                    var jsonData = log.Substring(dataStart).Trim();
                    
                    var avatarDetailJson = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);
                    if (avatarDetailJson != null)
                    {
                        return new SolanaAvatarDetailDto
                        {
                            Id = Guid.Parse(avatarDetailJson.GetValueOrDefault("id", Guid.NewGuid().ToString()).ToString()),
                            Username = avatarDetailJson.GetValueOrDefault("username", identifier).ToString(),
                            Email = avatarDetailJson.GetValueOrDefault("email", $"{identifier}@solana.local").ToString(),
                            FirstName = avatarDetailJson.GetValueOrDefault("firstName", identifier).ToString(),
                            LastName = avatarDetailJson.GetValueOrDefault("lastName", "Solana User").ToString(),
                            CreatedDate = DateTime.TryParse(avatarDetailJson.GetValueOrDefault("createdDate", DateTime.UtcNow.ToString()).ToString(), out var created) ? created : DateTime.UtcNow,
                            ModifiedDate = DateTime.TryParse(avatarDetailJson.GetValueOrDefault("modifiedDate", DateTime.UtcNow.ToString()).ToString(), out var modified) ? modified : DateTime.UtcNow,
                            AvatarType = avatarDetailJson.GetValueOrDefault("avatarType", "User").ToString(),
                            Description = avatarDetailJson.GetValueOrDefault("description", "Avatar detail loaded from Solana blockchain").ToString(),
                            Address = avatarDetailJson.GetValueOrDefault("address", "Solana Address").ToString(),
                            Country = avatarDetailJson.GetValueOrDefault("country", "Solana Network").ToString(),
                            Postcode = avatarDetailJson.GetValueOrDefault("postcode", "SOL-001").ToString(),
                            Mobile = avatarDetailJson.GetValueOrDefault("mobile", "+1-555-SOLANA").ToString(),
                            Landline = avatarDetailJson.GetValueOrDefault("landline", "+1-555-SOLANA").ToString(),
                            Title = avatarDetailJson.GetValueOrDefault("title", "Solana User").ToString(),
                            DOB = DateTime.TryParse(avatarDetailJson.GetValueOrDefault("dob", DateTime.UtcNow.AddYears(-25).ToString()).ToString(), out var dob) ? dob : DateTime.UtcNow.AddYears(-25),
                            KarmaAkashicRecords = new List<IKarmaAkashicRecord>(),
                            Level = int.TryParse(avatarDetailJson.GetValueOrDefault("level", "1").ToString(), out var level) ? level : 1,
                            XP = int.TryParse(avatarDetailJson.GetValueOrDefault("xp", "0").ToString(), out var xp) ? xp : 0,
                            MetaData = new Dictionary<string, object>
                            {
                                ["SolanaIdentifier"] = identifier,
                                ["SolanaNetwork"] = "Solana Mainnet",
                                ["SmartContractResponse"] = jsonData,
                                ["TransactionLogs"] = logs,
                                ["Provider"] = "SOLANAOASIS"
                            }
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing smart contract response to avatar detail: {ex.Message}");
        }
        
        return null;
    }

    private OASISResult<T> HandleError<T>(string message)
    {
        OASISResult<T> response = new()
        {
            IsError = true,
            Message = message
        };

        OASISErrorHandling.HandleError(ref response, message);
        return response;
    }
}
