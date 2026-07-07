<script setup lang="ts">
const props = defineProps<{ documentId: string; open: boolean }>()
const emit = defineEmits<{ close: [] }>()
const { inviteUser, createShareLink } = useDocsApi()

const inviteEmail = ref('')
const inviteRole = ref('Viewer')
const linkUrl = ref<string | null>(null)
const inviting = ref(false)
const generatingLink = ref(false)
const inviteError = ref('')

async function sendInvite() {
  inviteError.value = ''
  inviting.value = true
  try {
    await inviteUser(props.documentId, inviteEmail.value, inviteRole.value)
    inviteEmail.value = ''
  } catch (err: any) {
    inviteError.value = err?.data?.message ?? 'Could not send invite.'
  } finally {
    inviting.value = false
  }
}

async function generateLink() {
  generatingLink.value = true
  try {
    const link = (await createShareLink(props.documentId, 'Viewer')) as { token: string }
    linkUrl.value = `${window.location.origin}/documents/${props.documentId}?share=${link.token}`
  } finally {
    generatingLink.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="fixed inset-0 z-20 flex items-center justify-center bg-black/60 px-4" @click.self="emit('close')">
      <div class="glass-panel w-full max-w-md rounded-[var(--radius-card)] p-6">
        <div class="mb-5 flex items-center justify-between">
          <h2 class="text-base font-semibold text-[var(--text-heading)]">Share document</h2>
          <button
            class="rounded-full p-1 text-[var(--text-subtitle)] transition hover:bg-white/5 hover:text-[var(--text-heading)]"
            aria-label="Close"
            @click="emit('close')"
          >
            <Icon name="x-mark" class="h-4 w-4" />
          </button>
        </div>

        <div class="space-y-2">
          <label class="text-xs font-medium text-[var(--text-subtitle)]">Invite by email</label>
          <div class="flex gap-2">
            <input
              v-model="inviteEmail"
              type="email"
              placeholder="Email to invite"
              class="min-w-0 flex-1 rounded-[var(--radius-input)] border-0 bg-[var(--input-bg)] px-3 py-2 text-sm text-[var(--text-heading)] placeholder:text-[var(--text-muted)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-start)]"
              style="border: var(--input-border)"
            />
            <select
              v-model="inviteRole"
              class="shrink-0 rounded-[var(--radius-input)] border-0 bg-[var(--input-bg)] px-2 py-2 text-sm text-[var(--text-heading)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-start)]"
              style="border: var(--input-border)"
            >
              <option>Viewer</option>
              <option>Commenter</option>
              <option>Editor</option>
            </select>
          </div>
          <button
            class="accent-gradient w-full rounded-[var(--radius-input)] px-3 py-2 text-sm font-semibold text-white transition hover:brightness-110 disabled:opacity-50"
            :disabled="!inviteEmail || inviting"
            @click="sendInvite"
          >
            {{ inviting ? 'Inviting…' : 'Invite' }}
          </button>
          <p v-if="inviteError" class="text-xs text-red-400">{{ inviteError }}</p>
        </div>

        <div class="my-5 h-px bg-white/10" />

        <div class="space-y-2">
          <label class="text-xs font-medium text-[var(--text-subtitle)]">Share link</label>
          <button
            class="w-full rounded-[var(--radius-input)] border border-white/10 px-3 py-2 text-sm font-medium text-[var(--text-body)] transition hover:bg-white/5 disabled:opacity-50"
            :disabled="generatingLink"
            @click="generateLink"
          >
            {{ generatingLink ? 'Generating…' : 'Generate share link' }}
          </button>
          <input
            v-if="linkUrl"
            :value="linkUrl"
            readonly
            class="w-full rounded-[var(--radius-input)] border-0 bg-[var(--input-bg)] px-3 py-2 text-xs text-[var(--text-subtitle)] focus:outline-none"
            style="border: var(--input-border)"
            @focus="($event.target as HTMLInputElement).select()"
          />
        </div>
      </div>
    </div>
  </Teleport>
</template>
