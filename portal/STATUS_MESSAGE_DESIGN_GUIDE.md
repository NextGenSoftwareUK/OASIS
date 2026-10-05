# Status Message Design Guide

## Overview
This document captures the design pattern for status messages (errors, loading, coming soon, etc.) used throughout the OASIS Portal. This clean, minimal design is ideal for communicating important states to users.

## Design Specifications

### Visual Hierarchy

1. **Background**
   - Dark, almost black background
   - High contrast for text readability
   - Clean, uncluttered layout

2. **Icon (Top, Centered)**
   - Equilateral triangle with golden/yellow outline
   - Filled with same golden/yellow color
   - White exclamation mark or relevant icon in center
   - Prominent, immediately conveys alert/problem/status
   - Size: Approximately 64px × 64px

3. **Primary Message (Below Icon)**
   - Large, white, bold sans-serif font
   - Clear, concise title
   - Example: "Error Loading NFT Mint Studio" or "Coming Soon"
   - Font size: ~1.5rem to 2rem
   - Font weight: 700 (bold)

4. **Secondary Message/Details (Below Primary)**
   - Slightly smaller, light gray sans-serif font
   - Provides additional context or technical details
   - Example: "WIZARD_STEPS is not defined" or "This feature will be available soon"
   - Font size: ~0.875rem to 1rem
   - Font weight: 400 (regular)
   - Color: `var(--text-secondary)` or `rgba(255, 255, 255, 0.6)`

5. **Call to Action Button (Bottom, Centered)**
   - White background with black text
   - Subtle border or shadow for depth
   - Simple, direct label: "Retry", "Refresh", "Learn More", etc.
   - Font size: ~0.875rem to 1rem
   - Font weight: 600 (semi-bold)
   - Padding: ~0.75rem 1.5rem
   - Border radius: ~0.5rem
   - Hover state: Slightly darker background or subtle scale effect

### Layout

- **Container**: Centered vertically and horizontally
- **Spacing**: 
  - Icon to Primary Message: ~1.5rem
  - Primary to Secondary: ~0.75rem
  - Secondary to Button: ~2rem
- **Width**: Max-width ~600px for readability
- **Alignment**: All elements center-aligned

## Color Palette

- **Background**: `var(--bg-primary)` or `#0a0e1a` (dark)
- **Icon**: Golden/Yellow `#fbbf24` or `rgba(251, 191, 36, 1)`
- **Primary Text**: White `#ffffff` or `var(--text-primary)`
- **Secondary Text**: Light Gray `rgba(255, 255, 255, 0.6)` or `var(--text-secondary)`
- **Button Background**: White `#ffffff`
- **Button Text**: Black `#000000` or `#041321`

## Implementation Examples

### Error State
```html
<div style="
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 400px;
    padding: 2rem;
    text-align: center;
">
    <!-- Warning Icon -->
    <div style="
        width: 64px;
        height: 64px;
        margin-bottom: 1.5rem;
        display: flex;
        align-items: center;
        justify-content: center;
        background: #fbbf24;
        border-radius: 50%;
        color: white;
        font-size: 2rem;
    ">⚠</div>
    
    <!-- Primary Message -->
    <h2 style="
        font-size: 1.75rem;
        font-weight: 700;
        color: var(--text-primary);
        margin: 0 0 0.75rem 0;
    ">Error Loading NFT Mint Studio</h2>
    
    <!-- Secondary Message -->
    <p style="
        font-size: 1rem;
        color: var(--text-secondary);
        margin: 0 0 2rem 0;
        max-width: 500px;
    ">WIZARD_STEPS is not defined</p>
    
    <!-- CTA Button -->
    <button 
        onclick="location.reload()"
        style="
            background: white;
            color: #041321;
            border: none;
            padding: 0.75rem 1.5rem;
            border-radius: 0.5rem;
            font-size: 0.875rem;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s;
        "
        onmouseover="this.style.transform='scale(1.05)'; this.style.boxShadow='0 4px 12px rgba(255,255,255,0.2)';"
        onmouseout="this.style.transform='scale(1)'; this.style.boxShadow='none';"
    >
        Retry
    </button>
</div>
```

