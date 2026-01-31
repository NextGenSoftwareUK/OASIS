# Voice Memo to Infographic - Quick Start Guide

## What It Does

Converts David's voice memos (audio files) containing OASIS plans into visual infographics automatically.

## Quick Setup

### 1. Configure OpenAI (Required)

**Already using AI features?** If you're using the AI NFT Assistant or `/api/ai/*` endpoints, OpenAI is already configured! The Voice Memo API uses the same configuration.

If not configured, add to `OASIS_DNA.json`:
```json
{
  "OASIS": {
    "AI": {
      "OpenAI": {
        "ApiKey": "sk-...",
        "Model": "gpt-4o-mini"
      }
    }
  }
}
```

### 2. Configure Glif.app (Required)

**Already using MCP for NFT creation?** If you have `GLIF_API_TOKEN` set, you're all set! The Voice Memo API uses the same environment variable.

If not configured:
```bash
export GLIF_API_TOKEN="your-token-here"
```

Get token: https://glif.app/settings/api-tokens

**Note:** The API checks environment variables first (like MCP), then falls back to `OASIS_DNA.json` if needed.

## Usage

### Complete Workflow (Recommended)

```bash
curl -X POST \
  https://api.oasisplatform.world/api/voicememo/convert-to-infographic \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@voice-memo.mp3"
```

**What happens:**
1. Audio is transcribed to text (OpenAI Whisper)
2. Plan information is extracted (OpenAI GPT)
3. Infographic is generated (Glif.app)
4. Infographic is saved to OASIS storage

**Response includes:**
- Full transcription
- Extracted plan (title, goals, priorities, etc.)
- Generated infographic (URL + base64 image)

## Supported Audio Formats

- MP3, WAV, M4A, OGG, FLAC, WEBM
- Max size: 25MB

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/voicememo/convert-to-infographic` | POST | Complete workflow (recommended) |
| `/api/voicememo/transcribe` | POST | Step 1: Transcribe audio |
| `/api/voicememo/extract-plan` | POST | Step 2: Extract plan from text |
| `/api/voicememo/generate-infographic` | POST | Step 3: Generate infographic |

## Example Response

```json
{
  "Result": {
    "Transcription": "Today I want to talk about...",
    "Plan": {
      "Title": "OASIS Platform Expansion",
      "KeyPoints": ["Point 1", "Point 2"],
      "Goals": ["Goal 1"],
      "InfographicPrompt": "Create an infographic showing..."
    },
    "InfographicUrl": "https://...",
    "InfographicBytes": [...]
  }
}
```

## Troubleshooting

**"OpenAI API key not configured"**
→ Add OpenAI config to `OASIS_DNA.json`

**"Glif API token not configured"**
→ Set `GLIF_API_TOKEN` environment variable

**"File size exceeds 25MB"**
→ Split audio into smaller files

## Full Documentation

See [VOICE_MEMO_TO_INFographic_API.md](./VOICE_MEMO_TO_INFographic_API.md) for complete documentation.
