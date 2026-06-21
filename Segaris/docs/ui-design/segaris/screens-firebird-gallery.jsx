/* global React */
// Firebird — the module entry: a server-paginated avatar gallery of people
// with search, category & status filters and name/birthday sorting, plus the
// orchestrator that wires the URL-aware person editor and its username &
// interaction sub-popups over the gallery. Two gallery card compositions are
// offered. Canvas variants are exported at the end.
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Button, IconButton, Tooltip, Input, Select } = A;
const Icon = window.SegIcon;
const F = window.SegFire;
const { FB_STATUS, FB_STATUS_ORDER, FB_CATEGORIES, PLATFORM_ICON,
        fmtBirthday, daysUntilBirthday, birthdaySoon, birthdayRelative, birthdayCompare } = F;
const ALL = window.SEG_PEOPLE;
const StatusBadge = window.FbStatusBadge;
const CatChip = window.FbCatChip;
const PersonAvatar = window.FbPersonAvatar;

const MS_DAY = 86400000;
const MONTHS_SHORT = F.MONTHS_SHORT;
function lastInteraction(p) {
  if (!p.interactions.length) return null;
  return p.interactions.slice().sort((a, b) => b.date.localeCompare(a.date) || b.id.localeCompare(a.id))[0];
}
function relDays(s) {
  const [y, m, d] = s.split("-").map(Number);
  const n = Math.round((F.TODAY - new Date(y, m - 1, d)) / MS_DAY);
  if (n <= 0) return "today";
  if (n === 1) return "yesterday";
  if (n < 7) return `${n} days ago`;
  if (n < 14) return "last week";
  if (n < 60) return `${Math.round(n / 7)} weeks ago`;
  return `${Math.round(n / 30)} months ago`;
}

function VisCorner({ visibility }) {
  const priv = visibility === "Private";
  return (
    <Tooltip label={priv ? "Private — only you" : "Public"} side="left">
      <span className={"fb-corner fb-corner--vis" + (priv ? " is-private" : "")}>
        <Icon n={priv ? "lock" : "globe"} size={14} />
      </span>
    </Tooltip>
  );
}
function BirthdayLine({ person }) {
  if (!person.birthday) return <span className="fb-bday fb-bday--none"><Icon n="cake" size={14} />No birthday</span>;
  const soon = birthdaySoon(person.birthday);
  return (
    <span className={"fb-bday" + (soon ? " fb-bday--soon" : "")}>
      <Icon n="cake" size={14} />{fmtBirthday(person.birthday)}
      {soon && <span className="fb-bday__rel">{birthdayRelative(person.birthday)}</span>}
    </span>
  );
}

// ── Variant A · portrait card ───────────────────────────────────
function PortraitCard({ person, onOpen }) {
  const dim = person.status === "Blocked";
  const soon = person.birthday && birthdaySoon(person.birthday);
  return (
    <div role="button" tabIndex={0} className={"fb-pcard" + (dim ? " is-dim" : "")} onClick={() => onOpen(person)}>
      {soon && (
        <Tooltip label={"Birthday " + birthdayRelative(person.birthday)} side="right">
          <span className="fb-corner fb-corner--cake"><Icon n="cake" size={13} /></span>
        </Tooltip>
      )}
      <VisCorner visibility={person.visibility} />
      <div className="fb-pcard__avatar"><PersonAvatar person={person} style={{ "--_s": "72px" }} /></div>
      <div className="fb-pcard__name">{person.name}</div>
      <div className="fb-pcard__meta"><CatChip category={person.category} /><StatusBadge status={person.status} /></div>
      <div className="fb-pcard__foot">
        <BirthdayLine person={person} />
      </div>
    </div>
  );
}

