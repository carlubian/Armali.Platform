// Areas have arbitrary names/ids, so accent colours are assigned deterministically
// by the area's position (sorted by `order`), cycling through the coastal palette.
// This mirrors the mockup's azure → sea → gold → aqua district colouring.

export interface AreaTheme {
  /** Primary accent (the 500 shade). */
  hex: string;
  /** Deeper accent (the 600 shade) for text on soft backgrounds. */
  hex2: string;
  /** Soft tint used as a fill behind the accent (the 100 shade). */
  soft: string;
  /** Matching glow token name. */
  glow: string;
}

const PALETTE: readonly AreaTheme[] = [
  { hex: "#3A7CA5", hex2: "#2D6588", soft: "#DCEAF2", glow: "var(--glow-azure)" }, // azure
  { hex: "#519B79", hex2: "#3F7A62", soft: "#DEEFE4", glow: "var(--glow-sea)" }, // sea
  { hex: "#DBA63E", hex2: "#C58E2C", soft: "#F8ECCD", glow: "var(--glow-gold)" }, // gold
  { hex: "#16A6A6", hex2: "#0E8E92", soft: "#D8F0EC", glow: "var(--glow-aqua)" }, // aqua
];

/** Theme for the area at the given sorted index (0-based). */
export function getAreaTheme(index: number): AreaTheme {
  return PALETTE[((index % PALETTE.length) + PALETTE.length) % PALETTE.length];
}
