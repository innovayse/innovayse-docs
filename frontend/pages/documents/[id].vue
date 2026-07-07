<!-- pages/documents/[id].vue -->
<script setup lang="ts">
const route = useRoute()
const { accessToken, user, loadUser } = useAuth()
await loadUser()
</script>

<template>
  <div class="document-layout">
    <CollaborativeEditor
      v-if="accessToken"
      :document-id="route.params.id as string"
      :access-token="accessToken"
      :user-name="user?.profile.name ?? 'Anonymous'"
    />
    <ShareDialog :document-id="route.params.id as string" />
    <CommentsSidebar :document-id="route.params.id as string" />
  </div>
</template>
