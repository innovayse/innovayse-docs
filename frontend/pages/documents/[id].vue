<!-- pages/documents/[id].vue -->
<script setup lang="ts">
const route = useRoute()
const { accessToken, user, loadUser } = useAuth()
await loadUser()
</script>

<template>
  <CollaborativeEditor
    v-if="accessToken"
    :document-id="route.params.id as string"
    :access-token="accessToken"
    :user-name="user?.profile.name ?? 'Anonymous'"
  />
</template>
