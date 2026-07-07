export async function persistUpdate(
  documentId: string,
  update: Uint8Array,
  token: string,
  apiBaseUrl: string,
): Promise<void> {
  await fetch(`${apiBaseUrl}/documents/${documentId}/updates`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ updateBase64: Buffer.from(update).toString('base64') }),
  })
}

export async function loadUpdates(
  documentId: string,
  token: string,
  apiBaseUrl: string,
): Promise<Uint8Array[]> {
  const response = await fetch(`${apiBaseUrl}/documents/${documentId}/updates`, {
    headers: { Authorization: `Bearer ${token}` },
  })
  if (!response.ok) return []

  const updates = (await response.json()) as Array<{ updateBase64: string }>
  return updates.map((u) => new Uint8Array(Buffer.from(u.updateBase64, 'base64')))
}
