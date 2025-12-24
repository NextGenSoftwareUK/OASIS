"""
Agent Discovery System for MNEE Hackathon Submission
Discovers agents with capability matching, karma filtering, and price negotiation
"""

from typing import List, Dict, Any, Optional
from src.core.oasis_client import OASISClient
from src.core.a2a_client import A2AClient
from config import MIN_KARMA_THRESHOLD


class AgentDiscovery:
    """Agent discovery system with karma filtering and capability matching"""
    
    def __init__(self, oasis_client: OASISClient, a2a_client: A2AClient):
        self.oasis = oasis_client
        self.a2a = a2a_client
    
    def discover_agents(
        self,
        capability: str,
        min_karma: Optional[int] = None,
        max_price: Optional[str] = None,
        agent_endpoints: Optional[List[str]] = None
    ) -> List[Dict[str, Any]]:
        """
        Discover agents with specified capability
        
        Args:
            capability: Capability name to search for (e.g., "analyzeMarketData")
            min_karma: Minimum karma threshold (defaults to config value)
            max_price: Maximum price in MNEE (e.g., "0.01 MNEE")
            agent_endpoints: List of agent endpoints to search (optional)
        
        Returns:
            List of discovered agents matching criteria
        """
        if min_karma is None:
            min_karma = MIN_KARMA_THRESHOLD
        
        # If no endpoints provided, use discovery mechanism
        # In production, this would query a registry or use HyperDrive
        if agent_endpoints is None:
            # For demo, we'll use a simple list
            # In production, this would be a registry lookup
            agent_endpoints = []
        
        # Discover agents via A2A
        discovered = self.a2a.discover_agents(agent_endpoints, capability_filter=capability)
        
        # Filter by karma and price
        filtered_agents = []
        for agent in discovered:
            # Check karma
            avatar_id = agent.get("metadata", {}).get("oasis", {}).get("avatarId")
            if avatar_id:
                karma_result = self.oasis.get_karma_stats(avatar_id)
                if karma_result.get("result"):
                    total_karma = karma_result["result"].get("total", 0)
                    if total_karma < min_karma:
                        continue
                    # Update agent card with current karma
                    agent["metadata"]["oasis"]["karma"] = total_karma
            
            # Check price if specified
            if max_price:
                capabilities = agent.get("capabilities", [])
                for cap in capabilities:
                    if cap.get("name") == capability:
                        pricing = cap.get("pricing", "")
                        # Simple price comparison (in production, parse properly)
                        if pricing and max_price:
                            # Extract numeric value (simplified)
                            try:
                                cap_price = float(pricing.split()[0])
                                max_price_val = float(max_price.split()[0])
                                if cap_price > max_price_val:
                                    continue
                            except:
                                pass
            
            filtered_agents.append(agent)
        
        # Sort by karma (highest first)
        filtered_agents.sort(
            key=lambda a: a.get("metadata", {}).get("oasis", {}).get("karma", 0),
            reverse=True
        )
        
        return filtered_agents
    
    def verify_agent_trust(self, agent: Dict[str, Any], min_karma: int) -> bool:
        """Verify if agent meets trust requirements"""
        avatar_id = agent.get("metadata", {}).get("oasis", {}).get("avatarId")
        if not avatar_id:
            return False
        
        karma_result = self.oasis.get_karma_stats(avatar_id)
        if karma_result.get("result"):
            total_karma = karma_result["result"].get("total", 0)
            return total_karma >= min_karma
        
        return False
    
    def get_agent_pricing(self, agent: Dict[str, Any], capability: str) -> Optional[str]:
        """Get pricing for specific capability from agent"""
        capabilities = agent.get("capabilities", [])
        for cap in capabilities:
            if cap.get("name") == capability:
                return cap.get("pricing")
        return None

