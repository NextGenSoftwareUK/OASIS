using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Telegram
{
    /// <summary>
    /// Minimal DTO for Telegram webhook Update payload.
    /// </summary>
    public class TelegramWebhookUpdate
    {
        [JsonPropertyName("update_id")]
        public long UpdateId { get; set; }

        [JsonPropertyName("message")]
        public TelegramMessage Message { get; set; }
    }

    public class TelegramMessage
    {
        [JsonPropertyName("message_id")]
        public int MessageId { get; set; }

        [JsonPropertyName("from")]
        public TelegramUser From { get; set; }

        [JsonPropertyName("chat")]
        public TelegramChat Chat { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("caption")]
        public string Caption { get; set; }

        [JsonPropertyName("photo")]
        public List<TelegramPhotoSize> Photo { get; set; }

        [JsonPropertyName("document")]
        public TelegramDocument Document { get; set; }

        [JsonPropertyName("animation")]
        public TelegramAnimation Animation { get; set; }
    }

    public class TelegramDocument
    {
        [JsonPropertyName("file_id")]
        public string FileId { get; set; }

        [JsonPropertyName("file_name")]
        public string FileName { get; set; }

        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; }
    }

    public class TelegramAnimation
    {
        [JsonPropertyName("file_id")]
        public string FileId { get; set; }

        [JsonPropertyName("file_name")]
        public string FileName { get; set; }

        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; }
    }

    public class TelegramUser
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }
    }

    public class TelegramChat
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class TelegramPhotoSize
    {
        [JsonPropertyName("file_id")]
        public string FileId { get; set; }

        [JsonPropertyName("file_unique_id")]
        public string FileUniqueId { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("file_size")]
        public int? FileSize { get; set; }
    }

    public class TelegramFileResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("result")]
        public TelegramFileResult Result { get; set; }
    }

    public class TelegramFileResult
    {
        [JsonPropertyName("file_path")]
        public string FilePath { get; set; }

        [JsonPropertyName("file_id")]
        public string FileId { get; set; }
    }
}
