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
    createFolder: (name: string, parentFolderId: string | null) =>
      authedFetch('/folders', { method: 'POST', body: { name, parentFolderId } }),
    deleteFolder: (folderId: string) =>
      authedFetch(`/folders/${folderId}`, { method: 'DELETE' }),
    inviteUser: (documentId: string, email: string, role: string) =>
      authedFetch(`/documents/${documentId}/share/invite`, {
        method: 'POST',
        body: { email, role },
      }),
    createShareLink: (documentId: string, role: string) =>
      authedFetch(`/documents/${documentId}/share/link`, {
        method: 'POST',
        body: { role },
      }),
    redeemShareLink: (documentId: string, token: string) =>
      authedFetch(`/documents/${documentId}/share/redeem`, {
        method: 'POST',
        body: { token },
      }),
    listComments: (documentId: string) =>
      authedFetch(`/documents/${documentId}/comments`),
    createComment: (
      documentId: string,
      text: string,
      anchorPosition: number,
      authorName: string,
      parentCommentId?: string,
    ) =>
      authedFetch(`/documents/${documentId}/comments`, {
        method: 'POST',
        body: { text, anchorPosition, authorName, parentCommentId },
      }),
    setCommentResolved: (documentId: string, commentId: string, resolved: boolean) =>
      authedFetch(`/documents/${documentId}/comments/${commentId}`, {
        method: 'PATCH',
        body: { resolved },
      }),
    listVersions: (documentId: string) =>
      authedFetch(`/documents/${documentId}/versions`),
    createVersion: (documentId: string, snapshotBase64: string, label?: string) =>
      authedFetch(`/documents/${documentId}/versions`, {
        method: 'POST',
        body: { snapshotBase64, label },
      }),
    restoreVersion: (documentId: string, versionId: string) =>
      authedFetch(`/documents/${documentId}/versions/${versionId}/restore`, { method: 'POST' }),
  }
}
