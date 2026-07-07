// composables/useFontSize.ts
// Tiptap v2 has no built-in font-size mark (unlike font-family) — this adds one
// the same way, via the `TextStyle` mark's inline `style` attribute.
import { Extension } from '@tiptap/core'

export const FontSize = Extension.create({
  name: 'fontSize',

  addOptions() {
    return { types: ['textStyle'] }
  },

  addGlobalAttributes() {
    return [
      {
        types: this.options.types,
        attributes: {
          fontSize: {
            default: null,
            parseHTML: (element: HTMLElement) => element.style.fontSize || null,
            renderHTML: (attributes: { fontSize?: string | null }) => {
              if (!attributes.fontSize) return {}
              return { style: `font-size: ${attributes.fontSize}` }
            },
          },
        },
      },
    ]
  },

  addCommands() {
    return {
      setFontSize:
        (fontSize: string) =>
        ({ chain }: { chain: () => any }) =>
          chain().setMark('textStyle', { fontSize }).run(),
      unsetFontSize:
        () =>
        ({ chain }: { chain: () => any }) =>
          chain().setMark('textStyle', { fontSize: null }).removeEmptyTextStyle().run(),
    } as any
  },
})
