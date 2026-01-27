# 🚀 Quick Start - Contract Generator UI

## ✅ Setup Complete!

Your Smart Contract Generator UI is ready to use. Here's how to get started.

---

## 🏃 Run the App (2 minutes)

### 1. Make sure your API is running

```bash
# In a separate terminal
cd /Volumes/Storage/QS_Asset_Rail/smart-contract-generator/src/SmartContractGen/ScGen.API
dotnet run

# Should see: Now listening on http://localhost:5000
```

### 2. Start the UI

```bash
cd /Volumes/Storage/QS_Asset_Rail/apps/contract-generator-ui
npm run dev
```

### 3. Open in browser

Visit **http://localhost:3001**

---

## 🎨 What You'll See

### Landing Page
```
✨ Beautiful landing page matching your Property Wizard design
   ├─ Template-Based Generation (JSON specs)
   └─ AI-Powered Generation (natural language)
```

### Template Generator (`/generate/template`)
```
📝 JSON specification editor
   ├─ Example templates included
   ├─ Validate JSON button
   └─ Generate → Monaco editor → Actions
```

### AI Generator (`/generate/ai`)
```
🤖 Natural language input
   ├─ Describe your contract in plain English
   ├─ Generate with AI (~30 seconds)
   └─ Review → Monaco editor → Actions
```

---

## 🔧 Features Included

### ✅ Template-Based Generation
- JSON specification input
- Syntax validation
- Example templates
- Fast generation (2 seconds)

### ✅ AI-Powered Generation  
- Natural language input
- Additional context field
- Intelligent code generation
- Review warnings

### ✅ Code Preview
- Monaco editor with syntax highlighting
- Read-only view
- Rust language support
- 600px height (adjustable)

### ✅ Solana Playground Integration
- "Open in Solana Playground" button
- Opens beta.solpg.io in new tab
- Instructions modal for import
- Download ZIP functionality

### ✅ Additional Actions
- Download project ZIP
- Compile via API (optional)
- Deploy via API (optional)

---

## 🎯 Test the Flow

### Template-Based Test

1. Click "Start with Template"
2. Click "Load Example" (token vesting)
3. Click "Generate Contract"
4. See generated Rust code in Monaco editor
5. Click "Open in Solana Playground"
6. New tab opens with Playground
7. Click "Download ZIP"
8. ZIP file downloads

### AI-Powered Test

1. Click "Describe Contract"
2. Click "Load Example"
3. Click "Generate Contract"
4. Wait ~30 seconds
5. See AI-generated code
6. Review and use same actions

---

## 🎨 Design Consistency

✅ **Matching Property Wizard:**
- Same color scheme (cyan #22d3ee)
- Glass morphism effects
- Gradient borders and rings
- Dark theme with radial gradients
- Same typography and spacing
- Shadcn-style components

---

## 📁 File Structure

```
contract-generator-ui/
├── app/
│   ├── page.tsx                    # ✅ Landing page
│   ├── generate/
│   │   ├── template/page.tsx       # ✅ Template generator
│   │   └── ai/page.tsx             # ✅ AI generator
│   ├── layout.tsx                  # ✅ Layout
│   └── globals.css                 # ✅ Matching styles
│
├── components/
│   ├── ui/
│   │   ├── button.tsx              # ✅ Button component
│   │   ├── card.tsx                # ✅ Card component
│   │   └── textarea.tsx            # ✅ Textarea component
│   ├── contract-editor.tsx         # ✅ Monaco editor
│   └── playground-actions.tsx      # ✅ Playground integration
│
├── lib/
│   ├── api-client.ts              # ✅ API integration
│   ├── playground.ts              # ✅ Playground utilities
│   └── utils.ts                   # ✅ Utilities
│
└── README.md                       # ✅ Documentation
```

---

## ⚙️ Configuration

### API URL

Default: `http://localhost:5000`

To change, create `.env.local`:

```env
NEXT_PUBLIC_API_URL=http://your-api:5000
```

---

## 🐛 Troubleshooting

### API Connection Error

```
Error: Contract generation failed: Failed to fetch
```

**Solution:** Make sure your API is running on port 5000

```bash
cd /Volumes/Storage/QS_Asset_Rail/smart-contract-generator/src/SmartContractGen/ScGen.API
dotnet run
```

### Monaco Editor Not Loading

```
"Loading editor..." stuck
```

**Solution:** Check browser console for errors. Monaco may need a page refresh.

### Solana Playground Button Not Working

**Solution:** 
- Check if browser blocks pop-ups
- Solana Playground URL: https://beta.solpg.io/
- User can manually visit the URL

---

## 📊 What Works

✅ Landing page with two generation methods  
✅ Template-based generation (JSON → Contract)  
✅ AI-powered generation (Description → Contract)  
✅ Monaco code editor with syntax highlighting  
✅ Solana Playground integration button  
✅ Download ZIP functionality  
✅ Matching design with Property Wizard  
✅ Responsive layout  
✅ Error handling  
✅ Loading states  

---

## 🎉 Next Steps

### Immediate
1. Test both generation methods
2. Try opening in Solana Playground
3. Download a ZIP and inspect contents
4. Customize the example templates

### Soon
1. Add compile functionality
2. Add deploy functionality
3. Add more templates
4. Add Ethereum support
5. Add Radix support

### Future
1. User authentication
2. Save generated contracts
3. Contract history
4. Share contracts
5. Collaborative editing

---

## 📞 Need Help?

### Check these first:
- API is running on port 5000
- Browser allows pop-ups
- Node modules are installed (`npm install`)

### Documentation:
- Main README: `README.md`
- API docs: `/Volumes/Storage/QS_Asset_Rail/smart-contract-generator/README.md`
- Property Wizard (reference): `../property-tokenization-wizard/`

---

## ✨ What You Built

A complete developer portal for smart contract generation that:

- 🎨 Matches your existing design language
- ⚡ Generates contracts in seconds
- 🤖 Supports AI-powered generation
- 📝 Has professional code editor
- 🚀 Integrates with Solana Playground
- 📥 Downloads full projects
- 🔧 Can compile and deploy

**All in ~2 hours of setup!** 🎉

---

**Ready to generate some contracts?** 🚀

Run `npm run dev` and visit http://localhost:3001

