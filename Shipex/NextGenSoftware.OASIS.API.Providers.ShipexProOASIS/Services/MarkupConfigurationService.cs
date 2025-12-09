using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services
{
    /// <summary>
    /// Service for managing markup configurations (CRUD operations).
    /// </summary>
    public class MarkupConfigurationService
    {
        private readonly IShipexProRepository _repository;
        private readonly ILogger<MarkupConfigurationService> _logger;

        public MarkupConfigurationService(
            IShipexProRepository repository,
            ILogger<MarkupConfigurationService> logger = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger;
        }

        /// <summary>
        /// Get all active markups, optionally filtered by merchant.
        /// </summary>
        public async Task<OASISResult<List<MarkupConfiguration>>> GetMarkupsAsync(Guid? merchantId = null)
        {
            var result = new OASISResult<List<MarkupConfiguration>>();

            try
            {
                var markupsResult = await _repository.GetActiveMarkupsAsync(merchantId);
                if (markupsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, markupsResult.Message);
                    return result;
                }

                result.Result = markupsResult.Result ?? new List<MarkupConfiguration>();
                result.IsError = false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred while getting markups");
                OASISErrorHandling.HandleError(ref result, $"Failed to get markups: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Get a markup configuration by ID.
        /// </summary>
        public async Task<OASISResult<MarkupConfiguration>> GetMarkupAsync(Guid markupId)
        {
            var result = new OASISResult<MarkupConfiguration>();

            try
            {
                var markupResult = await _repository.GetMarkupAsync(markupId);
                if (markupResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, markupResult.Message);
                    return result;
                }

                if (markupResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Markup {markupId} not found");
                    return result;
                }

                result.Result = markupResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while getting markup {markupId}");
                OASISErrorHandling.HandleError(ref result, $"Failed to get markup: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Create a new markup configuration.
        /// </summary>
        public async Task<OASISResult<MarkupConfiguration>> CreateMarkupAsync(MarkupConfiguration markup)
        {
            var result = new OASISResult<MarkupConfiguration>();

            try
            {
                // Validate markup
                var validationResult = ValidateMarkup(markup);
                if (!validationResult.IsValid)
                {
                    OASISErrorHandling.HandleError(ref result, validationResult.ErrorMessage);
                    return result;
                }

                // Set timestamps
                if (markup.MarkupId == Guid.Empty)
                {
                    markup.MarkupId = Guid.NewGuid();
                }
                markup.CreatedAt = DateTime.UtcNow;
                markup.UpdatedAt = DateTime.UtcNow;

                var saveResult = await _repository.SaveMarkupAsync(markup);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }

                result.Result = saveResult.Result;
                result.IsError = false;

                _logger?.LogInformation($"Created markup {markup.MarkupId} for merchant {markup.MerchantId}, carrier {markup.Carrier}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred while creating markup");
                OASISErrorHandling.HandleError(ref result, $"Failed to create markup: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Update an existing markup configuration.
        /// </summary>
        public async Task<OASISResult<MarkupConfiguration>> UpdateMarkupAsync(Guid markupId, MarkupConfiguration markup)
        {
            var result = new OASISResult<MarkupConfiguration>();

            try
            {
                // Get existing markup
                var existingResult = await _repository.GetMarkupAsync(markupId);
                if (existingResult.IsError || existingResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Markup {markupId} not found");
                    return result;
                }

                // Validate markup
                var validationResult = ValidateMarkup(markup);
                if (!validationResult.IsValid)
                {
                    OASISErrorHandling.HandleError(ref result, validationResult.ErrorMessage);
                    return result;
                }

                // Update fields
                markup.MarkupId = markupId;
                markup.CreatedAt = existingResult.Result.CreatedAt; // Preserve original creation time
                markup.UpdatedAt = DateTime.UtcNow;

                var saveResult = await _repository.SaveMarkupAsync(markup);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }

                result.Result = saveResult.Result;
                result.IsError = false;

                _logger?.LogInformation($"Updated markup {markupId}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while updating markup {markupId}");
                OASISErrorHandling.HandleError(ref result, $"Failed to update markup: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Delete a markup configuration.
        /// </summary>
        public async Task<OASISResult<bool>> DeleteMarkupAsync(Guid markupId)
        {
            var result = new OASISResult<bool>();

            try
            {
                var deleteResult = await _repository.DeleteMarkupAsync(markupId);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, deleteResult.Message);
                    return result;
                }

                result.Result = deleteResult.Result;
                result.IsError = false;

                _logger?.LogInformation($"Deleted markup {markupId}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception occurred while deleting markup {markupId}");
                OASISErrorHandling.HandleError(ref result, $"Failed to delete markup: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validates markup configuration.
        /// </summary>
        private (bool IsValid, string ErrorMessage) ValidateMarkup(MarkupConfiguration markup)
        {
            if (markup == null)
                return (false, "Markup configuration cannot be null");

            if (string.IsNullOrWhiteSpace(markup.Carrier))
                return (false, "Carrier is required");

            if (markup.Value < 0)
                return (false, "Markup value cannot be negative");

            if (markup.Type == MarkupType.Percentage && markup.Value > 1000)
                return (false, "Percentage markup cannot exceed 1000%");

            if (markup.EffectiveTo.HasValue && markup.EffectiveTo.Value <= markup.EffectiveFrom)
                return (false, "EffectiveTo date must be after EffectiveFrom date");

            return (true, null);
        }
    }
}




