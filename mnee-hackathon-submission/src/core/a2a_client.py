"""
A2A Protocol Client for MNEE Hackathon Submission
Handles Agent Cards, discovery, and task invocation
"""

import requests
import json
from typing import Optional, Dict, Any, List
from config import A2A_PROTOCOL_VERSION, A2A_ENDPOINT_PATH


class A2AClient:
    """A2A Protocol client for agent communication"""
    
    def __init__(self):
        self.protocol_version = A2A_PROTOCOL_VERSION
    
    def create_agent_card(
        self,
        avatar_id: str,
        name: str,
        capabilities: List[Dict[str, Any]],
        endpoint: str,
        wallet_address: Optional[str] = None,
        karma_score: int = 0,
        version: str = "1.0.0",
        description: str = ""
    ) -> Dict[str, Any]:
        """Create A2A Agent Card from OASIS data"""
        agent_card = {
            "agentId": avatar_id,
            "name": name,
            "version": version,
            "description": description or f"A2A-compatible agent with OASIS infrastructure",
            "capabilities": capabilities,
            "endpoint": endpoint,
            "metadata": {
                "oasis": {
                    "avatarId": avatar_id,
                    "walletAddress": wallet_address,
                    "karma": karma_score,
                    "trustStatus": "verified" if karma_score >= 50 else "pending"
                }
            }
        }
        
        return agent_card
    
    def invoke_task(
        self,
        agent_endpoint: str,
        task_type: str,
        task_input: Dict[str, Any],
        request_id: Optional[str] = None
    ) -> Dict[str, Any]:
        """Invoke task on agent via A2A Protocol"""
        if not request_id:
            import uuid
            request_id = str(uuid.uuid4())
        
        payload = {
            "jsonrpc": self.protocol_version,
            "method": "invokeTask",
            "params": {
                "taskType": task_type,
                "input": task_input
            },
            "id": request_id
        }
        
        try:
            response = requests.post(
                f"{agent_endpoint}{A2A_ENDPOINT_PATH}",
                json=payload,
                headers={"Content-Type": "application/json"},
                timeout=60
            )
            response.raise_for_status()
            return response.json()
        except requests.exceptions.RequestException as e:
            return {
                "jsonrpc": self.protocol_version,
                "error": {
                    "code": -32603,
                    "message": f"Request failed: {str(e)}"
                },
                "id": request_id
            }
    
    def get_agent_card(self, agent_endpoint: str) -> Dict[str, Any]:
        """Get Agent Card from agent endpoint"""
        payload = {
            "jsonrpc": self.protocol_version,
            "method": "getAgentCard",
            "params": {},
            "id": "1"
        }
        
        try:
            response = requests.post(
                f"{agent_endpoint}{A2A_ENDPOINT_PATH}",
                json=payload,
                headers={"Content-Type": "application/json"},
                timeout=10
            )
            response.raise_for_status()
            result = response.json()
            if "result" in result:
                return result["result"]
            return result
        except requests.exceptions.RequestException as e:
            print(f"⚠️  Error getting agent card: {e}")
            return {}
    
    def query_capabilities(self, agent_endpoint: str) -> Dict[str, Any]:
        """Query agent capabilities"""
        payload = {
            "jsonrpc": self.protocol_version,
            "method": "queryCapabilities",
            "params": {},
            "id": "1"
        }
        
        try:
            response = requests.post(
                f"{agent_endpoint}{A2A_ENDPOINT_PATH}",
                json=payload,
                headers={"Content-Type": "application/json"},
                timeout=10
            )
            response.raise_for_status()
            result = response.json()
            if "result" in result:
                return result["result"]
            return result
        except requests.exceptions.RequestException as e:
            print(f"⚠️  Error querying capabilities: {e}")
            return {}
    
    def discover_agents(
        self,
        agent_endpoints: List[str],
        capability_filter: Optional[str] = None
    ) -> List[Dict[str, Any]]:
        """Discover agents from list of endpoints"""
        discovered_agents = []
        
        for endpoint in agent_endpoints:
            try:
                agent_card = self.get_agent_card(endpoint)
                if agent_card:
                    # Filter by capability if specified
                    if capability_filter:
                        capabilities = agent_card.get("capabilities", [])
                        if not any(
                            cap.get("name") == capability_filter 
                            for cap in capabilities
                        ):
                            continue
                    
                    discovered_agents.append(agent_card)
            except Exception as e:
                print(f"⚠️  Error discovering agent at {endpoint}: {e}")
                continue
        
        return discovered_agents

