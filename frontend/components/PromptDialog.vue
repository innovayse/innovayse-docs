<script setup lang="ts">
const props = defineProps<{ open: boolean; title: string; placeholder?: string }>()
const emit = defineEmits<{ confirm: [value: string]; close: [] }>()

const value = ref('')

watch(
  () => props.open,
  (isOpen) => {
    if (isOpen) value.value = ''
  },
)

function submit() {
  const trimmed = value.value.trim()
  if (!trimmed) return
  emit('confirm', trimmed)
}
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="fixed inset-0 z-20 flex items-center justify-center bg-black/60 px-4" @click.self="emit('close')">
      <div class="glass-panel w-full max-w-sm rounded-[var(--radius-card)] p-6">
        <div class="mb-4 flex items-center justify-between">
          <h2 class="text-base font-semibold text-[var(--text-heading)]">{{ title }}</h2>
          <button
            class="rounded-full p-1 text-[var(--text-subtitle)] transition hover:bg-white/5 hover:text-[var(--text-heading)]"
            aria-label="Close"
            @click="emit('close')"
          >
            <Icon name="x-mark" class="h-4 w-4" />
          </button>
        </div>

        <input
          v-model="value"
          type="text"
          :placeholder="placeholder"
          autofocus
          class="w-full rounded-[var(--radius-input)] border-0 bg-[var(--input-bg)] px-3 py-2 text-sm text-[var(--text-heading)] placeholder:text-[var(--text-muted)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-start)]"
          style="border: var(--input-border)"
          @keyup.enter="submit"
        />

        <div class="mt-5 flex justify-end gap-2">
          <button
            class="rounded-[var(--radius-input)] border border-white/10 px-3 py-2 text-sm font-medium text-[var(--text-subtitle)] transition hover:bg-white/5 hover:text-[var(--text-heading)]"
            @click="emit('close')"
          >
            Cancel
          </button>
          <button
            class="accent-gradient rounded-[var(--radius-input)] px-4 py-2 text-sm font-semibold text-white transition hover:brightness-110 disabled:opacity-50"
            :disabled="!value.trim()"
            @click="submit"
          >
            Create
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
