<script setup lang="ts">
const { user, login, logout, loadUser } = useAuth()

onMounted(() => {
  loadUser()
})
</script>

<template>
  <main class="flex min-h-screen flex-col items-center justify-center gap-4 p-8">
    <h1 class="text-2xl font-semibold">Innovayse Docs</h1>

    <template v-if="user">
      <p>Signed in as {{ user.profile.email ?? user.profile.sub }}</p>
      <button class="rounded bg-gray-800 px-4 py-2 text-white" @click="logout">
        Log out
      </button>
    </template>
    <template v-else>
      <p>Not signed in.</p>
      <button class="rounded bg-blue-600 px-4 py-2 text-white" @click="login">
        Log in
      </button>
    </template>
  </main>
</template>
