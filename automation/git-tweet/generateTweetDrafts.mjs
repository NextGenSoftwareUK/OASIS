#!/usr/bin/env node

/**
 * Generate tweet drafts from Markdown changes in the Git history.
 *
 * Usage:
 *   node automation/git-tweet/generateTweetDrafts.mjs --since HEAD~5 --until HEAD
 */

import { execSync } from "child_process";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const DEFAULT_SINCE = "HEAD~1";
const DEFAULT_UNTIL = "HEAD";
const DEFAULT_OUTPUT = path.join(__dirname, "output", "tweet-drafts.json");
const DEFAULT_TAGS = path.join(__dirname, "config", "social-tags.json");

const args = parseArgs(process.argv.slice(2));
const since = args.since || DEFAULT_SINCE;
const until = args.until || DEFAULT_UNTIL;
const outputPath = args.output || DEFAULT_OUTPUT;
const tagsPath = args.tags || DEFAULT_TAGS;
const maxTweetsPerFile = Number(args.max ?? 2);

const hashtagMap = readHashtagConfig(tagsPath);

async function main() {
  ensureOutputDir(outputPath);

  const commits = getCommitsInRange(since, until);
  if (commits.length === 0) {
    console.log(`No commits found from ${since}..${until}. Nothing to do.`);
    return;
  }

  const drafts = [];

  for (const commit of commits) {
    const message = getCommitMessage(commit);
    if (/\[skip tweet\]/i.test(message)) {
      console.log(`Skipping commit ${commit} due to [skip tweet] flag.`);
      continue;
    }

    const parent = getParentCommit(commit);
    if (!parent) {
      console.warn(`Commit ${commit} has no parent; skipping.`);
      continue;
    }

    const files = getMarkdownFilesChanged(parent, commit);
    if (files.length === 0) {
      continue;
    }

    for (const filePath of files) {
      const diff = getFileDiff(parent, commit, filePath);
      if (!diff.trim()) {
        continue;
      }

      const canonicalUrl = buildGitHubBlobUrl(commit, filePath);
      const allowedHashtags = deriveHashtags(filePath, hashtagMap);

      try {
        const tweetDraft = await generateTweetDraft({
          commit,
          message,
          filePath,
          diff,
          canonicalUrl,
          allowedHashtags,
          maxTweets: maxTweetsPerFile,
        });

        if (tweetDraft.tweets.length > 0) {
          drafts.push(tweetDraft);
          console.log(
            `Generated ${tweetDraft.tweets.length} tweet(s) for ${filePath} @ ${commit.substring(
              0,
              7
            )}`
          );
        }
      } catch (error) {
        console.error(
          `Failed to generate tweets for ${filePath} @ ${commit}:`,
          error?.message || error
        );
      }
    }
  }

  if (drafts.length === 0) {
    console.log("No tweet drafts generated.");
    return;
  }

  const existingDrafts = readExistingDrafts(outputPath);
  const merged = [...existingDrafts, ...drafts];
  fs.writeFileSync(outputPath, JSON.stringify(merged, null, 2));
  console.log(`Wrote ${drafts.length} new draft(s) to ${outputPath}`);
}

function parseArgs(argv) {
  const result = {};
  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (!arg.startsWith("--")) continue;
    const key = arg.replace(/^--/, "");
    const value = argv[i + 1];
    if (value && !value.startsWith("--")) {
      result[key] = value;
      i += 1;
    } else {
      result[key] = true;
    }
  }
  return result;
}

function runGit(command) {
  try {
    const output = execSync(`git ${command}`, {
      encoding: "utf-8",
      stdio: ["ignore", "pipe", "pipe"],
    });
    return output.trim();
  } catch (error) {
    console.error(`Git command failed: git ${command}`);
    throw error;
  }
}

function getCommitsInRange(sinceCommit, untilCommit) {
  const range = `${sinceCommit}..${untilCommit}`;
  const output = runGit(`rev-list --reverse ${range}`);
  return output ? output.split("\n") : [];
}

function getCommitMessage(commit) {
  return runGit(`log --format=%s -n 1 ${commit}`);
}

function getParentCommit(commit) {
  const output = runGit(`rev-list --parents -n 1 ${commit}`);
  const parts = output.split(" ");
  if (parts.length <= 1) {
    return null;
  }
  return parts[1]; // first parent
}

function getMarkdownFilesChanged(base, head) {
  const output = runGit(
    `diff --name-only ${base} ${head} -- '*.md' '*.mdx'`
  );
  if (!output) return [];
  return Array.from(new Set(output.split("\n").filter(Boolean)));
}

function getFileDiff(base, head, filePath) {
  const diff = runGit(
    `diff --unified=20 ${base} ${head} -- "${filePath.replace(/"/g, '\\"')}"`
  );
  return truncateDiff(diff, 6000);
}

function truncateDiff(diff, maxLength) {
  if (diff.length <= maxLength) return diff;
  return `${diff.slice(0, maxLength)}\n... (truncated)`;
}

function deriveHashtags(filePath, map) {
  const matches = new Set(map.default || []);
  for (const [prefix, tags] of Object.entries(map)) {
    if (prefix === "default") continue;
    if (filePath.startsWith(prefix)) {
      tags.forEach((tag) => matches.add(tag));
    }
    if (filePath.toLowerCase().includes(prefix.toLowerCase())) {
      tags.forEach((tag) => matches.add(tag));
    }
  }
  return Array.from(matches).slice(0, 4);
}

function buildGitHubBlobUrl(commit, filePath) {
  const repoUrl =
    process.env.GITHUB_BLOB_BASE ||
    "https://github.com/NextGenSoftwareUK/OASIS/commit";
  return `${repoUrl}/${commit}#diff-${Buffer.from(filePath).toString("hex")}`;
}

