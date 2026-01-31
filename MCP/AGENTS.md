# MCP – Agent instructions

When the user asks you to **create an NFT**, **mint an NFT**, or **generate an image and mint** (including with Glif):

1. **Call the MCP tool `oasis_create_nft`.** You can call it with no args or partial args:
   - If the user didn't provide everything, call it with what you have (or with no args). The tool returns `interactive: true`, `missingFields`, and `prompts` — then **ask the user** for each missing field using those prompts, and call `oasis_create_nft` again with the collected inputs.
   - **Required (prompt user if missing):** `username`, `password`, `imagePrompt`, `symbol`
   - **Optional:** `title`, `description`, `numberToMint`, `price`, `workflowId`

   Once all required fields are provided, one call runs: authenticate → generate image with Glif → mint NFT.

2. If the user needs **curl only** (no MCP), read [AGENT_NFT_MINT_WORKFLOW.md](./AGENT_NFT_MINT_WORKFLOW.md) for exact curl commands and response paths.
