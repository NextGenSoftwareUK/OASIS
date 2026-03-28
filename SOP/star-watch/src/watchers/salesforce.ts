import type { ObservedEvent } from '../types'
import { readConfig } from '../config'
import { log } from '../logger'

// ─── Salesforce Watcher ───────────────────────────────────────────────────
// Uses Salesforce Streaming API (server-sent events via long polling).
// Watches for record changes on configured objects (Opportunity, Case, etc.)
//
// NOTE: Requires the org admin to create PushTopics in Salesforce:
//   SELECT Id, Name, StageName, OwnerId FROM Opportunity WHERE StageName = 'Closed Won'
// star-watch can create these automatically during `star add salesforce`.

type EventCallback = (event: ObservedEvent) => void

const POLL_INTERVAL_MS = 15_000  // fallback polling interval

export async function startSalesforceWatcher(emit: EventCallback): Promise<void> {
  const config = readConfig()
  const sfConfig = config.connectors.salesforce as {
    accessToken:   string
    instanceUrl:   string
    watchObjects:  string[]
    refreshToken?: string
  } | undefined

  if (!sfConfig?.accessToken) {
    log.warn('Salesforce connector not configured. Run `star add salesforce` to connect.')
    return
  }

  log.info(`Salesforce watcher starting (objects: ${sfConfig.watchObjects.join(', ')})`)

  // Use polling as the Phase 1 implementation.
  // Phase 2 will switch to CometD/Bayeux streaming for real-time.
  pollSalesforce(sfConfig, emit)
  log.success('Salesforce watcher connected (polling every 15s)')
}

async function pollSalesforce(
  sfConfig: { accessToken: string; instanceUrl: string; watchObjects: string[] },
  emit: EventCallback
): Promise<void> {
  const lastSeen = new Map<string, string>()  // objectType → lastModifiedDate

  async function poll() {
    for (const objectType of sfConfig.watchObjects) {
      try {
        const since = lastSeen.get(objectType) ?? new Date(Date.now() - POLL_INTERVAL_MS * 2).toISOString()
        const soql = buildSOQL(objectType, since)
        const url  = `${sfConfig.instanceUrl}/services/data/v60.0/query?q=${encodeURIComponent(soql)}`

        const res = await fetch(url, {
          headers: { Authorization: `Bearer ${sfConfig.accessToken}` },
        })

        if (!res.ok) {
          log.error(`Salesforce poll failed for ${objectType}: ${res.status}`)
          continue
        }

        const data = await res.json() as { records: SalesforceRecord[] }

        for (const record of data.records) {
          emit(normaliseSalesforceRecord(record, objectType))
          const ts = record.LastModifiedDate as string
          if (!lastSeen.has(objectType) || ts > lastSeen.get(objectType)!) {
            lastSeen.set(objectType, ts)
          }
        }
      } catch (err) {
        log.error(`Salesforce poll error: ${err}`)
      }
    }
    setTimeout(poll, POLL_INTERVAL_MS)
  }

  poll()
}

interface SalesforceRecord {
  Id:               string
  Name?:            string
  StageName?:       string
  Status?:          string
  OwnerId?:         string
  Owner?:           { Name: string; Email?: string }
  LastModifiedDate: string
  LastModifiedById?: string
  [key: string]:    unknown
}

function buildSOQL(objectType: string, since: string): string {
  const base = `SELECT Id, Name, LastModifiedDate, LastModifiedById, OwnerId`
  const extras = objectType === 'Opportunity' ? ', StageName' :
                 objectType === 'Case'        ? ', Status' : ''
  return `${base}${extras} FROM ${objectType} WHERE LastModifiedDate > ${since} ORDER BY LastModifiedDate DESC LIMIT 50`
}

function normaliseSalesforceRecord(record: SalesforceRecord, objectType: string): ObservedEvent {
  const contextParts: string[] = [`${objectType} "${record.Name}" was modified`]
  if (record.StageName) contextParts.push(`Stage: ${record.StageName}`)
  if (record.Status)    contextParts.push(`Status: ${record.Status}`)

  return {
    id:        `sf-${record.Id}-${record.LastModifiedDate}`,
    source:    'salesforce',
    timestamp: new Date(record.LastModifiedDate as string),
    actor: {
      id:   record.LastModifiedById ?? record.OwnerId ?? 'system',
      name: record.Owner?.Name ?? record.OwnerId ?? 'System',
    },
    action:    inferSalesforceAction(record, objectType),
    entity: {
      type: objectType.toLowerCase(),
      id:   record.Id,
      name: record.Name,
      url:  `${readConfig().connectors.salesforce ? (readConfig().connectors.salesforce as { instanceUrl: string }).instanceUrl : ''}/lightning/r/${objectType}/${record.Id}/view`,
    },
    payload: record as unknown as Record<string, unknown>,
    context: contextParts,
  }
}

function inferSalesforceAction(record: SalesforceRecord, objectType: string): string {
  if (objectType === 'Opportunity' && record.StageName) return `stage_changed_to_${record.StageName.toLowerCase().replace(/\s/g, '_')}`
  if (objectType === 'Case' && record.Status)           return `status_changed_to_${record.Status.toLowerCase().replace(/\s/g, '_')}`
  return `${objectType.toLowerCase()}_updated`
}
