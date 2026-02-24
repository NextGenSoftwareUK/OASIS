# Voice Memo to Infographic API

## Overview

The Voice Memo to Infographic API converts David's long voice memos containing OASIS plans into visual infographics. This system automates the entire workflow from audio transcription to visual representation.

## Architecture

The system consists of three main components:

1. **VoiceMemoController** - REST API endpoints for processing voice memos
2. **VoiceMemoService** - Business logic for transcription, plan extraction, and infographic generation
3. **Integration Services**:
   - **OpenAI Whisper API** - Audio transcription
   - **OpenAI GPT** - Plan extraction and structured data extraction
   - **Glif.app API** - Infographic image generation

## Workflow

```
Voice Memo (Audio File)
    ↓
[1] Transcribe Audio (OpenAI Whisper)
    ↓
Transcription Text
    ↓
[2] Extract Plan (OpenAI GPT)
    ↓
Structured Plan Data
    ↓
[3] Generate Infographic (Glif.app)
    ↓
Infographic Image
```

## Setup

### 1. Configure OpenAI API

**Already configured?** If you're using the AI NFT Assistant in `oportal-repo` or the `/api/ai/*` endpoints, OpenAI is already configured in `OASIS_DNA.json`:

```json
{
  "OASIS": {
    "AI": {
      "OpenAI": {
        "ApiKey": "your-openai-api-key",
        "Model": "gpt-4o-mini",
        "BaseUrl": "https://api.openai.com/v1"
      }
    }
  }
}
```

The Voice Memo API uses the **same OpenAI configuration** as the existing AI endpoints, so no additional setup needed if already configured.

### 2. Configure Glif.app API

**Already configured?** If you're using Glif via MCP for NFT creation, you already have `GLIF_API_TOKEN` set as an environment variable. The Voice Memo API uses the **same configuration**.

**Option 1: Environment Variable (Recommended - matches MCP pattern)**
```bash
export GLIF_API_TOKEN="your-glif-api-token"
export GLIF_API_URL="https://simple-api.glif.app"  # Optional, defaults to this
```

**Option 2: OASIS_DNA.json**
```json
{
  "OASIS": {
    "AI": {
      "Glif": {
        "ApiToken": "your-glif-api-token",
        "ApiUrl": "https://simple-api.glif.app"
      }
    }
  }
}
```

**Priority:** Environment variables are checked first (matching MCP behavior), then falls back to OASIS_DNA.json.

Get your free Glif API token at: https://glif.app/settings/api-tokens

## API Endpoints

### Base URL

```
/api/voicememo
```

All endpoints require authentication (Bearer token).

---

### 1. Convert Voice Memo to Infographic (Complete Workflow)

**Endpoint:** `POST /api/voicememo/convert-to-infographic`

**Description:** Complete workflow that transcribes audio, extracts plan, and generates infographic in one call.

**Request:**
- **Method:** POST
- **Content-Type:** multipart/form-data
- **Parameters:**
  - `file` (required): Audio file (mp3, wav, m4a, ogg, flac, webm)
  - `workflowId` (optional): Glif workflow ID for custom infographic generation

**File Requirements:**
- **Max Size:** 25MB (OpenAI Whisper limit)
- **Supported Formats:** mp3, wav, m4a, ogg, flac, webm

**Response:**

```json
{
  "Result": {
    "Transcription": "Full transcription text...",
    "Plan": {
      "Title": "OASIS Platform Expansion Plan",
      "Summary": "Brief summary of the plan",
      "KeyPoints": ["Point 1", "Point 2", "Point 3"],
      "Goals": ["Goal 1", "Goal 2"],
      "Timeline": "Q1 2026",
      "Priorities": ["Priority 1", "Priority 2"],
      "Technologies": ["Blockchain", "AI"],
      "NextSteps": ["Step 1", "Step 2"],
      "InfographicPrompt": "Detailed prompt for infographic generation..."
    },
    "InfographicUrl": "https://...",
    "InfographicBytes": [/* base64 image bytes */]
  },
  "Message": "Voice memo processed successfully",
  "IsSaved": true
}
```

