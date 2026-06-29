import { ArrowLeft, ArrowRight, Check, Plus, Sparkles, Trash2 } from "lucide-react";
import { useMemo, useState } from "react";
import { createEra } from "../../api/endpoints";
import type {
  CreateDailyHabitDraft,
  CreateEraRequest,
  CreateWeeklyGoalDraft,
  WorldTemplate,
} from "../../api/types";
import { getAreaTheme } from "../../lib/areaTheme";
import { useEraData } from "../../state/EraDataContext";
import {
  AdminCard,
  Button,
  DateField,
  Field,
  IconButton,
  NumberField,
  SelectField,
  TextField,
} from "./fields";

const STEPS = ["Basics", "World", "Areas", "Daily habits", "Weekly goals", "Review"] as const;

interface AreaDraft {
  id: string;
  name: string;
}

interface QuestDraft {
  id: string;
  areaIndex: number; // 0-based index into areaDrafts
  label: string;
  xp: number | "";
}

let draftSeq = 0;
const nextId = () => `draft-${draftSeq++}`;
const today = () => new Date().toISOString().slice(0, 10);

export function EraWizard() {
  const { templates, refresh } = useEraData();

  const [step, setStep] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [name, setName] = useState("");
  const [startDate, setStartDate] = useState(today());
  const [weeks, setWeeks] = useState<number | "">(50);
  const [xpPerLevel, setXpPerLevel] = useState<number | "">(100);
  const [templateId, setTemplateId] = useState("");
  const [areaDrafts, setAreaDrafts] = useState<AreaDraft[]>([
    { id: nextId(), name: "" },
    { id: nextId(), name: "" },
  ]);
  const [dailyDrafts, setDailyDrafts] = useState<QuestDraft[]>([]);
  const [weeklyDrafts, setWeeklyDrafts] = useState<QuestDraft[]>([]);

  const template = templates.find((candidate) => candidate.id === templateId) ?? null;
  const maxAreas = template?.districts.length ?? 4;

  // ---- per-step validation -------------------------------------------------
  const namedAreas = areaDrafts.filter((area) => area.name.trim() !== "");
  const basicsValid = name.trim() !== "" && weeks !== "" && Number(weeks) > 0 && xpPerLevel !== "" && Number(xpPerLevel) > 0 && startDate !== "";
  const worldValid = templateId !== "";
  const areasValid =
    namedAreas.length >= 1 &&
    namedAreas.length === areaDrafts.length &&
    areaDrafts.length <= maxAreas;

  const stepValid = [basicsValid, worldValid, areasValid, true, true, true][step];

  const areaOptions = areaDrafts.map((area, index) => ({
    value: String(index),
    label: area.name.trim() === "" ? `Area ${index + 1}` : area.name.trim(),
  }));

  const create = async () => {
    if (!basicsValid || !worldValid || !areasValid) {
      setError("Some required fields are missing — revisit the earlier steps.");
      return;
    }
    const toDraft = (draft: QuestDraft) => ({
      areaOrder: draft.areaIndex + 1,
      label: draft.label.trim(),
      xp: Number(draft.xp),
    });
    const completeDaily: CreateDailyHabitDraft[] = dailyDrafts
      .filter((draft) => draft.label.trim() !== "" && draft.xp !== "" && Number(draft.xp) > 0)
      .map(toDraft);
    const completeWeekly: CreateWeeklyGoalDraft[] = weeklyDrafts
      .filter((draft) => draft.label.trim() !== "" && draft.xp !== "" && Number(draft.xp) > 0)
      .map(toDraft);

    const request: CreateEraRequest = {
      name: name.trim(),
      startDate,
      weeks: Number(weeks),
      templateId,
      xpPerLevel: Number(xpPerLevel),
      areas: areaDrafts.map((area, index) => ({ name: area.name.trim(), order: index + 1 })),
      dailyHabits: completeDaily.length > 0 ? completeDaily : undefined,
      weeklyGoals: completeWeekly.length > 0 ? completeWeekly : undefined,
    };

    setSubmitting(true);
    setError(null);
    try {
      await createEra(request);
      await refresh(); // active era now exists → AdminScreen swaps to the studio.
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not create the era.");
      setSubmitting(false);
    }
  };

  return (
    <div style={{ maxWidth: 880, margin: "0 auto", padding: "22px 30px 64px" }}>
      <div style={{ marginBottom: 18 }}>
        <span className="armali-eyebrow" style={{ fontSize: 10 }}>
          New era
        </span>
        <h1 style={{ fontSize: 28, marginTop: 4 }}>Found a new era</h1>
        <p style={{ marginTop: 6, fontSize: 14, color: "var(--text-secondary)" }}>
          Name it, choose a world and your areas of focus, then optionally seed its habits and goals. Belfalas grows one
          era at a time.
        </p>
      </div>

      <Stepper step={step} />

      <AdminCard style={{ marginTop: 18 }}>
        {step === 0 && (
          <Steps.Basics
            name={name}
            setName={setName}
            startDate={startDate}
            setStartDate={setStartDate}
            weeks={weeks}
            setWeeks={setWeeks}
            xpPerLevel={xpPerLevel}
            setXpPerLevel={setXpPerLevel}
          />
        )}
        {step === 1 && (
          <Steps.World templates={templates} templateId={templateId} setTemplateId={setTemplateId} />
        )}
        {step === 2 && (
          <Steps.Areas
            areaDrafts={areaDrafts}
            setAreaDrafts={setAreaDrafts}
            maxAreas={maxAreas}
            templateName={template?.name}
          />
        )}
        {step === 3 && (
          <Steps.Quests
            kind="daily"
            drafts={dailyDrafts}
            setDrafts={setDailyDrafts}
            areaOptions={areaOptions}
          />
        )}
        {step === 4 && (
          <Steps.Quests
            kind="weekly"
            drafts={weeklyDrafts}
            setDrafts={setWeeklyDrafts}
            areaOptions={areaOptions}
          />
        )}
        {step === 5 && (
          <Steps.Review
            name={name}
            startDate={startDate}
            weeks={Number(weeks) || 0}
            xpPerLevel={Number(xpPerLevel) || 0}
            templateName={template?.name ?? templateId}
            areaNames={areaDrafts.map((area) => area.name.trim()).filter(Boolean)}
            dailyCount={dailyDrafts.filter((d) => d.label.trim() && d.xp !== "" && Number(d.xp) > 0).length}
            weeklyCount={weeklyDrafts.filter((d) => d.label.trim() && d.xp !== "" && Number(d.xp) > 0).length}
          />
        )}

        {error ? <p style={{ marginTop: 14, fontSize: 12.5, color: "var(--danger-hover)" }}>{error}</p> : null}

        {/* Nav */}
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginTop: 22 }}>
          <Button variant="ghost" onClick={() => setStep((s) => Math.max(0, s - 1))} disabled={step === 0 || submitting}>
            <ArrowLeft size={15} aria-hidden /> Back
          </Button>

          {step < STEPS.length - 1 ? (
            <Button onClick={() => setStep((s) => Math.min(STEPS.length - 1, s + 1))} disabled={!stepValid}>
              Next <ArrowRight size={15} aria-hidden />
            </Button>
          ) : (
            <Button onClick={() => void create()} disabled={submitting}>
              <Sparkles size={15} aria-hidden /> {submitting ? "Creating…" : "Create era"}
            </Button>
          )}
        </div>
      </AdminCard>
    </div>
  );
}

