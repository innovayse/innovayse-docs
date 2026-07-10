<script setup lang="ts">
const props = defineProps<{
  documentId: string
  getAnchorPosition?: () => number
  canComment: boolean
  authorName: string
}>()
const { listComments, createComment, setCommentResolved } = useDocsApi()

interface CommentRecord {
  id: string
  text: string
  authorId: string
  authorName: string
  anchorPosition: number
  parentCommentId: string | null
  resolved: boolean
  createdAt: string
}

interface Thread {
  topLevel: CommentRecord
  replies: CommentRecord[]
}

const comments = ref<CommentRecord[]>([])
const newCommentText = ref('')
const posting = ref(false)
const replyingToId = ref<string | null>(null)
const replyText = ref('')
const postingReply = ref(false)

const threads = computed<Thread[]>(() => {
  const topLevel = comments.value
    .filter((c) => c.parentCommentId === null)
    .sort((a, b) => a.anchorPosition - b.anchorPosition)

  return topLevel.map((topLevelComment) => ({
    topLevel: topLevelComment,
    replies: comments.value
      .filter((c) => c.parentCommentId === topLevelComment.id)
      .sort((a, b) => a.createdAt.localeCompare(b.createdAt)),
  }))
})

async function refresh() {
  comments.value = (await listComments(props.documentId)) as CommentRecord[]
}

async function submit() {
  if (!newCommentText.value.trim()) return
  posting.value = true
  try {
    const anchorPosition = props.getAnchorPosition?.() ?? 0
    await createComment(props.documentId, newCommentText.value, anchorPosition, props.authorName)
    newCommentText.value = ''
    await refresh()
  } finally {
    posting.value = false
  }
}

function startReply(threadTopLevelId: string) {
  replyingToId.value = threadTopLevelId
  replyText.value = ''
}

function cancelReply() {
  replyingToId.value = null
  replyText.value = ''
}

async function submitReply(thread: Thread) {
  if (!replyText.value.trim()) return
  postingReply.value = true
  try {
    await createComment(
      props.documentId,
      replyText.value,
      thread.topLevel.anchorPosition,
      props.authorName,
      thread.topLevel.id,
    )
    cancelReply()
    await refresh()
  } finally {
    postingReply.value = false
  }
}

async function toggleResolved(thread: Thread) {
  await setCommentResolved(props.documentId, thread.topLevel.id, !thread.topLevel.resolved)
  await refresh()
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

    <ul class="flex-1 space-y-3 overflow-y-auto">
      <li
        v-for="thread in threads"
        :key="thread.topLevel.id"
        class="rounded-[var(--radius-input)] bg-[var(--input-bg)] px-3 py-2"
      >
        <div class="flex items-start justify-between gap-2">
          <div class="min-w-0">
            <p class="text-xs font-semibold text-[var(--text-heading)]">{{ thread.topLevel.authorName }}</p>
            <p
              class="text-sm"
              :class="thread.topLevel.resolved ? 'text-[var(--text-muted)] line-through' : 'text-[var(--text-body)]'"
            >
              {{ thread.topLevel.text }}
            </p>
          </div>
          <span
            v-if="thread.topLevel.resolved"
            class="shrink-0 rounded-full bg-white/10 px-2 py-0.5 text-[10px] font-medium text-[var(--text-muted)]"
          >
            Resolved
          </span>
        </div>

        <ul v-if="thread.replies.length" class="mt-2 space-y-1.5 border-l border-white/10 pl-3">
          <li v-for="reply in thread.replies" :key="reply.id">
            <p class="text-xs font-semibold text-[var(--text-heading)]">{{ reply.authorName }}</p>
            <p class="text-sm text-[var(--text-body)]">{{ reply.text }}</p>
          </li>
        </ul>

        <div v-if="canComment" class="mt-2 flex items-center gap-3">
          <button
            class="text-xs font-medium text-[var(--text-subtitle)] hover:text-[var(--text-heading)]"
            @click="replyingToId === thread.topLevel.id ? cancelReply() : startReply(thread.topLevel.id)"
          >
            {{ replyingToId === thread.topLevel.id ? 'Cancel' : 'Reply' }}
          </button>
          <button
            class="text-xs font-medium text-[var(--text-subtitle)] hover:text-[var(--text-heading)]"
            @click="toggleResolved(thread)"
          >
            {{ thread.topLevel.resolved ? 'Reopen' : 'Resolve' }}
          </button>
        </div>

        <div v-if="replyingToId === thread.topLevel.id" class="mt-2 space-y-1.5">
          <textarea
            v-model="replyText"
            rows="2"
            placeholder="Reply…"
            class="w-full resize-none rounded-[var(--radius-input)] border-0 bg-white/5 px-2 py-1.5 text-sm text-[var(--text-heading)] placeholder:text-[var(--text-muted)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-start)]"
          />
          <button
            class="accent-gradient w-full rounded-[var(--radius-input)] px-2 py-1.5 text-xs font-semibold text-white transition hover:brightness-110 disabled:opacity-50"
            :disabled="!replyText.trim() || postingReply"
            @click="submitReply(thread)"
          >
            {{ postingReply ? 'Posting…' : 'Post reply' }}
          </button>
        </div>
      </li>
      <li v-if="!comments.length" class="text-xs text-[var(--text-muted)]">No comments yet.</li>
    </ul>

    <div v-if="canComment" class="mt-3 space-y-2 border-t border-white/10 pt-3">
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
    <p v-else class="mt-3 border-t border-white/10 pt-3 text-xs text-[var(--text-muted)]">
      Viewing only — you don't have permission to comment on this document.
    </p>
  </aside>
</template>
