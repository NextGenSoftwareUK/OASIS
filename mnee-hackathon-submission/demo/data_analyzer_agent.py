#!/usr/bin/env python3
"""
Demo: Data Analyzer Agent
Starts a data analyzer agent server that can receive tasks via A2A Protocol
"""

import sys
import os

# Add parent directory to path
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from src.agents.data_analyzer import DataAnalyzerAgent
from config import OASIS_API_URL, AGENT_DISCOVERY_PORT

def main():
    """Start Data Analyzer Agent"""
    print("=" * 60)
    print("Data Analyzer Agent - MNEE Hackathon Submission")
    print("=" * 60)
    print()
    
    # Create agent
    agent = DataAnalyzerAgent(oasis_api_url=OASIS_API_URL)
    
    # Register with OASIS (email and password auto-generated)
    print("🔐 Registering with OASIS...")
    username = f"data_analyzer_{os.getpid()}"
    
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
    agent.run(port=AGENT_DISCOVERY_PORT)

if __name__ == "__main__":
    main()

