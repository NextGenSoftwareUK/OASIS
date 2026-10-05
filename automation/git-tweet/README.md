# Git-Triggered Tweet Draft Generator

Generates tweet drafts from Markdown changes in Git commits. Meant to run in CI or via GitHub Actions as part of the docs-to-social automation pipeline.

---

## Requirements

- Node.js 18+
- Git history available locally (either full clone or checkout in CI)
- `OPENAI_API_KEY` environment variable (optional – without it the script falls back to stub tweets)

---

## Usage

```bash
# from repository root
node automation/git-tweet/generateTweetDrafts.mjs --since HEAD~5 --until HEAD
```

Supported flags:

| Flag         | Description | Default |
|--------------|-------------|---------|
| `--since`    | Starting commit (exclusive) for the diff range | `HEAD~1` |
| `--until`    | Ending commit (inclusive) | `HEAD` |
| `--output`   | JSON file to append tweet drafts | `automation/git-tweet/output/tweet-drafts.json` |
| `--tags`     | Path to hashtag mapping JSON | `automation/git-tweet/config/social-tags.json` |
| `--max`      | Maximum tweets per file | `2` |

---

## Output

The script builds (or appends to) a JSON array of drafts:

```json
[
  {
    "commit": "abc123",
    "file": "Docs/Guides/Example.md",
    "tweets": [
      {
        "text": "New HyperDrive docs are live...",
        "confidence": 0.82,
        "hashtags": ["#OASIS", "#Web4"]
      }
    ],
    "notes": "LLM suggested highlighting the new release."
  }
]
```

Downstream automations (Airtable loaders, Slack bots, etc.) can consume this file.

---

## Configuration

- `config/social-tags.json` holds canonical hashtags per directory/prefix.
- Use commit message token `[skip tweet]` to suppress generation.
- Only Markdown (`.md`, `.mdx`) files are processed.

---

## Local Development

1. Update documentation in Markdown and commit locally.
2. Run the generator against `HEAD~1..HEAD`.
3. Review `automation/git-tweet/output/tweet-drafts.json`.
4. Optionally push the JSON to draft storage or share with comms for review.

---

## Known Limitations

- Currently relies on OpenAI Responses API; swap the `callOpenAI` helper if another LLM provider is preferred.
- Link selection is naive (points to GitHub blob); integrate with GitBook URL resolver if needed.
- No retry logic yet for API timeouts; wrap in external orchestrator for resilience.



