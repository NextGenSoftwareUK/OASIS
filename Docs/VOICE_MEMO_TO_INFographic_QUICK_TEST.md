# Quick Test Guide

## Fastest Way to Test

### 1. Get Authentication Token

```bash
# Authenticate and get token
curl -X POST https://localhost:5004/api/avatar/authenticate \
  -H "Content-Type: application/json" \
  -d '{"username": "your-username", "password": "your-password"}' \
  | jq -r '.Result.Token' > token.txt

export AUTH_TOKEN=$(cat token.txt)
```

### 2. Test with Script

**Bash script:**
```bash
./test-voice-memo-api.sh voice-memo.mp3 https://localhost:5004 $AUTH_TOKEN
```

**Python script:**
```bash
python3 test_voice_memo_api.py voice-memo.mp3 https://localhost:5004 $AUTH_TOKEN
```

### 3. Manual cURL Test

```bash
curl -X POST \
  https://localhost:5004/api/voicememo/convert-to-infographic \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -F "file=@voice-memo.mp3" \
  | jq '.'
```

## Quick Configuration Check

```bash
# Check OpenAI (should work if AI features work)
curl -X POST https://localhost:5004/api/ai/parse-intent \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"userInput": "test"}' \
  | jq '.Message'

# Check Glif token
echo $GLIF_API_TOKEN
```

## Expected Output

✅ **Success:**
- HTTP 200
- Transcription text
- Plan with title, goals, key points
- Infographic URL
- Image saved to `test-infographic.png`

❌ **Common Errors:**
- `401` - Invalid/missing token
- `400` - Invalid file or missing config
- `500` - Server error (check logs)

## Troubleshooting

| Error | Solution |
|-------|----------|
| "OpenAI API key not configured" | Add to `OASIS_DNA.json` |
| "Glif API token not configured" | Set `GLIF_API_TOKEN` env var |
| "File size exceeds 25MB" | Use smaller audio file |
| Connection refused | Check ONODE is running |
