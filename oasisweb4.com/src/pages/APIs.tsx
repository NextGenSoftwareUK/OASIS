import { Card, CardContent, CardHeader, Grid, Typography, Chip, Box } from '@mui/material';

const web4Apis = [
  { name: 'AVATAR', desc: 'Centralised user identity across the internet. Register, authenticate, manage profiles.' },
  { name: 'KARMA', desc: 'Track positive actions and build digital reputation. Transfer, history, leaderboards.' },
  { name: 'DATA', desc: 'Move and share data seamlessly between Web2 and Web3.' },
  { name: 'WALLET', desc: 'High-security cross-chain wallet with multi-provider support.' },
  { name: 'NFT', desc: 'Cross-chain NFTs with geo-caching for AR and gaming experiences.' },
  { name: 'KEYS', desc: 'Secure encrypted key storage and backup per avatar.' },
];

const web5Apis = [
  { name: 'QUEST', desc: 'Create and complete quests. Track objectives and rewards.' },
  { name: 'MISSION', desc: 'Multi-avatar missions with leaderboards, rewards, and stats.' },
  { name: 'OAPP', desc: 'OASIS Applications — download, install, activate, manage.' },
  { name: 'HOLON', desc: 'STAR holon graph — load, search, download, install holons.' },
  { name: 'CELESTIAL BODY', desc: 'Celestial bodies in the STAR ODK universe.' },
];

const web6Apis = [
  { name: 'AI COMPLETION', desc: 'Unified completion across 15+ providers — OpenAI, Anthropic, Gemini, Groq, Mistral, and more. Auto routing, failover, streaming.' },
  { name: 'FAHRN', desc: 'Fractal Adaptive Holonic Reasoning Network. Multi-agent orchestration with Serial, Parallel, Debate, Voting, and Decomposed modes.' },
  { name: 'HOLONIC BRAID', desc: 'Shared Mermaid reasoning graphs stored as holons. Agents reuse and improve graphs across sessions via EMA scoring.' },
  { name: 'HOLONIC MEMORY', desc: 'Fractal memory hierarchy from Session to Earth. Membrane rules, semantic search, TTL policies, multi-hop propagation.' },
  { name: 'EXTERNAL MEMORY', desc: 'Plug in Mem0, Zep, Letta, LangMem, or Graphiti. WEB6 searches them all and injects context automatically.' },
  { name: 'ORCHESTRATORS', desc: 'Connect any external agent — MCP, A2A, ACP, ANP, GraphQL, Kafka, AMQP, MQTT.' },
  { name: 'EMBEDDINGS', desc: 'Float embeddings via OpenAI, Cohere, or HuggingFace. One endpoint, normalised output.' },
  { name: 'DID / VC', desc: 'W3C Decentralised Identifiers and Verifiable Credentials. Deterministic did:key from avatar GUID.' },
  { name: 'TELEMETRY', desc: 'Real-time SSE stream of per-request events. Provider, model, tokens, cost, latency.' },
  { name: 'ML.NET', desc: 'In-process task classification, sentiment analysis, and loop anomaly scoring. Zero latency — no API call.' },
  { name: 'SKILLOPT', desc: 'Self-improving agent skill documents. Microsoft Research arXiv:2605.23904. +23.5% avg improvement.' },
  { name: 'WEBSOCKET SESSIONS', desc: 'Bidirectional agent sessions. Streaming chunks, tool calls, interrupts, keepalive.' },
];

type ApiItem = { name: string; desc: string };

function ApiGrid({ title, items, accent }: { title: string; items: ApiItem[]; accent: string }) {
  return (
    <>
      <Grid item xs={12}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 3, mb: 1 }}>
          <Typography variant="h5" fontWeight={700}>{title}</Typography>
          <Chip label={`${items.length} APIs`} size="small" sx={{ bgcolor: accent, color: '#fff' }} />
        </Box>
      </Grid>
      {items.map(api => (
        <Grid item xs={12} md={6} key={api.name}>
          <Card variant="outlined" sx={{ height: '100%' }}>
            <CardHeader
              title={<Typography variant="subtitle1" fontWeight={700}>{api.name}</Typography>}
            />
            <CardContent>
              <Typography color="text.secondary" variant="body2">{api.desc}</Typography>
            </CardContent>
          </Card>
        </Grid>
      ))}
    </>
  );
}

export default function APIs() {
  return (
    <Grid container spacing={2} sx={{ p: 3 }}>
      <Grid item xs={12}>
        <Typography variant="h4" fontWeight={800} gutterBottom>OASIS API Layers</Typography>
        <Typography color="text.secondary" sx={{ mb: 2 }}>
          WEB4 through WEB10 — every layer accessible via REST, MCP tools, or NuGet/npm packages.
        </Typography>
      </Grid>

      <ApiGrid title="WEB4 — Identity, Karma, NFTs, Wallets" items={web4Apis} accent="#1565c0" />
      <ApiGrid title="WEB5 — Quests, Missions, OAPPs" items={web5Apis} accent="#6a1b9a" />
      <ApiGrid title="WEB6 — AI Abstraction & Orchestration" items={web6Apis} accent="#00695c" />
    </Grid>
  );
}
