#!/usr/bin/env python3
"""
Demo: Image Generator Agent
Starts an image generator agent server that can receive tasks via A2A Protocol
"""

import sys
import os

# Add parent directory to path
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from src.agents.image_generator import ImageGeneratorAgent
from config import OASIS_API_URL, AGENT_DISCOVERY_PORT

def main():
    """Start Image Generator Agent"""
    print("=" * 60)
    print("Image Generator Agent - MNEE Hackathon Submission")
    print("=" * 60)
    print()
    
    # Create agent
    agent = ImageGeneratorAgent(oasis_api_url=OASIS_API_URL)
    
    # Register with OASIS (email and password auto-generated)
    print("🔐 Registering with OASIS...")
    username = f"image_generator_{os.getpid()}"
    
    try:
        # Register agent (email and password will be auto-generated)
        agent.register(username)
        
        # Generate wallet
        if agent.oasis.avatar_id:
            agent.oasis.generate_wallet()
    except Exception as e:
        print(f"❌ Registration error: {e}")
        print("💡 Make sure OASIS API is running and accessible")
        import traceback
        traceback.print_exc()
        return
    
    # Start server
    print()
    print("=" * 60)
    agent.run(port=AGENT_DISCOVERY_PORT + 1)  # Use different port

if __name__ == "__main__":
    main()

