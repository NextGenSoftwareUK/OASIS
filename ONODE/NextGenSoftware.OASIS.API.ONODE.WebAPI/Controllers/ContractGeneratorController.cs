using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Controller for generating smart contracts
    /// Supports multiple blockchain platforms including Aztec, Ethereum, Solana, etc.
    /// </summary>
    [ApiController]
    [Route("api/v1/contracts")]
    [ApiExplorerSettings(IgnoreApi = true)] // Exclude from Swagger until dependencies are implemented
    public class ContractGeneratorController : ControllerBase
    {
        private readonly ILogger<ContractGeneratorController> _logger;
        private readonly ContractGeneratorService _generatorService;

        public ContractGeneratorController(
            ILogger<ContractGeneratorController> logger,
            ContractGeneratorService generatorService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _generatorService = generatorService ?? throw new ArgumentNullException(nameof(generatorService));
        }

        /// <summary>
        /// Generate an Aztec bridge contract
        /// </summary>
        /// <param name="request">Contract generation request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated contract code</returns>
        [HttpPost("aztec/bridge")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateAztecBridgeContract(
            [FromBody] GenerateAztecBridgeContractRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating Aztec bridge contract: {ContractName}", request.ContractName);

                var result = await _generatorService.GenerateAztecBridgeContractAsync(request, cancellationToken);

                if (result.IsError)
                {
                    _logger.LogWarning("Contract generation failed: {Message}", result.Message);
                    return BadRequest(new { error = result.Message });
                }

                _logger.LogInformation("Contract generated successfully: {ContractName}", request.ContractName);
                return Ok(result.Result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GenerateAztecBridgeContract");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Generate a custom contract using AI
        /// </summary>
        /// <param name="request">AI contract generation request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated contract code</returns>
        [HttpPost("generate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateContract(
            [FromBody] GenerateContractRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating contract: {Platform} - {ContractType}", request.Platform, request.ContractType);

                var result = await _generatorService.GenerateContractAsync(request, cancellationToken);

                if (result.IsError)
                {
                    _logger.LogWarning("Contract generation failed: {Message}", result.Message);
                    return BadRequest(new { error = result.Message });
                }

                _logger.LogInformation("Contract generated successfully");
                return Ok(result.Result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GenerateContract");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}

