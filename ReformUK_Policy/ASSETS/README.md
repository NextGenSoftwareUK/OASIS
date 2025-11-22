# Reform UK × OASIS One-Pager - Setup Instructions

## 🎯 What's Included

- **`reform_uk_oasis_one_pager.tsx`** - Complete React/TypeScript component
- **`Logo_of_the_Reform_UK.svg.png`** - Reform UK logo
- **`reform_uk_web3_advisor.tsx`** - Original advisor application component

## 🚀 Quick Start (Option 1: Use Existing cv-template)

The easiest way is to add this to your existing `cv-template` setup:

```bash
cd /Volumes/Storage/OASIS_CLEAN/cv-template
```

### Update `main.tsx`:

```typescript
import ReformUKOASISOnePager from './reform_uk_oasis_one_pager'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ReformUKOASISOnePager />
  </React.StrictMode>,
)
```

### Copy the component:

```bash
cp ../ReformUK_Policy/ASSETS/reform_uk_oasis_one_pager.tsx ./
```

### Run it:

```bash
npm run dev
```

Open `http://localhost:5173` and click "Download PDF" to export.

---

## 🚀 Quick Start (Option 2: Standalone Vite Project)

Create a new Vite + React + TypeScript project:

```bash
cd /Volumes/Storage/OASIS_CLEAN/ReformUK_Policy/ASSETS
npm create vite@latest reform-uk-onepager -- --template react-ts
cd reform-uk-onepager
npm install
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
```

### Configure Tailwind (`tailwind.config.js`):

```javascript
/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}
```

### Update `src/index.css`:

```css
@tailwind base;
@tailwind components;
@tailwind utilities;
```

### Copy files:

```bash
cp ../reform_uk_oasis_one_pager.tsx ./src/
cp ../Logo_of_the_Reform_UK.svg.png ./public/
```

### Update `src/App.tsx`:

```typescript
import ReformUKOASISOnePager from './reform_uk_oasis_one_pager'

function App() {
  return <ReformUKOASISOnePager />
}

export default App
```

### Run it:

```bash
npm run dev
```

---

## 🚀 Quick Start (Option 3: Simple HTML File)

For a quick preview without setup, you can use the standalone HTML approach (though React needs to be included via CDN):

1. Open `index.html` in a modern browser
2. The component will render (simplified version)
3. For full functionality, use Option 1 or 2

---

## 📄 Exporting to PDF

### Method 1: Browser Print (Recommended)

1. Open the page in your browser
2. Click the "Download PDF" button (top right)
3. Or press `Cmd+P` (Mac) / `Ctrl+P` (Windows)
4. Choose "Save as PDF"
5. Adjust margins to "None" for best results

### Method 2: Programmatic Export (Advanced)

Install additional dependencies:

```bash
npm install jspdf html2canvas
```

Then modify the `downloadPDF` function in the component to use these libraries for better PDF generation.

---

## 🎨 Customization

The component data is stored in the `onePager` object at the top of `reform_uk_oasis_one_pager.tsx`. You can easily modify:

- **Savings figures** - Update `savingsBreakdown` array
- **Pledges** - Add/remove items in `corePledges` array
- **Roadmap** - Modify `roadmap` array
- **Contact info** - Update `contact` object
- **Styling** - All Tailwind CSS classes can be adjusted

---

## 📸 Screenshots

The rendered one-pager includes:

✅ Reform UK branding with logo  
✅ All 5 core pledges with OASIS solutions  
✅ £150bn savings breakdown table  
✅ CBDC opposition section  
✅ 3-phase implementation roadmap  
✅ ROI analysis (25-50x → 600-1,700x)  
✅ "Why OASIS" section  
✅ Contact information  

---

## 🔗 Related Files

- **Markdown version**: `../REFORM_UK_OASIS_ONE_PAGER.md`
- **Detailed docs**: See all numbered documents in `ReformUK_Policy/` folder
- **Original advisor app**: `reform_uk_web3_advisor.tsx`

---

## 🆘 Troubleshooting

**Issue**: TypeScript errors  
**Solution**: Ensure you have `@types/react` and `@types/react-dom` installed

**Issue**: Tailwind not working  
**Solution**: Make sure `tailwind.config.js` content paths are correct

**Issue**: Logo not showing  
**Solution**: Ensure `Logo_of_the_Reform_UK.svg.png` is in the correct path (adjust src in component)

**Issue**: Print layout broken  
**Solution**: Use `print:` Tailwind modifiers, adjust print CSS in component

---

## 📞 Support

**Max Gershfield**  
Email: max.gershfield1@gmail.com  
Twitter: @maxgershfield

---

**Last Updated**: November 2025  
**Powered by**: OASIS Web4 Infrastructure + React + Tailwind CSS





