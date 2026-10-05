import { type Config } from "tailwindcss";

/**
 * Zypherpunk Privacy Wallet Theme
 * Dark, cyberpunk-inspired theme with privacy-focused colors
 */
export default {
  darkMode: ["class"],
  content: [
    "./pages/**/*.{ts,tsx}",
    "./components/**/*.{ts,tsx}",
    "./app/**/*.{ts,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Zypherpunk Privacy Theme (Understated)
        zypherpunk: {
          bg: "#0a0a0a",
          surface: "#1a1a1a",
          border: "#2a2a2a",
          primary: "#4ade80", // Muted green
          secondary: "#a855f7", // Muted purple
          accent: "#60a5fa", // Muted blue
          warning: "#f59e0b", // Muted amber
          text: "#ffffff",
          "text-muted": "#888888",
          shielded: "#4ade80", // Muted green
          transparent: "#a855f7", // Muted purple
        },
        privacy: {
          low: "#ff4444",
          medium: "#ffaa00",
          high: "#00d4ff",
          maximum: "#00ff88",
        },
        wallet: {
          zcash: "#00ff88",
          aztec: "#00d4ff",
          ethereum: "#627EEA",
          solana: "#9945FF",
        },
      },
      keyframes: {
        "glitch": {
          "0%, 100%": { transform: "translate(0)" },
          "20%": { transform: "translate(-2px, 2px)" },
          "40%": { transform: "translate(-2px, -2px)" },
          "60%": { transform: "translate(2px, 2px)" },
          "80%": { transform: "translate(2px, -2px)" },
        },
        "shield-pulse": {
          "0%, 100%": { opacity: "1", transform: "scale(1)" },
          "50%": { opacity: "0.8", transform: "scale(1.05)" },
        },
        "neon-glow": {
          "0%, 100%": { 
            textShadow: "0 0 2px currentColor, 0 0 4px currentColor",
            opacity: "1"
          },
          "50%": { 
            textShadow: "0 0 4px currentColor, 0 0 8px currentColor",
            opacity: "0.95"
          },
        },
      },
      animation: {
        "glitch": "glitch 0.3s ease-in-out",
        "shield-pulse": "shield-pulse 2s ease-in-out infinite",
        "neon-glow": "neon-glow 2s ease-in-out infinite",
      },
      boxShadow: {
        "neon-green": "0 0 5px rgba(74, 222, 128, 0.3), 0 0 10px rgba(74, 222, 128, 0.2)",
        "neon-pink": "0 0 5px rgba(168, 85, 247, 0.3), 0 0 10px rgba(168, 85, 247, 0.2)",
        "neon-cyan": "0 0 5px rgba(96, 165, 250, 0.3), 0 0 10px rgba(96, 165, 250, 0.2)",
      },
    },
  },
  plugins: [require("tailwindcss-animate")],
} satisfies Config;

