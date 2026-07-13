import { describe, expect, it, vi } from 'vitest'
import { authorize } from './authorize'

describe('authorize', () => {
  it('returns the role when the API responds 200', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ role: 'Editor' }),
    }) as unknown as typeof fetch

    const result = await authorize('valid-token', 'doc-1:tab-1', 'http://api.local')

    expect(result).toEqual({ role: 'Editor' })
  })

  it('returns null when the API responds 403', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: false, status: 403 }) as unknown as typeof fetch

    const result = await authorize('bad-token', 'doc-1:tab-1', 'http://api.local')

    expect(result).toBeNull()
  })

  it('parses documentId out of a "{documentId}:{tabId}" room name for the authorize call', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => ({ role: 'Editor' }) })
    global.fetch = fetchMock as unknown as typeof fetch

    await authorize('valid-token', 'doc-1:tab-1', 'http://api.local')

    expect(fetchMock).toHaveBeenCalledWith(
      'http://api.local/internal/collab/authorize?documentId=doc-1',
      expect.anything(),
    )
  })
})
