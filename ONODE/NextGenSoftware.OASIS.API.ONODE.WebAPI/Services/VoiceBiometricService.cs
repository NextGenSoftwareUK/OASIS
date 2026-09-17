using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    public class VoiceEnrollResult
    {
        public bool Success { get; set; }
        /// <summary>Azure Speaker Recognition profile GUID to store on the Avatar.</summary>
        public string ProfileId { get; set; }
        public string Message { get; set; }
        /// <summary>Number of enrollment seconds accumulated (Azure requires >= 20 s).</summary>
        public double EnrollmentSecondsAccumulated { get; set; }
        public bool EnrollmentComplete { get; set; }
    }

    public class VoiceVerifyResult
    {
        public bool Accepted { get; set; }
        /// <summary>Raw Azure similarity score (0.0–1.0). Low = reject, high = accept.</summary>
        public double Score { get; set; }
        public string Message { get; set; }
    }

    public interface IVoiceBiometricService
    {
        /// <summary>Creates a new speaker profile and enrolls the provided audio. Returns the profile ID to store on the Avatar.</summary>
        Task<VoiceEnrollResult> EnrollAsync(Stream audioStream, string contentType = "audio/wav");

        /// <summary>Verifies that the audio matches the enrolled speaker profile.</summary>
        Task<VoiceVerifyResult> VerifyAsync(string profileId, Stream audioStream, string contentType = "audio/wav");

        /// <summary>Deletes the speaker profile from Azure and clears it from the Avatar.</summary>
        Task<bool> DeleteProfileAsync(string profileId);

        /// <summary>True when voice biometrics are configured and ready to use.</summary>
        bool IsAvailable { get; }
    }

    /// <summary>
    /// Azure Cognitive Services Speaker Recognition implementation.
    /// Uses the text-independent (free-speech) verification API.
    /// </summary>
    public sealed class AzureVoiceBiometricService : IVoiceBiometricService
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        private BiometricSettings Cfg => OASISDNAManager.OASISDNA?.OASIS?.Security?.Biometric ?? new BiometricSettings();
        private HerzIdSettings HerzCfg => OASISDNAManager.OASISDNA?.OASIS?.Security?.HerzId ?? new HerzIdSettings();

        private string Endpoint
        {
            get
            {
                var env = Environment.GetEnvironmentVariable("OASIS_AZURE_SPEECH_ENDPOINT");
                if (!string.IsNullOrEmpty(env)) return env.TrimEnd('/');
                // HerzID config is the legacy location; fall back for backwards compat
                if (!string.IsNullOrEmpty(Cfg.AzureSpeakerRecognitionEndpoint))
                    return Cfg.AzureSpeakerRecognitionEndpoint.TrimEnd('/');
                return HerzCfg.AzureSpeakerRecognitionEndpoint.TrimEnd('/');
            }
        }

        private string Key
        {
            get
            {
                var env = Environment.GetEnvironmentVariable("OASIS_AZURE_SPEECH_KEY");
                if (!string.IsNullOrEmpty(env)) return env;
                if (!string.IsNullOrEmpty(Cfg.AzureSpeakerRecognitionKey)) return Cfg.AzureSpeakerRecognitionKey;
                return HerzCfg.AzureSpeakerRecognitionKey;
            }
        }

        public bool IsAvailable => !string.IsNullOrEmpty(Endpoint) && !string.IsNullOrEmpty(Key);

        // ── Profile base URL ─────────────────────────────────────────────────
        private string ProfilesUrl => $"{Endpoint}/speaker/verification/v2.0/text-independent/profiles";

        public async Task<VoiceEnrollResult> EnrollAsync(Stream audioStream, string contentType = "audio/wav")
        {
            if (!IsAvailable)
                return new VoiceEnrollResult { Success = false, Message = "Azure Speaker Recognition is not configured." };

            try
            {
                // 1. Create a new profile
                using var createReq = new HttpRequestMessage(HttpMethod.Post, ProfilesUrl)
                {
                    Content = new StringContent("{\"locale\":\"en-us\"}", System.Text.Encoding.UTF8, "application/json")
                };
                createReq.Headers.Add("Ocp-Apim-Subscription-Key", Key);

                var createResp = await _http.SendAsync(createReq);
                if (!createResp.IsSuccessStatusCode)
                {
                    var err = await createResp.Content.ReadAsStringAsync();
                    LoggingManager.Log($"[VoiceBiometric] Create profile failed ({(int)createResp.StatusCode}): {err}", LogType.Warning);
                    return new VoiceEnrollResult { Success = false, Message = $"Failed to create Azure speaker profile: {(int)createResp.StatusCode}" };
                }

                var createBody = await createResp.Content.ReadAsStringAsync();
                using var createDoc = JsonDocument.Parse(createBody);
                var profileId = createDoc.RootElement.GetProperty("profileId").GetString();

                // 2. Enroll the audio
                using var enrollReq = new HttpRequestMessage(HttpMethod.Post, $"{ProfilesUrl}/{profileId}/enrollments");
                enrollReq.Headers.Add("Ocp-Apim-Subscription-Key", Key);
                var audioContent = new StreamContent(audioStream);
                audioContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                enrollReq.Content = audioContent;

                var enrollResp = await _http.SendAsync(enrollReq);
                var enrollBody = await enrollResp.Content.ReadAsStringAsync();

                if (!enrollResp.IsSuccessStatusCode)
                {
                    LoggingManager.Log($"[VoiceBiometric] Enroll failed ({(int)enrollResp.StatusCode}): {enrollBody}", LogType.Warning);
                    return new VoiceEnrollResult { Success = false, ProfileId = profileId, Message = $"Enrollment audio rejected: {(int)enrollResp.StatusCode}" };
                }

                using var enrollDoc = JsonDocument.Parse(enrollBody);
                var seconds = enrollDoc.RootElement.TryGetProperty("enrollmentsSpeechLength", out var sl) ? sl.GetDouble() : 0;
                var status = enrollDoc.RootElement.TryGetProperty("enrollmentStatus", out var st) ? st.GetString() : "";

                return new VoiceEnrollResult
                {
                    Success                      = true,
                    ProfileId                    = profileId,
                    EnrollmentSecondsAccumulated = seconds,
                    EnrollmentComplete           = string.Equals(status, "Enrolled", StringComparison.OrdinalIgnoreCase),
                    Message                      = $"Voice profile enrolled. Speech accumulated: {seconds:F1}s. Status: {status}."
                };
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"[VoiceBiometric] EnrollAsync exception: {ex.Message}", LogType.Error);
                return new VoiceEnrollResult { Success = false, Message = $"Internal error during enrollment: {ex.Message}" };
            }
        }

        public async Task<VoiceVerifyResult> VerifyAsync(string profileId, Stream audioStream, string contentType = "audio/wav")
        {
            if (!IsAvailable)
                return new VoiceVerifyResult { Accepted = false, Message = "Azure Speaker Recognition is not configured." };

            if (string.IsNullOrEmpty(profileId))
                return new VoiceVerifyResult { Accepted = false, Message = "No voice profile enrolled for this avatar." };

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, $"{ProfilesUrl}/{profileId}/verify");
                req.Headers.Add("Ocp-Apim-Subscription-Key", Key);
                var audioContent = new StreamContent(audioStream);
                audioContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                req.Content = audioContent;

                var resp = await _http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    LoggingManager.Log($"[VoiceBiometric] Verify failed ({(int)resp.StatusCode}): {body}", LogType.Warning);
                    return new VoiceVerifyResult { Accepted = false, Message = $"Verification call failed: {(int)resp.StatusCode}" };
                }

                using var doc = JsonDocument.Parse(body);
                var score = doc.RootElement.TryGetProperty("score", out var sc) ? sc.GetDouble() : 0;
                var result = doc.RootElement.TryGetProperty("recognitionResult", out var rr) ? rr.GetString() : "";

                var minScore = OASISDNAManager.OASISDNA?.OASIS?.Security?.Biometric?.VoiceVerificationMinScore ?? 0.5;
                var accepted = score >= minScore && string.Equals(result, "Accept", StringComparison.OrdinalIgnoreCase);

                return new VoiceVerifyResult
                {
                    Accepted = accepted,
                    Score    = score,
                    Message  = accepted
                        ? $"Voice verified (score {score:F3})."
                        : $"Voice not recognised (score {score:F3}, minimum {minScore:F2})."
                };
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"[VoiceBiometric] VerifyAsync exception: {ex.Message}", LogType.Error);
                return new VoiceVerifyResult { Accepted = false, Message = $"Internal error during verification: {ex.Message}" };
            }
        }

        public async Task<bool> DeleteProfileAsync(string profileId)
        {
            if (!IsAvailable || string.IsNullOrEmpty(profileId)) return false;
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Delete, $"{ProfilesUrl}/{profileId}");
                req.Headers.Add("Ocp-Apim-Subscription-Key", Key);
                var resp = await _http.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"[VoiceBiometric] DeleteProfileAsync exception: {ex.Message}", LogType.Warning);
                return false;
            }
        }
    }

    /// <summary>No-op implementation used when biometrics are disabled or Azure is not configured.</summary>
    public sealed class NullVoiceBiometricService : IVoiceBiometricService
    {
        public bool IsAvailable => false;

        public Task<VoiceEnrollResult> EnrollAsync(Stream audioStream, string contentType = "audio/wav")
            => Task.FromResult(new VoiceEnrollResult { Success = false, Message = "Voice biometrics are not enabled on this OASIS instance." });

        public Task<VoiceVerifyResult> VerifyAsync(string profileId, Stream audioStream, string contentType = "audio/wav")
            => Task.FromResult(new VoiceVerifyResult { Accepted = false, Message = "Voice biometrics are not enabled on this OASIS instance." });

        public Task<bool> DeleteProfileAsync(string profileId) => Task.FromResult(false);
    }
}
