/** Deterministic 32-bit hash from a string (FNV-1a inspired). */
export function hashString(input: string): number {
  let h = 2166136261 >>> 0
  const s = input.trim().toLowerCase() || 'seedmark'
  for (let i = 0; i < s.length; i++) {
    h ^= s.charCodeAt(i)
    h = Math.imul(h, 16777619)
  }
  return h >>> 0
}

/** Mulberry32 seeded PRNG. */
export function createRng(seed: number): () => number {
  let t = seed >>> 0
  return () => {
    t += 0x6d2b79f5
    let r = Math.imul(t ^ (t >>> 15), 1 | t)
    r ^= r + Math.imul(r ^ (r >>> 7), 61 | r)
    return ((r ^ (r >>> 14)) >>> 0) / 4294967296
  }
}

export function pick<T>(rng: () => number, items: T[]): T {
  return items[Math.floor(rng() * items.length)]!
}

export function range(rng: () => number, min: number, max: number): number {
  return min + rng() * (max - min)
}

export function int(rng: () => number, min: number, max: number): number {
  return Math.floor(range(rng, min, max + 1))
}
