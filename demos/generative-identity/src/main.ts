import './style.css'
import { generateIdentity, type IdentitySystem } from './engine/identity'
import { paletteEntries } from './engine/palette'
import { patternCssBackground } from './engine/pattern'
import { downloadBrandSheet, downloadMarkSvg, downloadPatternSvg } from './engine/export'

const DEFAULT_SEED = 'Aurora Lab'

function render(root: HTMLElement, identity: IdentitySystem, seedInput: string) {
  const entries = paletteEntries(identity.palette)
  const patternBg = patternCssBackground(identity.pattern)

  root.innerHTML = `
    <div class="page" style="--ink:${identity.palette.ink};--paper:${identity.palette.paper};--accent:${identity.palette.accent};--secondary:${identity.palette.secondary};--mute:${identity.palette.mute};">
      <div class="atmosphere" aria-hidden="true" style="background-image:${patternBg}"></div>

      <header class="hero">
        <div class="hero__copy">
          <p class="brand">SEEDMARK</p>
          <h1 class="headline">Type a word. Grow an identity.</h1>
          <p class="lede">A deterministic mark, palette, and pattern system for creative-technical portfolios.</p>

          <form class="seed-form" id="seed-form" autocomplete="off">
            <label class="seed-label" for="seed">Seed word</label>
            <div class="seed-row">
              <input id="seed" name="seed" type="text" maxlength="48" value="${escapeAttr(seedInput)}" placeholder="e.g. Nova Studio" />
              <button type="submit" class="btn btn--primary">Generate</button>
            </div>
          </form>
        </div>

        <div class="hero__mark" id="mark-stage">
          <div class="mark-frame regenerating" id="mark-frame">
            ${identity.mark.svg}
          </div>
          <p class="mark-meta">
            <span>${escapeHtml(identity.mark.family)}</span>
            <span>·</span>
            <span>${escapeHtml(identity.mark.initials)}</span>
            <span>·</span>
            <span>#${identity.seedHash}</span>
          </p>
        </div>
      </header>

      <section class="system" aria-label="Identity system">
        <div class="system__head">
          <h2>System output</h2>
          <p>Same seed always rebuilds the same identity. Export SVG for decks, sites, or prototypes.</p>
        </div>

        <div class="system__grid">
          <div class="block block--palette">
            <h3>Palette</h3>
            <ul class="swatches">
              ${entries
                .map(
                  (e) => `
                <li>
                  <button type="button" class="swatch" data-color="${escapeAttr(e.color)}" style="--swatch:${e.color}" title="Copy ${escapeAttr(e.color)}">
                    <span class="swatch__name">${escapeHtml(e.name)}</span>
                    <span class="swatch__value">${escapeHtml(e.color)}</span>
                  </button>
                </li>`,
                )
                .join('')}
            </ul>
          </div>

          <div class="block block--pattern">
            <h3>Pattern</h3>
            <div class="pattern-preview" style="background-image:${patternBg}"></div>
            <p class="block-meta">motif · ${escapeHtml(identity.pattern.motif)}</p>
          </div>

          <div class="block block--export">
            <h3>Export</h3>
            <div class="export-row">
              <button type="button" class="btn btn--ghost" id="export-mark">Mark SVG</button>
              <button type="button" class="btn btn--ghost" id="export-pattern">Pattern SVG</button>
              <button type="button" class="btn btn--accent" id="export-sheet">Brand sheet</button>
            </div>
            <p class="toast" id="toast" hidden></p>
          </div>
        </div>
      </section>

      <footer class="foot">
        <p>SEEDMARK · generative identity demo · seed-locked geometry</p>
      </footer>
    </div>
  `

  const form = root.querySelector<HTMLFormElement>('#seed-form')!
  const input = root.querySelector<HTMLInputElement>('#seed')!
  const markFrame = root.querySelector<HTMLElement>('#mark-frame')!

  requestAnimationFrame(() => {
    markFrame.classList.remove('regenerating')
    markFrame.classList.add('revealed')
  })

  form.addEventListener('submit', (e) => {
    e.preventDefault()
    const next = input.value.trim() || DEFAULT_SEED
    const nextIdentity = generateIdentity(next)
    markFrame.classList.remove('revealed')
    markFrame.classList.add('regenerating')
    window.setTimeout(() => render(root, nextIdentity, next), 180)
  })

  // Live regenerate on debounce while typing (feels technical + playful)
  let timer = 0
  input.addEventListener('input', () => {
    window.clearTimeout(timer)
    timer = window.setTimeout(() => {
      const next = input.value.trim() || DEFAULT_SEED
      render(root, generateIdentity(next), input.value)
      const again = root.querySelector<HTMLInputElement>('#seed')
      again?.focus()
      if (again) {
        const len = again.value.length
        again.setSelectionRange(len, len)
      }
    }, 320)
  })

  root.querySelector('#export-mark')?.addEventListener('click', () => {
    downloadMarkSvg(identity)
    flash(root, 'Mark SVG downloaded')
  })
  root.querySelector('#export-pattern')?.addEventListener('click', () => {
    downloadPatternSvg(identity)
    flash(root, 'Pattern SVG downloaded')
  })
  root.querySelector('#export-sheet')?.addEventListener('click', () => {
    downloadBrandSheet(identity)
    flash(root, 'Brand sheet downloaded')
  })

  root.querySelectorAll<HTMLButtonElement>('.swatch').forEach((btn) => {
    btn.addEventListener('click', async () => {
      const color = btn.dataset.color || ''
      try {
        await navigator.clipboard.writeText(color)
        flash(root, `Copied ${color}`)
      } catch {
        flash(root, color)
      }
    })
  })
}

function flash(root: HTMLElement, message: string) {
  const toast = root.querySelector<HTMLElement>('#toast')
  if (!toast) return
  toast.hidden = false
  toast.textContent = message
  window.setTimeout(() => {
    toast.hidden = true
  }, 1600)
}

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}

function escapeAttr(s: string): string {
  return escapeHtml(s).replace(/'/g, '&#39;')
}

const app = document.querySelector<HTMLDivElement>('#app')!
const initial = generateIdentity(DEFAULT_SEED)
render(app, initial, DEFAULT_SEED)