// ── Variant B · detail card ─────────────────────────────────────
function DetailCard({ person, onOpen }) {
  const dim = person.status === "Blocked";
  const last = lastInteraction(person);
  const handles = person.usernames;
  const shown = handles.slice(0, 3);
  const extra = handles.length - shown.length;
  return (
    <div role="button" tabIndex={0} className={"fb-dcard" + (dim ? " is-dim" : "")} onClick={() => onOpen(person)}>
      <VisCorner visibility={person.visibility} />
      <div className="fb-dcard__top">
        <PersonAvatar person={person} size="lg" />
        <div className="fb-dcard__id">
          <span className="fb-dcard__name">{person.name}</span>
          <div className="fb-dcard__meta"><CatChip category={person.category} /><StatusBadge status={person.status} /></div>
        </div>
      </div>

      <div className="fb-dcard__body">
        <div className="fb-dcard__row">
          <BirthdayLine person={person} />
        </div>
        <div className="fb-handles">
          {handles.length === 0
            ? <span className="fb-handles__empty">No usernames yet</span>
            : <React.Fragment>
                {shown.map((u) => (
                  <span key={u.id} className="fb-handle">
                    <Icon n={PLATFORM_ICON[u.platform] || "at-sign"} size={12} />
                    <span className="fb-handle__val">{u.value}</span>
                  </span>
                ))}
                {extra > 0 && <span className="fb-handle fb-handle--more">+{extra}</span>}
              </React.Fragment>}
        </div>
      </div>

      {last
        ? <div className="fb-dcard__last">
            <Icon n="messages-square" size={15} />
            <div className="fb-dcard__lasttxt">
              <div className="fb-dcard__lastdesc">{last.description}</div>
              <div className="fb-dcard__lastdate">{relDays(last.date)}</div>
            </div>
          </div>
        : <div className="fb-dcard__last fb-dcard__last--empty"><Icon n="messages-square" size={15} /><div className="fb-dcard__lasttxt"><div className="fb-dcard__lastdesc">No interactions logged</div></div></div>}
    </div>
  );
}

// ── Pager (demo: single page) ───────────────────────────────────
function Pager({ page, pages }) {
  const items = Array.from({ length: pages }, (_, i) => i + 1);
  return (
    <div className="seg-pager">
      <button className="seg-pager__btn" disabled={page <= 1} aria-label="Previous page"><Icon n="chevron-left" size={16} /></button>
      {items.map((p) => <button key={p} className={"seg-pager__btn" + (p === page ? " is-active" : "")}>{p}</button>)}
      <button className="seg-pager__btn" disabled={page >= pages} aria-label="Next page"><Icon n="chevron-right" size={16} /></button>
    </div>
  );
}

// ── Sorting ─────────────────────────────────────────────────────
function sortPeople(list, sort) {
  const arr = list.slice();
  arr.sort((a, b) => {
    let cmp = 0;
    if (sort.key === "name") cmp = a.name.localeCompare(b.name);
    else cmp = birthdayCompare(a.birthday, b.birthday); // calendar order, none last
    if (cmp === 0) cmp = a.id.localeCompare(b.id);
    return sort.dir === "desc" ? -cmp : cmp;
  });
  return arr;
}

