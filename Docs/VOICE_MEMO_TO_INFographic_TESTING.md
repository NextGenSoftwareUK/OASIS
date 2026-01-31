# Voice Memo to Infographic API - Testing Guide

## Prerequisites

1. **ONODE API Running**
   - Ensure the ONODE WebAPI is running
   - Default: `https://localhost:5004` or your configured URL

2. **Authentication Token**
   - You need a valid JWT token from avatar authentication
   - Get one by calling `/api/avatar/authenticate`

3. **Configuration Verified**
   - OpenAI API key configured (check `OASIS_DNA.json` or verify AIController works)
   - Glif API token set (check `GLIF_API_TOKEN` environment variable)

## Quick Configuration Check

### Test OpenAI Configuration

```bash
# Test if OpenAI is configured (uses same config as Voice Memo API)
curl -X POST \
  https://localhost:5004/api/ai/parse-intent \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"userInput": "test"}'
```

If this works, OpenAI is configured correctly.

### Test Glif Configuration

Check environment variable:
```bash
echo $GLIF_API_TOKEN
```

Or verify MCP Glif tool works (if MCP is set up).

## Testing Endpoints

### 1. Test Complete Workflow (Recommended)

**Endpoint:** `POST /api/voicememo/convert-to-infographic`

**Test with cURL:**

```bash
curl -X POST \
  https://localhost:5004/api/voicememo/convert-to-infographic \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "file=@test-voice-memo.mp3"
```

**Expected Response:**
```json
{
  "Result": {
    "Transcription": "Full transcription text...",
    "Plan": {
      "Title": "...",
      "Summary": "...",
      "KeyPoints": [...],
      "Goals": [...],
      "InfographicPrompt": "..."
    },
    "InfographicUrl": "https://...",
    "InfographicBytes": [...]
  },
  "Message": "Voice memo processed successfully",
  "IsSaved": true
}
```

**What to Check:**
- ✅ Transcription is accurate
- ✅ Plan extraction includes title, goals, key points
- ✅ Infographic URL is returned
- ✅ Image bytes are present
- ✅ No errors in response

---

### 2. Test Step-by-Step: Transcription

**Endpoint:** `POST /api/voicememo/transcribe`

```bash
curl -X POST \
  https://localhost:5004/api/voicememo/transcribe \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -F "file=@test-voice-memo.mp3" \
  -o transcription-response.json
```

**Expected Response:**
```json
{
  "Result": {
    "Transcription": "Full transcription...",
    "FileName": "test-voice-memo.mp3",
    "FileSize": 1234567
  },
  "Message": "Audio transcribed successfully"
}
```

**Verify:**
- Transcription text is present
- File name and size are correct

---

### 3. Test Step-by-Step: Plan Extraction

**Endpoint:** `POST /api/voicememo/extract-plan`

First, get a transcription (from step 2), then:

```bash
curl -X POST \
  https://localhost:5004/api/voicememo/extract-plan \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "Transcription": "Today I want to talk about expanding the OASIS platform. We need to focus on three main areas: blockchain integration, AI capabilities, and user experience. Our goal is to launch the new features by Q2 2026. Priority should be on the blockchain integration first, then AI, then UX improvements."
  }' \
  -o plan-response.json
```

**Expected Response:**
```json
{
  "Result": {
    "Title": "OASIS Platform Expansion Plan",
    "Summary": "Expanding OASIS platform with blockchain, AI, and UX improvements",
    "KeyPoints": [
      "Blockchain integration",
      "AI capabilities",
      "User experience improvements"
    ],
    "Goals": [
      "Launch new features by Q2 2026"
    ],
    "Timeline": "Q2 2026",
    "Priorities": [
      "Blockchain integration",
      "AI capabilities",
      "UX improvements"
    ],
    "Technologies": ["Blockchain", "AI"],
    "NextSteps": [...],
    "InfographicPrompt": "Detailed prompt for infographic..."
  },
  "Message": "Plan extracted successfully"
}
```

**Verify:**
- Title is meaningful
- Key points are extracted
- Goals are identified
- Infographic prompt is detailed and descriptive

---

### 4. Test Step-by-Step: Infographic Generation

**Endpoint:** `POST /api/voicememo/generate-infographic`

