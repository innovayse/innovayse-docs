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
    inviteUser: (documentId: string, email: string, role: string, inviterName: string) =>
      authedFetch(`/documents/${documentId}/share/invite`, {
        method: 'POST',
        body: { email, role, inviterName },
      }),
    inviteToFolder: (folderId: string, email: string, role: string, inviterName: string) =>
      authedFetch(`/folders/${folderId}/share/invite`, {
        method: 'POST',
        body: { email, role, inviterName },
      }),
    listNotifications: () => authedFetch('/notifications'),
    markNotificationRead: (id: string) =>
      authedFetch(`/notifications/${id}/read`, { method: 'PATCH' }),
    markAllNotificationsRead: () =>
      authedFetch('/notifications/mark-all-read', { method: 'POST' }),
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
    listTabs: (documentId: string) => authedFetch(`/documents/${documentId}/tabs`),
    createTab: (documentId: string, title: string) =>
      authedFetch(`/documents/${documentId}/tabs`, { method: 'POST', body: { title } }),
    renameTab: (documentId: string, tabId: string, title: string) =>
      authedFetch(`/documents/${documentId}/tabs/${tabId}`, { method: 'PATCH', body: { title } }),
    reorderTab: (documentId: string, tabId: string, orderIndex: number) =>
      authedFetch(`/documents/${documentId}/tabs/${tabId}`, { method: 'PATCH', body: { orderIndex } }),
    deleteTab: (documentId: string, tabId: string) =>
      authedFetch(`/documents/${documentId}/tabs/${tabId}`, { method: 'DELETE' }),
  }
}