// ── Orchestrator ────────────────────────────────────────────────
function FirebirdScreen({ variant = "portrait", initialDialog }) {
  const [search, setSearch] = React.useState("");
  const [cat, setCat] = React.useState("");
  const [status, setStatus] = React.useState("");
  const [sort, setSort] = React.useState({ key: "name", dir: "asc" });
  const [dialog, setDialog] = React.useState(initialDialog || null);

  const filtered = React.useMemo(() => {
    let list = ALL.filter((p) => {
      if (cat && p.category !== cat) return false;
      if (status && p.status !== status) return false;
      if (search.trim()) {
        const q = search.trim().toLowerCase();
        if (!(p.name.toLowerCase().includes(q) || (p.notes || "").toLowerCase().includes(q))) return false;
      }
      return true;
    });
    return sortPeople(list, sort);
  }, [search, cat, status, sort]);

  const total = ALL.length;
  const soonCount = ALL.filter((p) => p.birthday && birthdaySoon(p.birthday)).length;
  const activeCount = ALL.filter((p) => p.status === "Active").length;

  const close = () => setDialog(null);
  const openEdit = (person) => setDialog({ mode: "edit", person });
  const openNew = () => setDialog({ mode: "new" });

  const editorOpen = dialog && (dialog.mode === "new" || dialog.mode === "edit" || dialog.mode === "usernames" || dialog.mode === "interactions");
  const editorBehind = dialog && (dialog.mode === "usernames" || dialog.mode === "interactions");
  const editorMode = dialog && (dialog.mode === "new" ? "new" : "edit");

  const Card = variant === "detail" ? DetailCard : PortraitCard;

  return (
    <div className="seg-screen">
      {window.SegShellTopBar ? <window.SegShellTopBar eyebrow="Firebird" title="People" /> : null}
      <div className="seg-page">
        <div className="seg-page__inner">
          <div className="fb-bar">
            <div>
              <div className="armali-eyebrow">Personal register of known people</div>
              <h2>People</h2>
              <p>The people your household knows — their identities across services and a log of how you've interacted.</p>
            </div>
            <div className="fb-bar__stats">
              <div className="seg-stat-pill"><strong>{total}</strong><span>People</span></div>
              <Tooltip label="Birthdays in the next 7 days" side="bottom">
                <div className="seg-stat-pill is-attn"><strong>{soonCount}</strong><span>Birthdays this week</span></div>
              </Tooltip>
              <div className="seg-stat-pill"><strong>{activeCount}</strong><span>Active</span></div>
              <Button variant="primary" iconLeft={<Icon n="plus" size={17} />} onClick={openNew}>New person</Button>
            </div>
          </div>

          <div className="fb-toolbar">
            <div className="fb-toolbar__search">
              <Input placeholder="Search by name" value={search}
                onChange={(e) => setSearch(e.target.value)} iconLeft={<Icon n="search" size={16} />} />
            </div>
            <Select value={cat} onChange={(e) => setCat(e.target.value)}
              options={[{ value: "", label: "All categories" }, ...FB_CATEGORIES.map((c) => ({ value: c.value, label: c.value }))]} />
            <Select value={status} onChange={(e) => setStatus(e.target.value)}
              options={[{ value: "", label: "All statuses" }, ...FB_STATUS_ORDER.map((s) => ({ value: s, label: FB_STATUS[s].label }))]} />
            <span className="fb-toolbar__spacer" />
            <div className="fb-sort">
              <Select value={sort.key} onChange={(e) => setSort((s) => ({ ...s, key: e.target.value }))}
                options={[{ value: "name", label: "Sort: name" }, { value: "birthday", label: "Sort: birthday" }]} />
              <Tooltip label={sort.dir === "asc" ? "Ascending" : "Descending"} side="bottom">
                <button className="fb-sort__dir" onClick={() => setSort((s) => ({ ...s, dir: s.dir === "asc" ? "desc" : "asc" }))} aria-label="Toggle sort direction">
                  <Icon n={sort.dir === "asc" ? "arrow-down-a-z" : "arrow-up-z-a"} size={16} />
                </button>
              </Tooltip>
            </div>
          </div>

          <div className="fb-galleryscroll">
            {filtered.length === 0 ? (
              <div className="fb-gallery">
                <div className="fb-empty">
                  <span className="fb-empty__icon"><Icon n="user-round-search" size={26} /></span>
                  <h3>Nobody matches</h3>
                  <p>No people match your search and filters — try clearing them.</p>
                </div>
              </div>
            ) : (
              <div className={"fb-gallery fb-gallery--" + variant}>
                {filtered.map((p) => <Card key={p.id} person={p} onOpen={openEdit} />)}
              </div>
            )}
          </div>

          <div className="seg-selector__foot" style={{ borderTop: "none", padding: "0 var(--space-2)", background: "transparent" }}>
            <span className="seg-pageinfo">{filtered.length === 0 ? "No results" : <React.Fragment>Showing <b>1–{filtered.length}</b> of <b>{filtered.length}</b> people</React.Fragment>}</span>
            <div style={{ display: "flex", alignItems: "center", gap: "var(--space-4)" }}>
              <Select value="25" options={[{ value: "25", label: "25 per page" }, { value: "50", label: "50 per page" }]} onChange={() => {}} />
              <Pager page={1} pages={1} />
            </div>
          </div>
        </div>
      </div>

      {editorOpen && (
        <window.FbPersonEditDialog
          mode={editorMode}
          person={dialog.person}
          behind={editorBehind}
          onClose={close}
          onUsernames={() => setDialog({ mode: "usernames", person: dialog.person })}
          onInteractions={() => setDialog({ mode: "interactions", person: dialog.person })}
        />
      )}
      {dialog && dialog.mode === "usernames" && (
        <window.FbUsernamesDialog person={dialog.person} onClose={() => setDialog({ mode: "edit", person: dialog.person })} />
      )}
      {dialog && dialog.mode === "interactions" && (
        <window.FbInteractionsDialog person={dialog.person} onClose={() => setDialog({ mode: "edit", person: dialog.person })} />
      )}
    </div>
  );
}

// ── Canvas variants ─────────────────────────────────────────────
const featured = ALL.find((p) => p.id === "p-olivia") || ALL[0];
const FirebirdGalleryPortrait = () => <FirebirdScreen variant="portrait" />;
const FirebirdGalleryDetail   = () => <FirebirdScreen variant="detail" />;
const FirebirdEditor          = () => <FirebirdScreen variant="portrait" initialDialog={{ mode: "edit", person: featured }} />;
const FirebirdNew             = () => <FirebirdScreen variant="portrait" initialDialog={{ mode: "new" }} />;
const FirebirdUsernames       = () => <FirebirdScreen variant="portrait" initialDialog={{ mode: "usernames", person: featured }} />;
const FirebirdInteractions    = () => <FirebirdScreen variant="portrait" initialDialog={{ mode: "interactions", person: featured }} />;

Object.assign(window, {
  FirebirdGalleryPortrait, FirebirdGalleryDetail, FirebirdEditor,
  FirebirdNew, FirebirdUsernames, FirebirdInteractions,
});
})();
