using System;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models.TimoRides
{
    public class DriverActionPayload
    {
        public string DriverId { get; set; }
        public string BookingId { get; set; }
        public string Action { get; set; }
        public string Source { get; set; } = "telegram";
        public string ChatId { get; set; }
        public DriverActionMeta Meta { get; set; } = new DriverActionMeta();
        public string TraceId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class DriverActionMeta
    {
        public string Reason { get; set; }
        public string PaymentStatus { get; set; }
        public DriverLocationDto Coordinates { get; set; }
        public long? ActorTelegramId { get; set; }
        public string ActorUsername { get; set; }
        public string Notes { get; set; }
    }

    public class DriverLocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Speed { get; set; }
        public double? Bearing { get; set; }
        public double? Accuracy { get; set; }
    }

    public class DriverLocationPayload
    {
        public string DriverId { get; set; }
        public string BookingId { get; set; }
        public string Source { get; set; } = "telegram";
        public string ChatId { get; set; }
        public DriverLocationDto Location { get; set; }
        public string TraceId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class DriverActionResponse
    {
        public bool Success { get; set; }
        public string TraceId { get; set; }
        public BookingDto Booking { get; set; }
    }

    public class DriverLocationResponse
    {
        public bool Success { get; set; }
    }
}