**Example (cURL):**

```bash
curl -X POST \
  https://api.oasisweb4.com/api/voicememo/convert-to-infographic \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@voice-memo.mp3" \
  -F "workflowId=cmigcvfwm0000k004u9shifki"
```

---

### 2. Transcribe Audio

**Endpoint:** `POST /api/voicememo/transcribe`

**Description:** Transcribe audio file to text (step 1 of the process).

**Request:**
- **Method:** POST
- **Content-Type:** multipart/form-data
- **Parameters:**
  - `file` (required): Audio file

**Response:**

```json
{
  "Result": {
    "Transcription": "Full transcription text...",
    "FileName": "voice-memo.mp3",
    "FileSize": 1234567
  },
  "Message": "Audio transcribed successfully"
}
```

---

### 3. Extract Plan from Transcription

**Endpoint:** `POST /api/voicememo/extract-plan`

**Description:** Extract structured plan information from transcription text (step 2).

**Request:**
- **Method:** POST
- **Content-Type:** application/json
- **Body:**

```json
{
  "Transcription": "Full transcription text..."
}
```

**Response:**

```json
{
  "Result": {
    "Title": "OASIS Platform Expansion Plan",
    "Summary": "Brief summary...",
    "KeyPoints": ["Point 1", "Point 2"],
    "Goals": ["Goal 1", "Goal 2"],
    "Timeline": "Q1 2026",
    "Priorities": ["Priority 1"],
    "Technologies": ["Blockchain"],
    "NextSteps": ["Step 1"],
    "InfographicPrompt": "Detailed prompt for infographic..."
  },
  "Message": "Plan extracted successfully"
}
```

---

### 4. Generate Infographic

**Endpoint:** `POST /api/voicememo/generate-infographic`

**Description:** Generate infographic from a prompt (step 3).

**Request:**
- **Method:** POST
- **Content-Type:** application/json
- **Body:**

```json
{
  "Prompt": "Detailed infographic prompt...",
  "WorkflowId": "cmigcvfwm0000k004u9shifki" // optional
}
```

**Response:**

```json
{
  "Result": {
    "ImageUrl": "https://...",
    "ImageBase64": "base64-encoded-image",
    "Prompt": "Prompt used for generation"
  },
  "Message": "Infographic generated successfully"
}
```

---

## Plan Extraction

The AI extracts the following structured information from voice memos:

- **Title** - Concise title for the plan
- **Summary** - Brief overview of main points
- **KeyPoints** - List of important points mentioned
- **Goals** - Identified goals and objectives
- **Timeline** - Any mentioned timelines or deadlines
- **Priorities** - Prioritized items
- **Technologies** - Technologies or tools mentioned
- **NextSteps** - Actionable next steps
- **InfographicPrompt** - AI-generated prompt optimized for infographic generation

## Infographic Generation

Infographics are generated using Glif.app workflows. The system:

1. Uses the extracted plan to create a detailed visual prompt
2. Generates the image using Glif.app's Flux 2 Pro workflow (default)
3. Downloads and returns the generated image
4. Optionally saves the infographic to OASIS file storage

### Default Workflow

