import type { Container } from "pixi.js";
import type { WorldTemplateRenderContract } from "../api/types";

type CameraBounds = WorldTemplateRenderContract["cameraBounds"];

/**
 * RTS-style camera over the world container: drag to pan, wheel to zoom, always
 * clamped so the view stays within the authored `cameraBounds`. The camera is the
 * only way the user moves through the world — Belfalas has no direct world editing.
 *
 * State is tracked as the world coordinate at the top-left of the viewport plus a
 * zoom factor; the container's position/scale are derived from it.
 */
export class WorldCamera {
  private readonly target: Container;
  private readonly bounds: CameraBounds;

  private viewportWidth = 1;
  private viewportHeight = 1;

  private x = 0;
  private y = 0;
  private zoom = 1;
  private minZoom = 0.1;
  private readonly maxZoom = 2;

  constructor(target: Container, bounds: CameraBounds) {
    this.target = target;
    this.bounds = bounds;
  }

  /** Recomputes zoom limits for a viewport size and centres the map on first sizing. */
  setViewport(width: number, height: number, recenter = false): void {
    const first = recenter || this.viewportWidth <= 1;
    this.viewportWidth = Math.max(1, width);
    this.viewportHeight = Math.max(1, height);

    // Smallest zoom that still keeps the view inside the map on at least one axis,
    // so the user can pull back to take in the whole world but no further.
    const fit = Math.min(
      this.viewportWidth / this.boundsWidth,
      this.viewportHeight / this.boundsHeight,
    );
    this.minZoom = Math.min(fit, this.maxZoom);

    if (first) {
      this.zoom = clamp(fit * 1.3, this.minZoom, this.maxZoom);
      this.centerOnMap();
    }
    this.apply();
  }

  /** Pans by a screen-space delta (e.g. a drag in CSS pixels). */
  panBy(screenDx: number, screenDy: number): void {
    this.x -= screenDx / this.zoom;
    this.y -= screenDy / this.zoom;
    this.apply();
  }

  /** Zooms towards a screen point (cursor), keeping the world under it fixed. */
  zoomAt(screenX: number, screenY: number, factor: number): void {
    const next = clamp(this.zoom * factor, this.minZoom, this.maxZoom);
    if (next === this.zoom) {
      return;
    }
    const worldX = this.x + screenX / this.zoom;
    const worldY = this.y + screenY / this.zoom;
    this.zoom = next;
    this.x = worldX - screenX / this.zoom;
    this.y = worldY - screenY / this.zoom;
    this.apply();
  }

  private centerOnMap(): void {
    this.x = (this.bounds.minX + this.bounds.maxX) / 2 - this.viewportWidth / this.zoom / 2;
    this.y = (this.bounds.minY + this.bounds.maxY) / 2 - this.viewportHeight / this.zoom / 2;
  }

  private apply(): void {
    this.clampPosition();
    this.target.scale.set(this.zoom);
    this.target.position.set(-this.x * this.zoom, -this.y * this.zoom);
  }

  private clampPosition(): void {
    this.x = clampAxis(this.x, this.viewportWidth / this.zoom, this.bounds.minX, this.bounds.maxX);
    this.y = clampAxis(this.y, this.viewportHeight / this.zoom, this.bounds.minY, this.bounds.maxY);
  }

  private get boundsWidth(): number {
    return Math.max(1, this.bounds.maxX - this.bounds.minX);
  }

  private get boundsHeight(): number {
    return Math.max(1, this.bounds.maxY - this.bounds.minY);
  }
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}

/**
 * Clamps the top-left world coordinate on one axis. When the visible span is larger
 * than the map, the map is centred instead of pinned to an edge.
 */
function clampAxis(value: number, visibleSpan: number, min: number, max: number): number {
  if (visibleSpan >= max - min) {
    return (min + max) / 2 - visibleSpan / 2;
  }
  return clamp(value, min, max - visibleSpan);
}
