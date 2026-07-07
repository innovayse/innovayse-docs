<script setup lang="ts">
const route = useRoute()
const router = useRouter()
const { user, login, logout, loadUser } = useAuth()
const { listDocuments, createDocument, deleteDocument, moveDocument, listFolders, createFolder, deleteFolder } = useDocsApi()

interface DocSummary {
  id: string
  title: string
  updatedAt: string
  folderId: string | null
}

interface FolderSummary {
  id: string
  name: string
  parentFolderId: string | null
}

const documents = ref<DocSummary[]>([])
const folders = ref<FolderSummary[]>([])
const loadingDocuments = ref(false)
const creating = ref(false)
const creatingFolder = ref(false)
const searchQuery = ref('')
const deletingId = ref<string | null>(null)
const openMenuId = ref<string | null>(null)
const newFolderDialogOpen = ref(false)

/** The current folder lives in the URL (`?folder=<id>`), not local state — so a refresh, a
 * bookmark, or a shared link lands back in the same folder instead of always resetting to Home. */
const currentFolderId = computed(() => (route.query.folder as string | undefined) ?? null)

function goToFolder(folderId: string | null) {
  router.push({ query: folderId ? { folder: folderId } : {} })
}

const subfolders = computed(() => folders.value.filter((f) => f.parentFolderId === currentFolderId.value))

/** Home → … → current folder, for breadcrumb navigation. */
const breadcrumb = computed(() => {
  const chain: FolderSummary[] = []
  let cursor = currentFolderId.value
  while (cursor) {
    const folder = folders.value.find((f) => f.id === cursor)
    if (!folder) break
    chain.unshift(folder)
    cursor = folder.parentFolderId
  }
  return chain
})

const documentsInFolder = computed(() => documents.value.filter((doc) => doc.folderId === currentFolderId.value))

const filteredDocuments = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  if (!query) return documentsInFolder.value
  return documentsInFolder.value.filter((doc) => doc.title.toLowerCase().includes(query))
})

async function refreshDocuments() {
  loadingDocuments.value = true
  try {
    documents.value = (await listDocuments()) as DocSummary[]
  } finally {
    loadingDocuments.value = false
  }
}

async function refreshFolders() {
  folders.value = (await listFolders()) as FolderSummary[]
}

async function createBlankDocument() {
  creating.value = true
  try {
    const doc = (await createDocument('Untitled document')) as DocSummary
    if (currentFolderId.value) await moveDocument(doc.id, currentFolderId.value)
    await navigateTo(`/documents/${doc.id}`)
  } finally {
    creating.value = false
  }
}

async function addFolder(name: string) {
  newFolderDialogOpen.value = false
  creatingFolder.value = true
  try {
    const folder = (await createFolder(name, currentFolderId.value)) as FolderSummary
    folders.value.push(folder)
  } finally {
    creatingFolder.value = false
  }
}

async function removeFolder(folder: FolderSummary) {
  if (!confirm(`Delete folder "${folder.name}"? Documents inside it will move up a level, subfolders too.`)) return
  await deleteFolder(folder.id)
  folders.value = folders.value
    .filter((f) => f.id !== folder.id)
    .map((f) => (f.parentFolderId === folder.id ? { ...f, parentFolderId: folder.parentFolderId } : f))
  documents.value = documents.value.map((d) => (d.folderId === folder.id ? { ...d, folderId: folder.parentFolderId } : d))
  if (currentFolderId.value === folder.id) goToFolder(folder.parentFolderId)
}

async function removeDocument(doc: DocSummary) {
  openMenuId.value = null
  if (!confirm(`Delete "${doc.title}"? This cannot be undone.`)) return
  deletingId.value = doc.id
  try {
    await deleteDocument(doc.id)
    documents.value = documents.value.filter((d) => d.id !== doc.id)
  } catch {
    alert('Could not delete this document — only the owner can delete it.')
  } finally {
    deletingId.value = null
  }
}

