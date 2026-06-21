/* global React */
// Firebird — the URL-aware popups: the person editor (create / view / edit /
// delete) and the two dedicated sub-popups it launches, for managing a
// person's usernames and their chronological interaction log. The sub-popups
// float over the dimmed editor (the entity-selector pattern). Exposed on window.
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Badge, Button, IconButton, Tooltip, Input, Select } = A;
const Icon = window.SegIcon;
const F = window.SegFire;
const { FB_STATUS, FB_STATUS_ORDER, FB_CATEGORIES, CATEGORY_TONE, FB_PLATFORMS, PLATFORM_ICON,
        MONTHS, MONTHS_SHORT, DAYS_IN_MONTH, fmtBirthdayLong, daysUntilBirthday, birthdaySoon } = F;

// ── Shared little bits ──────────────────────────────────────────
function StatusBadge({ status, dot = true }) {
  const s = FB_STATUS[status];
  return <Badge tone={s.tone} dot={dot}>{s.label}</Badge>;
}
function CatChip({ category }) {
  const tone = CATEGORY_TONE[category] || "neutral";
  return <span className={"fb-cat fb-tone--" + tone}><i className="fb-cat__dot" />{category}</span>;
}
function PersonAvatar({ person, size = "md", style }) {
  if (person.avatar) return <A.Avatar name={person.name} size={size} style={style} />;
  const px = size === "lg" ? 56 : size === "sm" ? 28 : 40;
  const s = (style && style["--_s"]) ? parseInt(style["--_s"], 10) : px;
  return (
    <span className="fb-ph" style={{ width: s, height: s }}>
      <Icon n="user-round" size={Math.round(s * 0.5)} />
    </span>
  );
}

