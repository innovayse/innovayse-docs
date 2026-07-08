// innovayse-docs/frontend/composables/usePagination.ts
import { Extension } from '@tiptap/core'
import { Plugin, PluginKey } from 'prosemirror-state'
import { Decoration, DecorationSet } from 'prosemirror-view'
import type { EditorView } from 'prosemirror-view'

export interface PaginationOptions {
  /** Usable content height per page, in px — page height minus padding minus footer. */
  pageContentHeight: number
}

interface PaginationState {
  breaks: number[]
}

const paginationPluginKey = new PluginKey<PaginationState>('pagination')

/** Measures each top-level node's rendered height and returns the document positions
 * where a page break should fall — always between nodes, never inside one. A node
 * taller than a full page is allowed to overflow the page it starts on (not split),
 * and forces the *next* node onto a fresh page. */
function computeBreaks(view: EditorView, pageContentHeight: number, scale = 1): number[] {
  const breaks: number[] = []
  let used = 0
  let forceBreakNext = false

  view.state.doc.forEach((_node, offset) => {
    const dom = view.nodeDOM(offset) as HTMLElement | null
    if (!dom || typeof dom.getBoundingClientRect !== 'function') return

    const rect = dom.getBoundingClientRect()
    const style = window.getComputedStyle(dom)
    const height = (rect.height + parseFloat(style.marginTop || '0') + parseFloat(style.marginBottom || '0')) / scale

    if (forceBreakNext || (used > 0 && used + height > pageContentHeight)) {
      breaks.push(offset)
      used = 0
      forceBreakNext = false
    }

    used += height
    if (used > pageContentHeight) {
      forceBreakNext = true
    }
  })

  return breaks
}

/** CSS `zoom` scales rendered geometry, so getBoundingClientRect() measurements come back
 * already scaled. Walk up from the ProseMirror DOM element to find the `.page-surface`
 * ancestor carrying the reactive `zoom` style and read its computed zoom factor, so
 * measurements can be normalized back to logical (zoom=1) pixels. */
function getZoomFactor(dom: HTMLElement): number {
  const surface = dom.closest('.page-surface') as HTMLElement | null
  if (!surface) return 1
  const zoom = parseFloat(window.getComputedStyle(surface).zoom)
  return Number.isFinite(zoom) && zoom > 0 ? zoom : 1
}

function breaksEqual(a: number[], b: number[]): boolean {
  return a.length === b.length && a.every((value, index) => value === b[index])
}

function buildBreakWidget(pageNumber: number): HTMLElement {
  const wrapper = document.createElement('div')
  wrapper.setAttribute('contenteditable', 'false')

  const footer = document.createElement('div')
  footer.className = 'page-break-footer'
  footer.textContent = `Page ${pageNumber}`
  wrapper.appendChild(footer)

  const gap = document.createElement('div')
  gap.className = 'page-break-gap'
  wrapper.appendChild(gap)

  return wrapper
}

export const Pagination = Extension.create<PaginationOptions>({
  name: 'pagination',

  addOptions() {
    return { pageContentHeight: 995 }
  },

  addProseMirrorPlugins() {
    const { pageContentHeight } = this.options

    return [
      new Plugin<PaginationState>({
        key: paginationPluginKey,

        state: {
          init: () => ({ breaks: [] }),
          apply(tr, prev) {
            const next = tr.getMeta(paginationPluginKey) as PaginationState | undefined
            return next ?? prev
          },
        },

        props: {
          decorations(state) {
            const { breaks } = paginationPluginKey.getState(state) ?? { breaks: [] }
            const decorations = breaks.map((pos, index) =>
              Decoration.widget(pos, () => buildBreakWidget(index + 1), { side: -1 }),
            )
            // Break widgets only label the page *before* each break — the last (current)
            // page never has a break after it, so it needs its own trailing footer at the
            // very end of the document to show a page number too.
            decorations.push(
              Decoration.widget(state.doc.content.size, () => buildBreakWidget(breaks.length + 1), {
                side: 1,
              }),
            )
            return DecorationSet.create(state.doc, decorations)
          },
        },

        view(editorView) {
          let timer: ReturnType<typeof setTimeout> | undefined

          const recalculate = () => {
            const scale = getZoomFactor(editorView.dom as HTMLElement)
            const breaks = computeBreaks(editorView, pageContentHeight, scale)
            const current = paginationPluginKey.getState(editorView.state)?.breaks ?? []
            if (breaksEqual(breaks, current)) return
            editorView.dispatch(editorView.state.tr.setMeta(paginationPluginKey, { breaks }))
          }

          // Debounced per the spec — avoids remeasuring layout on every keystroke.
          const scheduleRecalculate = () => {
            if (timer) clearTimeout(timer)
            timer = setTimeout(recalculate, 250)
          }

          scheduleRecalculate()

          // Zoom changes, ruler margin drags, and window resizes all change the box size
          // of the .page-surface ancestor — observing it covers all three cases without
          // needing separate listeners.
          const surface = (editorView.dom as HTMLElement).closest('.page-surface')
          const resizeObserver = surface
            ? new ResizeObserver(() => scheduleRecalculate())
            : undefined
          if (surface && resizeObserver) resizeObserver.observe(surface)

          return {
            update: scheduleRecalculate,
            destroy() {
              if (timer) clearTimeout(timer)
              resizeObserver?.disconnect()
            },
          }
        },
      }),
    ]
  },
})
