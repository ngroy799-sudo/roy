import { createRng, hashString, int, pick, range } from './seed'
import type { Palette } from './palette'
import type { MarkSpec } from './mark'

export type PatternSpec = {
  motif: string
  svg: string
  tileSize: number
}

/** Seamless geometric pattern tile derived from mark DNA + palette. */
export function generatePattern(seedWord: string, palette: Palette, mark: MarkSpec): PatternSpec {
  const rng = createRng(hashString(`pattern:${seedWord}:${mark.family}`))
  const tileSize = 120
  const motif = pick(rng, ['dots', 'bars', 'chevrons', 'cells', 'arcs'] as const)
  const a = palette.accent
  const b = palette.secondary
  const ink = palette.ink
  const mute = palette.mute

  let content = ''
  switch (motif) {
    case 'dots': {
      const cols = int(rng, 3, 5)
      const rows = cols
      const gap = tileSize / cols
      const parts: string[] = []
      for (let y = 0; y < rows; y++) {
        for (let x = 0; x < cols; x++) {
          const r = range(rng, 4, gap * 0.35)
          parts.push(
            `<circle cx="${gap * (x + 0.5)}" cy="${gap * (y + 0.5)}" r="${r}" fill="${(x + y) % 2 ? a : b}" opacity="${0.45 + rng() * 0.5}"/>`,
          )
        }
      }
      content = parts.join('')
      break
    }
    case 'bars': {
      const n = int(rng, 5, 8)
      const w = tileSize / n
      const parts: string[] = []
      for (let i = 0; i < n; i++) {
        const h = range(rng, tileSize * 0.35, tileSize)
        parts.push(`<rect x="${i * w}" y="${tileSize - h}" width="${w - 2}" height="${h}" fill="${i % 2 ? a : b}" opacity="0.7"/>`)
      }
      content = parts.join('')
      break
    }
    case 'chevrons': {
      const parts: string[] = []
      for (let i = 0; i < 4; i++) {
        const y = 12 + i * 28
        parts.push(
          `<polyline points="10,${y + 18} 60,${y} 110,${y + 18}" fill="none" stroke="${i % 2 ? a : b}" stroke-width="${3 + (i % 2)}" stroke-linecap="square"/>`,
        )
      }
      content = parts.join('')
      break
    }
    case 'cells': {
      const n = 3
      const s = tileSize / n
      const parts: string[] = []
      for (let y = 0; y < n; y++) {
        for (let x = 0; x < n; x++) {
          if (rng() > 0.25) {
            parts.push(
              `<rect x="${x * s + 4}" y="${y * s + 4}" width="${s - 8}" height="${s - 8}" fill="${(x + y) % 2 ? a : mute}" opacity="${0.55 + rng() * 0.4}" rx="${rng() > 0.5 ? 0 : 8}"/>`,
            )
          }
        }
      }
      content = parts.join('')
      break
    }
    case 'arcs': {
      const parts: string[] = []
      for (let i = 0; i < 5; i++) {
        const r = 18 + i * 16
        parts.push(
          `<path d="M ${60 - r} 60 A ${r} ${r} 0 0 1 ${60 + r} 60" fill="none" stroke="${i % 2 ? a : b}" stroke-width="3"/>`,
        )
      }
      content = parts.join('')
      break
    }
  }

  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${tileSize} ${tileSize}" width="${tileSize}" height="${tileSize}">
  <rect width="${tileSize}" height="${tileSize}" fill="${ink}"/>
  ${content}
</svg>`

  return { motif, svg, tileSize }
}

export function patternCssBackground(pattern: PatternSpec): string {
  const encoded = encodeURIComponent(pattern.svg).replace(/'/g, '%27')
  return `url("data:image/svg+xml,${encoded}")`
}
