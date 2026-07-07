<!-- components/CollaborativeEditor.vue -->
<script setup lang="ts">
import { HocuspocusProvider } from '@hocuspocus/provider'
import { useEditor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import Collaboration from '@tiptap/extension-collaboration'
import CollaborationCursor from '@tiptap/extension-collaboration-cursor'
import * as Y from 'yjs'

const props = defineProps<{ documentId: string; accessToken: string; userName: string }>()

const ydoc = new Y.Doc()
const provider = new HocuspocusProvider({
  url: 'ws://localhost:1234',
  name: props.documentId,
  token: props.accessToken,
  document: ydoc,
})

const editor = useEditor({
  extensions: [
    StarterKit.configure({ history: false }), // Yjs owns undo history
    Collaboration.configure({ document: ydoc }),
    CollaborationCursor.configure({
      provider,
      user: { name: props.userName, color: '#f783ac' },
    }),
  ],
})

onBeforeUnmount(() => {
  provider.destroy()
  editor.value?.destroy()
})
</script>

<template>
  <EditorContent :editor="editor" class="max-w-none" />
</template>
