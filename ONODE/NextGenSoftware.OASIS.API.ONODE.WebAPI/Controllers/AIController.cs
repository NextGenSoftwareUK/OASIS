using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.AI;
using System.Text.Json;
using OpenAI.Chat;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// AI-powered endpoints for natural language processing and intent parsing.
    /// Provides AI assistance for creating NFTs, GeoNFTs, and other OASIS entities.
    /// </summary>
    [Route("api/ai")]
    [ApiController]
    [Authorize]
    public class AIController : OASISControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIController> _logger;
        private readonly string _openAIApiKey;
        private readonly string _openAIModel;

        public AIController(IConfiguration configuration, ILogger<AIController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Get OpenAI API key from environment variable or configuration
            _openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                ?? _configuration["OpenAI:ApiKey"] 
                ?? _configuration["OpenAI__ApiKey"]; // Support double underscore for environment variables
            
            _openAIModel = _configuration["OpenAI:Model"] ?? "gpt-4o";
        }

        /// <summary>
        /// Parse natural language input into structured intent and parameters for NFT/GeoNFT creation.
        /// </summary>
        /// <param name="request">The parse intent request containing user input and context</param>
        /// <returns>OASIS result containing parsed intent and parameters</returns>
        /// <response code="200">Intent parsed successfully</response>
        /// <response code="400">Invalid request or parsing failed</response>
        /// <response code="401">Unauthorized - authentication required</response>
        /// <response code="500">Server error during parsing</response>
        [HttpPost("parse-intent")]
        [ProducesResponseType(typeof(OASISResult<ParseIntentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status500InternalServerError)]
        public async Task<OASISResult<ParseIntentResponse>> ParseIntentAsync([FromBody] ParseIntentRequest request)
        {
            OASISResult<ParseIntentResponse> result = new OASISResult<ParseIntentResponse>();

            try
            {
                // Validate request
                if (request == null || string.IsNullOrWhiteSpace(request.UserInput))
                {
                    result.IsError = true;
                    result.Message = "User input is required";
                    return result;
                }

                // Check if API key is configured
                if (string.IsNullOrEmpty(_openAIApiKey))
                {
                    result.IsError = true;
                    result.Message = "OpenAI API key is not configured. Please set OPENAI_API_KEY environment variable or configure it in appsettings.json";
                    _logger.LogWarning("OpenAI API key is not configured");
                    return result;
                }

                // Build system prompt
                string systemPrompt = BuildSystemPrompt(request.Context);

                // Build user prompt
                string userPrompt = request.UserInput;

                // Build full prompt
                string fullPrompt = $"{systemPrompt}\n\nUser request: {userPrompt}";

                // Call OpenAI API
                ChatClient chatClient = new ChatClient(model: _openAIModel, apiKey: _openAIApiKey);
                var completion = await chatClient.CompleteChatAsync(fullPrompt);
                string jsonResponse = completion.Value.Content[0].Text.Trim();

                // Clean up JSON (remove markdown code blocks if present)
                jsonResponse = CleanJsonResponse(jsonResponse);

                // Parse JSON
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                ParseIntentResponse parsedResponse = JsonSerializer.Deserialize<ParseIntentResponse>(jsonResponse, options);

                if (parsedResponse == null)
                {
                    result.IsError = true;
                    result.Message = "Failed to parse AI response";
                    return result;
                }

                // Validate parsed response
                if (!parsedResponse.IsValid || !string.IsNullOrEmpty(parsedResponse.ErrorMessage))
                {
                    result.IsError = true;
                    result.Message = parsedResponse.ErrorMessage ?? "AI could not determine a valid intent";
                    return result;
                }

                result.Result = parsedResponse;
                result.IsError = false;
                result.Message = "Intent parsed successfully";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing intent with AI");
                result.IsError = true;
                result.Message = $"Error parsing intent: {ex.Message}";
                result.Exception = ex;
                return result;
            }
        }

        private string BuildSystemPrompt(AIContext context)
        {
            if (context == null)
            {
                context = new AIContext
                {
                    AvailableProviders = new List<string> { "SolanaOASIS", "MongoDBOASIS" },
                    DefaultOnChainProvider = "SolanaOASIS",
                    DefaultOffChainProvider = "MongoDBOASIS"
                };
            }

            return $@"You are a helpful STAR NFT assistant that guides users through creating NFTs by asking for required information in a friendly, conversational way.

Available operations:
1. create_nft - Create an NFT (REQUIRED: title, description; OPTIONAL: price, imageUrl, symbol, onChainProvider, thumbnailUrl, sendToAddress, numberToMint)
2. create_geonft - Create a GeoNFT at specific coordinates (REQUIRED: title, description, lat, long; OPTIONAL: price, imageUrl, onChainProvider)
3. place_geonft - Place an existing GeoNFT at specific coordinates
4. create_quest - Create a quest (REQUIRED: name, description; OPTIONAL: type, difficulty, rewards)
5. create_mission - Create a mission (REQUIRED: name, description; OPTIONAL: type, difficulty)
6. start_quest - Start a quest by ID or name
7. complete_quest - Complete a quest by ID or name
8. show_quest_progress - Show progress for a quest
9. show_nearby_geonfts - Show GeoNFTs near a location

Current context:
- Avatar: {context.Avatar ?? "Unknown"}
- Available providers: {string.Join(", ", context.AvailableProviders ?? new List<string>())}
- Default on-chain provider: {context.DefaultOnChainProvider ?? "SolanaOASIS"}

IMPORTANT VALIDATION RULES:
- For create_nft: If ""title"" or ""description"" is missing, set isValid=false and errorMessage should politely ask for the missing information
- For create_geonft: If ""title"", ""description"", ""lat"", or ""long"" is missing, set isValid=false and errorMessage should politely ask for the missing information
- For create_quest: If ""name"" or ""description"" is missing, set isValid=false and errorMessage should politely ask for the missing information
- For create_mission: If ""name"" or ""description"" is missing, set isValid=false and errorMessage should politely ask for the missing information

Parse the user's request and return ONLY valid JSON in this exact format:
{{
  ""intent"": ""create_nft"" | ""create_geonft"" | ""place_geonft"" | ""create_quest"" | ""create_mission"" | ""start_quest"" | ""complete_quest"" | ""show_quest_progress"" | ""show_nearby_geonfts"",
  ""parameters"": {{
    // For create_nft (REQUIRED: title, description):
    ""title"": ""string"" (REQUIRED - if missing, set isValid=false and ask in errorMessage),
    ""description"": ""string"" (REQUIRED - if missing, set isValid=false and ask in errorMessage),
    ""price"": 0.0 (optional, default: 0),
    ""imageUrl"": ""string"" (optional - user may upload file separately),
    ""thumbnailUrl"": ""string"" (optional),
    ""symbol"": ""string"" (optional, default: ""OASISNFT""),
    ""onChainProvider"": ""string"" (optional, default: ""SolanaOASIS"", options: ""SolanaOASIS"", ""EthereumOASIS"", ""PolygonOASIS"", etc.),
    ""sendToAddress"": ""string"" (optional, wallet address to send NFT to after minting),
    ""numberToMint"": 1 (optional, default: 1),
    ""jsonMetaDataURL"": ""string"" (optional, URL to JSON metadata file)
    
    // OR for create_geonft (REQUIRED: title, description, lat, long):
    ""title"": ""string"" (REQUIRED),
    ""description"": ""string"" (REQUIRED),
    ""lat"": 0.0 (REQUIRED - latitude in degrees, if missing set isValid=false),
    ""long"": 0.0 (REQUIRED - longitude in degrees, if missing set isValid=false),
    ""price"": 0.0 (optional),
    ""imageUrl"": ""string"" (optional),
    ""onChainProvider"": ""string"" (optional, default: ""SolanaOASIS"")
    
    // OR for place_geonft:
    ""geonftId"": ""string"" (Guid, REQUIRED),
    ""lat"": 0.0 (REQUIRED),
    ""long"": 0.0 (REQUIRED),
    ""address"": ""string"" (optional, location name like ""Big Ben, London"")
    
    // OR for create_quest (REQUIRED: name, description):
    ""name"": ""string"" (REQUIRED),
    ""description"": ""string"" (REQUIRED),
    ""questType"": ""string"" (optional, default: ""MainQuest"", options: ""MainQuest"", ""SideQuest"", ""MagicQuest"", ""EggQuest""),
    ""difficulty"": ""string"" (optional, default: ""Easy"", options: ""Easy"", ""Medium"", ""Hard"", ""Expert""),
    ""rewardKarma"": 0 (optional, default: 0),
    ""rewardXP"": 0 (optional, default: 0),
    ""parentMissionId"": ""string"" (optional, Guid)
    
    // OR for create_mission (REQUIRED: name, description):
    ""name"": ""string"" (REQUIRED),
    ""description"": ""string"" (REQUIRED),
    ""missionType"": ""string"" (optional, default: ""Easy"", options: ""Easy"", ""Medium"", ""Hard"", ""Expert"")
    
    // OR for start_quest, complete_quest, show_quest_progress:
    ""questId"": ""string"" (optional, Guid),
    ""questName"": ""string"" (optional, name of quest)
    
    // OR for show_nearby_geonfts:
    ""lat"": 0.0,
    ""long"": 0.0,
    ""radius"": 1000 (optional, default: 1000, in meters),
    ""address"": ""string"" (optional, location name)
  }},
  ""isValid"": true/false,
  ""errorMessage"": ""string"" (only if isValid is false - should be a friendly question asking for missing information)
}}

Rules:
- ALWAYS validate required fields before setting isValid=true
- If required fields are missing, set isValid=false and provide a friendly errorMessage asking for the missing information
- Extract all mentioned parameters from the user's request
- Use sensible defaults for optional parameters
- Return ONLY the JSON, no other text
- For prices, assume SOL if no currency is specified
- For coordinates, extract latitude and longitude from location names when possible (e.g., ""Big Ben, London"" -> lat: 51.4994, long: -0.1245)
- For quest types, map common terms: ""main"" -> ""MainQuest"", ""side"" -> ""SideQuest"", ""daily"" -> ""SideQuest""
- For difficulty, map common terms: ""easy"" -> ""Easy"", ""medium"" -> ""Medium"", ""hard"" -> ""Hard"", ""expert"" -> ""Expert""
- Be conversational and helpful in error messages - guide users to provide missing information";
        }

        private string CleanJsonResponse(string json)
        {
            // Remove markdown code blocks if present
            json = json.Trim();
            if (json.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                json = json.Substring(7);
            }
            if (json.StartsWith("```"))
            {
                json = json.Substring(3);
            }
            if (json.EndsWith("```"))
            {
                json = json.Substring(0, json.Length - 3);
            }
            return json.Trim();
        }
    }
}

