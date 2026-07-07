<!-- components/CollaborativeEditor.vue -->
<script setup lang="ts">
import { HocuspocusProvider } from '@hocuspocus/provider'
import { useEditor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import Collaboration from '@tiptap/extension-collaboration'
import CollaborationCursor from '@tiptap/extension-collaboration-cursor'
import * as Y from 'yjs'
import { yDocToProsemirrorJSON } from 'y-prosemirror'

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

// Browser-safe base64 helpers — no Buffer global here, this runs client-side.
function bytesToBase64(bytes: Uint8Array): string {
  let binary = ''
  for (const byte of bytes) binary += String.fromCharCode(byte)
  return btoa(binary)
}

function base64ToBytes(base64: string): Uint8Array {
  const binary = atob(base64)
  const bytes = new Uint8Array(binary.length)
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i)
  return bytes
}

/** Full merged Yjs state, suitable for storing as a version snapshot. */
function getSnapshotBase64() {
  return bytesToBase64(Y.encodeStateAsUpdate(ydoc))
}

/** Replaces the live document's content with a previously saved snapshot.
 * Goes through the editor's own commands (not a raw Yjs state swap) so the
 * change is a normal collaborative edit — it syncs to every connected
 * client and persists through the existing update-log pipeline. */
function restoreFromSnapshotBase64(snapshotBase64: string) {
  const snapshotDoc = new Y.Doc()
  Y.applyUpdate(snapshotDoc, base64ToBytes(snapshotBase64))
  const json = yDocToProsemirrorJSON(snapshotDoc, 'default')
  editor.value?.commands.setContent(json)
}

defineExpose({ getSnapshotBase64, restoreFromSnapshotBase64 })
</script>

<template>
  <EditorContent :editor="editor" class="max-w-none" />
</template>
