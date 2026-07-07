<!-- components/DocumentRuler.vue -->
<script setup lang="ts">
// A simplified Google-Docs-style ruler: tick marks along the page width, plus two
// draggable triangle markers for the left/right margin. Dragging a marker updates
// the bound margin (in px) live, which the parent applies as the page's padding.
const marginLeft = defineModel<number>('marginLeft', { default: 48 })
const marginRight = defineModel<number>('marginRight', { default: 48 })

const TICK_COUNT = 19
const MIN_MARGIN = 16
const rulerRef = ref<HTMLElement | null>(null)

type DragTarget = 'left' | 'right' | null
const dragging = ref<DragTarget>(null)

function startDrag(target: DragTarget, event: PointerEvent) {
  dragging.value = target
  event.preventDefault()
}

function onPointerMove(event: PointerEvent) {
  if (!dragging.value || !rulerRef.value) return
  const rect = rulerRef.value.getBoundingClientRect()
  const x = event.clientX - rect.left
  if (dragging.value === 'left') {
    marginLeft.value = Math.min(Math.max(x, MIN_MARGIN), rect.width - marginRight.value - MIN_MARGIN)
  } else {
    const fromRight = rect.width - x
    marginRight.value = Math.min(Math.max(fromRight, MIN_MARGIN), rect.width - marginLeft.value - MIN_MARGIN)
  }
}

function stopDrag() {
  dragging.value = null
}

onMounted(() => {
  window.addEventListener('pointermove', onPointerMove)
  window.addEventListener('pointerup', stopDrag)
})
onBeforeUnmount(() => {
  window.removeEventListener('pointermove', onPointerMove)
  window.removeEventListener('pointerup', stopDrag)
})
</script>

<template>
  <div ref="rulerRef" class="relative h-6 select-none border-b border-white/10">
    <div class="absolute inset-0 flex items-end">
      <span
        v-for="n in TICK_COUNT"
        :key="n"
        class="flex-1 border-l border-white/10 pl-0.5 text-[9px] leading-none text-[var(--text-muted)]"
      >
        {{ n }}
      </span>
    </div>

    <div
      class="absolute top-0 z-10 flex h-full w-3 -translate-x-1/2 cursor-ew-resize items-start justify-center"
      :style="{ left: `${marginLeft}px` }"
      title="Drag to adjust left margin"
      @pointerdown="startDrag('left', $event)"
    >
      <div class="h-0 w-0 border-x-[5px] border-t-[7px] border-x-transparent border-t-[var(--accent-start)]" />
    </div>
    <div
      class="absolute top-0 z-10 flex h-full w-3 translate-x-1/2 cursor-ew-resize items-start justify-center"
      :style="{ right: `${marginRight}px` }"
      title="Drag to adjust right margin"
      @pointerdown="startDrag('right', $event)"
    >
      <div class="h-0 w-0 border-x-[5px] border-t-[7px] border-x-transparent border-t-[var(--accent-start)]" />
    </div>
  </div>
</template>
