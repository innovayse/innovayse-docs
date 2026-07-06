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