async function generateTweetDraft({
  commit,
  message,
  filePath,
  diff,
  canonicalUrl,
  allowedHashtags,
  maxTweets,
}) {
  const payload = {
    commit,
    file: filePath,
    tweets: [],
    notes: "",
  };

  if (!process.env.OPENAI_API_KEY) {
    payload.tweets.push(
      buildStubTweet({ filePath, message, canonicalUrl, allowedHashtags })
    );
    payload.notes =
      "Stub tweet generated because OPENAI_API_KEY is not set. Update the environment to enable LLM summaries.";
    return payload;
  }

  const response = await callOpenAI({
    commit,
    message,
    filePath,
    diff,
    canonicalUrl,
    allowedHashtags,
    maxTweets,
  });

  if (response?.tweets?.length) {
    payload.tweets = response.tweets
      .filter((tweet) => tweet?.text)
      .slice(0, maxTweets)
      .map((tweet) => ({
        text: tweet.text.trim(),
        confidence: Number(tweet.confidence ?? 0),
        hashtags: (tweet.hashtags || []).slice(0, 4),
      }));
  }

  if (response?.notes) {
    payload.notes = response.notes;
  }

  if (payload.tweets.length === 0) {
    payload.tweets.push(
      buildStubTweet({ filePath, message, canonicalUrl, allowedHashtags })
    );
    payload.notes = `${
      payload.notes ? `${payload.notes} ` : ""
    }LLM returned no tweets; fallback stub applied.`;
  }

  return payload;
}

async function callOpenAI({
  commit,
  message,
  filePath,
  diff,
  canonicalUrl,
  allowedHashtags,
  maxTweets,
}) {
  const url = "https://api.openai.com/v1/responses";

  const prompt = [
    "You are the social media voice for the OASIS project.",
    "Convert the supplied Git diff into concise tweet options that highlight user-facing changes.",
    "Always respect the 280 character limit including hashtags and URLs (which count as 23 characters).",
    "Return JSON only.",
    "",
    `Commit: ${commit}`,
    `Message: ${message}`,
    `File: ${filePath}`,
    `Allowed Hashtags: ${allowedHashtags.join(", ") || "None"}`,
    `Link to use if needed: ${canonicalUrl}`,
    "",
    "Diff Snippet:",
    diff,
  ].join("\n");

  const response = await fetch(url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${process.env.OPENAI_API_KEY}`,
    },
    body: JSON.stringify({
      model: process.env.OPENAI_TWEET_MODEL || "gpt-4.1-mini",
      input: [
        {
          role: "system",
          content:
            "You craft professional, engaging tweets for the OASIS project. Always respond with valid JSON matching the provided schema.",
        },
        {
          role: "user",
          content: prompt,
        },
      ],
      response_format: {
        type: "json_schema",
        json_schema: {
          name: "tweet_drafts",
          schema: {
            type: "object",
            properties: {
              tweets: {
                type: "array",
                minItems: 0,
                maxItems: maxTweets,
                items: {
                  type: "object",
                  properties: {
                    text: {
                      type: "string",
                      description:
                        "Tweet content string <= 280 characters including hashtags and URLs.",
                    },
                    confidence: {
                      type: "number",
                      description:
                        "Confidence score between 0 and 1 for the usefulness of the tweet.",
                    },
                    hashtags: {
                      type: "array",
                      items: {
                        type: "string",
                      },
                    },
                  },
                  required: ["text"],
                  additionalProperties: false,
                },
              },
              notes: {
                type: "string",
                description: "Optional notes or considerations for reviewers.",
              },
            },
            required: ["tweets"],
            additionalProperties: false,
          },
        },
      },
      max_output_tokens: 600,
    }),
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(
      `OpenAI API error (${response.status} ${response.statusText}): ${text}`
    );
  }

  const data = await response.json();
  const content = data?.output?.[0]?.content?.[0]?.text;
  if (!content) {
    throw new Error("Unexpected OpenAI response format.");
  }

  try {
    return JSON.parse(content);
  } catch (error) {
    throw new Error(`Failed to parse LLM JSON payload: ${error.message}`);
  }
}

function buildStubTweet({ filePath, message, canonicalUrl, allowedHashtags }) {
  const hashtags = allowedHashtags.slice(0, 2);
  const base = `${message} (${path.basename(filePath)})`;
  const link = canonicalUrl ? ` ${canonicalUrl}` : "";
  const tags = hashtags.length ? ` ${hashtags.join(" ")}` : "";
  const text = `${truncateText(base, 200)}${tags}${link}`;
  return {
    text,
    confidence: 0,
    hashtags,
  };
}

function truncateText(text, maxLength) {
  if (text.length <= maxLength) return text;
  return `${text.slice(0, maxLength - 1)}…`;
}

function readHashtagConfig(filePath) {
  try {
    const raw = fs.readFileSync(filePath, "utf-8");
    return JSON.parse(raw);
  } catch (error) {
    console.warn(
      `Failed to load hashtag config at ${filePath}. Falling back to default.`
    );
    return { default: ["#OASIS"] };
  }
}

function ensureOutputDir(filePath) {
  const dir = path.dirname(filePath);
  fs.mkdirSync(dir, { recursive: true });
}

function readExistingDrafts(filePath) {
  if (!fs.existsSync(filePath)) {
    return [];
  }
  try {
    const raw = fs.readFileSync(filePath, "utf-8");
    const parsed = JSON.parse(raw);
    if (Array.isArray(parsed)) {
      return parsed;
    }
    return [];
  } catch (error) {
    console.warn(`Failed to parse existing drafts at ${filePath}, resetting.`);
    return [];
  }
}

await main();


