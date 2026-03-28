import { writeConfig, ensureStarDir } from '../config'
import { log } from '../logger'

const TEAL  = '\x1b[38;2;45;212;191m'
const RESET = '\x1b[0m'
const BOLD  = '\x1b[1m'
const DIM   = '\x1b[2m'

export async function connect(): Promise<void> {
  ensureStarDir()

  console.log(`\n${BOLD}${TEAL}STAR Watch${RESET} — Connect to OASIS\n`)

  const { default: inquirer } = await import('inquirer')

  const answers = await inquirer.prompt([
    {
      type:    'input',
      name:    'apiBase',
      message: 'STAR API endpoint:',
      default: 'https://api.oaisweb4.one',
    },
    {
      type:    'input',
      name:    'username',
      message: 'OASIS username:',
    },
    {
      type:    'password',
      name:    'password',
      message: 'OASIS password:',
      mask:    '•',
    },
    {
      type:    'input',
      name:    'org',
      message: 'Organisation ID (e.g. acme-corp):',
    },
  ])

  console.log()
  const { default: ora } = await import('ora')
  const spinner = ora('Authenticating with OASIS…').start()

  try {
    const res = await fetch(`${answers.apiBase}/api/avatar/authenticate`, {
      method:  'POST',
      headers: { 'Content-Type': 'application/json' },
      body:    JSON.stringify({ username: answers.username, password: answers.password }),
    })

    if (!res.ok) {
      spinner.fail(`Authentication failed (${res.status})`)
      process.exit(1)
    }

    const data = await res.json() as { result?: { jwtToken?: string; avatarId?: string } }
    const token = data?.result?.jwtToken

    if (!token) {
      spinner.fail('No token returned — check your credentials')
      process.exit(1)
    }

    writeConfig({
      avatar:   answers.username,
      token,
      apiBase:  answers.apiBase,
      org:      answers.org,
      connectors: {},
      sops: {
        autoLoad: true,
        orgId:    answers.org,
      },
      escalation: {
        defaultChannel:       'slack',
        requireSignOff:       true,
        notifyOnAutoComplete: false,
        digestSchedule:       '0 9 * * MON-FRI',
      },
    })

    spinner.succeed(`Connected as ${TEAL}${answers.username}${RESET}`)
    console.log(`\n${DIM}Config saved to ~/.star/config.json${RESET}`)
    console.log(`\nNext: add your first connector with ${TEAL}star add slack${RESET}\n`)

  } catch (err) {
    spinner.fail(`Connection error: ${err}`)
    process.exit(1)
  }
}
