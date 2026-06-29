import { useEffect, useMemo, useRef } from "react";
import type { WorldState, WorldTemplate, WorldTemplateDenizenSocket } from "../api/types";
import type { AreaView } from "../state/EraDataContext";
import { WorldRenderer, type WorldScene } from "../world/worldRenderer";

interface WorldCanvasProps {
  template: WorldTemplate;
  world: WorldState;
  areas: AreaView[];
}

/**
 * Mounts the PixiJS isometric world for the active era. The renderer is imperative
 * and theme-agnostic; this component only owns its lifecycle and feeds it the scene
 * derived from the template (terrain + district footprints) and the era's world
 * state (built plots with persisted variants).
 */
export function WorldCanvas({ template, world, areas }: WorldCanvasProps) {
  const hostRef = useRef<HTMLDivElement>(null);
  const rendererRef = useRef<WorldRenderer | null>(null);

  const scene = useMemo(() => buildScene(template, world, areas), [template, world, areas]);

  // The renderer is rebuilt only when the template changes (assets/terrain differ).
  useEffect(() => {
    const host = hostRef.current;
    if (!host) {
      return;
    }
    const renderer = new WorldRenderer(host);
    rendererRef.current = renderer;
    void renderer.init({ render: template.render, categories: template.categories });

    return () => {
      renderer.destroy();
      rendererRef.current = null;
    };
  }, [template]);

  // Push scene updates (e.g. a new built plot after a level-up) without remounting.
  useEffect(() => {
    rendererRef.current?.setScene(scene);
  }, [scene]);

  return <div ref={hostRef} style={{ position: "absolute", inset: 0 }} aria-hidden />;
}

function buildScene(template: WorldTemplate, world: WorldState, areas: AreaView[]): WorldScene {
  const colorByDistrict = new Map<string, string>();
  for (const area of areas) {
    if (area.districtId) {
      colorByDistrict.set(area.districtId, area.theme.hex);
    }
  }

  const districts: WorldScene["districts"] = template.districts.map((district) => ({
    color: colorByDistrict.get(district.districtId) ?? "#7c6e56",
    tiles: district.plots.map((plot) => ({ x: plot.positionX, y: plot.positionY })),
  }));

  const builtPlots: WorldScene["builtPlots"] = world.districts.flatMap((district) =>
    district.builtPlots.map((plot) => ({
      positionX: plot.positionX,
      positionY: plot.positionY,
      category: plot.category,
      spriteKey: plot.spriteKey,
    })),
  );

  const denizens = buildDenizenPlacements(template, world);

  return { districts, builtPlots, denizens };
}

function buildDenizenPlacements(template: WorldTemplate, world: WorldState): WorldScene["denizens"] {
  const templateDistricts = new Map(template.districts.map((district) => [district.districtId, district]));
  const denizenSpriteKeys = buildDenizenSpriteKeyMap(template);

  return world.districts.flatMap((district) => {
    const templateDistrict = templateDistricts.get(district.districtId);
    if (!templateDistrict) {
      return [];
    }

    const availableSockets = shuffle(templateDistrict.denizenSockets);
    const placements: WorldScene["denizens"] = [];

    for (const denizen of district.denizens) {
      const spriteKeys = denizenSpriteKeys.get(denizen.denizenType) ?? [];
      if (spriteKeys.length === 0 || denizen.count <= 0) {
        continue;
      }

      let placed = 0;
      for (let index = 0; index < availableSockets.length && placed < denizen.count; index++) {
        const socket = availableSockets[index];
        if (!socket || !isCompatibleSocket(socket, denizen.denizenType)) {
          continue;
        }

        placements.push({
          positionX: socket.positionX,
          positionY: socket.positionY,
          anchorX: socket.anchorX,
          anchorY: socket.anchorY,
          sortOffsetY: socket.sortOffsetY,
          spriteKey: pickRandom(spriteKeys),
        });
        availableSockets.splice(index, 1);
        index--;
        placed++;
      }
    }

    return placements;
  });
}

function buildDenizenSpriteKeyMap(template: WorldTemplate): Map<string, string[]> {
  const result = new Map<string, string[]>();
  for (const variant of template.variants) {
    const denizenType = variant.category.startsWith("denizen:")
      ? variant.category.slice("denizen:".length)
      : null;
    if (!denizenType) {
      continue;
    }

    const spriteKeys = result.get(denizenType) ?? [];
    spriteKeys.push(variant.spriteKey);
    result.set(denizenType, spriteKeys);
  }
  return result;
}

function isCompatibleSocket(socket: WorldTemplateDenizenSocket, denizenType: string): boolean {
  return socket.compatibleDenizenTypes.includes(denizenType);
}

function shuffle<T>(items: readonly T[]): T[] {
  const result = [...items];
  for (let index = result.length - 1; index > 0; index--) {
    const swapIndex = Math.floor(Math.random() * (index + 1));
    [result[index], result[swapIndex]] = [result[swapIndex], result[index]];
  }
  return result;
}

function pickRandom<T>(items: readonly T[]): T {
  return items[Math.floor(Math.random() * items.length)];
}
