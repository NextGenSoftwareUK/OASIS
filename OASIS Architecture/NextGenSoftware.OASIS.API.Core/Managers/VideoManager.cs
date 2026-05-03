using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Manages video calls, conferences, and real-time video communication
    /// </summary>
    public class VideoManager : OASISManager
    {
        private static VideoManager _instance = null;
        private readonly Dictionary<string, VideoCall> _activeCalls = new Dictionary<string, VideoCall>();
        private readonly Dictionary<string, List<VideoParticipant>> _callParticipants = new Dictionary<string, List<VideoParticipant>>();

        public static VideoManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new VideoManager(ProviderManager.Instance.CurrentStorageProvider);

                return _instance;
            }
        }

        public VideoManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
        }

        /// <summary>
        /// Starts a new video call
        /// </summary>
        /// <param name="initiatorId">Avatar ID of the call initiator</param>
        /// <param name="participantIds">List of participant avatar IDs</param>
        /// <param name="callType">Type of video call (one-on-one, group, conference)</param>
        /// <param name="callName">Optional call name</param>
        /// <returns>Call ID and connection details</returns>
        public async Task<OASISResult<string>> StartVideoCallAsync(Guid initiatorId, List<Guid> participantIds, VideoCallType callType = VideoCallType.OneOnOne, string callName = null)
        {
            var result = new OASISResult<string>();
            
            try
            {
                // Validate participants
                if (participantIds == null || participantIds.Count == 0)
                {
                    result.IsError = true;
                    result.Message = "At least one participant is required for a video call";
                    return result;
                }

                // Add initiator to participants if not already included
                if (!participantIds.Contains(initiatorId))
                {
                    participantIds.Insert(0, initiatorId);
                }

                // Generate unique call ID
                var callId = Guid.NewGuid().ToString();
                
                // Create video call
                var videoCall = new VideoCall
                {
                    Id = callId,
                    Name = callName ?? $"Video Call {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                    InitiatorId = initiatorId,
                    ParticipantIds = participantIds,
                    CallType = callType,
                    Status = VideoCallStatus.Initiating,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                // Store call
                _activeCalls[callId] = videoCall;
                _callParticipants[callId] = new List<VideoParticipant>();

                // Initialize call in storage
                var holon = new Holon
                {
                    Id = Guid.NewGuid(),
                    Name = videoCall.Name,
                    Description = $"Video call with {participantIds.Count} participants",
                    CreatedDate = DateTime.UtcNow,
                    MetaData = new Dictionary<string, object>
                    {
                        { "CallId", callId },
                        { "InitiatorId", initiatorId.ToString() },
                        { "ParticipantIds", string.Join(",", participantIds) },
                        { "CallType", callType.ToString() },
                        { "Status", VideoCallStatus.Initiating.ToString() },
                        { "IsActive", true }
                    }
                };

                var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to save video call: {saveResult.Message}";
                    return result;
                }

                // Update video statistics in settings system whenever video calls are started
                try
                {
                    var videoStats = new Dictionary<string, object>
                    {
                        ["totalCallsStarted"] = _activeCalls.Count(c => c.Value.InitiatorId == initiatorId),
                        ["totalCallsParticipated"] = _activeCalls.Count(c => c.Value.ParticipantIds.Contains(initiatorId)),
                        ["lastCallStarted"] = DateTime.UtcNow,
                        ["lastCallType"] = callType.ToString(),
                        ["participantsInLastCall"] = participantIds.Count
                    };
                    await HolonManager.Instance.SaveSettingsAsync(initiatorId, "video", videoStats);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the main operation
                    Console.WriteLine($"Warning: Failed to save video statistics: {ex.Message}");
                }

                result.Result = callId;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error starting video call: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Joins an existing video call
        /// </summary>
        /// <param name="callId">Video call ID</param>
        /// <param name="participantId">Avatar ID of the participant joining</param>
        /// <param name="connectionDetails">WebRTC connection details</param>
        /// <returns>Join status and updated participant list</returns>
        public async Task<OASISResult<bool>> JoinVideoCallAsync(string callId, Guid participantId, string connectionDetails = null)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                // Validate call exists
                if (!_activeCalls.ContainsKey(callId))
                {
                    result.IsError = true;
                    result.Message = "Video call not found";
                    return result;
                }

                var videoCall = _activeCalls[callId];
                
                // Validate participant is invited
                if (!videoCall.ParticipantIds.Contains(participantId))
                {
                    result.IsError = true;
                    result.Message = "User is not invited to this video call";
                    return result;
                }

                // Check if call is still active
                if (!videoCall.IsActive || videoCall.Status == VideoCallStatus.Ended)
                {
                    result.IsError = true;
                    result.Message = "Video call is no longer active";
                    return result;
                }

                // Add participant to call
                var participant = new VideoParticipant
                {
                    AvatarId = participantId,
                    CallId = callId,
                    JoinedDate = DateTime.UtcNow,
                    ConnectionDetails = connectionDetails,
                    Status = VideoParticipantStatus.Connected
                };

                _callParticipants[callId].Add(participant);

                // Update call status if this is the first participant joining
                if (videoCall.Status == VideoCallStatus.Initiating)
                {
                    videoCall.Status = VideoCallStatus.Active;
                    videoCall.StartedDate = DateTime.UtcNow;
                }

                result.Result = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error joining video call: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Leaves a video call
        /// </summary>
        /// <param name="callId">Video call ID</param>
        /// <param name="participantId">Avatar ID of the participant leaving</param>
        /// <returns>Leave status</returns>
        public async Task<OASISResult<bool>> LeaveVideoCallAsync(string callId, Guid participantId)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                // Validate call exists
                if (!_activeCalls.ContainsKey(callId))
                {
                    result.IsError = true;
                    result.Message = "Video call not found";
                    return result;
                }

                var videoCall = _activeCalls[callId];
                
                // Find and remove participant
                var participant = _callParticipants[callId].FirstOrDefault(p => p.AvatarId == participantId);
                if (participant != null)
                {
                    participant.Status = VideoParticipantStatus.Disconnected;
                    participant.LeftDate = DateTime.UtcNow;
                }

                // Check if call should end (no active participants)
                var activeParticipants = _callParticipants[callId].Count(p => p.Status == VideoParticipantStatus.Connected);
                if (activeParticipants == 0)
                {
                    videoCall.Status = VideoCallStatus.Ended;
                    videoCall.EndedDate = DateTime.UtcNow;
                    videoCall.IsActive = false;
                }

                result.Result = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error leaving video call: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Ends a video call
        /// </summary>
        /// <param name="callId">Video call ID</param>
        /// <param name="endedById">Avatar ID of the user ending the call</param>
        /// <returns>End status</returns>
        public async Task<OASISResult<bool>> EndVideoCallAsync(string callId, Guid endedById)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                if (!_activeCalls.ContainsKey(callId))
                {
                    result.IsError = true;
                    result.Message = "Video call not found";
                    return result;
                }

                var videoCall = _activeCalls[callId];
                
                // Validate user is participant
                if (!videoCall.ParticipantIds.Contains(endedById))
                {
                    result.IsError = true;
                    result.Message = "User is not a participant in this call";
                    return result;
                }

                // Mark call as ended
                videoCall.Status = VideoCallStatus.Ended;
                videoCall.EndedDate = DateTime.UtcNow;
                videoCall.EndedById = endedById;
                videoCall.IsActive = false;

                // Mark all participants as disconnected
                foreach (var participant in _callParticipants[callId])
                {
                    if (participant.Status == VideoParticipantStatus.Connected)
                    {
                        participant.Status = VideoParticipantStatus.Disconnected;
                        participant.LeftDate = DateTime.UtcNow;
                    }
                }

                result.Result = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error ending video call: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Gets active video calls for a user
        /// </summary>
        /// <param name="avatarId">Avatar ID</param>
        /// <returns>List of active video calls</returns>
        public async Task<OASISResult<List<VideoCall>>> GetActiveCallsAsync(Guid avatarId)
        {
            var result = new OASISResult<List<VideoCall>>();
            
            try
            {
                var userCalls = _activeCalls.Values
                    .Where(c => c.ParticipantIds.Contains(avatarId) && c.IsActive)
                    .ToList();

                result.Result = userCalls;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving active calls: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Gets participants in a video call
        /// </summary>
        /// <param name="callId">Video call ID</param>
        /// <returns>List of participants</returns>
        public async Task<OASISResult<List<VideoParticipant>>> GetCallParticipantsAsync(string callId)
        {
            var result = new OASISResult<List<VideoParticipant>>();
            
            try
            {
                if (!_callParticipants.ContainsKey(callId))
                {
                    result.IsError = true;
                    result.Message = "Video call not found";
                    return result;
                }

                result.Result = _callParticipants[callId];
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving call participants: {ex.Message}";
            }

            return result;
        }
    }

    /// <summary>
    /// Represents a video call
    /// </summary>
    public class VideoCall
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Guid InitiatorId { get; set; }
        public List<Guid> ParticipantIds { get; set; } = new List<Guid>();
        public VideoCallType CallType { get; set; }
        public VideoCallStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? StartedDate { get; set; }
        public DateTime? EndedDate { get; set; }
        public Guid? EndedById { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Represents a video call participant
    /// </summary>
    public class VideoParticipant
    {
        public Guid AvatarId { get; set; }
        public string CallId { get; set; }
        public DateTime JoinedDate { get; set; }
        public DateTime? LeftDate { get; set; }
        public string ConnectionDetails { get; set; }
        public VideoParticipantStatus Status { get; set; }
    }

    /// <summary>
    /// Types of video calls
    /// </summary>
    public enum VideoCallType
    {
        OneOnOne,
        Group,
        Conference,
        Webinar
    }

    /// <summary>
    /// Status of a video call
    /// </summary>
    public enum VideoCallStatus
    {
        Initiating,
        Active,
        Paused,
        Ended
    }

    /// <summary>
    /// Status of a video call participant
    /// </summary>
    public enum VideoParticipantStatus
    {
        Invited,
        Connected,
        Disconnected,
        Muted,
        VideoOff
    }
}