```bash
curl -X POST \
  https://localhost:5004/api/voicememo/generate-infographic \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "Prompt": "Create a professional infographic showing OASIS platform expansion plan. Include three main sections: Blockchain Integration, AI Capabilities, and User Experience. Use modern design with icons, timelines, and key metrics. Color scheme: blue and purple gradients. Include a timeline showing Q2 2026 launch date.",
    "WorkflowId": "cmigcvfwm0000k004u9shifki"
  }' \
  -o infographic-response.json
```

**Expected Response:**
```json
{
  "Result": {
    "ImageUrl": "https://...",
    "ImageBase64": "base64-encoded-image-data...",
    "Prompt": "..."
  },
  "Message": "Infographic generated successfully"
}
```

**Verify:**
- Image URL is returned
- Base64 image data is present
- You can decode and view the image

**To view the image:**
```bash
# Extract base64 from response and save
cat infographic-response.json | jq -r '.Result.ImageBase64' | base64 -d > infographic.png
```

---

## Test Script

Create a test script to automate testing:

### `test-voice-memo-api.sh`

```bash
#!/bin/bash

# Configuration
API_URL="${API_URL:-https://localhost:5004}"
TOKEN="${AUTH_TOKEN:-YOUR_JWT_TOKEN}"
TEST_FILE="${TEST_FILE:-test-voice-memo.mp3}"

echo "🧪 Testing Voice Memo to Infographic API"
echo "=========================================="
echo ""

# Check if test file exists
if [ ! -f "$TEST_FILE" ]; then
    echo "❌ Test file not found: $TEST_FILE"
    echo "   Create a test audio file or update TEST_FILE variable"
    exit 1
fi

# Test 1: Complete Workflow
echo "📝 Test 1: Complete Workflow (convert-to-infographic)"
echo "---------------------------------------------------"
RESPONSE=$(curl -s -X POST \
  "$API_URL/api/voicememo/convert-to-infographic" \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@$TEST_FILE" \
  -w "\n%{http_code}")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" = "200" ]; then
    echo "✅ Success! HTTP $HTTP_CODE"
    echo "$BODY" | jq -r '.Message // .message'
    
    # Extract and save infographic
    echo "$BODY" | jq -r '.Result.InfographicUrl // empty' > /dev/null 2>&1
    if [ $? -eq 0 ]; then
        IMG_URL=$(echo "$BODY" | jq -r '.Result.InfographicUrl')
        echo "   📸 Infographic URL: $IMG_URL"
        
        # Save image if base64 is present
        if echo "$BODY" | jq -e '.Result.InfographicBytes' > /dev/null 2>&1; then
            echo "$BODY" | jq -r '.Result.InfographicBytes' | base64 -d > test-infographic.png 2>/dev/null
            echo "   💾 Saved infographic to: test-infographic.png"
        fi
    fi
else
    echo "❌ Failed! HTTP $HTTP_CODE"
    echo "$BODY" | jq -r '.Message // .message // .error'
fi

echo ""
echo "=========================================="
echo "✅ Testing complete!"
```

**Usage:**
```bash
chmod +x test-voice-memo-api.sh
export AUTH_TOKEN="your-jwt-token"
export TEST_FILE="path/to/voice-memo.mp3"
./test-voice-memo-api.sh
```

---

## Testing with Postman

### Import Collection

Create a Postman collection with these requests:

1. **Convert to Infographic**
   - Method: POST
   - URL: `{{baseUrl}}/api/voicememo/convert-to-infographic`
   - Headers: `Authorization: Bearer {{token}}`
   - Body: form-data
     - Key: `file` (type: File)
     - Key: `workflowId` (optional, type: Text)

2. **Transcribe**
   - Method: POST
   - URL: `{{baseUrl}}/api/voicememo/transcribe`
   - Headers: `Authorization: Bearer {{token}}`
   - Body: form-data
     - Key: `file` (type: File)

3. **Extract Plan**
   - Method: POST
   - URL: `{{baseUrl}}/api/voicememo/extract-plan`
   - Headers: `Authorization: Bearer {{token}}`
   - Body: raw JSON
     ```json
     {
       "Transcription": "Your transcription text here..."
     }
     ```

4. **Generate Infographic**
   - Method: POST
   - URL: `{{baseUrl}}/api/voicememo/generate-infographic`
   - Headers: `Authorization: Bearer {{token}}`
   - Body: raw JSON
     ```json
     {
       "Prompt": "Your infographic prompt here...",
       "WorkflowId": "cmigcvfwm0000k004u9shifki"
     }
     ```

---

## Testing with Sample Data

### Create Test Audio File

