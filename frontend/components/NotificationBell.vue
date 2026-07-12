<script setup lang="ts">
const { listNotifications, markNotificationRead, markAllNotificationsRead } = useDocsApi()
const router = useRouter()

interface NotificationRecord {
  id: string
  type: 'NewComment' | 'NewReply' | 'DocumentShared' | 'FolderShared'
  actorName: string
  documentId: string | null
  folderId: string | null
  previewText: string
  createdAt: string
  readAt: string | null
}

const notifications = ref<NotificationRecord[]>([])
const open = ref(false)
const unreadCount = computed(() => notifications.value.filter((n) => n.readAt === null).length)

async function refresh() {
  notifications.value = (await listNotifications()) as NotificationRecord[]
}

function messageFor(n: NotificationRecord): string {
  switch (n.type) {
    case 'NewComment':
      return `${n.actorName} commented: "${n.previewText}"`
    case 'NewReply':
      return `${n.actorName} replied: "${n.previewText}"`
    case 'DocumentShared':
      return `${n.actorName} shared "${n.previewText}" with you`
    case 'FolderShared':
      return `${n.actorName} shared the folder "${n.previewText}" with you`
  }
}

async function selectNotification(n: NotificationRecord) {
  await markNotificationRead(n.id)
  n.readAt = new Date().toISOString()
  open.value = false
  if (n.documentId) {
    await router.push(`/documents/${n.documentId}`)
  } else if (n.folderId) {
    await router.push({ path: '/', query: { folder: n.folderId } })
  }
}

async function markAllRead() {
  await markAllNotificationsRead()
  const now = new Date().toISOString()
  notifications.value = notifications.value.map((n) => ({ ...n, readAt: n.readAt ?? now }))
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}

let timer: ReturnType<typeof setInterval> | undefined
onMounted(() => {
  refresh()
  timer = setInterval(refresh, 30000)
})
onBeforeUnmount(() => {
  if (timer) clearInterval(timer)
})
</script>

<template>
  <div class="relative">
    <button
      type="button"
      class="relative rounded-full p-2 text-[var(--text-subtitle)] transition hover:bg-white/5 hover:text-[var(--text-heading)]"
      aria-label="Notifications"
      @click="open = !open"
    >
      <Icon name="bell" class="h-5 w-5" />
      <span
        v-if="unreadCount > 0"
        class="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-500 px-1 text-[10px] font-semibold text-white"
      >
        {{ unreadCount }}
      </span>
    </button>

    <div
      v-if="open"
      class="glass-panel absolute right-0 top-10 z-20 max-h-96 w-80 overflow-y-auto rounded-[var(--radius-card)] p-3"
      @click.stop
    >
      <div class="mb-2 flex items-center justify-between">
        <h3 class="text-sm font-semibold text-[var(--text-heading)]">Notifications</h3>
        <button
          v-if="unreadCount > 0"
          class="text-xs font-medium text-[var(--accent-start)] hover:underline"
          @click="markAllRead"
        >
          Mark all as read
        </button>
      </div>
      <p v-if="!notifications.length" class="text-xs text-[var(--text-muted)]">No notifications yet.</p>
      <ul class="space-y-1">
        <li
          v-for="n in notifications"
          :key="n.id"
          class="cursor-pointer rounded-[var(--radius-input)] px-2 py-2 text-xs transition hover:bg-white/5"
          :class="n.readAt === null ? 'bg-white/[0.04]' : ''"
          @click="selectNotification(n)"
        >
          <p class="text-[var(--text-body)]">{{ messageFor(n) }}</p>
          <p class="mt-0.5 text-[10px] text-[var(--text-muted)]">{{ formatDate(n.createdAt) }}</p>
        </li>
      </ul>
    </div>
  </div>
</template>
