using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Agent;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Logging;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// A2A Protocol integration with Mission/Quest system
    /// Handles task delegation and mission management
    /// 
    /// NOTE: Full MissionManager/QuestManager integration requires STARDNA from STARNET.
    /// This integration provides a simplified task delegation system using A2A messages.
    /// For full mission/quest functionality, ensure STARDNA is available.
    /// </summary>
    public partial class A2AManager
    {
        private readonly Dictionary<Guid, A2ATask> _activeTasks = new Dictionary<Guid, A2ATask>();

        /// <summary>
        /// Delegate a task to an agent via A2A Protocol
        /// Creates a simplified task/mission structure using A2A messages
        /// </summary>
        /// <param name="fromAgentId">The agent delegating the task</param>
        /// <param name="toAgentId">The agent receiving the task</param>
        /// <param name="taskName">The task name</param>
        /// <param name="taskDescription">The task description</param>
        /// <param name="taskParameters">Optional task parameters</param>
        /// <param name="requiredServices">Optional list of required services</param>
        /// <returns>Task information</returns>
        public async Task<OASISResult<A2ATask>> DelegateTaskToAgentAsync(
            Guid fromAgentId,
            Guid toAgentId,
            string taskName,
            string taskDescription,
            Dictionary<string, object> taskParameters = null,
            List<string> requiredServices = null)
        {
            var result = new OASISResult<A2ATask>();
            try
            {
                // Validate agents exist
                var fromAvatarResult = await AvatarManager.Instance.LoadAvatarAsync(fromAgentId, false, true);
                if (fromAvatarResult.IsError || fromAvatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Sender agent {fromAgentId} not found");
                    return result;
                }

                var toAvatarResult = await AvatarManager.Instance.LoadAvatarAsync(toAgentId, false, true);
                if (toAvatarResult.IsError || toAvatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Recipient agent {toAgentId} not found");
                    return result;
                }

                if (fromAvatarResult.Result.AvatarType.Value != AvatarType.Agent ||
                    toAvatarResult.Result.AvatarType.Value != AvatarType.Agent)
                {
                    OASISErrorHandling.HandleError(ref result, "Both agents must be of type Agent");
                    return result;
                }

                // Create task
                var task = new A2ATask
                {
                    TaskId = Guid.NewGuid(),
                    FromAgentId = fromAgentId,
                    ToAgentId = toAgentId,
                    TaskName = taskName,
                    TaskDescription = taskDescription,
                    TaskParameters = taskParameters ?? new Dictionary<string, object>(),
                    RequiredServices = requiredServices ?? new List<string>(),
                    Status = A2ATaskStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                // Create A2A message for task delegation
                var message = new A2AMessage
                {
                    MessageId = Guid.NewGuid(),
                    FromAgentId = fromAgentId,
                    ToAgentId = toAgentId,
                    MessageType = A2AMessageType.TaskDelegation,
                    Content = taskDescription,
                    Payload = new Dictionary<string, object>
                    {
                        ["task_id"] = task.TaskId.ToString(),
                        ["task_name"] = taskName,
                        ["task_description"] = taskDescription,
                        ["task_parameters"] = taskParameters ?? new Dictionary<string, object>(),
                        ["required_services"] = requiredServices ?? new List<string>()
                    },
                    Timestamp = DateTime.UtcNow
                };

                // Send message
                var messageResult = await SendA2AMessageAsync(message);
                if (messageResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send task delegation message: {messageResult.Message}");
                    return result;
                }

                // Store task
                _activeTasks[task.TaskId] = task;
                task.MessageId = message.MessageId;

                result.Result = task;
                result.Message = "Task delegated successfully";
                LoggingManager.Log($"Task '{taskName}' delegated from agent {fromAgentId} to agent {toAgentId}", LogType.Info);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error delegating task: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Complete a delegated task
        /// </summary>
        /// <param name="taskId">The task ID</param>
        /// <param name="resultData">Optional result data</param>
        /// <param name="completionNotes">Optional completion notes</param>
        /// <returns>Success status</returns>
        public async Task<OASISResult<bool>> CompleteTaskAsync(
            Guid taskId,
            Dictionary<string, object> resultData = null,
            string completionNotes = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!_activeTasks.ContainsKey(taskId))
                {
                    OASISErrorHandling.HandleError(ref result, $"Task {taskId} not found");
                    return result;
                }

                var task = _activeTasks[taskId];
                task.Status = A2ATaskStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.CompletionNotes = completionNotes;
                task.ResultData = resultData ?? new Dictionary<string, object>();

                // Send completion notification
                var completionMessage = new A2AMessage
                {
                    MessageId = Guid.NewGuid(),
                    FromAgentId = task.ToAgentId,
                    ToAgentId = task.FromAgentId,
                    MessageType = A2AMessageType.TaskCompletion,
                    Content = $"Task '{task.TaskName}' completed",
                    Payload = new Dictionary<string, object>
                    {
                        ["task_id"] = taskId.ToString(),
                        ["task_name"] = task.TaskName,
                        ["completion_notes"] = completionNotes ?? "",
                        ["result_data"] = resultData ?? new Dictionary<string, object>()
                    },
                    Timestamp = DateTime.UtcNow
                };

                var messageResult = await SendA2AMessageAsync(completionMessage);
                if (messageResult.IsError)
                {
                    LoggingManager.Log($"Task completed but failed to send notification: {messageResult.Message}", LogType.Warning);
                }

                // Award karma for task completion
                await AwardKarmaForServiceCompletionAsync(task.ToAgentId, task.TaskName, taskId);

                result.Result = true;
                result.Message = "Task completed successfully";
                LoggingManager.Log($"Task {taskId} completed by agent {task.ToAgentId}", LogType.Info);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error completing task: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get task information
        /// </summary>
        /// <param name="taskId">The task ID</param>
        /// <returns>Task information</returns>
        public async Task<OASISResult<A2ATask>> GetTaskAsync(Guid taskId)
        {
            var result = new OASISResult<A2ATask>();
            try
            {
                if (!_activeTasks.ContainsKey(taskId))
                {
                    OASISErrorHandling.HandleError(ref result, $"Task {taskId} not found");
                    return result;
                }

                result.Result = _activeTasks[taskId];
                result.Message = "Task retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting task: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Get all tasks for an agent
        /// </summary>
        /// <param name="agentId">The agent ID</param>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of tasks</returns>
        public async Task<OASISResult<List<A2ATask>>> GetAgentTasksAsync(Guid agentId, A2ATaskStatus? status = null)
        {
            var result = new OASISResult<List<A2ATask>>();
            try
            {
                var tasks = new List<A2ATask>();
                foreach (var task in _activeTasks.Values)
                {
                    if ((task.FromAgentId == agentId || task.ToAgentId == agentId) &&
                        (status == null || task.Status == status))
                    {
                        tasks.Add(task);
                    }
                }

                result.Result = tasks;
                result.Message = $"Retrieved {tasks.Count} tasks for agent {agentId}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting agent tasks: {ex.Message}", ex);
            }
            return result;
        }
    }

    /// <summary>
    /// A2A Task representation
    /// </summary>
    public class A2ATask
    {
        public Guid TaskId { get; set; }
        public Guid FromAgentId { get; set; }
        public Guid ToAgentId { get; set; }
        public Guid MessageId { get; set; }
        public string TaskName { get; set; }
        public string TaskDescription { get; set; }
        public Dictionary<string, object> TaskParameters { get; set; } = new Dictionary<string, object>();
        public List<string> RequiredServices { get; set; } = new List<string>();
        public A2ATaskStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string CompletionNotes { get; set; }
        public Dictionary<string, object> ResultData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// A2A Task status
    /// </summary>
    public enum A2ATaskStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }
}

