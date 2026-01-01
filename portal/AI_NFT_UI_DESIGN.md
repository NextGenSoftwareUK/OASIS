# AI NFT Assistant - UI Design & Implementation Guide

## Recommended UI Design

### Option 1: Mode Toggle (⭐ Recommended)

Add a toggle at the top of the NFT Mint Studio to switch between "Form Mode" and "AI Mode":

```
┌─────────────────────────────────────────────────────────────┐
│  NFT Mint Studio                                            │
│  ┌────────────────────────────────────────────────────┐     │
│  │ [📝 Form Mode] [🤖 AI Mode]  ← Toggle buttons     │     │
│  └────────────────────────────────────────────────────┘     │
│                                                              │
│  [Content switches based on selected mode]                  │
└─────────────────────────────────────────────────────────────┘
```

**Pros:**
- ✅ Clean integration with existing UI
- ✅ Easy to switch between methods
- ✅ Doesn't clutter the interface
- ✅ Natural workflow

### Option 2: New Wizard Step

Add "AI Assistant" as the first step in the wizard:

```
Wizard Steps:
1. AI Assistant (NEW) ← Option to use AI
2. Select Chain
3. Authenticate & Providers
4. Assets & Metadata
5. x402 Revenue Sharing
6. Review & Mint
```

**Pros:**
- ✅ Fits existing wizard pattern
- ✅ Can skip if user prefers form

**Cons:**
- ⚠️ Adds extra step
- ⚠️ Might feel redundant

### Option 3: Button/Modal Overlay

Add an "Use AI Assistant" button that opens a modal:

```
┌─────────────────────────────────────────┐
│  NFT Mint Studio                        │
│  [Use AI Assistant] ← Button            │
│                                         │
│  [Existing form wizard]                 │
└─────────────────────────────────────────┘

         ↓ Click button

┌─────────────────────────────────────────┐
│  🤖 AI NFT Assistant      [X]           │
│  ┌───────────────────────────────────┐  │
│  │ [Chat interface]                  │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

## Visual Design Specifications

### Color Scheme
- Primary: Portal's existing color scheme
- AI accent: Purple/Blue gradient (`rgba(99, 102, 241, 1)` to `rgba(147, 51, 234, 1)`)
- Success: Green (`rgba(34, 197, 94, 1)`)
- Error: Red (`rgba(239, 68, 68, 1)`)

### Layout
- **Container**: Max-width 800px, centered
- **Chat Interface**: Scrollable message area, fixed input at bottom
- **Messages**: User messages (right-aligned, blue), AI messages (left-aligned, gray)
- **Preview**: Card layout showing parsed NFT details

### Typography
- **Headings**: Inter, 1.25rem, weight 500
- **Body**: Inter, 0.875rem, weight 400
- **Labels**: Inter, 0.75rem, weight 400, uppercase with letter-spacing

### Interactive Elements
- **Input Field**: Multi-line textarea, rounded corners, subtle border
- **Send Button**: Gradient background, rounded, hover effects
- **Example Buttons**: Subtle background, border, hover effects
- **Create Button**: Prominent, green gradient when ready

## Implementation Steps

### Step 1: Add Mode Toggle to NFT Mint Studio

Modify `nft-mint-studio.js` to add a mode state and toggle UI:

```javascript
// Add to nftMintStudioState
const nftMintStudioState = {
    // ... existing state ...
    mode: 'form', // 'form' or 'ai'
};

// Add toggle UI in renderNFTMintStudio()
function renderNFTMintStudio(container) {
    container.innerHTML = `
        <!-- Mode Toggle -->
        <div class="mode-toggle" style="...">
            <button onclick="switchNFTMode('form')">📝 Form Mode</button>
            <button onclick="switchNFTMode('ai')">🤖 AI Mode</button>
        </div>
        
        <!-- Content based on mode -->
        ${nftMintStudioState.mode === 'ai' 
            ? renderAINFTAssistant() 
            : renderExistingWizard()}
    `;
}
```

### Step 2: Integrate AI Assistant Component

Load the AI assistant when AI mode is active:

```javascript
function switchNFTMode(mode) {
    nftMintStudioState.mode = mode;
    const container = document.getElementById('nft-mint-studio-content');
    if (container) {
        renderNFTMintStudio(container);
        if (mode === 'ai') {
            initAINFTAssistant();
        }
    }
}
```

### Step 3: Add Backend Endpoint (or use existing)

Create an endpoint to handle AI parsing, or integrate with STAR API if available.

### Step 4: Add Styling

Add CSS styles to match portal design (already included in inline styles in `ai-nft-assistant.js`).

## File Structure

```
portal/
├── nft-mint-studio.js (modify - add mode toggle)
├── ai-nft-assistant.js (new - AI functionality)
├── portal.html (add script tag for ai-nft-assistant.js)
└── styles.css (optional - add any additional styles)
```

## Next Steps

1. ✅ Review this design proposal
2. ⏭️ Modify `nft-mint-studio.js` to add mode toggle
3. ⏭️ Integrate `ai-nft-assistant.js` 
4. ⏭️ Create backend endpoint for AI parsing (or use client-side)
5. ⏭️ Test the integration
6. ⏭️ Add styling refinements
7. ⏭️ Add error handling and edge cases

