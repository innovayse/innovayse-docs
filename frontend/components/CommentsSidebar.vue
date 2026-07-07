<script setup lang="ts">
const props = defineProps<{ documentId: string; getAnchorPosition?: () => number }>()
const { listComments, createComment } = useDocsApi()

const comments = ref<Array<{ id: string; text: string; authorId: string; anchorPosition: number }>>([])
const newCommentText = ref('')
const posting = ref(false)

const sortedComments = computed(() => [...comments.value].sort((a, b) => a.anchorPosition - b.anchorPosition))

async function refresh() {
  comments.value = (await listComments(props.documentId)) as typeof comments.value
}

async function submit() {
  if (!newCommentText.value.trim()) return
  posting.value = true
  try {
    const anchorPosition = props.getAnchorPosition?.() ?? 0
    await createComment(props.documentId, newCommentText.value, anchorPosition)
    newCommentText.value = ''
    await refresh()
  } finally {
    posting.value = false
  }
}

onMounted(refresh)

const textareaRef = ref<HTMLTextAreaElement | null>(null)
function focusNewComment() {
  textareaRef.value?.focus()
}

defineExpose({ focusNewComment })
</script>

<template>
  <aside class="glass-panel flex h-full flex-col rounded-[var(--radius-panel)] p-4">
    <h2 class="mb-3 text-sm font-semibold text-[var(--text-heading)]">Comments</h2>

    <ul class="flex-1 space-y-2 overflow-y-auto">
      <li
        v-for="comment in sortedComments"
        :key="comment.id"
        class="rounded-[var(--radius-input)] bg-[var(--input-bg)] px-3 py-2 text-sm text-[var(--text-body)]"
      >
        {{ comment.text }}
      </li>
      <li v-if="!comments.length" class="text-xs text-[var(--text-muted)]">No comments yet.</li>
    </ul>

    <div class="mt-3 space-y-2 border-t border-white/10 pt-3">
      <textarea
        ref="textareaRef"
        v-model="newCommentText"
        rows="2"
        placeholder="Add a comment…"
        class="w-full resize-none rounded-[var(--radius-input)] border-0 bg-[var(--input-bg)] px-3 py-2 text-sm text-[var(--text-heading)] placeholder:text-[var(--text-muted)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-start)]"
        style="border: var(--input-border)"
      />
      <button
        class="accent-gradient w-full rounded-[var(--radius-input)] px-3 py-2 text-sm font-semibold text-white transition hover:brightness-110 disabled:opacity-50"
        :disabled="!newCommentText.trim() || posting"
        @click="submit"
      >
        {{ posting ? 'Posting…' : 'Comment' }}
      </button>
    </div>
  </aside>
</template>
