<!-- pages/documents/[id].vue -->
<script setup lang="ts">
const route = useRoute()
const router = useRouter()
const documentId = route.params.id as string
const { accessToken, user, loadUser, login } = useAuth()
const { getDocument, renameDocument, createVersion, restoreVersion, listVersions, redeemShareLink } = useDocsApi()
await loadUser()

const shareOpen = ref(false)
const versionHistoryOpen = ref(false)
const title = ref('')
const savingTitle = ref(false)
const documentReady = ref(false)
const shareLinkError = ref('')
const editorRef = ref<{
  getSnapshotBase64: () => string
  restoreFromSnapshotBase64: (b64: string) => void
  getCursorPosition: () => number
} | null>(null)
const commentsSidebarRef = ref<{ focusNewComment: () => void } | null>(null)

onMounted(async () => {
  // A genuinely cold tab (no local session — e.g. a share link opened fresh, not via
  // in-app navigation from an already-authenticated tab) has no accessToken yet. Calling
  // getDocument() anyway would just 401 and leave the page stuck on a bare header forever.
  // Redirect to login instead, preserving this exact URL (including any ?share= token) so
  // the visitor lands back here — not on the home page — once signed in.
  if (!user.value) {
    await login(route.fullPath)
    return
  }

  const shareToken = route.query.share as string | undefined
  if (shareToken) {
    try {
      await redeemShareLink(documentId, shareToken)
    } catch (err: any) {
      // Invalid/expired link — fall through to the normal getDocument() call below,
      // which will surface its own permission error if the visitor truly has no access.
      const status = err?.response?.status ?? err?.statusCode
      shareLinkError.value =
        status === 410
          ? 'This share link has expired.'
          : 'This share link is invalid.'
    }
    await router.replace({ query: {} })
  }

  // Only mount the editor/comments children once any share-link redemption has been
  // attempted — otherwise CommentsSidebar's own onMounted (which fires before this
  // parent onMounted, per Vue's child-before-parent mount order) can fetch comments
  // before the redeemed permission actually exists, causing a transient 403 on a
  // visitor's very first click-through from a share link.
  documentReady.value = true

  const doc = (await getDocument(documentId)) as { title: string }
  title.value = doc.title
})

async function saveTitle() {
  const trimmed = title.value.trim()
  if (!trimmed) {
    title.value = 'Untitled document'
  }
  savingTitle.value = true
  try {
    await renameDocument(documentId, title.value)
  } finally {
    savingTitle.value = false
  }
}

async function handleSaveVersion(label: string | undefined) {
  const snapshot = editorRef.value?.getSnapshotBase64()
  if (!snapshot) return
  await createVersion(documentId, snapshot, label)
}

async function handleRestoreVersion(versionId: string) {
  const versions = (await listVersions(documentId)) as Array<{ id: string; snapshot: string }>
  const version = versions.find((v) => v.id === versionId)
  if (!version) return
  editorRef.value?.restoreFromSnapshotBase64(version.snapshot)
  await restoreVersion(documentId, versionId)
}
</script>

<template>
  <div class="flex min-h-screen flex-col">
    <header class="glass-panel sticky top-0 z-10 flex items-center justify-between gap-4 border-x-0 border-t-0 px-6 py-3">
      <div class="flex min-w-0 items-center gap-3">
        <NuxtLink to="/" class="shrink-0">
          <img src="/logo.png" alt="Innovayse" class="h-7 w-7 rounded-lg" />
        </NuxtLink>
        <input
          v-model="title"
          placeholder="Untitled document"
          class="min-w-0 max-w-xs rounded-[var(--radius-input)] border-0 bg-transparent px-2 py-1 text-sm font-semibold text-[var(--text-heading)] placeholder:text-[var(--text-muted)] focus:bg-[var(--input-bg)] focus:outline-none focus:ring-1 focus:ring-[var(--accent-start)]"
          @keyup.enter="($event.target as HTMLInputElement).blur()"
          @blur="saveTitle"
        />
        <span v-if="savingTitle" class="text-xs text-[var(--text-muted)]">Saving…</span>
      </div>
      <div class="flex items-center gap-3">
        <ClientOnly>
          <span v-if="user" class="hidden text-xs text-[var(--text-subtitle)] sm:inline">
            {{ user.profile.email }}
          </span>
        </ClientOnly>
        <button
          class="rounded-[var(--radius-input)] border border-white/10 px-3 py-2 text-xs font-medium text-[var(--text-subtitle)] transition hover:bg-white/5 hover:text-[var(--text-heading)]"
          @click="versionHistoryOpen = true"
        >
          History
        </button>
        <button
          class="accent-gradient rounded-[var(--radius-input)] px-4 py-2 text-xs font-semibold text-white shadow-md shadow-sky-500/20 transition hover:brightness-110"
          @click="shareOpen = true"
        >
          Share
        </button>
      </div>
    </header>

    <div
      v-if="shareLinkError"
      class="mx-auto mt-4 w-full max-w-6xl rounded-[var(--radius-input)] border border-red-400/30 bg-red-400/10 px-4 py-2 text-sm text-red-400"
    >
      {{ shareLinkError }}
    </div>

    <div class="mx-auto flex w-full max-w-6xl flex-1 items-stretch gap-6 px-6 py-8">
      <section v-if="documentReady" class="glass-panel min-w-0 flex-1 rounded-[var(--radius-card)] py-8">
        <ClientOnly>
          <CollaborativeEditor
            v-if="accessToken"
            ref="editorRef"
            :document-id="route.params.id as string"
            :access-token="accessToken"
            :user-name="user?.profile.name ?? 'Anonymous'"
            @insert-comment="commentsSidebarRef?.focusNewComment()"
          />
        </ClientOnly>
      </section>

      <aside v-if="documentReady" class="hidden w-80 shrink-0 lg:block">
        <CommentsSidebar
          ref="commentsSidebarRef"
          :document-id="route.params.id as string"
          :get-anchor-position="() => editorRef?.getCursorPosition() ?? 0"
        />
      </aside>
    </div>

    <ShareDialog
      :document-id="route.params.id as string"
      :open="shareOpen"
      @close="shareOpen = false"
    />
    <VersionHistoryDialog
      :document-id="documentId"
      :open="versionHistoryOpen"
      :on-save-version="handleSaveVersion"
      :on-restore-version="handleRestoreVersion"
      @close="versionHistoryOpen = false"
    />
  </div>
</template>
