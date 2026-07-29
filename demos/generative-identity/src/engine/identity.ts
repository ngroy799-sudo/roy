import { hashString } from './seed'
import { generatePalette, type Palette } from './palette'
import { generateMark, type MarkSpec } from './mark'
import { generatePattern, type PatternSpec } from './pattern'

export type IdentitySystem = {
  seedWord: string
  seedHash: string
  palette: Palette
  mark: MarkSpec
  pattern: PatternSpec
}

export function generateIdentity(raw: string): IdentitySystem {
  const seedWord = raw.trim() || 'Seedmark'
  const palette = generatePalette(seedWord)
  const mark = generateMark(seedWord, palette)
  const pattern = generatePattern(seedWord, palette, mark)
  return {
    seedWord,
    seedHash: hashString(seedWord.toLowerCase()).toString(16).padStart(8, '0'),
    palette,
    mark,
    pattern,
  }
}
