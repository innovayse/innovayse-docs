import { describe, expect, it, vi } from 'vitest'
import { authorize } from './authorize'

describe('authorize', () => {
  it('returns the role when the API responds 200', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ role: 'Editor' }),
    }) as unknown as typeof fetch

    const result = await authorize('valid-token', 'doc-1', 'http://api.local')

    expect(result).toEqual({ role: 'Editor' })
  })

  it('returns null when the API responds 403', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: false, status: 403 }) as unknown as typeof fetch

    const result = await authorize('bad-token', 'doc-1', 'http://api.local')

    expect(result).toBeNull()
  })
})