function Stepper({ step }: { step: number }) {
  return (
    <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
      {STEPS.map((label, index) => {
        const done = index < step;
        const active = index === step;
        return (
          <span
            key={label}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 7,
              padding: "6px 12px",
              borderRadius: 999,
              fontFamily: "var(--font-display)",
              fontWeight: 600,
              fontSize: 12,
              background: active ? "var(--aqua-100)" : done ? "var(--sea-100)" : "var(--bone-200)",
              color: active ? "var(--aqua-700)" : done ? "var(--sea-600)" : "var(--text-muted)",
            }}
          >
            <span
              style={{
                display: "inline-flex",
                alignItems: "center",
                justifyContent: "center",
                width: 18,
                height: 18,
                borderRadius: "50%",
                background: active ? "var(--aqua-500)" : done ? "var(--sea-500)" : "var(--sand-400)",
                color: "#fff",
                fontSize: 11,
              }}
            >
              {done ? <Check size={12} aria-hidden /> : index + 1}
            </span>
            {label}
          </span>
        );
      })}
    </div>
  );
}

const Steps = {
  Basics({
    name,
    setName,
    startDate,
    setStartDate,
    weeks,
    setWeeks,
    xpPerLevel,
    setXpPerLevel,
  }: {
    name: string;
    setName: (v: string) => void;
    startDate: string;
    setStartDate: (v: string) => void;
    weeks: number | "";
    setWeeks: (v: number | "") => void;
    xpPerLevel: number | "";
    setXpPerLevel: (v: number | "") => void;
  }) {
    return (
      <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
        <Field label="Era name" hint="Something to remember this chapter by.">
          <TextField value={name} onChange={setName} placeholder="e.g. The Tideturn Year" maxLength={80} />
        </Field>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 14 }}>
          <Field label="Start date">
            <DateField value={startDate} onChange={setStartDate} />
          </Field>
          <Field label="Weeks" hint="~50 for a working year.">
            <NumberField value={weeks} onChange={setWeeks} min={1} />
          </Field>
          <Field label="XP per level" hint="Flat cost of one level.">
            <NumberField value={xpPerLevel} onChange={setXpPerLevel} min={1} />
          </Field>
        </div>
      </div>
    );
  },

  World({
    templates,
    templateId,
    setTemplateId,
  }: {
    templates: WorldTemplate[];
    templateId: string;
    setTemplateId: (id: string) => void;
  }) {
    if (templates.length === 0) {
      return <p style={{ fontSize: 13, color: "var(--text-muted)" }}>No world templates are available.</p>;
    }
    return (
      <div>
        <span className="armali-eyebrow" style={{ fontSize: 9.5 }}>
          World template
        </span>
        <p style={{ marginTop: 4, marginBottom: 14, fontSize: 13, color: "var(--text-secondary)" }}>
          The template sets the map, districts and how each area's world evolves. Each district hosts one area.
        </p>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill,minmax(240px,1fr))", gap: 12 }}>
          {templates.map((tpl) => {
            const selected = tpl.id === templateId;
            return (
              <button
                key={tpl.id}
                type="button"
                onClick={() => setTemplateId(tpl.id)}
                style={{
                  textAlign: "left",
                  padding: "16px 18px",
                  borderRadius: 16,
                  cursor: "pointer",
                  border: `2px solid ${selected ? "var(--aqua-500)" : "var(--border-subtle)"}`,
                  background: selected ? "var(--aqua-50)" : "var(--bone-50)",
                  transition: "all 0.16s var(--ease-out)",
                }}
              >
                <div style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 16 }}>{tpl.name}</div>
                <div style={{ marginTop: 4, fontSize: 12.5, color: "var(--text-secondary)", textTransform: "capitalize" }}>
                  {tpl.theme}
                </div>
                <div style={{ marginTop: 10, fontSize: 12, color: "var(--text-muted)" }}>
                  {tpl.districts.length} district{tpl.districts.length === 1 ? "" : "s"} · up to {tpl.districts.length}{" "}
                  areas
                </div>
              </button>
            );
          })}
        </div>
      </div>
    );
  },

  Areas({
    areaDrafts,
    setAreaDrafts,
    maxAreas,
    templateName,
  }: {
    areaDrafts: AreaDraft[];
    setAreaDrafts: React.Dispatch<React.SetStateAction<AreaDraft[]>>;
    maxAreas: number;
    templateName?: string;
  }) {
    const update = (id: string, value: string) =>
      setAreaDrafts((current) => current.map((area) => (area.id === id ? { ...area, name: value } : area)));
    const removeArea = (id: string) => setAreaDrafts((current) => current.filter((area) => area.id !== id));
    const add = () => setAreaDrafts((current) => [...current, { id: nextId(), name: "" }]);

    return (
      <div>
        <span className="armali-eyebrow" style={{ fontSize: 9.5 }}>
          Areas of focus
        </span>
        <p style={{ marginTop: 4, marginBottom: 14, fontSize: 13, color: "var(--text-secondary)" }}>
          1–{maxAreas} areas{templateName ? ` (${templateName} has ${maxAreas} districts)` : ""}. Each gets its own
          district and levels independently. Areas can't be changed once the era is created.
        </p>
        <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
          {areaDrafts.map((area, index) => (
            <div key={area.id} style={{ display: "flex", alignItems: "center", gap: 10 }}>
              <span style={{ width: 8, height: 8, borderRadius: 3, background: getAreaTheme(index).hex, flex: "0 0 auto" }} />
              <div style={{ flex: 1 }}>
                <TextField
                  value={area.name}
                  onChange={(value) => update(area.id, value)}
                  placeholder={`Area ${index + 1} — e.g. Work, Social, Health`}
                  maxLength={60}
                />
              </div>
              <IconButton
                title="Remove area"
                onClick={() => removeArea(area.id)}
                danger
                disabled={areaDrafts.length <= 1}
              >
                <Trash2 size={15} aria-hidden />
              </IconButton>
            </div>
          ))}
        </div>
        {areaDrafts.length < maxAreas ? (
          <div style={{ marginTop: 14 }}>
            <Button variant="ghost" onClick={add}>
              <Plus size={15} aria-hidden /> Add area
            </Button>
          </div>
        ) : (
          <p style={{ marginTop: 12, fontSize: 12, color: "var(--text-muted)" }}>
            This template supports at most {maxAreas} areas.
          </p>
        )}
      </div>
    );
  },

  Quests({
    kind,
    drafts,
    setDrafts,
    areaOptions,
  }: {
    kind: "daily" | "weekly";
    drafts: QuestDraft[];
    setDrafts: React.Dispatch<React.SetStateAction<QuestDraft[]>>;
    areaOptions: { value: string; label: string }[];
  }) {
    const noun = kind === "daily" ? "daily habit" : "weekly goal";
    const defaultXp = kind === "daily" ? 10 : 40;
    const update = (id: string, patch: Partial<QuestDraft>) =>
      setDrafts((current) => current.map((draft) => (draft.id === id ? { ...draft, ...patch } : draft)));
    const removeDraft = (id: string) => setDrafts((current) => current.filter((draft) => draft.id !== id));
    const add = () =>
      setDrafts((current) => [...current, { id: nextId(), areaIndex: 0, label: "", xp: defaultXp }]);

    return (
      <div>
        <span className="armali-eyebrow" style={{ fontSize: 9.5 }}>
          {kind === "daily" ? "Daily habits" : "Weekly goal pool"} (optional)
        </span>
        <p style={{ marginTop: 4, marginBottom: 14, fontSize: 13, color: "var(--text-secondary)" }}>
          {kind === "daily"
            ? "Habit-style items that reset every day. You can also add these later in the studio."
            : "The pool the weekly set is drawn from. Optional now — add or refine them later in the studio."}
        </p>
        <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
          {drafts.length === 0 ? (
            <p style={{ fontSize: 13, color: "var(--text-muted)", padding: "2px 0" }}>No {noun}s yet.</p>
          ) : (
            drafts.map((draft) => (
              <div
                key={draft.id}
                style={{ display: "grid", gridTemplateColumns: "1fr 150px 90px auto", gap: 10, alignItems: "center" }}
              >
                <TextField
                  value={draft.label}
                  onChange={(value) => update(draft.id, { label: value })}
                  placeholder={`New ${noun}…`}
                />
                <SelectField
                  value={String(draft.areaIndex)}
                  onChange={(value) => update(draft.id, { areaIndex: Number(value) })}
                  options={areaOptions}
                />
                <NumberField value={draft.xp} onChange={(value) => update(draft.id, { xp: value })} min={1} />
                <IconButton title={`Remove ${noun}`} onClick={() => removeDraft(draft.id)} danger>
                  <Trash2 size={15} aria-hidden />
                </IconButton>
              </div>
            ))
          )}
        </div>
        <div style={{ marginTop: 14 }}>
          <Button variant="ghost" onClick={add} disabled={areaOptions.length === 0}>
            <Plus size={15} aria-hidden /> Add {noun}
          </Button>
        </div>
      </div>
    );
  },

  Review({
    name,
    startDate,
    weeks,
    xpPerLevel,
    templateName,
    areaNames,
    dailyCount,
    weeklyCount,
  }: {
    name: string;
    startDate: string;
    weeks: number;
    xpPerLevel: number;
    templateName: string;
    areaNames: string[];
    dailyCount: number;
    weeklyCount: number;
  }) {
    return (
      <div>
        <span className="armali-eyebrow" style={{ fontSize: 9.5 }}>
          Review & create
        </span>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 14, marginTop: 14 }}>
          <Summary label="Era">{name || "—"}</Summary>
          <Summary label="World">{templateName || "—"}</Summary>
          <Summary label="Starts">{startDate}</Summary>
          <Summary label="Duration">{weeks} weeks</Summary>
          <Summary label="XP per level">{xpPerLevel}</Summary>
          <Summary label="Areas">{areaNames.length > 0 ? areaNames.join(", ") : "—"}</Summary>
          <Summary label="Daily habits">{dailyCount}</Summary>
          <Summary label="Weekly goals">{weeklyCount}</Summary>
        </div>
        <p style={{ marginTop: 16, fontSize: 12.5, color: "var(--text-muted)" }}>
          Creating the era makes it the active one immediately. You can keep authoring habits and goals afterwards in
          the era studio.
        </p>
      </div>
    );
  },
};

function Summary({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div
      style={{
        padding: "12px 14px",
        borderRadius: 12,
        background: "var(--bone-50)",
        border: "1px solid var(--border-subtle)",
      }}
    >
      <div className="armali-eyebrow" style={{ fontSize: 9 }}>
        {label}
      </div>
      <div style={{ marginTop: 4, fontSize: 14, fontWeight: 600, color: "var(--text-primary)" }}>{children}</div>
    </div>
  );
}
