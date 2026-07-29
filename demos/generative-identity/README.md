# SEEDMARK — Generative Identity Demo

Portfolio demo: type a seed word and generate a **deterministic** visual identity — logo mark, 5-color palette, and seamless pattern — with SVG export.

## Location

`demos/generative-identity/`

## Run

```bash
cd demos/generative-identity
npm install
npm run dev
```

Build:

```bash
npm run build
npm run preview
```

## What it does

1. Enter a name / keyword (seed)
2. Engine hashes the seed → PRNG → mark family, palette family, pattern motif
3. Same seed always produces the same system
4. Export mark SVG, pattern SVG, or a combined brand sheet

## Tech

- Vite + TypeScript (vanilla, no framework)
- Seeded hash + Mulberry32 PRNG
- SVG mark families: `orbit`, `knot`, `shard`, `ringgrid`, `slash`
- Pattern motifs: `dots`, `bars`, `chevrons`, `cells`, `arcs`
- Fonts: Syne + IBM Plex Mono

## Source map (for agents)

| Path | Role |
|------|------|
| `src/engine/seed.ts` | Hash + PRNG helpers |
| `src/engine/palette.ts` | 5-swatch palette generator |
| `src/engine/mark.ts` | Geometric logo mark SVG |
| `src/engine/pattern.ts` | Tile pattern SVG |
| `src/engine/identity.ts` | Compose full identity system |
| `src/engine/export.ts` | Download SVG / brand sheet |
| `src/main.ts` | UI wiring, live regenerate |
| `src/style.css` | Layout, motion, tokens |

## Keywords

generative design, identity system, logo generator, seeded PRNG, SVG export, portfolio demo, creative coding, brand toolkit
