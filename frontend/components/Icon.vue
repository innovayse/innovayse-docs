<script setup lang="ts">
type IconName = 'document' | 'folder' | 'dots-vertical' | 'x-mark' | 'plus' | 'search' | 'chevron-down'

defineProps<{ name: IconName; class?: string }>()

const strokePaths: Partial<Record<IconName, string>> = {
  document:
    'M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z',
  folder:
    'M2.25 12.75V12A2.25 2.25 0 0 1 4.5 9.75h15A2.25 2.25 0 0 1 21.75 12v.75m-19.5 0v6a2.25 2.25 0 0 0 2.25 2.25h15a2.25 2.25 0 0 0 2.25-2.25v-6m-19.5 0a2.25 2.25 0 0 1 2.25-2.25h15a2.25 2.25 0 0 1 2.25 2.25m-19.5 0v-.75A2.25 2.25 0 0 1 4.5 7.5h3.879a1.5 1.5 0 0 1 1.06.44l1.122 1.12a1.5 1.5 0 0 0 1.06.44H19.5a2.25 2.25 0 0 1 2.25 2.25v.75',
  'x-mark': 'M6 18 18 6M6 6l12 12',
  plus: 'M12 4.5v15m7.5-7.5h-15',
  search: 'm21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z',
  'chevron-down': 'm19.5 8.25-7.5 7.5-7.5-7.5',
}

const fillPaths: Partial<Record<IconName, string>> = {
  'dots-vertical':
    'M12 6.75c.621 0 1.125-.504 1.125-1.125S12.621 4.5 12 4.5s-1.125.504-1.125 1.125S11.379 6.75 12 6.75ZM12 12.75c.621 0 1.125-.504 1.125-1.125S12.621 10.5 12 10.5s-1.125.504-1.125 1.125.504 1.125 1.125 1.125ZM12 18.75c.621 0 1.125-.504 1.125-1.125s-.504-1.125-1.125-1.125-1.125.504-1.125 1.125.504 1.125 1.125 1.125Z',
}
</script>

<template>
  <svg
    :class="class"
    viewBox="0 0 24 24"
    :fill="fillPaths[name] ? 'currentColor' : 'none'"
    stroke="currentColor"
    :stroke-width="strokePaths[name] ? 1.8 : 0"
    stroke-linecap="round"
    stroke-linejoin="round"
    aria-hidden="true"
  >
    <path :d="strokePaths[name] ?? fillPaths[name]" />
  </svg>
</template>
