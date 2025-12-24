"""
Image Generator Agent for MNEE Hackathon Submission
Generates images based on text descriptions
"""

from src.agents.base_agent import BaseAgent
from typing import Dict, Any
import random
import base64


class ImageGeneratorAgent(BaseAgent):
    """Agent that generates images"""
    
    def __init__(self, oasis_api_url: str = None):
        capabilities = [
            {
                "name": "generateImage",
                "description": "Generates images based on text descriptions",
                "pricing": "0.05 SOL",  # Using SOL for testing
                "inputSchema": {
                    "type": "object",
                    "properties": {
                        "prompt": {"type": "string", "description": "Image description"},
                        "style": {"type": "string", "description": "Art style (realistic, cartoon, abstract)"},
                        "size": {"type": "string", "description": "Image size (256x256, 512x512, 1024x1024)"}
                    },
                    "required": ["prompt"]
                },
                "outputSchema": {
                    "type": "object",
                    "properties": {
                        "image_url": {"type": "string"},
                        "image_data": {"type": "string", "description": "Base64 encoded image"},
                        "metadata": {"type": "object"}
                    }
                }
            }
        ]
        
        super().__init__(
            name="Image Generator Agent",
            capabilities=capabilities,
            oasis_api_url=oasis_api_url
        )
    
    def execute_task(self, task_type: str, task_input: Dict[str, Any]) -> Dict[str, Any]:
        """Execute image generation task"""
        if task_type != "generateImage":
            return {
                "error": f"Unknown task type: {task_type}",
                "supported_tasks": ["generateImage"]
            }
        
        prompt = task_input.get("prompt", "A beautiful landscape")
        style = task_input.get("style", "realistic")
        size = task_input.get("size", "512x512")
        
        # Simulate image generation
        # In production, this would call an image generation API (DALL-E, Stable Diffusion, etc.)
        
        # Generate a simple placeholder image (1x1 pixel PNG in base64)
        # In production, this would be the actual generated image
        placeholder_image = base64.b64encode(
            b'\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR\x00\x00\x00\x01\x00\x00\x00\x01\x08\x06\x00\x00\x00\x1f\x15\xc4\x89\x00\x00\x00\nIDATx\x9cc\x00\x01\x00\x00\x05\x00\x01\r\n-\xdb\x00\x00\x00\x00IEND\xaeB`\x82'
        ).decode('utf-8')
        
        # Generate metadata
        metadata = {
            "prompt": prompt,
            "style": style,
            "size": size,
            "generation_time": "2.3s",
            "model": "simulated-v1.0",
            "seed": random.randint(1000, 9999)
        }
        
        return {
            "image_url": f"https://example.com/generated/{random.randint(1000, 9999)}.png",
            "image_data": placeholder_image,
            "metadata": metadata,
            "prompt": prompt,
            "style": style,
            "size": size
        }

