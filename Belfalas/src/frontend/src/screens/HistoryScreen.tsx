import { Archive, CalendarRange, History, Layers, Loader2, type LucideIcon } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { getArchivedEra, listArchivedEras } from "../api/endpoints";
import type { ArchivedEra, ArchivedEraSummary } from "../api/types";
import { EmptyState } from "../components/EmptyState";
import { WorldCanvas } from "../components/WorldCanvas";
import { Button } from "../components/admin/fields";
import { buildAreaViews, useEraData } from "../state/EraDataContext";

export function HistoryScreen() {
  const { templates } = useEraData();
  const [archives, setArchives] = useState<ArchivedEraSummary[]>([]);
  const [selectedEraId, setSelectedEraId] = useState<string | null>(null);
  const [archive, setArchive] = useState<ArchivedEra | null>(null);
  const [loadingList, setLoadingList] = useState(true);
  const [loadingArchive, setLoadingArchive] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function loadArchives() {
      setLoadingList(true);
      setError(null);
      try {
        const result = await listArchivedEras();
        if (cancelled) {
          return;
        }
        setArchives(result);
        setSelectedEraId((current) => current ?? result[0]?.eraId ?? null);
      } catch (cause) {
        if (!cancelled) {
          setError(cause instanceof Error ? cause.message : "Could not load history.");
        }
      } finally {
        if (!cancelled) {
          setLoadingList(false);
        }
      }
    }

    void loadArchives();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!selectedEraId) {
      setArchive(null);
      return;
    }

    const eraId = selectedEraId;
    let cancelled = false;
    async function loadArchive() {
      setLoadingArchive(true);
      setError(null);
      try {
        const result = await getArchivedEra(eraId);
        if (!cancelled) {
          setArchive(result);
        }
      } catch (cause) {
        if (!cancelled) {
          setError(cause instanceof Error ? cause.message : "Could not open the archived era.");
        }
      } finally {
        if (!cancelled) {
          setLoadingArchive(false);
        }
      }
    }

    void loadArchive();
    return () => {
      cancelled = true;
    };
  }, [selectedEraId]);

  const template = useMemo(
    () => templates.find((candidate) => candidate.id === archive?.world.templateId) ?? null,
    [templates, archive],
  );
  const areas = useMemo(
    () => buildAreaViews(archive?.progression ?? null, archive?.world ?? null, templates),
    [archive, templates],
  );

  if (loadingList) {
    return (
      <div style={{ position: "absolute", inset: 0, background: "linear-gradient(180deg,var(--bone-100),var(--bone-200))" }}>
        <EmptyState icon={Loader2} title="Loading history">
          Belfalas is gathering archived eras and their world snapshots.
        </EmptyState>
      </div>
    );
  }

  if (archives.length === 0) {
    return (
      <div style={{ position: "absolute", inset: 0, background: "linear-gradient(180deg,var(--bone-100),var(--bone-200))" }}>
        <EmptyState icon={History} title="No archived eras yet">
          Once you archive an era, its world and progress become a read-only chapter you can browse here.
        </EmptyState>
      </div>
    );
  }

  return (
    <div style={{ position: "absolute", inset: 0, display: "flex", background: "var(--bone-100)" }}>
      <aside
        style={{
          width: 320,
          flex: "0 0 320px",
          padding: "22px 18px",
          borderRight: "1px solid var(--border-subtle)",
          background: "var(--surface-card-solid)",
          overflowY: "auto",
        }}
      >
        <span className="armali-eyebrow" style={{ fontSize: 10 }}>
          History
        </span>
        <h1 style={{ marginTop: 4, fontSize: 26 }}>Archived eras</h1>
        <p style={{ marginTop: 8, fontSize: 13, lineHeight: 1.45, color: "var(--text-secondary)" }}>
          Read-only snapshots of past progress and worlds.
        </p>

        {error ? <p style={{ marginTop: 14, fontSize: 12.5, color: "var(--danger-hover)" }}>{error}</p> : null}

        <div style={{ display: "flex", flexDirection: "column", gap: 10, marginTop: 18 }}>
          {archives.map((item) => {
            const selected = item.eraId === selectedEraId;
            return (
              <button
                key={item.eraId}
                type="button"
                className="belf-district-row"
                data-active={selected}
                onClick={() => setSelectedEraId(item.eraId)}
                style={{
                  alignItems: "flex-start",
                  padding: "13px 12px",
                  borderColor: selected ? "var(--aqua-300)" : "var(--border-subtle)",
                  background: selected ? "var(--aqua-50)" : "var(--bone-50)",
                }}
              >
                <Archive
                  size={16}
                  style={{ marginTop: 2, color: selected ? "var(--aqua-700)" : "var(--text-muted)" }}
                  aria-hidden
                />
                <span style={{ minWidth: 0 }}>
                  <strong style={{ display: "block", fontSize: 14, color: "var(--text-primary)" }}>{item.name}</strong>
                  <span style={{ display: "block", marginTop: 4, fontSize: 12, color: "var(--text-secondary)" }}>
                {formatDate(item.startDate)} - {item.weeks} weeks
                  </span>
                  <span style={{ display: "block", marginTop: 3, fontSize: 11.5, color: "var(--text-muted)" }}>
                    Archived {formatDateTime(item.archivedAt)}
                  </span>
                </span>
              </button>
            );
          })}
        </div>
      </aside>

      <section className="armali-aurora" style={{ flex: 1, minWidth: 0, position: "relative", overflow: "hidden" }}>
        {archive && template && !loadingArchive ? (
          <WorldCanvas template={template} world={archive.world} areas={areas} />
        ) : null}

        {loadingArchive ? (
          <div style={{ position: "absolute", inset: 0, display: "grid", placeItems: "center", color: "var(--text-secondary)" }}>
            <Loader2 size={26} aria-hidden />
          </div>
        ) : null}

        {archive ? <HistoryOverlay archive={archive} areas={areas} /> : null}
      </section>
    </div>
  );
}

