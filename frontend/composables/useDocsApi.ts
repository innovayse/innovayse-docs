export function useDocsApi() {
  const { accessToken } = useAuth()
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase as string

  const authedFetch = (path: string, init: Record<string, any> = {}) =>
    $fetch(`${apiBase}${path}`, {
      ...init,
      headers: { ...init.headers, Authorization: `Bearer ${accessToken.value}` },
    })

  return {
    listDocuments: () => authedFetch('/documents'),
    createDocument: (title: string) =>
      authedFetch('/documents', { method: 'POST', body: { title } }),
    getDocument: (documentId: string) => authedFetch(`/documents/${documentId}`),
    renameDocument: (documentId: string, title: string) =>
      authedFetch(`/documents/${documentId}`, { method: 'PATCH', body: { title } }),
    deleteDocument: (documentId: string) =>
      authedFetch(`/documents/${documentId}`, { method: 'DELETE' }),
    moveDocument: (documentId: string, folderId: string | null) =>
      authedFetch(`/documents/${documentId}/folder`, { method: 'PATCH', body: { folderId } }),
    listFolders: () => authedFetch('/folders'),
    createFolder: (name: string) =>
      authedFetch('/folders', { method: 'POST', body: { name } }),
    inviteUser: (documentId: string, userId: string, role: string) =>
      authedFetch(`/documents/${documentId}/share/invite`, {
        method: 'POST',
        body: { userId, role },
      }),
    createShareLink: (documentId: string, role: string) =>
      authedFetch(`/documents/${documentId}/share/link`, {
        method: 'POST',
        body: { role },
      }),
    listComments: (documentId: string) =>
      authedFetch(`/documents/${documentId}/comments`),
    createComment: (documentId: string, text: string, anchorPosition: number) =>
      authedFetch(`/documents/${documentId}/comments`, {
        method: 'POST',
        body: { text, anchorPosition },
      }),
  }
}