// ── Person editor (create / edit) ───────────────────────────────
function PersonEditDialog({ mode, person, behind, onClose, onUsernames, onInteractions }) {
  const editing = mode === "edit";
  const p = editing ? person : null;
  const [status, setStatus] = React.useState(editing ? p.status : "Unknown");
  const [cat, setCat] = React.useState(editing ? p.category : FB_CATEGORIES[0].value);
  const [vis, setVis] = React.useState(editing ? p.visibility : "Public");
  const [hasB, setHasB] = React.useState(editing ? !!p.birthday : false);
  const [bm, setBm] = React.useState(editing && p.birthday ? p.birthday.m : 1);
  const [bd, setBd] = React.useState(editing && p.birthday ? p.birthday.d : 1);
  const maxD = DAYS_IN_MONTH[bm - 1];
  React.useEffect(() => { if (bd > maxD) setBd(maxD); }, [bm]); // keep day valid for month

  const unameCount = editing ? p.usernames.length : 0;
  const interCount = editing ? p.interactions.length : 0;

  return (
    <div className={"seg-modal" + (behind ? " is-under" : "")} onClick={behind ? undefined : onClose}>
      <div className={"seg-modal__card fb-editdlg" + (behind ? " is-behind" : "")} onClick={(e) => e.stopPropagation()}>
        <div className="seg-modal__head">
          <h3>{editing ? "Edit person" : "New person"}</h3>
          <p>{editing
            ? "Update this person's details. Usernames and interactions are managed in their own popups."
            : "Add someone to the register. New people start as Unknown and Public, with no birthday."}</p>
        </div>

        <div className="fb-editdlg__body">
          {/* Identity: avatar + name / category / status */}
          <div className="fb-identity">
            <div className="fb-uploader">
              <button type="button" className="fb-uploader__btn" aria-label="Change photo">
                <PersonAvatar person={editing ? p : { name: "", avatar: false }} style={{ "--_s": "76px" }} />
                <span className="fb-uploader__edit"><Icon n="camera" size={13} /></span>
              </button>
              <span className="fb-uploader__hint">{editing && p.avatar ? "Replace photo" : "Add photo"}</span>
            </div>

            <div className="fb-identity__fields">
              <Input label="Name" defaultValue={editing ? p.name : ""} placeholder="Who is this?" iconLeft={<Icon n="user-round" size={16} />} />
              <div className="fb-identity__row">
                <div>
                  <span className="seg-field-label">Category</span>
                  <Select value={cat} onChange={(e) => setCat(e.target.value)}
                    options={FB_CATEGORIES.map((c) => ({ value: c.value, label: c.value }))} />
                </div>
                <div>
                  <span className="seg-field-label">Status</span>
                  <Select value={status} onChange={(e) => setStatus(e.target.value)}
                    options={FB_STATUS_ORDER.map((s) => ({ value: s, label: FB_STATUS[s].label }))} />
                </div>
              </div>
            </div>
          </div>

          {/* Birthday — month + day, all-or-nothing */}
          <div className="fb-bdayfield">
            <button type="button" className={"fb-bdaytoggle" + (hasB ? " is-on" : "")} onClick={() => setHasB((v) => !v)}>
              <span className="fb-bdaytoggle__box">{hasB && <Icon n="check" size={13} />}</span>
              Has a birthday
            </button>
            <div className={"fb-bdayrow" + (hasB ? "" : " is-off")}>
              <Select value={String(bm)} onChange={(e) => setBm(Number(e.target.value))}
                options={MONTHS.map((m, i) => ({ value: String(i + 1), label: m }))} />
              <Select value={String(bd)} onChange={(e) => setBd(Number(e.target.value))}
                options={Array.from({ length: maxD }, (_, i) => ({ value: String(i + 1), label: String(i + 1) }))} />
            </div>
            <span className="seg-field-hint">Only day and month are stored — never a year. February allows 29.</span>
          </div>

          {/* Notes */}
          <div className="seg-field">
            <span className="seg-field-label">Notes</span>
            <textarea className="mood-textarea" defaultValue={editing ? (p.notes || "") : ""}
              placeholder="How you know them, context, anything worth remembering… (optional)" />
            <span className="seg-field-hint">Up to 2,000 characters.</span>
          </div>

          {/* Visibility */}
          <div className="seg-field">
            <span className="seg-field-label">Visibility</span>
            <div className="mood-seg seg-visseg">
              {[["Public", "globe"], ["Private", "lock"]].map(([v, ic]) => (
                <button key={v} className={"mood-seg__btn" + (vis === v ? " is-active" : "")} onClick={() => setVis(v)}>
                  <Icon n={ic} size={14} /> {v}
                </button>
              ))}
            </div>
            <span className="seg-field-hint">Public people are editable by everyone in the household. Only the creator can change visibility.</span>
          </div>

          {/* Manage usernames + interactions */}
          <div className="fb-manage">
            <div className="fb-managecard">
              <div className="fb-managecard__head">
                <span className="fb-managecard__icon"><Icon n="at-sign" size={17} /></span>
                <span className="fb-managecard__title">Usernames</span>
                <span className="fb-managecard__n">{unameCount}</span>
              </div>
              <span className="fb-managecard__sub">{editing ? "Handles across platforms — repeats allowed." : "Add usernames after saving."}</span>
              <Button variant="outline" size="sm" iconLeft={<Icon n="at-sign" size={15} />}
                disabled={!editing} onClick={onUsernames}>Manage usernames</Button>
            </div>
            <div className="fb-managecard">
              <div className="fb-managecard__head">
                <span className="fb-managecard__icon"><Icon n="messages-square" size={17} /></span>
                <span className="fb-managecard__title">Interactions</span>
                <span className="fb-managecard__n">{interCount}</span>
              </div>
              <span className="fb-managecard__sub">{editing ? "A dated log, most recent first." : "Log interactions after saving."}</span>
              <Button variant="outline" size="sm" iconLeft={<Icon n="messages-square" size={15} />}
                disabled={!editing} onClick={onInteractions}>Open log</Button>
            </div>
          </div>
        </div>

        <div className="seg-modal__foot">
          <span className="seg-modal__foot-note">
            {editing
              ? <React.Fragment><Icon n="user-round" size={13} /> {p.owner} · created {p.created}</React.Fragment>
              : <React.Fragment><Icon n="sparkles" size={13} /> Starts Unknown · Public · no birthday</React.Fragment>}
          </span>
          <div className="seg-modal__foot-actions">
            {editing && <Button variant="ghost" size="sm" className="seg-danger" iconLeft={<Icon n="trash-2" size={15} />}>Delete</Button>}
            <Button variant="ghost" onClick={onClose}>Cancel</Button>
            <Button variant="primary" iconLeft={<Icon n={editing ? "check" : "plus"} size={17} />} onClick={onClose}>
              {editing ? "Save changes" : "Create person"}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}

// ── Usernames popup ─────────────────────────────────────────────
let _nuid = 5000;
function UsernamesDialog({ person, onClose }) {
  const [rows, setRows] = React.useState(() => person.usernames.map((u) => ({ ...u })));
  const setField = (id, key, val) => setRows((rs) => rs.map((r) => r.id === id ? { ...r, [key]: val } : r));
  const remove = (id) => setRows((rs) => rs.filter((r) => r.id !== id));
  const add = () => setRows((rs) => [...rs, { id: "u" + (++_nuid), platform: FB_PLATFORMS[0].value, value: "", notes: "" }]);

  return (
    <div className="seg-selector" onClick={onClose}>
      <div className="seg-selector__card fb-subcard" onClick={(e) => e.stopPropagation()}>
        <div className="seg-selector__head">
          <div className="seg-selector__head-txt">
            <div className="armali-eyebrow">Usernames</div>
            <h3>Handles &amp; accounts</h3>
            <div className="fb-sub__person"><Icon n="user-round" size={14} /> {person.name}</div>
          </div>
          <IconButton label="Close" variant="ghost" icon={<Icon n="x" size={18} />} onClick={onClose} />
        </div>

        {rows.length === 0 ? (
          <div className="fb-sublist">
            <div className="fb-subempty">
              <span className="fb-subempty__icon"><Icon n="at-sign" size={24} /></span>
              <p>Nothing here yet — add a platform and handle. The same platform can appear more than once.</p>
            </div>
          </div>
        ) : (
          <div className="fb-sublist">
            {rows.map((r) => (
              <div key={r.id} className="fb-urow">
                <Select value={r.platform} onChange={(e) => setField(r.id, "platform", e.target.value)}
                  options={FB_PLATFORMS.map((pl) => ({ value: pl.value, label: pl.value }))} />
                <input className="fb-uinput" value={r.value} placeholder="Handle or value"
                  onChange={(e) => setField(r.id, "value", e.target.value)} />
                <input className="fb-uinput fb-uinput--notes" value={r.notes || ""} placeholder="Notes (optional)"
                  onChange={(e) => setField(r.id, "notes", e.target.value)} />
                <Tooltip label="Remove" side="left">
                  <IconButton size="sm" variant="ghost" label="Remove username" icon={<Icon n="trash-2" size={15} />} onClick={() => remove(r.id)} />
                </Tooltip>
              </div>
            ))}
          </div>
        )}
        <button className="fb-subadd" onClick={add}><Icon n="plus" size={16} /> Add username</button>

        <div className="seg-selector__foot">
          <span className="seg-pageinfo"><Icon n="info" size={14} /> {rows.length} {rows.length === 1 ? "username" : "usernames"} · inherits this person's visibility</span>
          <div className="seg-modal__foot-actions">
            <Button variant="ghost" onClick={onClose}>Cancel</Button>
            <Button variant="primary" iconLeft={<Icon n="check" size={17} />} onClick={onClose}>Save usernames</Button>
          </div>
        </div>
      </div>
    </div>
  );
}

// ── Interactions popup ──────────────────────────────────────────
const MS_DAY = 86400000;
function parseDate(s) { const [y, m, d] = s.split("-").map(Number); return new Date(y, m - 1, d); }
function fmtDay(s) { const d = parseDate(s); return `${d.getDate()} ${MONTHS_SHORT[d.getMonth()]} ${d.getFullYear()}`; }
function relDays(s) {
  const n = Math.round((F.TODAY - parseDate(s)) / MS_DAY);
  if (n <= 0) return "today";
  if (n === 1) return "yesterday";
  if (n < 7) return `${n} days ago`;
  if (n < 14) return "last week";
  if (n < 60) return `${Math.round(n / 7)} weeks ago`;
  return `${Math.round(n / 30)} months ago`;
}
let _niid = 9000;
function InteractionsDialog({ person, onClose }) {
  const [rows, setRows] = React.useState(() =>
    person.interactions.slice().sort((a, b) => b.date.localeCompare(a.date) || b.id.localeCompare(a.id)));
  const [draft, setDraft] = React.useState("");
  const today = `${F.TODAY.getFullYear()}-${String(F.TODAY.getMonth() + 1).padStart(2, "0")}-${String(F.TODAY.getDate()).padStart(2, "0")}`;
  const [date, setDate] = React.useState(today);

  const add = () => {
    if (!draft.trim()) return;
    const next = { id: "i" + (++_niid), date, description: draft.trim() };
    setRows((rs) => [...rs, next].sort((a, b) => b.date.localeCompare(a.date) || b.id.localeCompare(a.id)));
    setDraft("");
  };
  const remove = (id) => setRows((rs) => rs.filter((r) => r.id !== id));

  return (
    <div className="seg-selector" onClick={onClose}>
      <div className="seg-selector__card fb-subcard" onClick={(e) => e.stopPropagation()}>
        <div className="seg-selector__head">
          <div className="seg-selector__head-txt">
            <div className="armali-eyebrow">Interactions</div>
            <h3>Chronological log</h3>
            <div className="fb-sub__person"><Icon n="user-round" size={14} /> {person.name}</div>
          </div>
          <IconButton label="Close" variant="ghost" icon={<Icon n="x" size={18} />} onClick={onClose} />
        </div>

        {/* Composer — date defaults to today, never in the future */}
        <div className="fb-composer">
          <div className="fb-composer__date">
            <div className="fb-composer__field">
              <span>Date</span>
              <Input type="date" value={date} max={today} onChange={(e) => setDate(e.target.value)} />
            </div>
          </div>
          <div className="fb-composer__desc">
            <div className="fb-composer__field">
              <span>What happened</span>
              <Input value={draft} placeholder="Describe the interaction…" onChange={(e) => setDraft(e.target.value)} />
            </div>
          </div>
          <Button variant="primary" iconLeft={<Icon n="plus" size={16} />} onClick={add}>Log</Button>
        </div>

        <div className="fb-sublist">
          {rows.length === 0 ? (
            <div className="fb-subempty">
              <span className="fb-subempty__icon"><Icon n="messages-square" size={24} /></span>
              <p>No interactions logged yet — record the first one above. The newest entry always sits on top.</p>
            </div>
          ) : (
            <div className="fb-timeline">
              {rows.map((r) => (
                <div key={r.id} className="fb-tlitem">
                  <div className="fb-tlitem__date">
                    <span className="fb-tlitem__day">{fmtDay(r.date)}</span>
                    <span className="fb-tlitem__rel">{relDays(r.date)}</span>
                  </div>
                  <div className="fb-tlitem__node">
                    <p className="fb-tlitem__desc">{r.description}</p>
                  </div>
                  <div className="fb-tlitem__act">
                    <Tooltip label="Delete" side="left">
                      <IconButton size="sm" variant="ghost" label="Delete interaction" icon={<Icon n="trash-2" size={15} />} onClick={() => remove(r.id)} />
                    </Tooltip>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="seg-selector__foot">
          <span className="seg-pageinfo"><Icon n="info" size={14} /> {rows.length} {rows.length === 1 ? "interaction" : "interactions"} · most recent first</span>
          <div className="seg-modal__foot-actions">
            <Button variant="ghost" onClick={onClose}>Close</Button>
            <Button variant="primary" iconLeft={<Icon n="check" size={17} />} onClick={onClose}>Done</Button>
          </div>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, {
  FbPersonEditDialog: PersonEditDialog,
  FbUsernamesDialog: UsernamesDialog,
  FbInteractionsDialog: InteractionsDialog,
  FbStatusBadge: StatusBadge,
  FbCatChip: CatChip,
  FbPersonAvatar: PersonAvatar,
});
})();
