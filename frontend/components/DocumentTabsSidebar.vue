<!-- components/DocumentTabsSidebar.vue -->
<script setup lang="ts">
interface Tab {
  id: string
  title: string
  orderIndex: number
}

const props = defineProps<{ documentId: string; canEdit: boolean }>()
const emit = defineEmits<{
  'update:activeTabId': [tabId: string]
  'tabs-loaded': [tabs: Tab[]]
}>()

const { listTabs, createTab, renameTab, reorderTab, deleteTab } = useDocsApi()

const tabs = ref<Tab[]>([])
const activeTabId = ref<string>('')
const renamingTabId = ref<string | null>(null)
const renameDraft = ref('')
const draggingTabId = ref<string | null>(null)
const deleteCandidateId = ref<string | null>(null)

async function loadTabs() {
  const result = (await listTabs(props.documentId)) as Tab[]
  tabs.value = [...result].sort((a, b) => a.orderIndex - b.orderIndex)
  if (!tabs.value.some((t) => t.id === activeTabId.value)) {
    activeTabId.value = tabs.value[0]?.id ?? ''
    emit('update:activeTabId', activeTabId.value)
  }
  emit('tabs-loaded', tabs.value)
}

onMounted(loadTabs)

function selectTab(tabId: string) {
  activeTabId.value = tabId
  emit('update:activeTabId', tabId)
}

async function addTab() {
  const nextNumber = tabs.value.length + 1
  await createTab(props.documentId, `Tab ${nextNumber}`)
  await loadTabs()
  const lastTab = tabs.value[tabs.value.length - 1]
  if (lastTab) selectTab(lastTab.id)
}

function startRename(tab: Tab) {
  renamingTabId.value = tab.id
  renameDraft.value = tab.title
}

async function commitRename() {
  if (!renamingTabId.value) return
  const trimmed = renameDraft.value.trim()
  if (trimmed) {
    await renameTab(props.documentId, renamingTabId.value, trimmed)
    await loadTabs()
  }
  renamingTabId.value = null
}

function confirmDelete(tabId: string) {
  deleteCandidateId.value = tabId
}

async function performDelete() {
  if (!deleteCandidateId.value) return
  await deleteTab(props.documentId, deleteCandidateId.value)
  deleteCandidateId.value = null
  await loadTabs()
}

function onDragStart(tabId: string) {
  draggingTabId.value = tabId
}

async function onDrop(targetTabId: string) {
  if (!draggingTabId.value || draggingTabId.value === targetTabId) {
    draggingTabId.value = null
    return
  }
  const reordered = [...tabs.value]
  const fromIndex = reordered.findIndex((t) => t.id === draggingTabId.value)
  const toIndex = reordered.findIndex((t) => t.id === targetTabId)
  const [moved] = reordered.splice(fromIndex, 1)
  if (!moved) {
    draggingTabId.value = null
    return
  }
  reordered.splice(toIndex, 0, moved)

  tabs.value = reordered
  draggingTabId.value = null

  await Promise.all(reordered.map((tab, index) => reorderTab(props.documentId, tab.id, index)))
  await loadTabs()
}
</script>

<template>
  <div class="w-56 shrink-0">
    <div class="mb-2 flex items-center justify-between px-2">
      <span class="text-xs font-semibold uppercase tracking-wide text-[var(--text-muted)]">Tabs</span>
      <button
        v-if="canEdit"
        class="rounded-[var(--radius-input)] px-2 py-1 text-xs font-semibold text-[var(--text-subtitle)] hover:bg-white/5"
        aria-label="Add tab"
        @click="addTab"
      >
        +
      </button>
    </div>

    <ul class="flex flex-col gap-1">
      <li
        v-for="tab in tabs"
        :key="tab.id"
        :draggable="canEdit"
        class="group flex items-center gap-2 rounded-[var(--radius-input)] px-2 py-1.5 text-sm"
        :class="tab.id === activeTabId ? 'bg-white/10 text-[var(--text-heading)]' : 'text-[var(--text-subtitle)] hover:bg-white/5'"
        @dragstart="onDragStart(tab.id)"
        @dragover.prevent
        @drop="onDrop(tab.id)"
        @click="selectTab(tab.id)"
      >
        <input
          v-if="renamingTabId === tab.id"
          v-model="renameDraft"
          autofocus
          class="min-w-0 flex-1 rounded bg-transparent px-1 text-sm focus:outline-none focus:ring-1 focus:ring-[var(--accent-start)]"
          @keyup.enter="commitRename"
          @blur="commitRename"
          @click.stop
        />
        <span v-else class="min-w-0 flex-1 truncate" @dblclick="canEdit && startRename(tab)">{{ tab.title }}</span>

        <button
          v-if="canEdit"
          class="hidden shrink-0 text-xs text-[var(--text-muted)] hover:text-red-400 group-hover:inline"
          aria-label="Delete tab"
          @click.stop="confirmDelete(tab.id)"
        >
          ✕
        </button>
      </li>
    </ul>

    <div
      v-if="deleteCandidateId"
      class="glass-panel fixed inset-0 z-20 flex items-center justify-center"
      @click.self="deleteCandidateId = null"
    >
      <div class="glass-panel w-80 rounded-[var(--radius-card)] p-4">
        <p class="mb-4 text-sm text-[var(--text-subtitle)]">Delete this tab? This can't be undone.</p>
        <div class="flex justify-end gap-2">
          <button class="rounded-[var(--radius-input)] px-3 py-1.5 text-xs" @click="deleteCandidateId = null">Cancel</button>
          <button class="rounded-[var(--radius-input)] bg-red-500/80 px-3 py-1.5 text-xs text-white" @click="performDelete">Delete</button>
        </div>
      </div>
    </div>
  </div>
</template>