- **Workflow ID:** `cmigcvfwm0000k004u9shifki` (Flux 2 Pro)
- **Best For:** Accurate, realistic image generation
- **Custom Workflows:** Browse and use workflows from [glif.app](https://glif.app)

## Error Handling

All endpoints return `OASISResult<T>` which includes:

- `IsError` - Boolean indicating if an error occurred
- `Message` - Human-readable message
- `Exception` - Exception details (if error occurred)

**Common Errors:**

- **400 Bad Request:** Invalid file type, file too large, missing required fields
- **401 Unauthorized:** Missing or invalid authentication token
- **500 Internal Server Error:** API configuration issues, service failures

## File Storage

When using the complete workflow (`convert-to-infographic`), the generated infographic is automatically saved to OASIS file storage with metadata:

- Source: "voice_memo"
- Original audio file name
- Transcription text
- Plan title

## Usage Examples

### Example 1: Complete Workflow (Recommended)

```csharp
// C# example
using (var client = new HttpClient())
{
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    var content = new MultipartFormDataContent();
    var fileContent = new ByteArrayContent(audioBytes);
    fileContent.Headers.ContentType = 
        new MediaTypeHeaderValue("audio/mpeg");
    content.Add(fileContent, "file", "voice-memo.mp3");
    
    var response = await client.PostAsync(
        "https://api.oasisweb4.com/api/voicememo/convert-to-infographic",
        content
    );
    
    var result = await response.Content.ReadAsStringAsync();
    // Process result...
}
```

### Example 2: Step-by-Step Processing

```bash
# Step 1: Transcribe
curl -X POST \
  https://api.oasisweb4.com/api/voicememo/transcribe \
  -H "Authorization: Bearer TOKEN" \
  -F "file=@voice-memo.mp3" \
  > transcription.json

# Step 2: Extract Plan
curl -X POST \
  https://api.oasisweb4.com/api/voicememo/extract-plan \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"Transcription": "..."}' \
  > plan.json

# Step 3: Generate Infographic
curl -X POST \
  https://api.oasisweb4.com/api/voicememo/generate-infographic \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"Prompt": "..."}' \
  > infographic.json
```

## Integration with OASIS

The Voice Memo API integrates seamlessly with OASIS:

- **Authentication:** Uses OASIS avatar authentication
- **File Storage:** Saves infographics to OASIS file storage
- **Metadata:** Stores transcription and plan data with files
- **Holons:** Can be extended to save plans as holons

## Performance Considerations

- **Transcription:** Typically 1-2 minutes for 10-minute audio
- **Plan Extraction:** Usually < 10 seconds
- **Infographic Generation:** 30-120 seconds depending on complexity
- **Total Workflow:** 2-4 minutes for complete processing

## Cost Considerations

- **OpenAI Whisper:** ~$0.006 per minute of audio
- **OpenAI GPT:** ~$0.01-0.03 per request (depending on transcription length)
- **Glif.app:** Uses credit system (free tier available)

## Best Practices

1. **Audio Quality:** Use clear audio recordings for better transcription accuracy
2. **File Size:** Keep files under 25MB (split longer memos if needed)
3. **Workflow Selection:** Choose appropriate Glif workflow for your infographic style
4. **Error Handling:** Always check `IsError` flag in responses
5. **Storage:** Infographics are automatically saved, but you can also store transcriptions and plans separately

## Troubleshooting

### "OpenAI API key not configured"
- Add OpenAI configuration to `OASIS_DNA.json`
- Ensure `ApiKey` field is set correctly

### "Glif API token not configured"
- Set `GLIF_API_TOKEN` environment variable
- Or add to `OASIS_DNA.json` under `OASIS.AI.Glif.ApiToken`

### "File size exceeds 25MB limit"
- Split long voice memos into smaller segments
- Or use audio compression

### "No transcription text returned"
- Check audio file format is supported
- Verify audio file is not corrupted
- Ensure OpenAI API has sufficient credits

### "Infographic generation failed"
- Check Glif.app account has credits
- Verify API token is valid
- Try a different workflow ID

## Future Enhancements

Potential improvements:

- Support for video memos (extract audio track)
- Multiple infographic styles/templates
- Batch processing of multiple voice memos
- Real-time transcription streaming
- Custom infographic templates
- Integration with OASIS quest/mission system
- Automatic plan-to-action item conversion

## References

- [OpenAI Whisper API](https://platform.openai.com/docs/guides/speech-to-text)
- [OpenAI GPT API](https://platform.openai.com/docs/api-reference)
- [Glif.app API](https://docs.glif.app/api/getting-started)
- [OASIS API Documentation](../Devs/API%20Documentation/)