function HistoryOverlay({ archive, areas }: { archive: ArchivedEra; areas: ReturnType<typeof buildAreaViews> }) {
  const globalLevel = archive.progression.globalLevel.toFixed(1);
  return (
    <>
      <div
        style={{
          position: "absolute",
          left: 24,
          top: 22,
          width: 310,
          padding: "18px 18px",
          borderRadius: "var(--radius-card)",
          background: "var(--surface-overlay)",
          backdropFilter: "var(--blur-chip)",
          WebkitBackdropFilter: "var(--blur-chip)",
          border: "1px solid var(--border-glass)",
          boxShadow: "var(--glow-card)",
        }}
      >
        <span className="armali-eyebrow" style={{ fontSize: 10 }}>
          Read-only snapshot
        </span>
        <h2 style={{ marginTop: 4, fontSize: 24 }}>{archive.era.name}</h2>
        <div style={{ display: "flex", flexDirection: "column", gap: 7, marginTop: 12, fontSize: 12.5, color: "var(--text-secondary)" }}>
          <Meta icon={CalendarRange}>
            {formatDate(archive.era.startDate)} - {archive.era.weeks} weeks
          </Meta>
          <Meta icon={Layers}>Global level {globalLevel} / {archive.progression.maxLevel}</Meta>
          <Meta icon={Archive}>Archived {formatDateTime(archive.archivedAt)}</Meta>
        </div>
      </div>

      <div
        style={{
          position: "absolute",
          right: 24,
          top: 22,
          width: 300,
          maxHeight: "calc(100% - 44px)",
          overflowY: "auto",
          padding: "16px",
          borderRadius: "var(--radius-card)",
          background: "var(--surface-overlay)",
          backdropFilter: "var(--blur-chip)",
          WebkitBackdropFilter: "var(--blur-chip)",
          border: "1px solid var(--border-glass)",
          boxShadow: "var(--glow-card)",
        }}
      >
        <span className="armali-eyebrow" style={{ fontSize: 10 }}>
          Areas
        </span>
        <div style={{ display: "flex", flexDirection: "column", gap: 10, marginTop: 12 }}>
          {areas.map((area) => (
            <div key={area.areaId} style={{ padding: "11px 12px", borderRadius: 8, background: "var(--bone-50)", border: "1px solid var(--border-subtle)" }}>
              <div style={{ display: "flex", justifyContent: "space-between", gap: 12, fontSize: 13.5, fontWeight: 700 }}>
                <span>{area.areaName}</span>
                <span>Lv {area.level}</span>
              </div>
              <div style={{ marginTop: 7, height: 7, borderRadius: 999, background: "var(--bone-200)", overflow: "hidden" }}>
                <div
                  style={{
                    width: `${Math.min(100, Math.max(0, (area.xpIntoLevel / area.xpPerLevel) * 100))}%`,
                    height: "100%",
                    background: area.theme.hex,
                  }}
                />
              </div>
              <p style={{ marginTop: 6, fontSize: 11.5, color: "var(--text-muted)" }}>
                {area.builtPlots.length} built plots / {area.denizens.reduce((total, item) => total + item.count, 0)} denizens
              </p>
            </div>
          ))}
        </div>
      </div>

      <div style={{ position: "absolute", left: 24, bottom: 22 }}>
        <Button variant="ghost" disabled>
          Archived world
        </Button>
      </div>
    </>
  );
}

function Meta({ icon: Icon, children }: { icon: LucideIcon; children: React.ReactNode }) {
  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: 7 }}>
      <Icon size={14} aria-hidden /> {children}
    </span>
  );
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(new Date(`${value}T00:00:00`));
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}
