import { Check, Pencil, Plus, Trash2, X } from "lucide-react";
import { useMemo, useState } from "react";
import {
  createDailyHabit,
  createWeeklyGoal,
  deleteDailyHabit,
  deleteWeeklyGoal,
  updateDailyHabit,
  updateWeeklyGoal,
} from "../../api/endpoints";
import type { AreaResponse, UpsertDailyHabit } from "../../api/types";
import { getAreaTheme } from "../../lib/areaTheme";
import { AdminCard, Button, CardHeader, IconButton, NumberField, SelectField, TextField } from "./fields";

/** Daily habits and weekly goals share the same authoring shape and endpoints. */
type Kind = "daily" | "weekly";

interface AuthoredItem {
  id: string;
  areaId: string;
  label: string;
  xp: number;
}

const COPY: Record<Kind, { title: string; subtitle: string; noun: string }> = {
  daily: {
    title: "Daily habits",
    subtitle: "A fixed list for the era — each resets every day. XP credits its area on completion.",
    noun: "habit",
  },
  weekly: {
    title: "Weekly goal pool",
    subtitle: "Larger goals; the week's set is drawn from this pool (one per area) and can be overridden below.",
    noun: "goal",
  },
};

const create = (kind: Kind, eraId: string, body: UpsertDailyHabit) =>
  kind === "daily" ? createDailyHabit(eraId, body) : createWeeklyGoal(eraId, body);
const update = (kind: Kind, id: string, body: UpsertDailyHabit) =>
  kind === "daily" ? updateDailyHabit(id, body) : updateWeeklyGoal(id, body);
const remove = (kind: Kind, id: string) => (kind === "daily" ? deleteDailyHabit(id) : deleteWeeklyGoal(id));

export function QuestPoolCard({
  kind,
  eraId,
  areas,
  items,
  onChanged,
}: {
  kind: Kind;
  eraId: string;
  areas: AreaResponse[];
  items: AuthoredItem[];
  onChanged: () => Promise<void>;
}) {
  const copy = COPY[kind];
  const themeByArea = useThemeByArea(areas);
  const areaOptions = areas.map((area) => ({ value: area.id, label: area.name }));
  const defaultAreaId = areas[0]?.id ?? "";

  const [editingId, setEditingId] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // New-item draft.
  const [draftAreaId, setDraftAreaId] = useState(defaultAreaId);
  const [draftLabel, setDraftLabel] = useState("");
  const [draftXp, setDraftXp] = useState<number | "">(kind === "daily" ? 10 : 40);

  const sorted = useMemo(() => {
    const orderByArea = new Map(areas.map((area, index) => [area.id, area.order * 1000 + index]));
    return [...items].sort((a, b) => {
      const byArea = (orderByArea.get(a.areaId) ?? 0) - (orderByArea.get(b.areaId) ?? 0);
      return byArea !== 0 ? byArea : a.label.localeCompare(b.label);
    });
  }, [items, areas]);

  const run = async (action: () => Promise<unknown>) => {
    setBusy(true);
    setError(null);
    try {
      await action();
      await onChanged();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : `Could not update the ${copy.noun}.`);
    } finally {
      setBusy(false);
    }
  };

  const addItem = async () => {
    if (!draftAreaId || draftLabel.trim() === "" || draftXp === "" || draftXp <= 0) {
      setError(`A ${copy.noun} needs an area, a label and positive XP.`);
      return;
    }
    await run(async () => {
      await create(kind, eraId, { areaId: draftAreaId, label: draftLabel.trim(), xp: Number(draftXp) });
      setDraftLabel("");
      setDraftXp(kind === "daily" ? 10 : 40);
    });
  };

  return (
    <AdminCard>
      <CardHeader title={copy.title} subtitle={copy.subtitle} />

      {areas.length === 0 ? (
        <p style={{ marginTop: 14, fontSize: 13, color: "var(--text-muted)" }}>This era has no areas to assign.</p>
      ) : (
        <>
          <div style={{ display: "flex", flexDirection: "column", gap: 8, marginTop: 16 }}>
            {sorted.length === 0 ? (
              <p style={{ fontSize: 13, color: "var(--text-muted)", padding: "4px 2px" }}>
                No {copy.noun}s yet — add the first below.
              </p>
            ) : (
              sorted.map((item) =>
                editingId === item.id ? (
                  <EditRow
                    key={item.id}
                    item={item}
                    areaOptions={areaOptions}
                    busy={busy}
                    onCancel={() => setEditingId(null)}
                    onSave={(body) =>
                      run(async () => {
                        await update(kind, item.id, body);
                        setEditingId(null);
                      })
                    }
                  />
                ) : (
                  <ViewRow
                    key={item.id}
                    item={item}
                    areaName={areas.find((area) => area.id === item.areaId)?.name ?? "—"}
                    accent={themeByArea.get(item.areaId) ?? "var(--text-secondary)"}
                    busy={busy}
                    onEdit={() => setEditingId(item.id)}
                    onDelete={() => run(() => remove(kind, item.id))}
                  />
                ),
              )
            )}
          </div>

          {/* Add row */}
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "1fr 150px 90px auto",
              gap: 10,
              alignItems: "end",
              marginTop: 16,
              paddingTop: 16,
              borderTop: "1px dashed var(--border-default)",
            }}
          >
            <FieldStack label="Label">
              <TextField value={draftLabel} onChange={setDraftLabel} placeholder={`New ${copy.noun}…`} disabled={busy} />
            </FieldStack>
            <FieldStack label="Area">
              <SelectField value={draftAreaId} onChange={setDraftAreaId} options={areaOptions} disabled={busy} />
            </FieldStack>
            <FieldStack label="XP">
              <NumberField value={draftXp} onChange={setDraftXp} min={1} disabled={busy} />
            </FieldStack>
            <Button onClick={() => void addItem()} disabled={busy}>
              <Plus size={15} aria-hidden /> Add
            </Button>
          </div>
        </>
      )}

      {error ? <p style={{ marginTop: 12, fontSize: 12.5, color: "var(--danger-hover)" }}>{error}</p> : null}
    </AdminCard>
  );
}

