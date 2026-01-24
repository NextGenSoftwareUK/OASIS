# OASIS IDE: Development Tools Brief

**For:** OASIS Development Tools Team  
**Status:** 📋 Implementation Brief  
**Reference:** Master Brief (`OASIS_IDE_MASTER_BRIEF.md`)

---

## 🎯 Objective

Build **specialized OASIS development tools** that make it easy to:
- Create OAPPs visually
- Mint NFTs
- Manage wallets
- Explore holons
- Create missions/quests
- Deploy smart contracts

---

## 📦 Components to Build

### 1. OAPP Builder (Visual)

**Requirements:**
- [ ] Drag-and-drop interface
- [ ] Component library (Zomes, Holons, Templates, etc.)
- [ ] Visual canvas for OAPP design
- [ ] Component properties panel
- [ ] Real-time preview
- [ ] OAPP code generation
- [ ] OAPP deployment
- [ ] OAPP templates

**Reference:**
- STAR OAPP Builder: `/Docs/Devs/STARNET_OAPP_BUILDER_UI_GUIDE.md`
- STAR CLI: `/Docs/Devs/STAR_CLI_DOCUMENTATION.md`

**Implementation:**
```typescript
const OAPPBuilder: React.FC = () => {
  const [components, setComponents] = useState<Component[]>([]);
  const [selectedComponent, setSelectedComponent] = useState<Component | null>(null);
  
  const handleDrop = (component: ComponentDefinition, position: Point) => {
    const newComponent = {
      id: generateId(),
      type: component.type,
      position,
      properties: component.defaultProperties
    };
    setComponents([...components, newComponent]);
  };
  
  const generateOAPP = async () => {
    const oappStructure = {
      name: 'MyOAPP',
      components: components.map(c => ({
        type: c.type,
        properties: c.properties
      }))
    };
    
    // Use STAR API to create OAPP
    const result = await starAPI.createOAPP(oappStructure);
    return result;
  };
  
  return (
    <div className="oapp-builder">
      <ComponentLibrary onDragStart={handleDragStart} />
      <Canvas
        components={components}
        onDrop={handleDrop}
        onSelect={setSelectedComponent}
      />
      <PropertiesPanel component={selectedComponent} />
      <button onClick={generateOAPP}>Generate OAPP</button>
    </div>
  );
};
```

### 2. NFT Minting UI

**Requirements:**
- [ ] Image upload
- [ ] Metadata editor (name, description, attributes)
- [ ] Collection creation
- [ ] Batch minting
- [ ] Cross-chain selection (Solana, Ethereum, etc.)
- [ ] Preview before minting
- [ ] Minting progress
- [ ] NFT gallery

**Implementation:**
```typescript
const NFTMintingUI: React.FC = () => {
  const [nftData, setNftData] = useState<NFTData>({
    name: '',
    description: '',
    image: null,
    attributes: []
  });
  const [blockchain, setBlockchain] = useState('Solana');
  const [minting, setMinting] = useState(false);
  
  const handleMint = async () => {
    setMinting(true);
    
    // Upload image to IPFS if needed
    let imageUrl = nftData.image;
    if (nftData.image instanceof File) {
      const uploadResult = await mcpManager.executeTool('oasis_upload_file', {
        filePath: nftData.image.path
      });
      imageUrl = uploadResult.result;
    }
    
    // Mint NFT
    const result = await mcpManager.executeTool('oasis_mint_nft', {
      Title: nftData.name,
      Description: nftData.description,
      ImageUrl: imageUrl,
      Symbol: nftData.name.toUpperCase().replace(/\s/g, ''),
      OnChainProvider: blockchain + 'OASIS',
      MetaData: {
        attributes: nftData.attributes
      }
    });
    
    setMinting(false);
    showSuccess(`NFT minted! ${result.mintAddress}`);
  };
  
  return (
    <div className="nft-minting">
      <ImageUpload onUpload={setImage} />
      <MetadataEditor data={nftData} onChange={setNftData} />
      <BlockchainSelector value={blockchain} onChange={setBlockchain} />
      <button onClick={handleMint} disabled={minting}>
        {minting ? 'Minting...' : 'Mint NFT'}
      </button>
    </div>
  );
};
```

### 3. Wallet Manager

**Requirements:**
- [ ] Multi-chain wallet display
- [ ] Balance viewing
- [ ] Transaction history
- [ ] Send/receive UI
- [ ] Portfolio value
- [ ] Wallet creation
- [ ] Wallet import

**Implementation:**
```typescript
const WalletManager: React.FC = () => {
  const { wallets, balances, transactions } = useWallets();
  const [selectedWallet, setSelectedWallet] = useState<Wallet | null>(null);
  
  return (
    <div className="wallet-manager">
      <WalletList
        wallets={wallets}
        onSelect={setSelectedWallet}
      />
      {selectedWallet && (
        <>
          <BalanceDisplay
            wallet={selectedWallet}
            balance={balances[selectedWallet.id]}
          />
          <TransactionHistory
            wallet={selectedWallet}
            transactions={transactions[selectedWallet.id]}
          />
          <SendReceivePanel wallet={selectedWallet} />
        </>
      )}
    </div>
  );
};
```

### 4. Holon Explorer

**Requirements:**
- [ ] Visual holon tree
- [ ] Holon relationships graph
- [ ] Holon search
- [ ] Holon editor
- [ ] Holon sharing
- [ ] Holon versioning

