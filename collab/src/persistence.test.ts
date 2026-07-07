import { describe, expect, it, vi } from 'vitest'
import { loadUpdates, persistUpdate } from './persistence'

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

describe('loadUpdates', () => {
  it('GETs and decodes previously persisted updates', async () => {
    const encoded = Buffer.from(new Uint8Array([1, 2, 3])).toString('base64')
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => [{ updateBase64: encoded }],
    })
    global.fetch = fetchMock as unknown as typeof fetch

    const result = await loadUpdates('doc-1', 'token-123', 'http://api.local')

    expect(fetchMock).toHaveBeenCalledWith(
      'http://api.local/documents/doc-1/updates',
      expect.objectContaining({
        headers: expect.objectContaining({ Authorization: 'Bearer token-123' }),
      }),
    )
    expect(result).toEqual([new Uint8Array([1, 2, 3])])
  })

  it('returns an empty array when the request fails', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: false })
    global.fetch = fetchMock as unknown as typeof fetch

    const result = await loadUpdates('doc-1', 'token-123', 'http://api.local')

    expect(result).toEqual([])
  })
})
