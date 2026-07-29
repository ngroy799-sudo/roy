import { createRng, hashString, int, pick, range } from './seed'
import type { Palette } from './palette'

export type MarkSpec = {
  family: string
  initials: string
  svg: string
  viewBox: string
}

function initialsFrom(seedWord: string): string {
  const parts = seedWord
    .trim()
    .split(/[\s_\-.]+/)
    .filter(Boolean)
  if (parts.length >= 2) {
    return (parts[0]![0]! + parts[1]![0]!).toUpperCase()
  }
  const w = (parts[0] || 'SM').replace(/[^a-zA-Z0-9]/g, '')
  if (w.length >= 2) return (w[0]! + w[1]!).toUpperCase()
  return (w[0] || 'S').toUpperCase() + 'M'
}

/** Geometric logo mark families driven by seed. */
export function generateMark(seedWord: string, palette: Palette): MarkSpec {
  const rng = createRng(hashString(`mark:${seedWord}`))
  const initials = initialsFrom(seedWord)
  const family = pick(rng, ['orbit', 'knot', 'shard', 'ringgrid', 'slash'] as const)
  const vb = '0 0 200 200'
  const a = palette.accent
  const b = palette.secondary
  const ink = palette.ink
  const paper = palette.paper

  let body = ''
  switch (family) {
    case 'orbit': {
      const rings = int(rng, 2, 4)
      const arcs: string[] = []
      for (let i = 0; i < rings; i++) {
        const r = 28 + i * 22
        const start = range(rng, 0, Math.PI * 2)
        const sweep = range(rng, Math.PI * 0.7, Math.PI * 1.4)
        arcs.push(arcPath(100, 100, r, start, start + sweep))
      }
      body = `
        <circle cx="100" cy="100" r="8" fill="${a}"/>
        ${arcs
          .map(
            (d, i) =>
              `<path d="${d}" fill="none" stroke="${i % 2 ? b : a}" stroke-width="${i === 0 ? 6 : 3.5}" stroke-linecap="round"/>`,
          )
          .join('')}
        <text x="100" y="108" text-anchor="middle" font-family="Syne, sans-serif" font-weight="800" font-size="28" fill="${paper}">${escapeXml(initials)}</text>
      `
      break
    }
    case 'knot': {
      const rot = int(rng, 0, 3) * 15
      body = `
        <g transform="rotate(${rot} 100 100)">
          <rect x="46" y="46" width="108" height="108" rx="8" fill="none" stroke="${a}" stroke-width="5"/>
          <rect x="64" y="64" width="72" height="72" rx="4" fill="none" stroke="${b}" stroke-width="3" transform="rotate(18 100 100)"/>
          <path d="M70 100 H130 M100 70 V130" stroke="${paper}" stroke-width="4" stroke-linecap="square"/>
          <circle cx="100" cy="100" r="18" fill="${ink}" stroke="${a}" stroke-width="3"/>
          <text x="100" y="107" text-anchor="middle" font-family="Syne, sans-serif" font-weight="800" font-size="18" fill="${paper}">${escapeXml(initials[0]!)}</text>
        </g>
      `
      break
    }
    case 'shard': {
      const pts = shardPoints(rng)
      body = `
        <polygon points="${pts}" fill="${a}" opacity="0.92"/>
        <polygon points="${shardPoints(rng, 0.72)}" fill="${b}" opacity="0.85"/>
        <circle cx="100" cy="100" r="34" fill="${ink}"/>
        <text x="100" y="110" text-anchor="middle" font-family="Syne, sans-serif" font-weight="800" font-size="32" fill="${paper}">${escapeXml(initials)}</text>
      `
      break
    }
    case 'ringgrid': {
      const n = pick(rng, [3, 4, 5])
      const cells: string[] = []
      const step = 110 / (n - 1)
      const origin = 45
      for (let y = 0; y < n; y++) {
        for (let x = 0; x < n; x++) {
          const cx = origin + x * step
          const cy = origin + y * step
          const r = range(rng, 4, 14)
          const fill = (x + y) % 2 === 0 ? a : b
          if (rng() > 0.18) cells.push(`<circle cx="${cx}" cy="${cy}" r="${r}" fill="${fill}" opacity="${0.55 + rng() * 0.45}"/>`)
        }
      }
      body = `
        ${cells.join('')}
        <rect x="58" y="58" width="84" height="84" fill="${ink}" opacity="0.88"/>
        <text x="100" y="112" text-anchor="middle" font-family="Syne, sans-serif" font-weight="800" font-size="34" fill="${paper}">${escapeXml(initials)}</text>
      `
      break
    }
    case 'slash': {
      const slant = range(rng, -28, 28)
      body = `
        <g transform="skewX(${slant * 0.15})">
          <path d="M40 150 L100 40 L160 150 Z" fill="none" stroke="${a}" stroke-width="5"/>
          <path d="M55 140 L100 60 L145 140 Z" fill="${b}" opacity="0.35"/>
          <line x1="55" y1="55" x2="145" y2="145" stroke="${a}" stroke-width="7" stroke-linecap="square"/>
          <line x1="145" y1="55" x2="55" y2="145" stroke="${b}" stroke-width="3" stroke-linecap="square"/>
          <text x="100" y="118" text-anchor="middle" font-family="Syne, sans-serif" font-weight="800" font-size="26" fill="${paper}">${escapeXml(initials)}</text>
        </g>
      `
      break
    }
  }

  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="${vb}" width="200" height="200" role="img" aria-label="Generated mark ${escapeXml(initials)}">
  <rect width="200" height="200" fill="${ink}"/>
  ${body}
</svg>`

  return { family, initials, svg, viewBox: vb }
}

function arcPath(cx: number, cy: number, r: number, start: number, end: number): string {
  const s = polar(cx, cy, r, start)
  const e = polar(cx, cy, r, end)
  const large = end - start > Math.PI ? 1 : 0
  return `M ${s.x} ${s.y} A ${r} ${r} 0 ${large} 1 ${e.x} ${e.y}`
}

function polar(cx: number, cy: number, r: number, a: number) {
  return { x: cx + r * Math.cos(a), y: cy + r * Math.sin(a) }
}

function shardPoints(rng: () => number, scale = 1): string {
  const n = int(rng, 5, 7)
  const pts: string[] = []
  for (let i = 0; i < n; i++) {
    const ang = (Math.PI * 2 * i) / n + range(rng, -0.2, 0.2)
    const rad = range(rng, 55, 92) * scale
    const p = polar(100, 100, rad, ang)
    pts.push(`${p.x.toFixed(1)},${p.y.toFixed(1)}`)
  }
  return pts.join(' ')
}

function escapeXml(s: string): string {
  return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
}
