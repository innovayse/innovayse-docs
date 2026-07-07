<!-- pages/documents/[id].vue -->
<script setup lang="ts">
const route = useRoute()
const { accessToken, user, loadUser } = useAuth()
await loadUser()

const shareOpen = ref(false)
</script>

<template>
  <div class="flex min-h-screen flex-col">
    <header class="glass-panel sticky top-0 z-10 flex items-center justify-between gap-4 border-x-0 border-t-0 px-6 py-3">
      <div class="flex items-center gap-3">
        <img src="/logo.png" alt="Innovayse" class="h-7 w-7 rounded-lg" />
        <span class="text-sm font-semibold text-[var(--text-heading)]">Innovayse Docs</span>
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
