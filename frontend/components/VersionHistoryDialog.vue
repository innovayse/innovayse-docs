<script setup lang="ts">
const props = defineProps<{
  documentId: string
  open: boolean
  canEdit: boolean
  onSaveVersion: (label: string | undefined) => Promise<void>
  onRestoreVersion: (versionId: string) => Promise<void>
}>()
const emit = defineEmits<{ close: [] }>()
const { listVersions } = useDocsApi()

interface VersionSummary {
  id: string
  createdAt: string
  label: string | null
}

const versions = ref<VersionSummary[]>([])
const loading = ref(false)
const saving = ref(false)
const restoringId = ref<string | null>(null)
const labelInput = ref('')

async function refresh() {
  loading.value = true
  try {
    versions.value = (await listVersions(props.documentId)) as VersionSummary[]
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  try {
    await props.onSaveVersion(labelInput.value.trim() || undefined)
    labelInput.value = ''
    await refresh()
  } finally {
    saving.value = false
  }
}

async function restore(version: VersionSummary) {
  if (!confirm('Replace the current document content with this version? This creates a new edit — nothing is lost, but it will affect everyone viewing the document.')) return
  restoringId.value = version.id
  try {
    await props.onRestoreVersion(version.id)
  } finally {
    restoringId.value = null
  }
}

function formatDate(value: string) {
  return new Date(value).toLocaleString(undefined, {
    year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
  })
}

watch(() => props.open, (isOpen) => {
  if (isOpen) refresh()
})
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="no-print fixed inset-0 z-20 flex items-center justify-center bg-black/60 px-4" @click.self="emit('close')">
      <div class="glass-panel flex max-h-[80vh] w-full max-w-md flex-col rounded-[var(--radius-card)] p-6">
        <div class="mb-4 flex items-center justify-between">
          <h2 class="text-base font-semibold text-[var(--text-heading)]">Version history</h2>
          <button
            class="rounded-full p-1 text-[var(--text-subtitle)] transition hover:bg-white/5 hover:text-[var(--text-heading)]"
            aria-label="Close"
            @click="emit('close')"
          >
            <Icon name="x-mark" class="h-4 w-4" />
          </button>
        </div>

        <div v-if="canEdit" class="mb-4 flex gap-2">
          <input
            v-model="labelInput"
            placeholder="Label (optional)"
            class="min-w-0 flex-1 rounded-[var(--radius-input)] border-0 bg-[var(--input-bg)] px-3 py-2 text-sm text-[var(--text-heading)] placeholder:text-[var(--text-muted)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-start)]"
            style="border: var(--input-border)"
          />
          <button
            class="accent-gradient shrink-0 rounded-[var(--radius-input)] px-3 py-2 text-sm font-semibold text-white transition hover:brightness-110 disabled:opacity-50"
            :disabled="saving"
            @click="save"
          >
            {{ saving ? 'Saving…' : 'Save version' }}
          </button>
        </div>
        <p v-else class="mb-4 text-xs text-[var(--text-muted)]">
          Viewing only — you don't have permission to save or restore versions.
        </p>

        <div class="flex-1 space-y-2 overflow-y-auto">
          <p v-if="loading" class="text-sm text-[var(--text-muted)]">Loading…</p>
          <p v-else-if="!versions.length" class="text-sm text-[var(--text-muted)]">
            No saved versions yet — "Save version" captures a restore point.
          </p>
          <div
            v-for="version in versions"
            :key="version.id"
            class="flex items-center justify-between rounded-[var(--radius-input)] bg-[var(--input-bg)] px-3 py-2"
          >
            <div class="min-w-0">
              <p class="truncate text-sm font-medium text-[var(--text-heading)]">
                {{ version.label || 'Untitled version' }}
              </p>
              <p class="text-xs text-[var(--text-muted)]">{{ formatDate(version.createdAt) }}</p>
            </div>
            <button
              v-if="canEdit"
              class="shrink-0 rounded-[var(--radius-input)] border border-white/10 px-2.5 py-1 text-xs font-medium text-[var(--text-body)] transition hover:bg-white/5 disabled:opacity-50"
              :disabled="restoringId === version.id"
              @click="restore(version)"
            >
              {{ restoringId === version.id ? 'Restoring…' : 'Restore' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>
