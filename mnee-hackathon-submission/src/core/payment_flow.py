"""
Payment Flow for MNEE Hackathon Submission
Handles autonomous payment negotiation, task execution, and payment processing
"""

from typing import Dict, Any, Optional
from src.core.oasis_client import OASISClient
from src.core.a2a_client import A2AClient
from config import PAYMENT_TIMEOUT_SECONDS, PAYMENT_TOKEN_SYMBOL
import time


class PaymentFlow:
    """Autonomous payment flow for agent-to-agent transactions"""
    
    def __init__(self, oasis_client: OASISClient, a2a_client: A2AClient):
        self.oasis = oasis_client
        self.a2a = a2a_client
    
    def execute_and_pay(
        self,
        requester_avatar_id: str,
        provider_agent: Dict[str, Any],
        task: Dict[str, Any],
        payment_amount: str,
        verify_completion: bool = True
    ) -> Dict[str, Any]:
        """
        Execute task and process payment
        
        Args:
            requester_avatar_id: Avatar ID of agent requesting service
            provider_agent: Agent Card of service provider
            task: Task definition
            payment_amount: Amount to pay in MNEE (e.g., "0.01")
            verify_completion: Whether to verify task completion before payment
        
        Returns:
            Result containing task result and payment information
        """
        provider_avatar_id = provider_agent.get("metadata", {}).get("oasis", {}).get("avatarId")
        agent_endpoint = provider_agent.get("endpoint")
        task_type = task.get("type", "unknown")
        
        if not provider_avatar_id or not agent_endpoint:
            return {
                "success": False,
                "error": "Invalid agent card: missing avatar ID or endpoint"
            }
        
        print(f"🔄 Executing task: {task_type}")
        print(f"   Provider: {provider_agent.get('name')} ({provider_avatar_id})")
        print(f"   Payment: {payment_amount} {PAYMENT_TOKEN_SYMBOL}")
        
        # Step 1: Execute task via A2A
        task_result = self.a2a.invoke_task(
            agent_endpoint=agent_endpoint,
            task_type=task_type,
            task_input=task.get("input", {})
        )
        
        if "error" in task_result:
            return {
                "success": False,
                "error": task_result["error"].get("message", "Task execution failed"),
                "task_result": task_result
            }
        
        # Extract result from A2A response
        result_data = task_result.get("result", {})
        task_status = result_data.get("status", "unknown")
        
        # Step 2: Verify completion if required
        if verify_completion and task_status != "completed":
            return {
                "success": False,
                "error": f"Task not completed: status = {task_status}",
                "task_result": result_data
            }
        
        # Step 3: Process payment
        print(f"💳 Processing payment: {payment_amount} {PAYMENT_TOKEN_SYMBOL}")
        # Get wallet address from agent card if available
        provider_wallet = provider_agent.get("metadata", {}).get("oasis", {}).get("walletAddress")
        
        payment_result = self.oasis.send_payment(
            to_avatar_id=provider_avatar_id,
            amount=payment_amount,
            description=f"Payment for {task_type} task",
            to_wallet_address=provider_wallet
        )
        
        if payment_result.get("isError"):
            return {
                "success": False,
                "error": payment_result.get("message", "Payment failed"),
                "task_result": result_data,
                "payment_result": payment_result
            }
        
        # Step 4: Update karma for both parties
        print(f"⭐ Updating karma for successful transaction")
        
        # Provider earns karma for completing task
        provider_oasis = OASISClient(self.oasis.base_url)
        provider_oasis.token = self.oasis.token  # Use same token (in production, provider would have own token)
        provider_oasis.avatar_id = provider_avatar_id
        provider_oasis.add_karma(
            karma_type="Helpful",
            source_title=f"Completed {task_type} task",
            source_desc=f"Successfully completed task and received {payment_amount} {PAYMENT_TOKEN_SYMBOL}"
        )
        
        # Requester earns karma for successful transaction
        requester_oasis = OASISClient(self.oasis.base_url)
        requester_oasis.token = self.oasis.token
        requester_oasis.avatar_id = requester_avatar_id
        requester_oasis.add_karma(
            karma_type="Helpful",
            source_title=f"Paid for {task_type} task",
            source_desc=f"Successfully paid {payment_amount} {PAYMENT_TOKEN_SYMBOL} for service"
        )
        
        return {
            "success": True,
            "task_result": result_data,
            "payment_result": payment_result,
            "karma_updated": True
        }
    
    def negotiate_payment(
        self,
        provider_agent: Dict[str, Any],
        capability: str,
        max_price: str
    ) -> Optional[str]:
        """
        Negotiate payment amount with agent
        
        Args:
            provider_agent: Agent Card of service provider
            capability: Capability name
            max_price: Maximum price willing to pay
        
        Returns:
            Negotiated price or None if negotiation fails
        """
        capabilities = provider_agent.get("capabilities", [])
        for cap in capabilities:
            if cap.get("name") == capability:
                pricing = cap.get("pricing", "")
                if pricing:
                    # Extract numeric value
                    try:
                        cap_price = float(pricing.split()[0])
                        max_price_val = float(max_price.split()[0])
                        # Accept if within budget
                        if cap_price <= max_price_val:
                            return pricing
                    except:
                        pass
        return None