function ViewRow({
  item,
  areaName,
  accent,
  busy,
  onEdit,
  onDelete,
}: {
  item: AuthoredItem;
  areaName: string;
  accent: string;
  busy: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 12,
        padding: "10px 12px",
        borderRadius: 13,
        border: "1px solid var(--border-subtle)",
        background: "var(--bone-50)",
      }}
    >
      <span style={{ width: 8, height: 8, borderRadius: 3, background: accent, flex: "0 0 auto" }} />
      <span style={{ flex: 1, minWidth: 0, fontSize: 14, fontWeight: 600, color: "var(--text-primary)" }}>
        {item.label}
      </span>
      <span style={{ fontSize: 12, color: "var(--text-muted)" }}>{areaName}</span>
      <span style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 12.5, color: accent }}>
        +{item.xp}
      </span>
      <IconButton title="Edit" onClick={onEdit} disabled={busy}>
        <Pencil size={15} aria-hidden />
      </IconButton>
      <IconButton title="Delete" onClick={onDelete} danger disabled={busy}>
        <Trash2 size={15} aria-hidden />
      </IconButton>
    </div>
  );
}

function EditRow({
  item,
  areaOptions,
  busy,
  onCancel,
  onSave,
}: {
  item: AuthoredItem;
  areaOptions: { value: string; label: string }[];
  busy: boolean;
  onCancel: () => void;
  onSave: (body: UpsertDailyHabit) => void;
}) {
  const [label, setLabel] = useState(item.label);
  const [areaId, setAreaId] = useState(item.areaId);
  const [xp, setXp] = useState<number | "">(item.xp);

  const valid = label.trim() !== "" && areaId !== "" && xp !== "" && Number(xp) > 0;

  return (
    <div
      style={{
        display: "grid",
        gridTemplateColumns: "1fr 150px 90px auto auto",
        gap: 10,
        alignItems: "center",
        padding: "10px 12px",
        borderRadius: 13,
        border: "1px solid var(--ring-focus)",
        background: "var(--surface-card-solid)",
      }}
    >
      <TextField value={label} onChange={setLabel} disabled={busy} />
      <SelectField value={areaId} onChange={setAreaId} options={areaOptions} disabled={busy} />
      <NumberField value={xp} onChange={setXp} min={1} disabled={busy} />
      <IconButton
        title="Save"
        onClick={() => valid && onSave({ areaId, label: label.trim(), xp: Number(xp) })}
        disabled={busy || !valid}
      >
        <Check size={15} aria-hidden />
      </IconButton>
      <IconButton title="Cancel" onClick={onCancel} disabled={busy}>
        <X size={15} aria-hidden />
      </IconButton>
    </div>
  );
}

function FieldStack({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
      <span className="armali-eyebrow" style={{ fontSize: 9.5 }}>
        {label}
      </span>
      {children}
    </div>
  );
}

function useThemeByArea(areas: AreaResponse[]): Map<string, string> {
  return useMemo(() => {
    const sorted = [...areas].sort((a, b) => a.order - b.order);
    return new Map(sorted.map((area, index) => [area.id, getAreaTheme(index).hex]));
  }, [areas]);
}
