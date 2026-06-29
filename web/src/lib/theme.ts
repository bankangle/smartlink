/** Artwork-driven theming: turn one accent hex into a cohesive page palette. */

interface Rgb {
  r: number;
  g: number;
  b: number;
}

function parseHex(hex: string | null | undefined): Rgb {
  const fallback = { r: 91, g: 110, b: 245 }; // pleasant indigo default
  if (!hex) return fallback;
  let h = hex.replace('#', '').trim();
  if (h.length === 3) h = h.split('').map((c) => c + c).join('');
  if (h.length !== 6) return fallback;
  const n = parseInt(h, 16);
  if (Number.isNaN(n)) return fallback;
  return { r: (n >> 16) & 255, g: (n >> 8) & 255, b: n & 255 };
}

const clamp = (v: number) => Math.max(0, Math.min(255, Math.round(v)));

function mix({ r, g, b }: Rgb, target: Rgb, t: number): Rgb {
  return {
    r: clamp(r + (target.r - r) * t),
    g: clamp(g + (target.g - g) * t),
    b: clamp(b + (target.b - b) * t)
  };
}

const toCss = ({ r, g, b }: Rgb, a = 1) => (a === 1 ? `rgb(${r} ${g} ${b})` : `rgb(${r} ${g} ${b} / ${a})`);

function luminance({ r, g, b }: Rgb): number {
  const f = (c: number) => {
    const s = c / 255;
    return s <= 0.03928 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
  };
  return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b);
}

interface Hsl {
  h: number;
  s: number;
  l: number;
}

function rgbToHsl({ r, g, b }: Rgb): Hsl {
  const rn = r / 255, gn = g / 255, bn = b / 255;
  const max = Math.max(rn, gn, bn), min = Math.min(rn, gn, bn);
  const d = max - min;
  let h = 0;
  if (d !== 0) {
    if (max === rn) h = ((gn - bn) / d) % 6;
    else if (max === gn) h = (bn - rn) / d + 2;
    else h = (rn - gn) / d + 4;
    h *= 60;
    if (h < 0) h += 360;
  }
  const l = (max + min) / 2;
  const s = d === 0 ? 0 : d / (1 - Math.abs(2 * l - 1));
  return { h, s, l };
}

function hslToRgb({ h, s, l }: Hsl): Rgb {
  const c = (1 - Math.abs(2 * l - 1)) * s;
  const x = c * (1 - Math.abs(((h / 60) % 2) - 1));
  const m = l - c / 2;
  let r = 0, g = 0, b = 0;
  if (h < 60) [r, g, b] = [c, x, 0];
  else if (h < 120) [r, g, b] = [x, c, 0];
  else if (h < 180) [r, g, b] = [0, c, x];
  else if (h < 240) [r, g, b] = [0, x, c];
  else if (h < 300) [r, g, b] = [x, 0, c];
  else [r, g, b] = [c, 0, x];
  return { r: clamp((r + m) * 255), g: clamp((g + m) * 255), b: clamp((b + m) * 255) };
}

const BLACK = { r: 8, g: 8, b: 11 };
const WHITE = { r: 255, g: 255, b: 255 };

/**
 * "Extends" a single accent into a harmonious 3-color palette by rotating hue
 * (analogous + a far accent) and normalising saturation/lightness so each color
 * reads as a vivid glow on a dark background.
 */
function meshFrom(accent: Rgb): string[] {
  const base = rgbToHsl(accent);
  const s = Math.min(0.95, Math.max(0.55, base.s + 0.2));
  const glow = (h: number, l: number): string => toCss(hslToRgb({ h: (h + 360) % 360, s, l }));
  return [
    glow(base.h, 0.55),        // the accent itself, vivid
    glow(base.h + 32, 0.5),    // analogous, warmer/cooler neighbour
    glow(base.h - 48, 0.45)    // a farther hue for contrast
  ];
}

export interface Theme {
  /** Full-page background (radial glow over a near-black base). */
  background: string;
  /** Solid accent for the primary button. */
  accent: string;
  /** Brighter accent for hovers/borders. */
  accentSoft: string;
  /** Readable text color on top of the accent button. */
  onAccent: string;
  /** Subtle accent tint for cards/borders. */
  tint: string;
  /** Three harmonious glow colors for the animated background blobs. */
  mesh: string[];
}

export function buildTheme(accentHex: string | null | undefined): Theme {
  const accent = parseHex(accentHex);
  const bright = mix(accent, WHITE, 0.18);
  const deep = mix(accent, BLACK, 0.55);
  const onAccent = luminance(accent) > 0.45 ? '#0a0a0b' : '#ffffff';

  // Dark base; the animated blobs (mesh) supply the color on top of this.
  const background = `radial-gradient(125% 90% at 50% -5%, ${toCss(
    mix(accent, BLACK, 0.62)
  )} 0%, rgb(10 10 11) 68%)`;

  return {
    background,
    accent: toCss(accent),
    accentSoft: toCss(bright),
    onAccent,
    tint: toCss(accent, 0.14),
    mesh: meshFrom(accent)
  };
}
