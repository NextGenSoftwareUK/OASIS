# Asset Kit

This folder holds reusable placeholders for the Timo Rides presentation theme. Swap these files for brand imagery when you have final exports.

- `logo.svg` – temporary logomark used on the title slide.
- `hero-gradient.svg` – soft gradient background referenced by the `.hero-slide` CSS class.
- `map-path.svg` – map-style placeholder for the expansion slide.
- `product-placeholder.svg` – UI/product mock stand-in for the product slide.
- `pathpulse-dashboard.svg` – dashboard graphic for the road-intelligence slide.

### Replacing Assets

1. Export your visuals (PNG/SVG) from Figma.
2. Match the filenames in this directory or update the paths inside `slides.md` / `template.md`.
3. Re-run the Marp preview or export command:
   ```bash
   marp --preview --allow-local-files Docs/Presentations/TimoRidesDeck/slides.md
   ```

Keep all assets under this directory so relative paths stay valid for the Marp CLI.






