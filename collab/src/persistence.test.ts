import { describe, expect, it, vi } from 'vitest'
import { persistUpdate } from './persistence'

describe('persistUpdate', () => {
  it('POSTs the update as base64 to the documents updates endpoint', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true })
    global.fetch = fetchMock as unknown as typeof fetch

    await persistUpdate('doc-1', new Uint8Array([1, 2, 3]), 'token-123', 'http://api.local')

    expect(fetchMock).toHaveBeenCalledWith(
      'http://api.local/documents/doc-1/updates',
      expect.objectContaining({
        method: 'POST',
        headers: expect.objectContaining({ Authorization: 'Bearer token-123' }),
      }),
    )
  })
})
