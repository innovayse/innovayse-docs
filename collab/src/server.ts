import { Server } from '@hocuspocus/server'
import { authorize } from './authorize'

const API_BASE_URL = process.env.DOCS_API_BASE_URL ?? 'http://localhost:5000'

const server = new Server({
  port: Number(process.env.COLLAB_PORT ?? 1234),
  async onAuthenticate(data: { token: string; documentName: string }) {
    const result = await authorize(data.token, data.documentName, API_BASE_URL)
    if (!result) {
      throw new Error('Unauthorized')
    }
    // Readonly connections are only allowed to view, not write, updates.
    return { role: result.role, readOnly: result.role === 'Viewer' }
  },
})

server.listen()
