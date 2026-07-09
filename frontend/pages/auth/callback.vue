<script setup lang="ts">
// Completes the OIDC authorization code flow initiated by useAuth().login()
// and redirects back to the app once signed in. Later tasks may replace this
// with richer error handling / return-url support.
const { handleLoginCallback } = useAuth()

onMounted(async () => {
  const signedInUser = await handleLoginCallback()
  const returnTo = (signedInUser?.state as { returnTo?: string } | undefined)?.returnTo
  await navigateTo(returnTo || '/')
})
</script>

<template>
  <main class="flex min-h-screen items-center justify-center gap-3 text-[var(--text-subtitle)]">
    <span class="h-4 w-4 animate-spin rounded-full border-2 border-white/20 border-t-[var(--accent-start)]" />
    <p class="text-sm">Signing you in...</p>
  </main>
</template>
