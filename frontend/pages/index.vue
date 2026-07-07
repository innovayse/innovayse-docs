<script setup lang="ts">
const { user, login, logout, loadUser } = useAuth()
const { listDocuments, createDocument } = useDocsApi()

interface DocSummary {
  id: string
  title: string
  updatedAt: string
}

const documents = ref<DocSummary[]>([])
const loadingDocuments = ref(false)
const creating = ref(false)

async function refreshDocuments() {
  loadingDocuments.value = true
  try {
    documents.value = (await listDocuments()) as DocSummary[]
  } finally {
    loadingDocuments.value = false
  }
}

async function createBlankDocument() {
  creating.value = true
  try {
    const doc = (await createDocument('Untitled document')) as DocSummary
    await navigateTo(`/documents/${doc.id}`)
  } finally {
    creating.value = false
  }
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

onMounted(async () => {
  await loadUser()
  if (user.value) await refreshDocuments()
})
</script>

<template>
  <main class="min-h-screen">
    <template v-if="user">
      <header class="glass-panel sticky top-0 z-10 flex items-center justify-between gap-4 border-x-0 border-t-0 px-6 py-3">
        <div class="flex items-center gap-3">
          <img src="/logo.png" alt="Innovayse" class="h-7 w-7 rounded-lg" />
          <span class="text-sm font-semibold text-[var(--text-heading)]">Documents</span>
        </div>
        <div class="flex items-center gap-3">
          <span class="hidden text-xs text-[var(--text-subtitle)] sm:inline">{{ user.profile.email }}</span>
          <button
            class="rounded-[var(--radius-input)] border border-white/10 px-3 py-1.5 text-xs font-medium text-[var(--text-subtitle)] transition hover:bg-white/5 hover:text-[var(--text-heading)]"
            @click="logout"
          >
            Log out
          </button>
        </div>
      </header>

      <div class="mx-auto max-w-5xl px-6 py-10">
        <h2 class="mb-4 text-sm font-semibold text-[var(--text-subtitle)]">Start a new document</h2>
        <button
          class="glass-panel group flex h-40 w-32 flex-col items-center justify-center gap-2 rounded-[var(--radius-panel)] transition hover:ring-2 hover:ring-[var(--accent-start)] disabled:opacity-50"
          :disabled="creating"
          @click="createBlankDocument"
        >
          <span class="accent-gradient flex h-9 w-9 items-center justify-center rounded-full text-lg font-bold text-white">
            +
          </span>
          <span class="text-xs text-[var(--text-subtitle)] group-hover:text-[var(--text-heading)]">
            {{ creating ? 'Creating…' : 'Blank document' }}
          </span>
        </button>

        <h2 class="mb-4 mt-10 text-sm font-semibold text-[var(--text-subtitle)]">Recent documents</h2>

        <p v-if="loadingDocuments" class="text-sm text-[var(--text-muted)]">Loading…</p>
        <p v-else-if="!documents.length" class="text-sm text-[var(--text-muted)]">
          No documents yet — create one above to get started.
        </p>
        <div v-else class="grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4">
          <NuxtLink
            v-for="doc in documents"
            :key="doc.id"
            :to="`/documents/${doc.id}`"
            class="glass-panel group flex flex-col overflow-hidden rounded-[var(--radius-panel)] transition hover:ring-2 hover:ring-[var(--accent-start)]"
          >
            <div class="flex h-28 items-center justify-center border-b border-white/10 bg-white/[0.02]">
              <span class="text-2xl">📄</span>
            </div>
            <div class="px-3 py-2">
              <p class="truncate text-sm font-medium text-[var(--text-heading)]">{{ doc.title }}</p>
              <p class="text-xs text-[var(--text-muted)]">{{ formatDate(doc.updatedAt) }}</p>
            </div>
          </NuxtLink>
        </div>
      </div>
    </template>

    <div v-else class="flex min-h-screen flex-col items-center justify-center gap-8 px-6">
      <div class="flex flex-col items-center gap-3">
        <img src="/logo.png" alt="Innovayse" class="h-12 w-12 rounded-xl" />
        <h1 class="text-2xl font-extrabold tracking-tight text-[var(--text-heading)]">Innovayse Docs</h1>
        <p class="text-sm text-[var(--text-subtitle)]">Collaborative documents for your workspace</p>
      </div>

      <div class="glass-panel w-full max-w-sm rounded-[var(--radius-card)] p-8 text-center">
        <p class="text-sm text-[var(--text-subtitle)]">Sign in with your Innovayse account to continue.</p>
        <button
          class="accent-gradient mt-6 w-full rounded-[var(--radius-input)] px-4 py-2.5 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 transition hover:brightness-110"
          @click="login"
        >
          Log in
        </button>
      </div>
    </div>
  </main>
</template>
