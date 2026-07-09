// composables/useIndent.ts
// Tiptap's sinkListItem/liftListItem only affect list items — clicking indent/outdent on a
// plain paragraph or heading silently does nothing. This adds paragraph/heading-level
// indentation via an `indent` attribute, the same pattern as useFontSize.ts/useLineHeight.ts.
import { Extension } from '@tiptap/core'

const MAX_INDENT = 8
const INDENT_STEP_PX = 24

export const Indent = Extension.create({
  name: 'indent',

  addOptions() {
    return { types: ['paragraph', 'heading'] }
  },

  addGlobalAttributes() {
    return [
      {
        types: this.options.types,
        attributes: {
          indent: {
            default: 0,
            parseHTML: (element: HTMLElement) => {
              const margin = parseFloat(element.style.marginLeft || '0')
              return margin > 0 ? Math.round(margin / INDENT_STEP_PX) : 0
            },
            renderHTML: (attributes: { indent?: number }) => {
              if (!attributes.indent) return {}
              return { style: `margin-left: ${attributes.indent * INDENT_STEP_PX}px` }
            },
          },
        },
      },
    ]
  },

  addCommands() {
    return {
      increaseIndent:
        () =>
        ({ tr, state, dispatch }: { tr: any; state: any; dispatch: any }) => {
          const { types } = this.options
          const { selection } = state
          let changed = false
          state.doc.nodesBetween(selection.from, selection.to, (node: any, pos: number) => {
            if (types.includes(node.type.name)) {
              const current = node.attrs.indent ?? 0
              if (current < MAX_INDENT) {
                tr.setNodeAttribute(pos, 'indent', current + 1)
                changed = true
              }
            }
          })
          if (changed && dispatch) dispatch(tr)
          return changed
        },
      decreaseIndent:
        () =>
        ({ tr, state, dispatch }: { tr: any; state: any; dispatch: any }) => {
          const { types } = this.options
          const { selection } = state
          let changed = false
          state.doc.nodesBetween(selection.from, selection.to, (node: any, pos: number) => {
            if (types.includes(node.type.name)) {
              const current = node.attrs.indent ?? 0
              if (current > 0) {
                tr.setNodeAttribute(pos, 'indent', current - 1)
                changed = true
              }
            }
          })
          if (changed && dispatch) dispatch(tr)
          return changed
        },
    } as any
  },
})