async function moveToFolder(doc: DocSummary, folderId: string | null) {
  openMenuId.value = null
  try {
    await moveDocument(doc.id, folderId)
    doc.folderId = folderId
  } catch {
    alert('Could not move this document.')
  }
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

onMounted(async () => {
  await loadUser()
  if (user.value) await Promise.all([refreshDocuments(), refreshFolders()])
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
        <div class="relative hidden max-w-xs flex-1 sm:block">
          <Icon name="search" class="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--text-muted)]" />
          <input
            v-model="searchQuery"
            type="search"
            placeholder="Search documents"
            class="w-full rounded-[var(--radius-input)] border-0 bg-[var(--input-bg)] py-1.5 pl-8 pr-3 text-sm text-[var(--text-heading)] placeholder:text-[var(--text-muted)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-start)]"
            style="border: var(--input-border)"
          />
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
          <span class="accent-gradient flex h-9 w-9 items-center justify-center rounded-full text-white">
            <Icon name="plus" class="h-5 w-5" />
          </span>
          <span class="text-xs text-[var(--text-subtitle)] group-hover:text-[var(--text-heading)]">
            {{ creating ? 'Creating…' : 'Blank document' }}
          </span>
        </button>

        <div class="mb-4 mt-10 flex items-center justify-between">
          <nav class="flex flex-wrap items-center gap-1 text-sm">
            <button
              class="rounded px-1.5 py-0.5 font-medium transition"
              :class="currentFolderId === null ? 'text-[var(--text-heading)]' : 'text-[var(--text-subtitle)] hover:text-[var(--text-heading)]'"
              @click="goToFolder(null)"
            >
              Home
            </button>
            <template v-for="crumb in breadcrumb" :key="crumb.id">
              <span class="text-[var(--text-muted)]">/</span>
              <button
                class="rounded px-1.5 py-0.5 font-medium transition"
                :class="currentFolderId === crumb.id ? 'text-[var(--text-heading)]' : 'text-[var(--text-subtitle)] hover:text-[var(--text-heading)]'"
                @click="goToFolder(crumb.id)"
              >
                {{ crumb.name }}
              </button>
            </template>
          </nav>
          <button
            class="inline-flex shrink-0 items-center gap-1 rounded-full border border-dashed border-white/15 px-3 py-1.5 text-xs font-medium text-[var(--text-subtitle)] transition hover:bg-white/5 disabled:opacity-50"
            :disabled="creatingFolder"
            @click="newFolderDialogOpen = true"
          >
            <Icon name="plus" class="h-3.5 w-3.5" /> New folder
          </button>
        </div>

        <p v-if="loadingDocuments" class="text-sm text-[var(--text-muted)]">Loading…</p>
        <p v-else-if="!subfolders.length && !documentsInFolder.length" class="text-sm text-[var(--text-muted)]">
          Empty — create a document or folder above to get started.
        </p>
        <p v-else-if="searchQuery && !filteredDocuments.length" class="text-sm text-[var(--text-muted)]">
          No documents match "{{ searchQuery }}".
        </p>
        <div v-else class="grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4">
          <div
            v-for="folder in subfolders"
            :key="folder.id"
            class="glass-panel group relative flex cursor-pointer flex-col items-center justify-center gap-2 rounded-[var(--radius-panel)] py-6 transition hover:ring-2 hover:ring-[var(--accent-start)]"
            @click="goToFolder(folder.id)"
          >
            <Icon name="folder" class="h-8 w-8 text-[var(--text-muted)]" />
            <p class="max-w-[90%] truncate text-sm font-medium text-[var(--text-heading)]">{{ folder.name }}</p>
            <button
              class="absolute right-1.5 top-1.5 rounded-full p-1 text-[var(--text-subtitle)] opacity-0 transition hover:bg-black/40 hover:text-red-400 group-hover:opacity-100"
              aria-label="Delete folder"
              @click.stop="removeFolder(folder)"
            >
              <Icon name="x-mark" class="h-3.5 w-3.5" />
            </button>
          </div>
          <div
            v-for="doc in filteredDocuments"
            :key="doc.id"
            class="glass-panel group relative flex flex-col overflow-hidden rounded-[var(--radius-panel)] transition hover:ring-2 hover:ring-[var(--accent-start)]"
            :class="{ 'opacity-40': deletingId === doc.id }"
          >
            <NuxtLink :to="`/documents/${doc.id}`" class="flex flex-col">
              <div class="flex h-28 items-center justify-center border-b border-white/10 bg-white/[0.02]">
                <Icon name="document" class="h-8 w-8 text-[var(--text-muted)]" />
              </div>
              <div class="px-3 py-2">
                <p class="truncate pr-5 text-sm font-medium text-[var(--text-heading)]">{{ doc.title }}</p>
                <p class="text-xs text-[var(--text-muted)]">{{ formatDate(doc.updatedAt) }}</p>
              </div>
            </NuxtLink>

            <button
              class="absolute right-1.5 top-1.5 rounded-full p-1 text-[var(--text-subtitle)] opacity-0 transition hover:bg-black/40 hover:text-[var(--text-heading)] group-hover:opacity-100"
              aria-label="Document options"
              @click.prevent.stop="openMenuId = openMenuId === doc.id ? null : doc.id"
            >
              <Icon name="dots-vertical" class="h-4 w-4" />
            </button>
            <div
              v-if="openMenuId === doc.id"
              class="glass-panel absolute right-1.5 top-8 z-10 min-w-[10rem] rounded-[var(--radius-input)] p-1"
              @click.stop
            >
              <p class="px-2 pb-1 pt-1.5 text-[10px] font-semibold uppercase tracking-wide text-[var(--text-muted)]">
                Move to
              </p>
              <button
                v-if="doc.folderId !== null"
                class="w-full rounded-md px-2 py-1.5 text-left text-xs text-[var(--text-body)] hover:bg-white/5"
                @click.prevent="moveToFolder(doc, null)"
              >
                No folder
              </button>
              <button
                v-for="folder in folders.filter((f) => f.id !== doc.folderId)"
                :key="folder.id"
                class="w-full rounded-md px-2 py-1.5 text-left text-xs text-[var(--text-body)] hover:bg-white/5"
                @click.prevent="moveToFolder(doc, folder.id)"
              >
                <Icon name="folder" class="mr-1 inline h-3.5 w-3.5 align-[-2px]" /> {{ folder.name }}
              </button>
              <div class="my-1 h-px bg-white/10" />
              <button
                class="w-full rounded-md px-2 py-1.5 text-left text-xs text-red-400 hover:bg-red-500/10"
                @click.prevent="removeDocument(doc)"
              >
                Delete
              </button>
            </div>
          </div>
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

    <PromptDialog
      :open="newFolderDialogOpen"
      title="New folder"
      placeholder="Folder name"
      @confirm="addFolder"
      @close="newFolderDialogOpen = false"
    />
  </main>
</template>