**Implementation:**
```typescript
const HolonExplorer: React.FC = () => {
  const { holons, loadHolons } = useHolons();
  const [selectedHolon, setSelectedHolon] = useState<Holon | null>(null);
  
  useEffect(() => {
    loadHolons();
  }, []);
  
  return (
    <div className="holon-explorer">
      <HolonTree
        holons={holons}
        onSelect={setSelectedHolon}
      />
      {selectedHolon && (
        <HolonViewer
          holon={selectedHolon}
          onEdit={handleEdit}
        />
      )}
      <HolonGraph holons={holons} />
    </div>
  );
};
```

### 5. Mission/Quest Creator

**Requirements:**
- [ ] Mission builder UI
- [ ] Quest flow designer
- [ ] Reward configuration
- [ ] Karma integration
- [ ] Objective management
- [ ] Publishing tools

**Implementation:**
```typescript
const MissionCreator: React.FC = () => {
  const [mission, setMission] = useState<Mission>({
    name: '',
    description: '',
    objectives: [],
    rewards: {
      karma: 0,
      nfts: []
    }
  });
  
  const handlePublish = async () => {
    // Use STAR API to create mission
    const result = await starAPI.createMission(mission);
    showSuccess(`Mission published! ${result.id}`);
  };
  
  return (
    <div className="mission-creator">
      <MissionForm mission={mission} onChange={setMission} />
      <ObjectiveEditor
        objectives={mission.objectives}
        onChange={objectives => setMission({...mission, objectives})}
      />
      <RewardConfig
        rewards={mission.rewards}
        onChange={rewards => setMission({...mission, rewards})}
      />
      <button onClick={handlePublish}>Publish Mission</button>
    </div>
  );
};
```

### 6. Smart Contract Deployer

**Requirements:**
- [ ] Contract code editor
- [ ] Contract compilation
- [ ] Deployment configuration
- [ ] Multi-chain deployment
- [ ] Deployment status
- [ ] Contract verification

**Implementation:**
```typescript
const SmartContractDeployer: React.FC = () => {
  const [contractCode, setContractCode] = useState('');
  const [blockchain, setBlockchain] = useState('Solana');
  const [deploying, setDeploying] = useState(false);
  
  const handleDeploy = async () => {
    setDeploying(true);
    
    // Generate contract if needed
    let code = contractCode;
    if (!code) {
      const generated = await mcpManager.executeTool('scgen_generate_contract', {
        spec: contractSpec,
        blockchain: blockchain
      });
      code = generated.code;
    }
    
    // Compile
    const compiled = await mcpManager.executeTool('scgen_compile_contract', {
      code: code,
      blockchain: blockchain
    });
    
    // Deploy
    const deployed = await mcpManager.executeTool('scgen_deploy_contract', {
      contractId: compiled.contractId,
      blockchain: blockchain
    });
    
    setDeploying(false);
    showSuccess(`Deployed to ${deployed.address}`);
  };
  
  return (
    <div className="contract-deployer">
      <CodeEditor value={contractCode} onChange={setContractCode} />
      <BlockchainSelector value={blockchain} onChange={setBlockchain} />
      <button onClick={handleDeploy} disabled={deploying}>
        {deploying ? 'Deploying...' : 'Deploy Contract'}
      </button>
    </div>
  );
};
```

---

## 🔧 Technical Requirements

### Dependencies

```json
{
  "dependencies": {
    "react-dnd": "^16.0.0",
    "react-flow-renderer": "^10.0.0",
    "react-json-view": "^1.21.0"
  }
}
```

### File Structure

```
src/
├── tools/
│   ├── OAPPBuilder/
│   │   ├── OAPPBuilder.tsx
│   │   ├── ComponentLibrary.tsx
│   │   ├── Canvas.tsx
│   │   └── PropertiesPanel.tsx
│   ├── NFTMinting/
│   │   ├── NFTMintingUI.tsx
│   │   ├── ImageUpload.tsx
│   │   └── MetadataEditor.tsx
│   ├── WalletManager/
│   │   ├── WalletManager.tsx
│   │   ├── BalanceDisplay.tsx
│   │   └── TransactionHistory.tsx
│   ├── HolonExplorer/
│   │   ├── HolonExplorer.tsx
│   │   ├── HolonTree.tsx
│   │   └── HolonGraph.tsx
│   ├── MissionCreator/
│   │   └── MissionCreator.tsx
│   └── ContractDeployer/
│       └── ContractDeployer.tsx
```

---

## 🔗 Integration Points

### With MCP Integration
- All tools use MCP tools internally
- OAPP Builder uses STAR API via MCP
- NFT Minting uses OASIS API via MCP

### With Chat Interface
- Chat can trigger tools
- "Create an OAPP called MyGame"
- "Mint 10 NFTs for my collection"

---

## ✅ Acceptance Criteria

- [ ] OAPP Builder creates valid OAPPs
- [ ] NFT Minting successfully mints NFTs
- [ ] Wallet Manager shows correct balances
- [ ] Holon Explorer displays holon relationships
- [ ] Mission Creator publishes missions
- [ ] Contract Deployer deploys contracts

---

## 📚 Resources

- **STAR OAPP Builder:** `/Docs/Devs/STARNET_OAPP_BUILDER_UI_GUIDE.md`
- **STAR API:** `/Docs/Devs/API Documentation/WEB5_STAR_API_Documentation.md`
- **OASIS API:** `/Docs/Devs/API Documentation/WEB4_OASIS_API_Documentation.md`

---

## 🎯 Success Metrics

- OAPP creation time < 5 minutes
- NFT minting success rate > 95%
- Wallet operations complete in < 2 seconds
- Holon loading time < 1 second

---

*This brief covers OASIS development tools. These tools use MCP and APIs that are already built.*
