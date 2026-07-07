<!-- pages/documents/[id].vue -->
<script setup lang="ts">
const route = useRoute()
const documentId = route.params.id as string
const { accessToken, user, loadUser } = useAuth()
const { getDocument, renameDocument } = useDocsApi()
await loadUser()

const shareOpen = ref(false)
const title = ref('')
const savingTitle = ref(false)

onMounted(async () => {
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
        <span v-if="user" class="hidden text-xs text-[var(--text-subtitle)] sm:inline">
          {{ user.profile.email }}
        </span>
        <button
          class="accent-gradient rounded-[var(--radius-input)] px-4 py-2 text-xs font-semibold text-white shadow-md shadow-sky-500/20 transition hover:brightness-110"
          @click="shareOpen = true"
        >
          Share
        </button>
      </div>
    </header>

    <div class="mx-auto flex w-full max-w-6xl flex-1 items-stretch gap-6 px-6 py-8">
      <section class="glass-panel min-w-0 flex-1 rounded-[var(--radius-card)] px-10 py-8">
        <ClientOnly>
          <CollaborativeEditor
            v-if="accessToken"
            :document-id="route.params.id as string"
            :access-token="accessToken"
            :user-name="user?.profile.name ?? 'Anonymous'"
          />
        </ClientOnly>
      </section>

      <aside class="hidden w-80 shrink-0 lg:block">
        <CommentsSidebar :document-id="route.params.id as string" />
      </aside>
    </div>

    <ShareDialog
      :document-id="route.params.id as string"
      :open="shareOpen"
      @close="shareOpen = false"
    />
  </div>
</template>
