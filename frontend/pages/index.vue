<script setup lang="ts">
const { user, login, logout, loadUser } = useAuth()

onMounted(() => {
  loadUser()
})
</script>

<template>
  <main class="flex min-h-screen flex-col items-center justify-center gap-8 px-6">
    <div class="flex flex-col items-center gap-3">
      <img src="/logo.png" alt="Innovayse" class="h-12 w-12 rounded-xl" />
      <h1 class="text-2xl font-extrabold tracking-tight text-[var(--text-heading)]">
        Innovayse Docs
      </h1>
      <p class="text-sm text-[var(--text-subtitle)]">Collaborative documents for your workspace</p>
    </div>

    <div class="glass-panel w-full max-w-sm rounded-[var(--radius-card)] p-8 text-center">
      <template v-if="user">
        <p class="text-sm text-[var(--text-body)]">
          Signed in as
          <span class="font-medium text-[var(--text-heading)]">{{ user.profile.email ?? user.profile.sub }}</span>
        </p>
        <div class="mt-6 flex flex-col gap-2">
          <button
            class="rounded-[var(--radius-input)] border border-white/10 px-4 py-2.5 text-sm font-medium text-[var(--text-subtitle)] transition hover:bg-white/5 hover:text-[var(--text-heading)]"
            @click="logout"
          >
            Log out
          </button>
        </div>
      </template>
      <template v-else>
        <p class="text-sm text-[var(--text-subtitle)]">Sign in with your Innovayse account to continue.</p>
        <button
          class="accent-gradient mt-6 w-full rounded-[var(--radius-input)] px-4 py-2.5 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 transition hover:brightness-110"
          @click="login"
        >
          Log in
        </button>
      </template>
    </div>
  </main>
</template>
