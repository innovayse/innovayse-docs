import { Server } from '@hocuspocus/server'
import type { onAuthenticatePayload, onLoadDocumentPayload } from '@hocuspocus/server'
import * as Y from 'yjs'
import { authorize } from './authorize'
import { persistUpdate, loadUpdates } from './persistence'

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
    // Commenter is comment-only, not document-body-edit — it must be read-only here
    // too, same as Viewer. Only Editor/Owner may write to the shared Y.Doc.
    data.connectionConfig.readOnly = result.role === 'Viewer' || result.role === 'Commenter'
    // token is stored in the returned context so onChange can use it to
    // authenticate persistence calls back to the Docs API.
    return { role: result.role, token: data.token }
  },
  async onChange(data) {
    await persistUpdate(data.documentName, data.update, data.context.token, API_BASE_URL)
  },
  // Hocuspocus starts every newly-loaded Y.Doc empty. Without this, a document
  // reloaded after being evicted from memory (or after a server restart) loses
  // all previously persisted content even though it's still in the updates log.
  async onLoadDocument(data: onLoadDocumentPayload) {
    const updates = await loadUpdates(data.documentName, data.context.token, API_BASE_URL)
    for (const update of updates) {
      Y.applyUpdate(data.document, update)
    }
    return data.document
  },
})

server.listen()
