<script setup lang="ts">
// Completes the OIDC authorization code flow initiated by useAuth().login()
// and redirects back to the app once signed in. Later tasks may replace this
// with richer error handling / return-url support.
const { handleLoginCallback } = useAuth()

onMounted(async () => {
  await handleLoginCallback()
  await navigateTo('/')
})
</script>

<template>
  <main class="flex min-h-screen items-center justify-center">
    <p>Signing you in...</p>
  </main>
</template>
