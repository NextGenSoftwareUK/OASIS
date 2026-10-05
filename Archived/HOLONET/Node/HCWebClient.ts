import { Client } from 'rpc-websockets'

const CONDUCTOR_CONFIG = '/_dna_connections.json'

type Call = (...segments: Array<string>) => (params: any) => Promise<any>
type CallZome = (instanceId: string, zome: string, func: string) => (params: any) => Promise<any>
type Close = () => Promise<any>

export const connect = (paramUrl?: string) => new Promise<{call: Call, callZome: CallZome, close: Close, ws: any}>(async (fulfill, reject) => {
  const url = paramUrl || await getUrlFromContainer().catch(() => reject(
    'Could not auto-detect DNA interface from conductor. \
Ensure the web UI is hosted by a Holochain Conductor or manually specify url as parameter to connect'))

  const ws = new Client(url)
  ws.on('open', () => {
    const call = (...methodSegments) => (params) => {
      const method = methodSegments.length === 1 ? methodSegments[0] : methodSegments.join('/')
      return ws.call(method, params)
    }
    const callZome = (instanceId, zome, func) => (params) => {
      const callObject = {
        'instance_id': instanceId,
        zome,
        'function': func,
        params
      }
      return ws.call('call', callObject)
    }
    // define a function which will close the websocket connection
    const close = () => ws.close()
    fulfill({ call, callZome, close, ws })
  })
})

function getUrlFromContainer (): Promise<string> {
  return fetch(CONDUCTOR_CONFIG)
    .then(data => data.json())
    .then(json => json.dna_interface.driver.port)
    .then(port => `ws://localhost:${port}`)
}

const holochainclient = { connect }
