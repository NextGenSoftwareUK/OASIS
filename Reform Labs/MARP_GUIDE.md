# Marp Presentation Guide

This guide explains how to create presentations using Marp based on the examples in your codebase.

## What is Marp?

Marp is a Markdown presentation ecosystem that lets you create beautiful slide decks using Markdown syntax. You write your slides in Markdown, and Marp converts them to HTML, PDF, or PowerPoint.

## Basic Structure

A Marp presentation is a Markdown file with a YAML frontmatter header:

```markdown
---
marp: true
theme: path/to/theme.css
paginate: true
title: Your Presentation Title
description: Brief description
---

<!-- Your slides go here -->
```

## Slide Separation

Slides are separated by three dashes (`---`):

```markdown
---
marp: true
theme: Docs/Presentations/TimoRidesDeck/themes/timo.css
paginate: true
---

# Slide 1 Title

Content for slide 1

---

# Slide 2 Title

Content for slide 2
```

## Examples in Your Codebase

You have two excellent examples:

1. **TimoRidesDeck** (`/Docs/Presentations/TimoRidesDeck/`)
   - `slides.md` - Full investor deck
   - `template.md` - Template with common slide patterns
   - `themes/timo.css` - Custom dark theme

2. **RecapDeck** (`/Docs/Presentations/RecapDeck/`)
   - `slides.md` - Another presentation example
   - `themes/recap.css` - Different theme style

## Common Slide Patterns

### Title Slide

```markdown
<!-- class: title-slide -->

<div class="logo-lockup">
  <img src="./assets/logo.svg" width="120" alt="Logo" />
  <span><strong>Company Name</strong></span>
</div>

<div class="accent-bar"></div>

# Main Title

<p class="subtitle">Subtitle or tagline</p>

- Presenter name · Role · Date
```

### Section Divider

```markdown
---
class: section-divider

## Section Title

**Optional subtitle or description**
```

### Agenda Slide

```markdown
## Agenda

<div class="agenda">
  <div class="agenda-item">
    <div class="step">01</div>
    <div>
      <strong>Section Title</strong><br />
      Brief description
    </div>
  </div>
  <div class="agenda-item">
    <div class="step">02</div>
    <div>
      <strong>Another Section</strong><br />
      Description
    </div>
  </div>
</div>
```

### Grid Layouts

```markdown
---
class: grid-2
---

## Two Column Layout

<div class="grid-2">
  <div class="card">
    <h3>Card Title</h3>
    <p>Content here</p>
  </div>
  <div class="card">
    <h3>Another Card</h3>
    <p>More content</p>
  </div>
</div>
```

### Metrics Display

```markdown
## Key Metrics

<div class="grid-2">
  <div class="metric">
    <div class="metric-label">Users</div>
    <div class="metric-value">10K</div>
  </div>
  <div class="metric">
    <div class="metric-label">Revenue</div>
    <div class="metric-value">$350K</div>
  </div>
</div>
```

### Images

```markdown
## Product Overview

![bg right](./assets/product-image.svg)

Or full-width:

![full-bleed](./assets/hero-image.svg)
```

### Tables

```markdown
## Comparison

| Feature | Us | Competitor |
| --- | --- | --- |
| Feature 1 | ✅ | ❌ |
| Feature 2 | ✅ | ⚠️ Limited |
```

## CSS Classes Available

Based on the Timo theme, these classes are available:

- `title-slide` - Title slide styling
- `section-divider` - Section break slides
- `hero-slide` - Hero/feature slide
- `grid-2` - Two-column grid layout
- `grid-3` - Three-column grid layout
- `columns-3` - Three-column layout
- `card` - Card container
- `metric` - Metric display box
- `agenda` - Agenda container
- `team-grid` - Team member grid
- `timeline` - Timeline layout

## Creating a New Presentation

1. **Create your presentation folder:**
   ```bash
   mkdir -p Docs/Presentations/YourDeck/{themes,assets}
   ```

2. **Copy a theme** (or create your own):
   ```bash
   cp Docs/Presentations/TimoRidesDeck/themes/timo.css Docs/Presentations/YourDeck/themes/
   ```

3. **Create your slides.md:**
   ```markdown
   ---
   marp: true
   theme: Docs/Presentations/YourDeck/themes/timo.css
   paginate: true
   title: Your Presentation
   ---
   
   # Your First Slide
   
   Content here
   
   ---
   
   # Second Slide
   ```

4. **Add assets** (images, logos) to the `assets/` folder

## Rendering Your Presentation

### Using Marp CLI

Install Marp CLI:
```bash
npm install -g @marp-team/marp-cli
```

Preview your presentation:
```bash
marp --preview --allow-local-files Docs/Presentations/YourDeck/slides.md
```

Export to HTML:
```bash
marp --html --allow-local-files Docs/Presentations/YourDeck/slides.md -o output.html
```

Export to PDF:
```bash
marp --pdf --allow-local-files Docs/Presentations/YourDeck/slides.md -o output.pdf
```

Export to PowerPoint:
```bash
marp --pptx --allow-local-files Docs/Presentations/YourDeck/slides.md -o output.pptx
```

### Using VS Code Extension

1. Install the "Marp for VS Code" extension
2. Open your `.md` file
3. Click the preview icon or use `Cmd+Shift+V` (Mac) / `Ctrl+Shift+V` (Windows)

## Tips & Best Practices

1. **Use relative paths** for assets (e.g., `./assets/logo.svg`)
2. **Keep assets organized** in an `assets/` folder
3. **Test your theme** with the preview before finalizing
4. **Use semantic HTML** when needed (divs, spans) for complex layouts
5. **Leverage CSS classes** from your theme for consistent styling
6. **Use `<!-- class: className -->`** comments to apply classes to slides

## Customizing Themes

Themes are CSS files. You can:

1. Modify existing themes in the `themes/` folder
2. Create new themes by copying and editing
3. Use CSS variables for easy customization:
   ```css
   :root {
     --primary-color: #1b44ff;
     --secondary-color: #00d9ff;
     --bg-color: #05070f;
   }
   ```

## Resources

- [Marp Official Documentation](https://marp.app/)
- [Marp CLI Documentation](https://github.com/marp-team/marp-cli)
- Example presentations in `/Docs/Presentations/`

## Quick Reference

| Command | Description |
| --- | --- |
| `---` | Slide separator |
| `<!-- class: name -->` | Apply CSS class to slide |
| `![bg right](image)` | Background image on right |
| `![full-bleed](image)` | Full-width background image |
| `# Title` | H1 heading |
| `## Title` | H2 heading |
| `> Quote` | Blockquote |








