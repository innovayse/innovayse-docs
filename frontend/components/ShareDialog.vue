<script setup lang="ts">
const props = defineProps<{ documentId: string }>()
const { inviteUser, createShareLink } = useDocsApi()

const inviteEmail = ref('')
const inviteRole = ref('Viewer')
const linkUrl = ref<string | null>(null)

async function sendInvite() {
  await inviteUser(props.documentId, inviteEmail.value, inviteRole.value)
  inviteEmail.value = ''
}

async function generateLink() {
  const link = (await createShareLink(props.documentId, 'Viewer')) as { token: string }
  linkUrl.value = `${window.location.origin}/documents/${props.documentId}?share=${link.token}`
}
</script>

<template>
  <div class="share-dialog">
    <input v-model="inviteEmail" placeholder="User ID to invite" />
    <select v-model="inviteRole">
      <option>Viewer</option>
      <option>Commenter</option>
      <option>Editor</option>
    </select>
    <button @click="sendInvite">Invite</button>

    <button @click="generateLink">Generate share link</button>
    <input v-if="linkUrl" :value="linkUrl" readonly />
  </div>
</template>
