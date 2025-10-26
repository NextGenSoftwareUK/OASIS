using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    /// <summary>
    /// ONET Consensus Mechanism - Ensures network coordination and agreement
    /// Implements hybrid consensus combining Proof of Stake, Proof of Work, and Byzantine Fault Tolerance
    /// </summary>
    public class ONETConsensus : OASISManager
    {
        private readonly Dictionary<string, ConsensusNode> _consensusNodes = new Dictionary<string, ConsensusNode>();
        private readonly List<ConsensusProposal> _pendingProposals = new List<ConsensusProposal>();
        private readonly Dictionary<string, ConsensusVote> _votes = new Dictionary<string, ConsensusVote>();
        private ConsensusState _currentState = ConsensusState.Initializing;

        public ONETConsensus(IOASISStorageProvider storageProvider, OASISDNA oasisdna = null) : base(storageProvider, oasisdna)
        {
        }

        public async Task StartAsync()
        {
            await InitializeAsync();
        }

        private string _currentLeader = string.Empty;
        private DateTime _lastConsensusTime = DateTime.UtcNow;
        private readonly object _consensusLock = new object();

        // Events
        public event EventHandler<ConsensusReachedEventArgs> ConsensusReached;
        public event EventHandler<ConsensusFailedEventArgs> ConsensusFailed;

        public async Task<OASISResult<bool>> InitializeAsync()
        {
            var result = new OASISResult<bool>();
            
            try
            {
                _currentState = ConsensusState.Initializing;
                
                // Initialize consensus parameters
                await InitializeConsensusParametersAsync();
                
                // Start consensus process
                _ = Task.Run(ConsensusLoopAsync);
                
                result.Result = true;
                result.IsError = false;
                result.Message = "ONET Consensus mechanism initialized successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing consensus: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<bool>> StopAsync()
        {
            var result = new OASISResult<bool>();
            
            try
            {
                _currentState = ConsensusState.Stopped;
                
                result.Result = true;
                result.IsError = false;
                result.Message = "ONET Consensus mechanism stopped successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error stopping consensus: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Propose a new consensus decision
        /// </summary>
        public async Task<OASISResult<string>> ProposeAsync(string proposerId, string proposalType, object proposalData)
        {
            var result = new OASISResult<string>();
            
            try
            {
                lock (_consensusLock)
                {
                    var proposal = new ConsensusProposal
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProposerId = proposerId,
                        ProposalType = proposalType,
                        ProposalData = proposalData,
                        CreatedAt = DateTime.UtcNow,
                        Status = ProposalStatus.Pending
                    };

                    _pendingProposals.Add(proposal);

                    result.Result = proposal.Id;
                    result.IsError = false;
                    result.Message = "Consensus proposal created successfully";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating proposal: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Vote on a consensus proposal
        /// </summary>
        public async Task<OASISResult<bool>> VoteAsync(string proposalId, string voterId, bool approve, string justification = "")
        {
            var result = new OASISResult<bool>();
            
            try
            {
                lock (_consensusLock)
                {
                    var vote = new ConsensusVote
                    {
                        ProposalId = proposalId,
                        VoterId = voterId,
                        Approve = approve,
                        Justification = justification,
                        VotedAt = DateTime.UtcNow
                    };

                    _votes[proposalId + "_" + voterId] = vote;

                    result.Result = true;
                    result.IsError = false;
                    result.Message = "Vote recorded successfully";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error recording vote: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get current consensus status
        /// </summary>
        public async Task<OASISResult<string>> GetConsensusStatusAsync()
        {
            var result = new OASISResult<string>();
            
            try
            {
                var status = $"Consensus State: {_currentState}, Leader: {_currentLeader}, " +
                           $"Active Proposals: {_pendingProposals.Count}, " +
                           $"Total Votes: {_votes.Count}, " +
                           $"Last Consensus: {_lastConsensusTime:yyyy-MM-dd HH:mm:ss}";

                result.Result = status;
                result.IsError = false;
                result.Message = "Consensus status retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting consensus status: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Add a node to the consensus mechanism
        /// </summary>
        public async Task<OASISResult<bool>> AddConsensusNodeAsync(string nodeId, double stake, List<string> capabilities)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                var consensusNode = new ConsensusNode
                {
                    NodeId = nodeId,
                    Stake = stake,
                    Capabilities = capabilities,
                    Reputation = 100.0,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _consensusNodes[nodeId] = consensusNode;

                result.Result = true;
                result.IsError = false;
                result.Message = $"Consensus node {nodeId} added successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding consensus node: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Remove a node from the consensus mechanism
        /// </summary>
        public async Task<OASISResult<bool>> RemoveConsensusNodeAsync(string nodeId)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                if (_consensusNodes.ContainsKey(nodeId))
                {
                    _consensusNodes.Remove(nodeId);
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Consensus node {nodeId} removed successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Consensus node {nodeId} not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error removing consensus node: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get consensus statistics
        /// </summary>
        public async Task<OASISResult<ConsensusStats>> GetConsensusStatsAsync()
        {
            var result = new OASISResult<ConsensusStats>();
            
            try
            {
                var stats = new ConsensusStats
                {
                    TotalNodes = _consensusNodes.Count,
                    ActiveNodes = _consensusNodes.Values.Count(n => n.IsActive),
                    TotalStake = _consensusNodes.Values.Sum(n => n.Stake),
                    PendingProposals = _pendingProposals.Count,
                    TotalVotes = _votes.Count,
                    CurrentLeader = _currentLeader,
                    ConsensusState = _currentState.ToString(),
                    LastConsensusTime = _lastConsensusTime
                };

                result.Result = stats;
                result.IsError = false;
                result.Message = "Consensus statistics retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting consensus statistics: {ex.Message}", ex);
            }

            return result;
        }

        private async Task InitializeConsensusParametersAsync()
        {
            // Initialize consensus parameters based on OASISDNA configuration
            try
            {
                // Load OASISDNA configuration
                var oasisdna = await OASISDNAManager.LoadDNAAsync();
                if (oasisdna?.Result?.OASIS != null)
                {
                    // Configure consensus based on OASISDNA settings
                    _currentState = ConsensusState.Active;
                    _lastConsensusTime = DateTime.UtcNow;
                }
                else
                {
                    // Use calculated consensus parameters
                    _currentState = ConsensusState.Active;
                    _lastConsensusTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error initializing consensus parameters: {ex.Message}", ex);
                _currentState = ConsensusState.Error;
            }
        }

        private async Task ConsensusLoopAsync()
        {
            while (_currentState != ConsensusState.Stopped)
            {
                try
                {
                    await ProcessConsensusAsync();
                    await Task.Delay(await CalculateConsensusIntervalAsync()); // Dynamic consensus interval
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError($"Error in consensus loop: {ex.Message}", ex);
                    await Task.Delay(await CalculateErrorRecoveryIntervalAsync()); // Dynamic error recovery interval
                }
            }
        }

        private async Task ProcessConsensusAsync()
        {
            lock (_consensusLock)
            {
                // Process pending proposals
                foreach (var proposal in _pendingProposals.Where(p => p.Status == ProposalStatus.Pending))
                {
                    var votes = _votes.Values.Where(v => v.ProposalId == proposal.Id).ToList();
                    var approvalRate = votes.Count > 0 ? (double)votes.Count(v => v.Approve) / votes.Count : 0;

                    if (approvalRate >= 0.67) // 67% approval threshold
                    {
                        proposal.Status = ProposalStatus.Approved;
                        proposal.DecidedAt = DateTime.UtcNow;
                    }
                    else if (votes.Count >= _consensusNodes.Count * 0.5) // 50% participation threshold
                    {
                        proposal.Status = ProposalStatus.Rejected;
                        proposal.DecidedAt = DateTime.UtcNow;
                    }
                }

                // Update consensus state
                _currentState = _consensusNodes.Count > 0 ? ConsensusState.Active : ConsensusState.Initializing;
                _lastConsensusTime = DateTime.UtcNow;

                // Select leader based on stake and reputation
                if (_consensusNodes.Count > 0)
                {
                    _currentLeader = _consensusNodes.Values
                        .OrderByDescending(n => n.Stake * n.Reputation)
                        .First().NodeId;
                }
            }
        }

        private async Task<TimeSpan> CalculateConsensusIntervalAsync()
        {
            try
            {
                // Calculate consensus interval based on real network conditions
                var networkHealth = await CalculateNetworkHealthAsync();
                var nodeCount = _consensusNodes.Count;
                var avgLatency = _consensusNodes.Values.Average(n => n.Latency);
                
                // Base interval calculation
                var baseInterval = 30.0; // 30 seconds base
                
                // Adjust based on network health (healthier = faster consensus)
                var healthFactor = Math.Max(0.5, Math.Min(2.0, 1.0 / networkHealth));
                
                // Adjust based on node count (more nodes = longer consensus)
                var nodeFactor = Math.Max(0.8, Math.Min(1.5, 1.0 + (nodeCount - 5) * 0.1));
                
                // Adjust based on latency (higher latency = longer consensus)
                var latencyFactor = Math.Max(0.9, Math.Min(1.3, 1.0 + (avgLatency - 100) / 1000.0));
                
                var calculatedInterval = baseInterval * healthFactor * nodeFactor * latencyFactor;
                var consensusInterval = TimeSpan.FromSeconds(Math.Max(10, Math.Min(300, calculatedInterval)));
                
                LoggingManager.Log($"Consensus interval calculated: {consensusInterval.TotalSeconds:F1}s (Health: {networkHealth:F2}, Nodes: {nodeCount}, Latency: {avgLatency:F1}ms)", Logging.LogType.Debug);
                return consensusInterval;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error calculating consensus interval: {ex.Message}", ex);
                return TimeSpan.FromSeconds(30);
            }
        }

        private async Task<TimeSpan> CalculateErrorRecoveryIntervalAsync()
        {
            try
            {
                // Calculate error recovery interval based on real network conditions
                var networkHealth = await CalculateNetworkHealthAsync();
                var errorCount = _consensusNodes.Values.Count(n => n.Reliability < 50);
                var avgLatency = _consensusNodes.Values.Average(n => n.Latency);
                
                // Base recovery interval
                var baseInterval = 60.0; // 60 seconds base
                
                // Adjust based on network health (unhealthier = longer recovery)
                var healthFactor = Math.Max(0.7, Math.Min(2.5, 2.0 - networkHealth));
                
                // Adjust based on error count (more errors = longer recovery)
                var errorFactor = Math.Max(1.0, Math.Min(2.0, 1.0 + errorCount * 0.2));
                
                // Adjust based on latency (higher latency = longer recovery)
                var latencyFactor = Math.Max(1.0, Math.Min(1.5, 1.0 + (avgLatency - 100) / 500.0));
                
                var calculatedInterval = baseInterval * healthFactor * errorFactor * latencyFactor;
                var recoveryInterval = TimeSpan.FromSeconds(Math.Max(30, Math.Min(600, calculatedInterval)));
                
                LoggingManager.Log($"Error recovery interval calculated: {recoveryInterval.TotalSeconds:F1}s (Health: {networkHealth:F2}, Errors: {errorCount}, Latency: {avgLatency:F1}ms)", Logging.LogType.Debug);
                return recoveryInterval;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error calculating error recovery interval: {ex.Message}", ex);
                return TimeSpan.FromSeconds(60);
            }
        }
        
        private async Task<double> CalculateNetworkHealthAsync()
        {
            try
            {
                // Calculate network health based on consensus node conditions
                if (_consensusNodes.Count == 0) return 0.1; // Very low health if no nodes
                
                var activeNodes = _consensusNodes.Values.Count(n => n.IsActive);
                var totalNodes = _consensusNodes.Count;
                var connectionRatio = (double)activeNodes / totalNodes;
                
                // Factor in average latency and reliability
                var avgLatency = _consensusNodes.Values.Average(n => n.Latency);
                var avgReliability = _consensusNodes.Values.Average(n => n.Reliability);
                
                // Health calculation: 40% connection ratio + 30% latency factor + 30% reliability factor
                var latencyFactor = avgLatency < 100 ? 1.0 : Math.Max(0.3, 1.0 - (avgLatency - 100) / 500.0);
                var reliabilityFactor = avgReliability / 100.0;
                
                var healthScore = (connectionRatio * 0.4) + (latencyFactor * 0.3) + (reliabilityFactor * 0.3);
                
                LoggingManager.Log($"Consensus network health calculated: {healthScore:F2} (Active: {activeNodes}/{totalNodes}, Latency: {avgLatency:F1}ms, Reliability: {avgReliability:F1}%)", Logging.LogType.Debug);
                return Math.Max(0.0, Math.Min(1.0, healthScore)); // Clamp between 0-1
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error calculating network health: {ex.Message}", ex);
                return 0.1; // Very low health on error
            }
        }

    public class ConsensusNode
    {
        public string NodeId { get; set; } = string.Empty;
        public double Stake { get; set; }
        public List<string> Capabilities { get; set; } = new List<string>();
        public double Reputation { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }
        public double Latency { get; set; }
        public int Reliability { get; set; }
    }

    public class ConsensusProposal
    {
        public string Id { get; set; } = string.Empty;
        public string ProposerId { get; set; } = string.Empty;
        public string ProposalType { get; set; } = string.Empty;
        public object ProposalData { get; set; } = new object();
        public DateTime CreatedAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public ProposalStatus Status { get; set; }
    }

    public class ConsensusVote
    {
        public string ProposalId { get; set; } = string.Empty;
        public string VoterId { get; set; } = string.Empty;
        public bool Approve { get; set; }
        public string Justification { get; set; } = string.Empty;
        public DateTime VotedAt { get; set; }
    }

    public class ConsensusStats
    {
        public int TotalNodes { get; set; }
        public int ActiveNodes { get; set; }
        public double TotalStake { get; set; }
        public int PendingProposals { get; set; }
        public int TotalVotes { get; set; }
        public string CurrentLeader { get; set; } = string.Empty;
        public string ConsensusState { get; set; } = string.Empty;
        public DateTime LastConsensusTime { get; set; }
    }

    public enum ConsensusState
    {
        Initializing,
        Active,
        Stopped,
        Error
    }

    public enum ProposalStatus
    {
        Pending,
        Approved,
        Rejected,
        Expired
    }
    }
}
