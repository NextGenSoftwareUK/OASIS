import { updateConfig } from '../config'
import { log } from '../logger'

const TEAL  = '\x1b[38;2;45;212;191m'
const RESET = '\x1b[0m'

export async function addConnector(connector: string): Promise<void> {
  const { default: inquirer } = await import('inquirer')

  switch (connector.toLowerCase()) {

    case 'slack': {
      console.log(`\n${TEAL}Connecting Slack${RESET} (Socket Mode — no public URL required)\n`)
      const answers = await inquirer.prompt([
        { type: 'input',    name: 'token',      message: 'Slack Bot Token (xoxb-…):' },
        { type: 'input',    name: 'appToken',   message: 'Slack App-Level Token (xapp-…):' },
        { type: 'input',    name: 'deliverTo',  message: 'Channel for STAR alerts (e.g. #star-alerts):', default: '#star-alerts' },
        { type: 'input',    name: 'channels',   message: 'Channels to watch (comma-separated, blank = all):' },
      ])
      updateConfig({
        connectors: {
          slack: {
            token:     answers.token,
            appToken:  answers.appToken,
            deliverTo: answers.deliverTo,
            channels:  answers.channels ? answers.channels.split(',').map((c: string) => c.trim()) : [],
          },
        },
      })
      log.success('Slack connector saved. Restart `star watch` to activate.')
      break
    }

    case 'salesforce': {
      console.log(`\n${TEAL}Connecting Salesforce${RESET}\n`)
      const answers = await inquirer.prompt([
        { type: 'input',    name: 'instanceUrl',  message: 'Salesforce instance URL (e.g. https://acme.salesforce.com):' },
        { type: 'input',    name: 'accessToken',  message: 'Access token:' },
        { type: 'input',    name: 'watchObjects', message: 'Objects to watch (comma-separated):', default: 'Opportunity,Case' },
      ])
      updateConfig({
        connectors: {
          salesforce: {
            instanceUrl:  answers.instanceUrl,
            accessToken:  answers.accessToken,
            watchObjects: answers.watchObjects.split(',').map((o: string) => o.trim()),
          },
        },
      })
      log.success('Salesforce connector saved. Restart `star watch` to activate.')
      break
    }

    case 'github': {
      console.log(`\n${TEAL}Connecting GitHub${RESET}\n`)
      const answers = await inquirer.prompt([
        { type: 'input', name: 'token', message: 'GitHub personal access token (ghp_…):' },
        { type: 'input', name: 'repos', message: 'Repos to watch (comma-separated, e.g. acme/backend):' },
      ])
      updateConfig({
        connectors: {
          github: {
            token: answers.token,
            repos: answers.repos.split(',').map((r: string) => r.trim()),
          },
        },
      })
      log.success('GitHub connector saved. Restart `star watch` to activate.')
      break
    }

    case 'email': {
      console.log(`\n${TEAL}Connecting Email (IMAP)${RESET}\n`)
      const answers = await inquirer.prompt([
        { type: 'input',    name: 'host',     message: 'IMAP host (e.g. imap.gmail.com):' },
        { type: 'input',    name: 'user',     message: 'Email address:' },
        { type: 'password', name: 'password', message: 'Password / app-specific password:', mask: '•' },
      ])
      updateConfig({
        connectors: {
          email: { host: answers.host, user: answers.user, password: answers.password },
        },
      })
      log.success('Email connector saved. Restart `star watch` to activate.')
      break
    }

    default:
      console.error(`Unknown connector: ${connector}. Supported: slack, salesforce, github, email, jira`)
      process.exit(1)
  }
}
