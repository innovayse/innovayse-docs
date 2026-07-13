export interface AuthorizeResult {
  role: string
}

export async function authorize(
  token: string,
  roomName: string,
  apiBaseUrl: string,
): Promise<AuthorizeResult | null> {
  const [documentId] = roomName.split(':')

  const response = await fetch(
    `${apiBaseUrl}/internal/collab/authorize?documentId=${documentId}`,
    { headers: { Authorization: `Bearer ${token}` } },
  )

  if (!response.ok) return null
  return (await response.json()) as AuthorizeResult
}
