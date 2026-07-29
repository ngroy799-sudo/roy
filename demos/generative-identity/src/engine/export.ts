import type { IdentitySystem } from './identity'
import { paletteEntries } from './palette'

function download(filename: string, contents: string, mime: string) {
  const blob = new Blob([contents], { type: mime })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}

function slug(s: string): string {
  return (
    s
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '') || 'seedmark'
  )
}

function escapeXml(s: string): string {
  return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
}

function svgInner(svg: string): string {
  return svg.replace(/^[\s\S]*?<svg[^>]*>/, '').replace(/<\/svg>\s*$/, '')
}

export function downloadMarkSvg(identity: IdentitySystem) {
  download(`${slug(identity.seedWord)}-mark.svg`, identity.mark.svg, 'image/svg+xml')
}

export function downloadPatternSvg(identity: IdentitySystem) {
  download(`${slug(identity.seedWord)}-pattern.svg`, identity.pattern.svg, 'image/svg+xml')
}

/** Combined brand sheet: mark + palette swatches + pattern strip. */
export function downloadBrandSheet(identity: IdentitySystem) {
  const { palette, mark, pattern, seedWord, seedHash } = identity
  const entries = paletteEntries(palette)
  const swatches = entries
    .map(
      (e, i) => `
    <rect x="${40 + i * 88}" y="320" width="76" height="76" fill="${e.color}"/>
    <text x="${40 + i * 88 + 38}" y="418" text-anchor="middle" font-family="IBM Plex Mono, monospace" font-size="10" fill="${palette.paper}">${escapeXml(e.name)}</text>`,
    )
    .join('')

  const sheet = `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="520" height="520" viewBox="0 0 520 520">
  <rect width="520" height="520" fill="${palette.ink}"/>
  <text x="40" y="48" font-family="Syne, sans-serif" font-weight="800" font-size="28" fill="${palette.paper}">SEEDMARK</text>
  <text x="40" y="74" font-family="IBM Plex Mono, monospace" font-size="12" fill="${palette.secondary}">seed · ${escapeXml(seedWord)} · #${seedHash}</text>
  <g transform="translate(40 100)">
    <svg width="200" height="200" viewBox="0 0 200 200">${svgInner(mark.svg)}</svg>
  </g>
  <g transform="translate(280 100)">
    <svg width="200" height="200" viewBox="0 0 120 120">${svgInner(pattern.svg)}</svg>
  </g>
  ${swatches}
  <text x="40" y="460" font-family="IBM Plex Mono, monospace" font-size="11" fill="${palette.mute}">mark · ${escapeXml(mark.family)} · ${escapeXml(mark.initials)}</text>
  <text x="40" y="482" font-family="IBM Plex Mono, monospace" font-size="11" fill="${palette.mute}">pattern · ${escapeXml(pattern.motif)}</text>
</svg>`

  download(`${slug(seedWord)}-brand-sheet.svg`, sheet, 'image/svg+xml')
}