### Coming Soon State
```html
<div style="
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 400px;
    padding: 2rem;
    text-align: center;
">
    <!-- Info Icon (can be different icon for coming soon) -->
    <div style="
        width: 64px;
        height: 64px;
        margin-bottom: 1.5rem;
        display: flex;
        align-items: center;
        justify-content: center;
        background: var(--accent-color);
        border-radius: 50%;
        color: #041321;
        font-size: 2rem;
    ">🚀</div>
    
    <!-- Primary Message -->
    <h2 style="
        font-size: 1.75rem;
        font-weight: 700;
        color: var(--text-primary);
        margin: 0 0 0.75rem 0;
    ">Coming Soon</h2>
    
    <!-- Secondary Message -->
    <p style="
        font-size: 1rem;
        color: var(--text-secondary);
        margin: 0 0 2rem 0;
        max-width: 500px;
    ">This feature will be available in an upcoming release.</p>
    
    <!-- CTA Button -->
    <button 
        onclick="window.switchTab('trading')"
        style="
            background: white;
            color: #041321;
            border: none;
            padding: 0.75rem 1.5rem;
            border-radius: 0.5rem;
            font-size: 0.875rem;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s;
        "
        onmouseover="this.style.transform='scale(1.05)'; this.style.boxShadow='0 4px 12px rgba(255,255,255,0.2)';"
        onmouseout="this.style.transform='scale(1)'; this.style.boxShadow='none';"
    >
        Explore Other Features
    </button>
</div>
```

### Loading State
```html
<div style="
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    min-height: 400px;
    padding: 2rem;
    text-align: center;
">
    <!-- Loading Spinner -->
    <div style="
        width: 64px;
        height: 64px;
        margin-bottom: 1.5rem;
        border: 4px solid var(--border-color);
        border-top: 4px solid var(--accent-color);
        border-radius: 50%;
        animation: spin 1s linear infinite;
    "></div>
    
    <!-- Primary Message -->
    <h2 style="
        font-size: 1.75rem;
        font-weight: 700;
        color: var(--text-primary);
        margin: 0 0 0.75rem 0;
    ">Loading...</h2>
    
    <!-- Secondary Message -->
    <p style="
        font-size: 1rem;
        color: var(--text-secondary);
        margin: 0;
        max-width: 500px;
    ">Please wait while we load the content.</p>
</div>

<style>
@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}
</style>
```

## Icon Variations

Different states can use different icons:

- **Error**: ⚠ Warning triangle (golden/yellow)
- **Coming Soon**: 🚀 Rocket or 📅 Calendar or ⏳ Hourglass
- **Loading**: Spinner or ⏳ Hourglass
- **Success**: ✓ Checkmark (green)
- **Info**: ℹ️ Information circle (blue)

## Usage Guidelines

1. **Keep messages concise**: Primary message should be 3-6 words, secondary can provide more detail
2. **Use appropriate icons**: Match icon to the message type
3. **Provide clear actions**: Always include a button for the next step
4. **Maintain consistency**: Use the same spacing, sizing, and colors across all status messages
5. **Accessibility**: Ensure sufficient contrast ratios and consider screen readers

## Portal Integration

This design pattern should be used for:
- Error states in dynamic content loading
- Coming soon pages for unreleased features
- Loading states during async operations
- Success confirmations after actions
- Maintenance/upgrade notifications

## CSS Variables Reference

Use these portal CSS variables for consistency:
- `var(--bg-primary)` - Main background
- `var(--text-primary)` - Primary text color (white)
- `var(--text-secondary)` - Secondary text color (light gray)
- `var(--accent-color)` - Accent color (teal/cyan)
- `var(--border-color)` - Border color

## Notes

- This design was first used in the NFT Mint Studio error handler
- The minimalist approach ensures the message is the focal point
- The design works well on both desktop and mobile when using responsive sizing
- Consider adding subtle animations for icon/button interactions
