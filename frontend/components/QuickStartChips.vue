<!-- components/QuickStartChips.vue -->
<script setup lang="ts">
import type { Editor } from '@tiptap/vue-3'
import type { JSONContent } from '@tiptap/core'

const props = defineProps<{ editor: Editor }>()

type Template = {
  key: string
  label: string
  icon: 'calendar' | 'envelope' | 'sparkles'
  content: JSONContent[]
}

const templates: Template[] = [
  {
    key: 'meeting-notes',
    label: 'Meeting notes',
    icon: 'calendar',
    content: [
      { type: 'heading', attrs: { level: 1 }, content: [{ type: 'text', text: 'Meeting notes' }] },
      {
        type: 'paragraph',
        content: [
          { type: 'text', marks: [{ type: 'bold' }], text: 'Date: ' },
          { type: 'text', text: ' ' },
          { type: 'text', marks: [{ type: 'bold' }], text: 'Attendees: ' },
        ],
      },
      { type: 'heading', attrs: { level: 2 }, content: [{ type: 'text', text: 'Agenda' }] },
      {
        type: 'bulletList',
        content: [
          { type: 'listItem', content: [{ type: 'paragraph', content: [] }] },
        ],
      },
      { type: 'heading', attrs: { level: 2 }, content: [{ type: 'text', text: 'Action items' }] },
      {
        type: 'bulletList',
        content: [
          { type: 'listItem', content: [{ type: 'paragraph', content: [] }] },
        ],
      },
    ],
  },
  {
    key: 'email-draft',
    label: 'Email draft',
    icon: 'envelope',
    content: [
      { type: 'paragraph', content: [{ type: 'text', text: 'Subject: ' }] },
      { type: 'paragraph', content: [{ type: 'text', text: 'Hi,' }] },
      { type: 'paragraph', content: [] },
      { type: 'paragraph', content: [] },
      { type: 'paragraph', content: [{ type: 'text', text: 'Best,' }] },
    ],
  },
  {
    key: 'blank-outline',
    label: 'More',
    icon: 'sparkles',
    content: [
      { type: 'heading', attrs: { level: 1 }, content: [{ type: 'text', text: 'Untitled outline' }] },
      { type: 'paragraph', content: [] },
    ],
  },
]

function apply(template: Template) {
  props.editor.chain().focus().insertContent(template.content).run()
}
</script>

<template>
  <div class="flex flex-wrap gap-2 pb-6">
    <button
      v-for="template in templates"
      :key="template.key"
      type="button"
      class="flex items-center gap-1.5 rounded-[var(--radius-input)] border border-white/10 px-3 py-2 text-xs font-medium text-[var(--text-subtitle)] transition hover:bg-white/5 hover:text-[var(--text-heading)]"
      @click="apply(template)"
    >
      <Icon :name="template.icon" class="h-3.5 w-3.5" />
      {{ template.label }}
    </button>
  </div>
</template>
