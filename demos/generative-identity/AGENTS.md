# AGENTS.md — SEEDMARK / Generative Identity

## Project purpose

Creative-technical portfolio demo that turns a text seed into a reproducible brand kit (mark + palette + pattern) and exports SVG.

## Search keywords

`SEEDMARK`, `generative identity`, `seeded mark`, `palette generator`, `pattern tile`, `SVG brand sheet`, `Mulberry32`, `deterministic design`

## Architecture

```
seed word
  → hashString / createRng
  → generatePalette + generateMark + generatePattern
  → IdentitySystem
  → UI preview + export downloads
```

## Edit guide

- New mark style → add family in `src/engine/mark.ts` switch + `pick()` list
- New pattern → add motif in `src/engine/pattern.ts`
- New palette family hues → `families` in `src/engine/palette.ts`
- UI copy / layout → `src/main.ts` + `src/style.css`
- Do not break seed determinism: same seed must yield same outputs

## Commands

- Dev: `npm run dev`
- Build: `npm run build`

## Constraints

- Keep folder self-contained under `demos/generative-identity/`
- Prefer SVG (no canvas raster) for exportability
- Avoid purple-default / generic AI aesthetic drift when changing visuals