If you don't have a voice memo, you can:

1. **Record a test memo:**
   ```bash
   # On macOS
   say "Today I want to talk about the OASIS platform expansion. We need to focus on three main areas: blockchain integration, AI capabilities, and user experience. Our goal is to launch by Q2 2026." -o test-voice-memo.aiff
   # Convert to MP3
   ffmpeg -i test-voice-memo.aiff test-voice-memo.mp3
   ```

2. **Use text-to-speech:**
   - Use online TTS services
   - Or use Python with `gTTS`:
     ```python
     from gtts import gTTS
     text = "Today I want to talk about the OASIS platform expansion..."
     tts = gTTS(text=text, lang='en')
     tts.save("test-voice-memo.mp3")
     ```

### Test Transcription with Sample Text

You can test plan extraction directly with sample transcription:

```bash
curl -X POST \
  https://localhost:5004/api/voicememo/extract-plan \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "Transcription": "Today I want to discuss our OASIS platform roadmap. We have three main priorities: first, integrating blockchain technology for NFT support. Second, enhancing our AI capabilities with better natural language processing. Third, improving the user experience with a modern interface. Our timeline is aggressive - we want to launch these features by Q2 2026. The blockchain integration is the highest priority, followed by AI, then UX. We will need to work with Solana for the blockchain piece, and potentially integrate OpenAI for the AI features."
  }'
```

---

## Common Issues & Solutions

### Issue: "OpenAI API key not configured"

**Solution:**
1. Check `OASIS_DNA.json` has OpenAI config:
   ```json
   {
     "OASIS": {
       "AI": {
         "OpenAI": {
           "ApiKey": "sk-..."
         }
       }
     }
   }
   ```
2. Verify AIController works: `curl .../api/ai/parse-intent`

### Issue: "Glif API token not configured"

**Solution:**
1. Check environment variable:
   ```bash
   echo $GLIF_API_TOKEN
   ```
2. Set it if missing:
   ```bash
   export GLIF_API_TOKEN="your-token"
   ```
3. Or add to `OASIS_DNA.json`:
   ```json
   {
     "OASIS": {
       "AI": {
         "Glif": {
           "ApiToken": "your-token"
         }
       }
     }
   }
   ```

### Issue: "File size exceeds 25MB limit"

**Solution:**
- Split audio into smaller segments
- Compress audio file
- Use shorter voice memos for testing

### Issue: "No transcription text returned"

**Solution:**
- Check audio file format (must be: mp3, wav, m4a, ogg, flac, webm)
- Verify audio file is not corrupted
- Check OpenAI API has credits/quota available

### Issue: "Infographic generation failed"

**Solution:**
- Verify Glif API token is valid
- Check Glif account has credits
- Try a different workflow ID
- Check network connectivity to `simple-api.glif.app`

---

## Performance Testing

### Test Processing Time

```bash
time curl -X POST \
  https://localhost:5004/api/voicememo/convert-to-infographic \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@test-voice-memo.mp3"
```

**Expected Times:**
- Transcription: 1-2 minutes (for 10-minute audio)
- Plan Extraction: 5-10 seconds
- Infographic Generation: 30-120 seconds
- **Total:** 2-4 minutes for complete workflow

---

## Integration Testing

### Test with Real OASIS Workflow

1. **Authenticate:**
   ```bash
   curl -X POST https://localhost:5004/api/avatar/authenticate \
     -H "Content-Type: application/json" \
     -d '{"username": "your-username", "password": "your-password"}' \
     -o auth-response.json
   
   TOKEN=$(cat auth-response.json | jq -r '.Result.Token')
   ```

2. **Upload Voice Memo:**
   ```bash
   curl -X POST \
     https://localhost:5004/api/voicememo/convert-to-infographic \
     -H "Authorization: Bearer $TOKEN" \
     -F "file=@david-voice-memo.mp3" \
     -o result.json
   ```

3. **Verify Infographic Saved:**
   ```bash
   # Check if infographic was saved to OASIS storage
   curl -X GET \
     https://localhost:5004/api/files/get-all-files-stored-for-current-logged-in-avatar \
     -H "Authorization: Bearer $TOKEN"
   ```

---

## Next Steps

After successful testing:

1. ✅ Verify all endpoints work
2. ✅ Check infographic quality
3. ✅ Test with different audio lengths
4. ✅ Verify files are saved to OASIS storage
5. ✅ Test error handling (invalid files, missing config, etc.)
