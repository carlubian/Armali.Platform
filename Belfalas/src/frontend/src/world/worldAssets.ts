import { Assets, Spritesheet, type Texture } from "pixi.js";
import type { WorldTemplateRenderContract } from "../api/types";

/** Shape of the authored base map (see WORLD_TEMPLATE_CONTRACT.md → Base Tile Maps). */
export interface BaseMap {
  width: number;
  height: number;
  tileWidth: number;
  tileHeight: number;
  origin: { x: number; y: number };
  terrainLegend: Record<string, string>;
  rows: string[][];
}

/** Atlas JSON in Pixi spritesheet format; only the fields we read are typed. */
interface AtlasData {
  frames: Record<string, unknown>;
  meta: { image: string };
}

export interface WorldAssets {
  map: BaseMap;
  sheet: Spritesheet;
}

/**
 * Loads a template's base map and sprite atlas following the default loading
 * contract: `map.json` and `{atlasKey}.json` under `assetBasePath`, with the atlas
 * image resolved relative to the atlas metadata. Theme-agnostic — every path is
 * derived from the render contract, never hardcoded to a specific template.
 */
export async function loadWorldAssets(
  render: Pick<WorldTemplateRenderContract, "assetBasePath" | "atlasKey">,
): Promise<WorldAssets> {
  const base = render.assetBasePath.replace(/\/$/, "");

  const [map, atlas] = await Promise.all([
    fetchJson<BaseMap>(`${base}/map.json`),
    fetchJson<AtlasData>(`${base}/${render.atlasKey}.json`),
  ]);

  const imageUrl = `${base}/${atlas.meta.image}`;
  const texture = await Assets.load<Texture>(imageUrl);

  const sheet = new Spritesheet(texture, atlas as never);
  await sheet.parse();

  return { map, sheet };
}

async function fetchJson<T>(url: string): Promise<T> {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`Could not load world asset ${url} (${response.status})`);
  }
  return (await response.json()) as T;
}
