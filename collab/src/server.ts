import { Server } from '@hocuspocus/server'
import type { onAuthenticatePayload } from '@hocuspocus/server'
import { authorize } from './authorize'
import { persistUpdate } from './persistence'

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
    // token is stored in the returned context so onChange can use it to
    // authenticate persistence calls back to the Docs API.
    return { role: result.role, token: data.token }
  },
  async onChange(data) {
    await persistUpdate(data.documentName, data.update, data.context.token, API_BASE_URL)
  },
})

server.listen()
