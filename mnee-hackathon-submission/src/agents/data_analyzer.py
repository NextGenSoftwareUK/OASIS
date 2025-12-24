"""
Data Analyzer Agent for MNEE Hackathon Submission
Analyzes market data and provides insights
"""

from src.agents.base_agent import BaseAgent
from typing import Dict, Any
import random


class DataAnalyzerAgent(BaseAgent):
    """Agent that analyzes market data"""
    
    def __init__(self, oasis_api_url: str = None):
        capabilities = [
            {
                "name": "analyzeMarketData",
                "description": "Analyzes cryptocurrency market data and provides insights",
                "pricing": "0.01 SOL",  # Using SOL for testing
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "symbol": {"type": "string", "description": "Trading pair (e.g., BTC/USD)"},
                        "timeframe": {"type": "string", "description": "Timeframe (1h, 24h, 7d)"}
                    },
                    "required": ["symbol"]
                },
                "outputSchema": {
                    "type": "object",
                    "properties": {
                        "analysis": {"type": "string"},
                        "confidence": {"type": "number"},
                        "recommendation": {"type": "string"}
                    }
                }
            }
        ]
        
        super().__init__(
            name="Data Analyzer Agent",
            capabilities=capabilities,
            oasis_api_url=oasis_api_url
        )
    
    def execute_task(self, task_type: str, task_input: Dict[str, Any]) -> Dict[str, Any]:
        """Execute market data analysis task"""
        if task_type != "analyzeMarketData":
            return {
                "error": f"Unknown task type: {task_type}",
                "supported_tasks": ["analyzeMarketData"]
            }
        
        symbol = task_input.get("symbol", "BTC/USD")
        timeframe = task_input.get("timeframe", "24h")
        
        # Simulate market analysis
        # In production, this would call real market data APIs
        price_change = random.uniform(-5, 5)  # Simulated price change %
        volume = random.uniform(1000000, 10000000)  # Simulated volume
        
        if price_change > 0:
            recommendation = "BUY"
            confidence = min(0.9, 0.5 + (price_change / 10))
        else:
            recommendation = "SELL"
            confidence = min(0.9, 0.5 + (abs(price_change) / 10))
        
        analysis = f"""
Market Analysis for {symbol} ({timeframe}):

Current Price: ${random.uniform(30000, 50000):.2f}
Price Change: {price_change:+.2f}%
Volume: ${volume:,.0f}
Trend: {'Bullish' if price_change > 0 else 'Bearish'}

Technical Indicators:
- RSI: {random.uniform(30, 70):.1f}
- MACD: {'Positive' if price_change > 0 else 'Negative'}
- Support Level: ${random.uniform(25000, 30000):.2f}
- Resistance Level: ${random.uniform(50000, 60000):.2f}

Recommendation: {recommendation}
Confidence: {confidence:.1%}
        """.strip()
        
        return {
            "symbol": symbol,
            "timeframe": timeframe,
            "analysis": analysis,
            "confidence": confidence,
            "recommendation": recommendation,
            "price_change": price_change,
            "volume": volume
        }

