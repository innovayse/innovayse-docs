export interface AuthorizeResult {
  role: string
}

export async function authorize(
  token: string,
  documentId: string,
  apiBaseUrl: string,
): Promise<AuthorizeResult | null> {
  const response = await fetch(
    `${apiBaseUrl}/internal/collab/authorize?documentId=${documentId}`,
    { headers: { Authorization: `Bearer ${token}` } },
  )

  if (!response.ok) return null
  return (await response.json()) as AuthorizeResult
}
