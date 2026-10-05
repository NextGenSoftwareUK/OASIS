"""
Base Agent Class for MNEE Hackathon Submission
Provides foundation for all agents with OASIS and A2A integration
"""

from flask import Flask, request, jsonify
from typing import Dict, Any, List, Optional
from src.core.oasis_client import OASISClient
from src.core.a2a_client import A2AClient
from config import AGENT_DISCOVERY_PORT, AGENT_DISCOVERY_HOST, A2A_ENDPOINT_PATH
import uuid


class BaseAgent:
    """Base class for all agents in the payment network"""
    
    def __init__(
        self,
        name: str,
        capabilities: List[Dict[str, Any]],
        oasis_api_url: Optional[str] = None
    ):
        self.name = name
        self.capabilities = capabilities
        self.oasis = OASISClient(oasis_api_url)
        self.a2a = A2AClient()
        self.app = Flask(__name__)
        self.agent_card: Optional[Dict[str, Any]] = None
        self.setup_routes()
    
    def setup_routes(self):
        """Setup Flask routes for A2A Protocol"""
        
        @self.app.route(A2A_ENDPOINT_PATH, methods=["POST"])
        def handle_a2a():
            """Handle A2A JSON-RPC 2.0 requests"""
            try:
                data = request.json
                
                # Validate JSON-RPC 2.0 format
                if data.get("jsonrpc") != "2.0":
                    return jsonify({
                        "jsonrpc": "2.0",
                        "error": {"code": -32600, "message": "Invalid Request"},
                        "id": data.get("id")
                    }), 400
                
                method = data.get("method")
                params = data.get("params", {})
                request_id = data.get("id")
                
                # Route to method handler
                if method == "getAgentCard":
                    result = self.get_agent_card_handler()
                elif method == "invokeTask":
                    result = self.invoke_task_handler(params)
                elif method == "queryCapabilities":
                    result = self.query_capabilities_handler()
                else:
                    return jsonify({
                        "jsonrpc": "2.0",
                        "error": {"code": -32601, "message": "Method not found"},
                        "id": request_id
                    }), 404
                
                return jsonify({
                    "jsonrpc": "2.0",
                    "result": result,
                    "id": request_id
                })
            
            except Exception as e:
                return jsonify({
                    "jsonrpc": "2.0",
                    "error": {"code": -32603, "message": str(e)},
                    "id": data.get("id")
                }), 500
        
        @self.app.route("/agent-card", methods=["GET"])
        def get_agent_card_http():
            """HTTP endpoint for Agent Card"""
            return jsonify(self.get_agent_card_handler())
        
        @self.app.route("/health", methods=["GET"])
        def health():
            """Health check endpoint"""
            return jsonify({"status": "healthy", "agent": self.name})
    
    def register(self, username: str, email: Optional[str] = None, password: Optional[str] = None):
        """
        Register agent with OASIS and generate Agent Card
        
        Args:
            username: Username for the agent
            email: Email (auto-generated if None)
            password: Password (auto-generated if None)
        """
        # Use convenience method that handles registration or authentication
        result = self.oasis.register_or_authenticate_agent(username, password)
        
        if not result.get("isError"):
            # Generate wallet
            self.oasis.generate_wallet()
            
            # Get karma stats
            karma_result = self.oasis.get_karma_stats()
            karma_score = 0
            if karma_result.get("result"):
                karma_score = karma_result["result"].get("total", 0)
            
            # Create Agent Card
            self.agent_card = self.a2a.create_agent_card(
                avatar_id=self.oasis.avatar_id,
                name=self.name,
                capabilities=self.capabilities,
                endpoint=f"http://{AGENT_DISCOVERY_HOST}:{AGENT_DISCOVERY_PORT}",
                wallet_address=self.oasis.wallet_address,
                karma_score=karma_score
            )
            
            print(f"✅ Agent registered: {self.name}")
            print(f"   Avatar ID: {self.oasis.avatar_id}")
            print(f"   Wallet: {self.oasis.wallet_address}")
            print(f"   Karma: {karma_score}")
        
        return result
    
    def get_agent_card_handler(self) -> Dict[str, Any]:
        """Return Agent Card for A2A Protocol"""
        if not self.agent_card:
            # Generate on-the-fly if not registered
            karma_result = self.oasis.get_karma_stats()
            karma_score = 0
            if karma_result.get("result"):
                karma_score = karma_result["result"].get("total", 0)
            
            self.agent_card = self.a2a.create_agent_card(
                avatar_id=self.oasis.avatar_id or str(uuid.uuid4()),
                name=self.name,
                capabilities=self.capabilities,
                endpoint=f"http://{AGENT_DISCOVERY_HOST}:{AGENT_DISCOVERY_PORT}",
                wallet_address=self.oasis.wallet_address,
                karma_score=karma_score
            )
        
        return self.agent_card
    
    def invoke_task_handler(self, params: Dict[str, Any]) -> Dict[str, Any]:
        """Handle task invocation - to be implemented by subclasses"""
        task_type = params.get("taskType")
        task_input = params.get("input", {})
        
        # Execute task (implemented by subclasses)
        result = self.execute_task(task_type, task_input)
        
        return {
            "status": "completed",
            "result": result
        }
    
    def query_capabilities_handler(self) -> Dict[str, Any]:
        """Return agent capabilities"""
        return {
            "capabilities": self.capabilities
        }
    
    def execute_task(self, task_type: str, task_input: Dict[str, Any]) -> Dict[str, Any]:
        """
        Execute task - to be implemented by subclasses
        
        Args:
            task_type: Type of task to execute
            task_input: Input parameters for task
        
        Returns:
            Task result
        """
        raise NotImplementedError("Subclasses must implement execute_task")
    
    def run(self, port: Optional[int] = None, host: Optional[str] = None):
        """Run agent server"""
        port = port or AGENT_DISCOVERY_PORT
        host = host or AGENT_DISCOVERY_HOST
        
        print(f"🚀 Starting {self.name} agent server...")
        print(f"   Endpoint: http://{host}:{port}{A2A_ENDPOINT_PATH}")
        print(f"   Agent Card: http://{host}:{port}/agent-card")
        
        self.app.run(host=host, port=port, debug=False)

