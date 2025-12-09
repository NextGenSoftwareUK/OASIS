using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services
{
    /// <summary>
    /// Validates shipment status transitions according to business rules.
    /// </summary>
    public static class ShipmentStatusValidator
    {
        /// <summary>
        /// Valid status transitions map.
        /// Key is current status, value is list of valid next statuses.
        /// </summary>
        private static readonly Dictionary<ShipmentStatus, List<ShipmentStatus>> ValidTransitions = new()
        {
            { ShipmentStatus.QuoteRequested, new List<ShipmentStatus> { ShipmentStatus.QuoteProvided, ShipmentStatus.Cancelled, ShipmentStatus.Error } },
            { ShipmentStatus.QuoteProvided, new List<ShipmentStatus> { ShipmentStatus.QuoteAccepted, ShipmentStatus.Cancelled, ShipmentStatus.Error } },
            { ShipmentStatus.QuoteAccepted, new List<ShipmentStatus> { ShipmentStatus.ShipmentCreated, ShipmentStatus.Cancelled, ShipmentStatus.Error } },
            { ShipmentStatus.ShipmentCreated, new List<ShipmentStatus> { ShipmentStatus.LabelGenerated, ShipmentStatus.Cancelled, ShipmentStatus.Error } },
            { ShipmentStatus.LabelGenerated, new List<ShipmentStatus> { ShipmentStatus.InTransit, ShipmentStatus.Cancelled, ShipmentStatus.Error } },
            { ShipmentStatus.InTransit, new List<ShipmentStatus> { ShipmentStatus.Delivered, ShipmentStatus.Error } },
            { ShipmentStatus.Delivered, new List<ShipmentStatus>() }, // Terminal state
            { ShipmentStatus.Cancelled, new List<ShipmentStatus>() }, // Terminal state
            { ShipmentStatus.Error, new List<ShipmentStatus> { ShipmentStatus.QuoteRequested, ShipmentStatus.ShipmentCreated } } // Can retry from error
        };

        /// <summary>
        /// Checks if a status transition is valid.
        /// </summary>
        /// <param name="currentStatus">Current shipment status</param>
        /// <param name="newStatus">Proposed new status</param>
        /// <returns>True if transition is valid, false otherwise</returns>
        public static bool IsValidTransition(ShipmentStatus currentStatus, ShipmentStatus newStatus)
        {
            // Same status is always valid (idempotent)
            if (currentStatus == newStatus)
                return true;

            // Check if transition is in valid transitions map
            if (!ValidTransitions.ContainsKey(currentStatus))
                return false;

            return ValidTransitions[currentStatus].Contains(newStatus);
        }

        /// <summary>
        /// Gets all valid next statuses for a given current status.
        /// </summary>
        /// <param name="currentStatus">Current shipment status</param>
        /// <returns>List of valid next statuses</returns>
        public static List<ShipmentStatus> GetValidNextStatuses(ShipmentStatus currentStatus)
        {
            if (!ValidTransitions.ContainsKey(currentStatus))
                return new List<ShipmentStatus>();

            return new List<ShipmentStatus>(ValidTransitions[currentStatus]);
        }

        /// <summary>
        /// Gets error message for invalid transition.
        /// </summary>
        /// <param name="currentStatus">Current shipment status</param>
        /// <param name="newStatus">Proposed new status</param>
        /// <returns>Error message describing valid transitions</returns>
        public static string GetTransitionErrorMessage(ShipmentStatus currentStatus, ShipmentStatus newStatus)
        {
            var validNextStatuses = GetValidNextStatuses(currentStatus);
            
            if (validNextStatuses.Count == 0)
            {
                return $"Status {currentStatus} is a terminal state and cannot be changed.";
            }

            var validStatusesStr = string.Join(", ", validNextStatuses);
            return $"Invalid status transition from {currentStatus} to {newStatus}. Valid next statuses are: {validStatusesStr}";
        }
    }
}




