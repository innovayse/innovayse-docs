<!-- components/CollaborativeEditor.vue -->
<script setup lang="ts">
import { HocuspocusProvider } from '@hocuspocus/provider'
import { useEditor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import Collaboration from '@tiptap/extension-collaboration'
import CollaborationCursor from '@tiptap/extension-collaboration-cursor'
import Underline from '@tiptap/extension-underline'
import TextAlign from '@tiptap/extension-text-align'
import TextStyle from '@tiptap/extension-text-style'
import Color from '@tiptap/extension-color'
import Highlight from '@tiptap/extension-highlight'
import Link from '@tiptap/extension-link'
import TaskList from '@tiptap/extension-task-list'
import TaskItem from '@tiptap/extension-task-item'
import FontFamily from '@tiptap/extension-font-family'
import Image from '@tiptap/extension-image'
import Table from '@tiptap/extension-table'
import TableRow from '@tiptap/extension-table-row'
import TableCell from '@tiptap/extension-table-cell'
import TableHeader from '@tiptap/extension-table-header'
import * as Y from 'yjs'
import { yDocToProsemirrorJSON, ySyncPluginKey } from 'y-prosemirror'
import { FontSize } from '~/composables/useFontSize'
import { LineHeight } from '~/composables/useLineHeight'
import { Indent } from '~/composables/useIndent'
import { Pagination } from '~/composables/usePagination'

const props = defineProps<{ documentId: string; accessToken: string; userName: string }>()
const emit = defineEmits<{ 'insert-comment': [] }>()

const ydoc = new Y.Doc()
const provider = new HocuspocusProvider({
  url: 'ws://localhost:1234',
  name: props.documentId,
  token: props.accessToken,
  document: ydoc,
})

// Tiptap's Collaboration extension defers undo/redo entirely to Yjs — StarterKit's
// own history is disabled below, so this UndoManager is the only undo/redo there is.
// It must track the same shared type Collaboration binds to (the 'default' XML fragment),
// and the origin y-prosemirror's sync plugin actually stamps local edits with
// (ySyncPluginKey) — not the doc's clientID, which local edits are never tagged with.
const undoManager = new Y.UndoManager(ydoc.getXmlFragment('default'), {
  trackedOrigins: new Set([ySyncPluginKey]),
})

// A4 at 96 CSS px/in: 210mm × 297mm ≈ 794 × 1123px — a fixed page size, like a real
// document, rather than stretching to fill whatever width the surrounding panel has.
const PAGE_WIDTH = 794
const PAGE_MIN_HEIGHT = 1123
// Usable content height per page for pagination: page height minus the page-surface's
// own py-12 top+bottom padding (96px) minus a 32px reserved footer strip.
const PAGE_CONTENT_HEIGHT = PAGE_MIN_HEIGHT - 96 - 32

const editor = useEditor({
  extensions: [
    StarterKit.configure({ history: false }), // Yjs owns undo history
    Collaboration.configure({ document: ydoc }),
    CollaborationCursor.configure({
      provider,
      user: { name: props.userName, color: '#f783ac' },
    }),
    Underline,
    TextAlign.configure({ types: ['heading', 'paragraph'] }),
    TextStyle,
    Color,
    Highlight.configure({ multicolor: true }),
    Link.configure({ openOnClick: false }),
    TaskList,
    TaskItem.configure({ nested: true }),
    FontFamily,
    FontSize,
    LineHeight,
    Indent,
    Image,
    Table.configure({ resizable: true }),
    TableRow,
    TableHeader,
    TableCell,
    Pagination.configure({ pageContentHeight: PAGE_CONTENT_HEIGHT }),
  ],
})

// Once Yjs has synced, an empty shared doc still starts with `isEmpty === true`,
// and stays true until either this client or a remote peer types something —
// so the chips correctly stay hidden for late joiners of a doc that already has content.
const isEditorEmpty = ref(true)
watch(editor, (instance) => {
  if (!instance) return
  isEditorEmpty.value = instance.isEmpty
  instance.on('update', () => {
    isEditorEmpty.value = instance.isEmpty
  })
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

/** The caret/selection start position, for anchoring a new comment to where
 * the user is actually looking rather than always the top of the document. */
function getCursorPosition() {
  return editor.value?.state.selection.from ?? 0
}

defineExpose({ getSnapshotBase64, restoreFromSnapshotBase64, getCursorPosition })

const zoom = ref(1)
const pageMarginLeft = ref(48)
const pageMarginRight = ref(48)

// Zoom and margin changes reflow content height without ever dispatching a document
// transaction, so the pagination plugin's own update()-triggered recompute never fires
// for them on its own. Dispatching a no-op transaction forces that same update() hook to
// run — more reliable than guessing which DOM element to ResizeObserver for these two
// already-known, already-reactive triggers (window resize is comparatively rare and left
// to the pagination extension's own ResizeObserver).
watch([zoom, pageMarginLeft, pageMarginRight], async () => {
  // Wait for Vue to actually patch the DOM with the new zoom/padding style values first —
  // dispatching immediately (Vue's default pre-flush timing) fires before the browser has
  // applied the new layout, so the pagination plugin's later remeasure still sees the old
  // dimensions.
  await nextTick()
  const view = editor.value?.view
  if (view) view.dispatch(view.state.tr)
})
</script>

<template>
  <EditorToolbar
    v-if="editor"
    :editor="editor"
    :undo-manager="undoManager"
    @insert-comment="emit('insert-comment')"
    @zoom="zoom = $event"
  />
  <div class="mx-auto" :style="{ width: `${PAGE_WIDTH}px`, maxWidth: '100%' }">
    <DocumentRuler v-model:margin-left="pageMarginLeft" v-model:margin-right="pageMarginRight" />
  </div>
  <div class="flex justify-center overflow-x-auto px-4 pb-10 pt-6">
    <div
      class="page-surface shrink-0 rounded-sm py-12 shadow-xl"
      :style="{
        zoom,
        width: `${PAGE_WIDTH}px`,
        paddingLeft: `${pageMarginLeft}px`,
        paddingRight: `${pageMarginRight}px`,
      }"
    >
      <QuickStartChips v-if="editor && isEditorEmpty" :editor="editor" />
      <EditorContent :editor="editor" class="max-w-none" />
    </div>
  </div>
</template>
