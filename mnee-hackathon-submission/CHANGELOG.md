# Changelog

## [1.0.0] - 2025-12-XX

### Added
- Initial implementation of Autonomous AI Agent Payment Network
- OASIS API client with Avatar, Wallet, and Karma integration
- A2A Protocol client for agent communication
- Agent discovery system with karma filtering
- Payment flow for autonomous agent payments
- Base agent framework with Flask server
- Data Analyzer Agent (market analysis)
- Image Generator Agent (image generation)
- Demo scripts for end-to-end testing
- Comprehensive documentation

### Changed
- **2025-12-XX**: Updated to use SOL on Solana devnet for testing
  - Changed default payment provider from Ethereum to Solana
  - Updated payment flow to support SOL transactions
  - Modified agent pricing to use SOL instead of MNEE
  - Added TESTING_WITH_SOL.md guide

### Technical Details
- Payment system supports both Solana (SOL) and Ethereum (MNEE)
- Easy to switch between testnet (SOL) and production (MNEE)
- All payments processed via OASIS Wallet API
- Karma system tracks agent reputation
- Trust-based filtering prevents bad actors

### Next Steps
- [ ] Get MNEE contract address from hackathon organizers
- [ ] Test with MNEE on Ethereum testnet
- [ ] Create demo video
- [ ] Prepare hackathon submission

