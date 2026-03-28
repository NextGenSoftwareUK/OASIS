import { writeLink, readLinks } from '../config'
import { log } from '../logger'

export async function linkAvatar(
  source: string,
  sourceId: string,
  options: { avatar?: string; me?: boolean }
): Promise<void> {
  if (options.me) {
    log.info(`Self-linking not yet implemented. Use: star link ${source} <your-source-id> --avatar <email>`)
    return
  }
  if (!options.avatar) {
    console.error('Provide --avatar <email> to link this user to an OASIS Avatar.')
    process.exit(1)
  }

  writeLink({
    source,
    sourceId,
    avatarId:    options.avatar,
    avatarEmail: options.avatar,
    linkedAt:    new Date().toISOString(),
  })

  const all = readLinks()
  log.success(`Linked ${source}:${sourceId} → ${options.avatar} (${all.length} total links)`)
}
