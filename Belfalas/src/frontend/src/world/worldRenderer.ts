import { Application, Container, Graphics, Sprite } from "pixi.js";
import type {
  WorldTemplateCategoryContract,
  WorldTemplateRenderContract,
} from "../api/types";
import { loadWorldAssets, type BaseMap, type WorldAssets } from "./worldAssets";
import { WorldCamera } from "./worldCamera";
import { isoToScreen, worldSortKey, type IsoGridPoint } from "./renderingContract";

/** A built plot to draw: an authored grid position with a persisted sprite variant. */
export interface SceneBuiltPlot {
  positionX: number;
  positionY: number;
  category: string;
  spriteKey: string;
}

/** A district footprint used to tint the ground so areas read at a glance. */
export interface SceneDistrict {
  /** CSS hex colour for the district's owning area. */
  color: string;
  tiles: IsoGridPoint[];
}

export interface WorldScene {
  districts: SceneDistrict[];
  builtPlots: SceneBuiltPlot[];
}

interface RendererInput {
  render: WorldTemplateRenderContract;
  categories: WorldTemplateCategoryContract[];
}

const DISTRICT_TINT_ALPHA = 0.22;

/**
 * Owns the PixiJS application and isometric scene graph. The renderer is
 * theme-agnostic: it draws whatever the render contract, base map and scene data
 * describe, with no assumptions about the tropical (or any) template.
 *
 * Layering, back to front: terrain → district tints → built plots (depth-sorted).
 */
export class WorldRenderer {
  private readonly host: HTMLElement;
  private app: Application | null = null;
  private destroyed = false;

  private assets: WorldAssets | null = null;
  private render: WorldTemplateRenderContract | null = null;
  private categories = new Map<string, WorldTemplateCategoryContract>();

  private readonly worldLayer = new Container();
  private readonly terrainLayer = new Container();
  private readonly districtLayer = new Container();
  private readonly objectLayer = new Container();

  private camera: WorldCamera | null = null;
  private resizeObserver: ResizeObserver | null = null;

  /** Latest scene, kept so it can be (re)drawn once assets finish loading. */
  private pendingScene: WorldScene | null = null;

  // Drag state for panning.
  private dragging = false;
  private lastPointerX = 0;
  private lastPointerY = 0;

  constructor(host: HTMLElement) {
    this.host = host;
  }

  /** Boots Pixi, loads assets and draws the static terrain. Safe to abort on unmount. */
  async init(input: RendererInput): Promise<void> {
    this.render = input.render;
    this.categories = new Map(input.categories.map((category) => [category.category, category]));

    const app = new Application();
    await app.init({
      resizeTo: this.host,
      backgroundAlpha: 0,
      antialias: true,
      autoDensity: true,
      resolution: window.devicePixelRatio || 1,
    });

    // The component may have unmounted while Pixi was initialising.
    if (this.destroyed) {
      app.destroy(true);
      return;
    }

    this.app = app;
    this.host.appendChild(app.canvas);
    app.canvas.style.touchAction = "none";

    this.objectLayer.sortableChildren = true;
    this.worldLayer.addChild(this.terrainLayer, this.districtLayer, this.objectLayer);
    app.stage.addChild(this.worldLayer);

    this.assets = await loadWorldAssets(input.render);
    if (this.destroyed) {
      return;
    }

    this.drawTerrain(this.assets.map);

    this.camera = new WorldCamera(this.worldLayer, input.render.cameraBounds);
    this.camera.setViewport(app.screen.width, app.screen.height, true);

    this.attachInput();
    this.observeResize();

    // A scene may have been pushed before assets finished loading.
    this.renderScene();
  }

  /**
   * Sets the scene to draw (district tints + built plots). Kept even if assets are
   * still loading, so it can be applied once `init` finishes.
   */
  setScene(scene: WorldScene): void {
    this.pendingScene = scene;
    this.renderScene();
  }

  /** Redraws the pending scene; terrain is static and left untouched. */
  private renderScene(): void {
    if (!this.assets || !this.render || !this.pendingScene) {
      return;
    }

    this.districtLayer.removeChildren().forEach((child) => child.destroy());
    this.objectLayer.removeChildren().forEach((child) => child.destroy());

    for (const district of this.pendingScene.districts) {
      this.drawDistrictTint(district);
    }
    for (const plot of this.pendingScene.builtPlots) {
      this.drawBuiltPlot(plot);
    }
  }

  destroy(): void {
    this.destroyed = true;
    this.resizeObserver?.disconnect();
    this.resizeObserver = null;
    this.detachInput();
    this.app?.destroy(true, { children: true });
    this.app = null;
  }

