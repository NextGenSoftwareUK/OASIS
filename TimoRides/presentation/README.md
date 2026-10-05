# Timo Presentation — Elevating Travel for Tourists & Corporates

This folder contains the Marp presentation for Timo's premium mobility platform.

## Quick Start

### Preview the Presentation

If you have Marp CLI installed:
```bash
cd /Volumes/Storage/OASIS_CLEAN/TimoRides/presentation
marp --preview --allow-local-files slides.md
```

### Using VS Code

1. Install the "Marp for VS Code" extension
2. Open `slides.md`
3. Click the preview icon or press `Cmd+Shift+V` (Mac) / `Ctrl+Shift+V` (Windows)

### Export to PDF

```bash
marp --pdf --allow-local-files slides.md -o Timo_Presentation.pdf
```

### Export to HTML

```bash
marp --html --allow-local-files slides.md -o Timo_Presentation.html
```

### Export to PowerPoint

```bash
marp --pptx --allow-local-files slides.md -o Timo_Presentation.pptx
```

## Presentation Structure

The presentation contains 13 slides:

1. **Cover** — Title slide with Timo branding
2. **What Timo Is** — Premium mobility marketplace overview
3. **The Problem** — Pain points for corporates and tourists
4. **The Solution** — Timo's premium experience features
5. **Timo's Unique Advantage** — Differentiators
6. **Traction** — Pilot results from Durban
7. **Unit Economics** — Financial validation
8. **Market Opportunity** — TAM/SAM/SOM analysis
9. **Strategic Evolution** — From rides to intelligent mobility
10. **PathPulse Integration** — Intelligence layer
11. **Second Revenue Engine** — B2G opportunity
12. **Business Model** — Two engines, one platform
13. **Team, Ask & Vision** — Closing slide

## Customization

### Adding Assets

If you want to add logos or images:
1. Create an `assets/` folder in this directory
2. Reference them in slides using: `![alt](./assets/image.png)`

### Modifying Content

Edit `slides.md` directly. Each slide is separated by `---`.

### Changing Theme

The presentation uses the Timo theme from `/Docs/Presentations/TimoRidesDeck/themes/timo.css`. To use a different theme, update the `theme:` line in the frontmatter.

## Installation

If you don't have Marp CLI installed:

```bash
npm install -g @marp-team/marp-cli
```

## Notes

- The presentation uses the existing Timo theme with dark background and blue accent colors
- All slides are designed to be self-contained and readable
- Metrics and statistics are highlighted using the theme's metric cards
- The presentation follows the structure provided in the outline








