// composables/useLineHeight.ts
// No official Tiptap extension for line-height — adds a `lineHeight` attribute
// to block nodes, rendered as an inline style, same pattern as font-size.
import { Extension } from '@tiptap/core'

export const LineHeight = Extension.create({
  name: 'lineHeight',

  addOptions() {
    return { types: ['paragraph', 'heading'] }
  },

  addGlobalAttributes() {
    return [
      {
        types: this.options.types,
        attributes: {
          lineHeight: {
            default: null,
            parseHTML: (element: HTMLElement) => element.style.lineHeight || null,
            renderHTML: (attributes: { lineHeight?: string | null }) => {
              if (!attributes.lineHeight) return {}
              return { style: `line-height: ${attributes.lineHeight}` }
            },
          },
        },
      },
    ]
  },

  addCommands() {
    return {
      setLineHeight:
        (lineHeight: string) =>
        ({ commands }: { commands: any }) =>
          this.options.types.every((type: string) => commands.updateAttributes(type, { lineHeight })),
    } as any
  },
})