  // ---- Drawing -----------------------------------------------------------

  private drawTerrain(map: BaseMap): void {
    if (!this.assets) {
      return;
    }
    for (let y = 0; y < map.rows.length; y++) {
      const row = map.rows[y];
      for (let x = 0; x < row.length; x++) {
        const spriteKey = map.terrainLegend[row[x]];
        const texture = spriteKey ? this.assets.sheet.textures[spriteKey] : undefined;
        if (!texture) {
          continue;
        }
        const sprite = new Sprite(texture);
        sprite.anchor.set(0.5, 0); // diamond top vertex sits on the grid point
        const screen = this.toScreen({ x, y });
        sprite.position.set(screen.x, screen.y);
        this.terrainLayer.addChild(sprite);
      }
    }
  }

  private drawDistrictTint(district: SceneDistrict): void {
    if (!this.render) {
      return;
    }
    const { tileWidth, tileHeight } = this.render;
    const color = district.color;
    const graphics = new Graphics();
    for (const tile of district.tiles) {
      const screen = this.toScreen(tile);
      const cx = screen.x;
      const cy = screen.y + tileHeight / 2; // diamond centre
      graphics
        .poly([
          cx, cy - tileHeight / 2,
          cx + tileWidth / 2, cy,
          cx, cy + tileHeight / 2,
          cx - tileWidth / 2, cy,
        ])
        .fill({ color, alpha: DISTRICT_TINT_ALPHA });
    }
    this.districtLayer.addChild(graphics);
  }

  private drawBuiltPlot(plot: SceneBuiltPlot): void {
    if (!this.assets || !this.render) {
      return;
    }
    const texture = this.assets.sheet.textures[plot.spriteKey];
    if (!texture) {
      return;
    }
    const category = this.categories.get(plot.category);
    const anchorX = category?.anchorX ?? 0.5;
    const anchorY = category?.anchorY ?? 0.9;
    const sortOffsetY = category?.sortOffsetY ?? 0;

    const sprite = new Sprite(texture);
    sprite.anchor.set(anchorX, anchorY);

    const point = { x: plot.positionX, y: plot.positionY };
    const screen = this.toScreen(point);
    sprite.position.set(screen.x, screen.y + this.render.tileHeight / 2);
    sprite.zIndex = worldSortKey(point, this.render, sortOffsetY);

    this.objectLayer.addChild(sprite);
  }

  private toScreen(point: IsoGridPoint) {
    return isoToScreen(point, this.render!);
  }

  // ---- Input & sizing ----------------------------------------------------

  private attachInput(): void {
    const canvas = this.app?.canvas;
    if (!canvas) {
      return;
    }
    canvas.style.cursor = "grab";
    canvas.addEventListener("pointerdown", this.onPointerDown);
    window.addEventListener("pointermove", this.onPointerMove);
    window.addEventListener("pointerup", this.onPointerUp);
    canvas.addEventListener("wheel", this.onWheel, { passive: false });
  }

  private detachInput(): void {
    const canvas = this.app?.canvas;
    canvas?.removeEventListener("pointerdown", this.onPointerDown);
    window.removeEventListener("pointermove", this.onPointerMove);
    window.removeEventListener("pointerup", this.onPointerUp);
    canvas?.removeEventListener("wheel", this.onWheel);
  }

  private observeResize(): void {
    this.resizeObserver = new ResizeObserver(() => {
      if (this.app && this.camera) {
        this.camera.setViewport(this.app.screen.width, this.app.screen.height);
      }
    });
    this.resizeObserver.observe(this.host);
  }

  private readonly onPointerDown = (event: PointerEvent) => {
    this.dragging = true;
    this.lastPointerX = event.clientX;
    this.lastPointerY = event.clientY;
    if (this.app) {
      this.app.canvas.style.cursor = "grabbing";
    }
  };

  private readonly onPointerMove = (event: PointerEvent) => {
    if (!this.dragging || !this.camera) {
      return;
    }
    this.camera.panBy(event.clientX - this.lastPointerX, event.clientY - this.lastPointerY);
    this.lastPointerX = event.clientX;
    this.lastPointerY = event.clientY;
  };

  private readonly onPointerUp = () => {
    this.dragging = false;
    if (this.app) {
      this.app.canvas.style.cursor = "grab";
    }
  };

  private readonly onWheel = (event: WheelEvent) => {
    if (!this.camera || !this.app) {
      return;
    }
    event.preventDefault();
    const rect = this.app.canvas.getBoundingClientRect();
    const factor = Math.exp(-event.deltaY * 0.0015);
    this.camera.zoomAt(event.clientX - rect.left, event.clientY - rect.top, factor);
  };
}
