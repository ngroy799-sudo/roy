import { createRng, hashString, int, pick } from './seed'

export type Palette = {
  ink: string
  paper: string
  accent: string
  secondary: string
  mute: string
  names: Record<keyof Omit<Palette, 'names'>, string>
}

function hsl(h: number, s: number, l: number): string {
  return `hsl(${Math.round(h)} ${Math.round(s)}% ${Math.round(l)}%)`
}

function wrapHue(h: number): number {
  return ((h % 360) + 360) % 360
}

/** Build a 5-swatch identity palette from a seed word. Avoids purple-heavy defaults. */
export function generatePalette(seedWord: string): Palette {
  const rng = createRng(hashString(`palette:${seedWord}`))

  const families = [
    { base: 8, name: 'signal' }, // vermillion / coral
    { base: 175, name: 'tide' }, // teal / cyan
    { base: 42, name: 'volt' }, // amber / gold
    { base: 145, name: 'moss' }, // green
    { base: 210, name: 'steel' }, // blue-steel
    { base: 25, name: 'ember' }, // orange
  ] as const

  const family = pick(rng, [...families])
  const hue = wrapHue(family.base + rangeShift(rng))
  const mode = pick(rng, ['ink', 'paper'] as const)

  const accent = hsl(hue, range(rng, 72, 92), range(rng, 48, 58))
  const secondaryHue = wrapHue(hue + pick(rng, [148, 168, 190, -150]))
  const secondary = hsl(secondaryHue, range(rng, 55, 78), range(rng, 40, 52))

  if (mode === 'ink') {
    const ink = hsl(wrapHue(hue + 220), range(rng, 8, 18), range(rng, 6, 11))
    const paper = hsl(wrapHue(hue + 40), range(rng, 6, 14), range(rng, 92, 96))
    const mute = hsl(wrapHue(hue + 200), range(rng, 8, 16), range(rng, 18, 28))
    return {
      ink,
      paper,
      accent,
      secondary,
      mute,
      names: {
        ink: `${family.name}-ink`,
        paper: `${family.name}-paper`,
        accent: `${family.name}-accent`,
        secondary: `${family.name}-second`,
        mute: `${family.name}-mute`,
      },
    }
  }

  const paper = hsl(wrapHue(hue + 30), range(rng, 8, 16), range(rng, 93, 97))
  const ink = hsl(wrapHue(hue + 210), range(rng, 10, 22), range(rng, 10, 16))
  const mute = hsl(wrapHue(hue + 180), range(rng, 10, 20), range(rng, 78, 86))
  return {
    ink,
    paper,
    accent,
    secondary,
    mute,
    names: {
      ink: `${family.name}-ink`,
      paper: `${family.name}-paper`,
      accent: `${family.name}-accent`,
      secondary: `${family.name}-second`,
      mute: `${family.name}-mute`,
    },
  }
}

function range(rng: () => number, min: number, max: number): number {
  return min + rng() * (max - min)
}

function rangeShift(rng: () => number): number {
  return int(rng, -18, 18)
}

export function paletteEntries(palette: Palette): { key: string; color: string; name: string }[] {
  return (['ink', 'paper', 'accent', 'secondary', 'mute'] as const).map((key) => ({
    key,
    color: palette[key],
    name: palette.names[key],
  }))
}
