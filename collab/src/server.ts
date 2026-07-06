import { Server } from '@hocuspocus/server'
import type { onAuthenticatePayload } from '@hocuspocus/server'
import { authorize } from './authorize'

const API_BASE_URL = process.env.DOCS_API_BASE_URL ?? 'http://localhost:5000'

const server = new Server({
  port: Number(process.env.COLLAB_PORT ?? 1234),
  async onAuthenticate(data: onAuthenticatePayload) {
    const result = await authorize(data.token, data.documentName, API_BASE_URL)
    if (!result) {
      throw new Error('Unauthorized')
    }
    // NOTE: Hocuspocus v4 does NOT apply a returned `readOnly` field to the
    // connection — the return value is only merged into hookPayload.context.
    // Write permission is controlled via data.connectionConfig.readOnly,
    // which must be mutated directly here.
    data.connectionConfig.readOnly = result.role === 'Viewer'
    return { role: result.role }
  },
})

server.listen()
