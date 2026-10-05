# Git-Triggered Tweet Automation Pipeline

**Owner:** Comms Automation Team  
**Last Updated:** November 8, 2025

---

## Purpose

Create an automated workflow that turns documentation work (Markdown commits) into draft tweets that can be reviewed, approved, and published with minimal manual effort. The pipeline supersedes the previous GitBook-only trigger by using Git commits as the authoritative source of truth.

---

## Goals & Non-Goals

- **Goals**
  - Detect documentation changes committed to the repository.
  - Extract the meaningful deltas worth amplifying publicly.
  - Generate concise tweet-ready summaries with contextual metadata.
  - Provide a human-in-the-loop approval queue.
  - Publish approved tweets via the X/Twitter API with rate limiting and logging.

- **Non-Goals**
  - Replacing long-form release notes (we still keep CHANGELOGs).
  - Automated approval (humans must sign off).
  - Direct GitBook integration (can run in parallel but no longer required).

---

## High-Level Flow

1. **Trigger**  
   - GitHub webhook on `push` events to `main` (or designated docs branch).  
   - Optional filter: only commits that touch `*.md` / `Docs/**` / `UAT/**`.

2. **Collector**  
   - Fetch commit payload via webhook or from CI runner.  
   - For each changed Markdown file, compute `git diff --unified=20` to capture surrounding context.  
   - Store assets (commit SHA, author, timestamp, file path, diff snippet).

3. **Summariser**  
   - Feed diff context into an LLM prompt that produces 1–2 tweet candidates.  
   - Enforce 280-character limit, include optional hashtags / CTA based on metadata.  
   - Attach structured metadata (repo path, topic tags, confidence score).

4. **Review Queue**  
   - Persist outputs in Airtable, Notion database, or Supabase table.  
   - Slack notification to `#social-drafts` with deep link for quick approval/edit.  
   - Reviewer edits copy, toggles status (`draft` → `approved` / `rejected`).

5. **Publisher**  
   - Scheduled job (cron, GitHub Action, Supabase Edge Function, n8n) polls for `approved` entries.  
   - Publishes via X API v2 using OAuth 2.0 client credentials.  
   - Records tweet ID back on the draft record and stores in S3/Firestore for audit.

6. **Monitoring**  
   - CloudWatch/Datadog logs for failures.  
   - Dead-letter queue for retries on API errors.  
   - Optional analytics: reconcile tweet engagement with source commit.

---

## Components

| Layer        | Implementation Notes |
|--------------|----------------------|
| Trigger      | GitHub webhook → serverless function (AWS Lambda / Cloud Functions) or Zapier/n8n endpoint. |
| Diff Parser  | Node.js script (`scripts/generateTweetDrafts.ts`) executed in CI; uses `simple-git` to pull diffs. |
| Summariser   | OpenAI `gpt-4.1` or Anthropic `Claude 3.5` via REST. Prompt enforces character limit and requires a safety summary. |
| Draft Store  | Airtable (simple) or Supabase Postgres (self-hosted). Fields: `id`, `commit_sha`, `file_path`, `tweet_text`, `status`, `reviewer`, `tweet_id`, `created_at`, `updated_at`. |
| Reviewer UX  | Notion database, Retool panel, or custom Next.js dashboard. Slack bot posts new drafts with action buttons. |
| Publisher    | Node.js worker using `twitter-api-v2` package; rate-limit guard (max 10 per hour). Managed via Docker container or GitHub Action cron. |
| Telemetry    | Sentry for error reporting, CloudWatch metrics, Slack alerts on failures. |

---

## Processing Rules

- Ignore commits where `commit.message` includes `[skip tweet]`.
- Deduplicate by `commit_sha + file_path` to avoid regenerating drafts on force-push.
- Diff window: keep 20 lines of context to help the LLM capture intent; strip unchanged sections before prompt.
- Markdown sanitisation: remove tables and HTML blocks unless flagged by keywords (`release`, `announcement`, `milestone`).
- Hashtag policy:  
  - Use repository-specific tags (`#OASIS`, `#Web4`, etc.) from a reference map (`config/social-tags.json`).  
  - Limit to <= 2 hashtags per tweet.
- CTA guidelines: apply only on milestone keywords (`launch`, `demo`, `live`, `guide`, `proposal`). Provide link to canonical GitHub blob or docs site.
- Threading: group drafts by `commit_sha`. If >1 tweet is generated for the same commit, flag as `multi-part` for manual thread assembly.

---

## Prompt Template (LLM)

```
You are the social voice for the OASIS project. Convert the following Git diff into up to two tweet options.

Context:
- Commit: {{commit_sha}}
- Author: {{author}}
- File: {{file_path}}
- Summary: {{commit_message}}
- Diff Snippet:
{{diff_excerpt}}

Guidelines:
- Each tweet <= 280 characters (count emojis/links as modern Twitter lengths).
- Use neutral, confident tone. Avoid hype or speculation.
- Include at most two hashtags from this list: {{allowed_hashtags}}.
- If a link is recommended, reference {{canonical_url}}.
- Provide JSON: { "tweets": [ { "text": "...", "confidence": 0-1 } ], "notes": "..." }
```

---

## Implementation Options

### Option A: GitHub Actions + Supabase (Recommended)
- Webhook → Supabase Edge Function (TypeScript) stores commit data.
- Cron GitHub Action runs `scripts/generateTweetDrafts.ts`, writes to Supabase.
- Reviewers use Supabase Studio or lightweight dashboard.
- Another Edge Function posts approved tweets.

### Option B: n8n / Make.com (Low-Code)
- Use GitHub trigger node, diff via webhook payload.
- Call OpenAI node for summary.
- Store in Airtable.
- Manual approval via Airtable view.
- Scheduler node posts via Twitter API.

### Option C: Serverless Lambda (Fully Custom)
- Lambda triggered by GitHub webhook.
- EFS or S3 for temporary repo clones.
- Step Functions orchestrate summarise → review → publish.
- DynamoDB for drafts.

---

## Security & Compliance

- Store API keys (OpenAI, Twitter) in Secrets Manager / GitHub Secrets. Never commit plaintext credentials.
- Implement rate limiting and exponential backoff when calling external APIs.
- Log and monitor for PII leakage; restrict prompts to documentation content only.
- Keep audit log linking tweet IDs back to commit SHA and reviewer.
- GDPR considerations: tweets derived from open-source docs; review step ensures no personal data slips through.

---

## Rollout Plan

1. **Week 1 – Prototype**  
   - Build diff parser script.  
   - Manual LLM prompt via CLI.  
   - Store drafts in local JSON for validation.

2. **Week 2 – Automation Backbone**  
   - Deploy Supabase (or configure Airtable).  
   - Wire webhook → script → draft store.  
   - Slack notifications for new drafts.

3. **Week 3 – Publishing**  
   - Implement approval UI/workflow.  
   - Build publisher worker.  
   - Add monitoring and alerting.

4. **Week 4 – Harden & Document**  
   - Write runbooks, add tests, rehearse failure scenarios.  
   - Onboard comms team, collect feedback.  
   - Transition to production schedule.

---

## Next Steps

- Create `config/social-tags.json` with canonical hashtag mappings.
- Implement `scripts/generateTweetDrafts.ts`.
- Stand up draft storage (Airtable or Supabase) and configure Slack notifications.
- Define approval rota with comms team.



