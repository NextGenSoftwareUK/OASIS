import { readConfig, configExists } from '../config'
import { getActiveRuns } from '../star-api'

const TEAL  = '\x1b[38;2;45;212;191m'
const RESET = '\x1b[0m'
const BOLD  = '\x1b[1m'
const DIM   = '\x1b[2m'
const GREEN = '\x1b[32m'
const GREY  = '\x1b[90m'

export async function showStatus(): Promise<void> {
  if (!configExists()) {
    console.log('\nNot connected. Run `star connect` to get started.\n')
    return
  }

  const config = readConfig()

  console.log(`\n${BOLD}${TEAL}★ STAR Watch${RESET}  ${DIM}status${RESET}\n`)
  console.log(`  ${DIM}Avatar${RESET}   ${config.avatar}`)
  console.log(`  ${DIM}Org${RESET}      ${config.org}`)
  console.log(`  ${DIM}API${RESET}      ${config.apiBase}\n`)

  // Connected integrations
  const connectors = Object.keys(config.connectors ?? {})
  console.log(`${BOLD}Connectors${RESET}  (${connectors.length})`)
  if (connectors.length === 0) {
    console.log(`  ${GREY}None connected. Run \`star add slack\` to connect your first integration.${RESET}`)
  } else {
    for (const name of connectors) {
      console.log(`  ${GREEN}●${RESET}  ${name}`)
    }
  }

  // Active runs
  console.log()
  try {
    const { default: ora } = await import('ora')
    const spinner = ora('Loading active runs…').start()
    const runs = await getActiveRuns(config.sops.orgId)
    spinner.stop()
    console.log(`${BOLD}Active SOP runs${RESET}  (${runs.length})`)
    if (runs.length === 0) {
      console.log(`  ${GREY}No active runs.${RESET}`)
    } else {
      for (const run of runs.slice(0, 10)) {
        const step = run.steps[run.currentStepIndex]
        console.log(`  ${TEAL}●${RESET}  ${run.sopName}  ${DIM}·  Step ${run.currentStepIndex + 1}/${run.steps.length}: ${step?.name ?? 'complete'}${RESET}`)
      }
      if (runs.length > 10) {
        console.log(`  ${DIM}…and ${runs.length - 10} more${RESET}`)
      }
    }
  } catch {
    console.log(`  ${GREY}Could not reach STAR API.${RESET}`)
  }
  console.log()
}
