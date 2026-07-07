<script setup lang="ts">
const props = defineProps<{ documentId: string }>()
const { listComments, createComment } = useDocsApi()

const comments = ref<Array<{ id: string; text: string; authorId: string }>>([])
const newCommentText = ref('')

async function refresh() {
  comments.value = (await listComments(props.documentId)) as typeof comments.value
}

async function submit() {
  await createComment(props.documentId, newCommentText.value, 0)
  newCommentText.value = ''
  await refresh()
}

onMounted(refresh)
</script>

<template>
  <aside class="comments-sidebar">
    <ul>
      <li v-for="comment in comments" :key="comment.id">{{ comment.text }}</li>
    </ul>
    <textarea v-model="newCommentText" />
    <button @click="submit">Comment</button>
  </aside>
</template>
