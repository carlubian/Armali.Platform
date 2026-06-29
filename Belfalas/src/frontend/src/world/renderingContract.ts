import type { WorldTemplateRenderContract } from "../api/types";

export interface IsoGridPoint {
  x: number;
  y: number;
}

export interface ScreenPoint {
  x: number;
  y: number;
}

export function isoToScreen(
  point: IsoGridPoint,
  contract: Pick<WorldTemplateRenderContract, "tileWidth" | "tileHeight" | "originX" | "originY">,
): ScreenPoint {
  return {
    x: contract.originX + (point.x - point.y) * (contract.tileWidth / 2),
    y: contract.originY + (point.x + point.y) * (contract.tileHeight / 2),
  };
}

export function screenToIso(
  point: ScreenPoint,
  contract: Pick<WorldTemplateRenderContract, "tileWidth" | "tileHeight" | "originX" | "originY">,
): IsoGridPoint {
  const localX = point.x - contract.originX;
  const localY = point.y - contract.originY;

  return {
    x: localY / contract.tileHeight + localX / contract.tileWidth,
    y: localY / contract.tileHeight - localX / contract.tileWidth,
  };
}

export function clampCameraPosition(
  point: ScreenPoint,
  bounds: WorldTemplateRenderContract["cameraBounds"],
): ScreenPoint {
  return {
    x: Math.min(Math.max(point.x, bounds.minX), bounds.maxX),
    y: Math.min(Math.max(point.y, bounds.minY), bounds.maxY),
  };
}

export function worldSortKey(
  point: IsoGridPoint,
  contract: Pick<WorldTemplateRenderContract, "tileHeight">,
  sortOffsetY = 0,
): number {
  return (point.x + point.y) * contract.tileHeight + sortOffsetY;
}
