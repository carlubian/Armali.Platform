/* global React */
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Badge, Button, IconButton, Tooltip, Input, Select } = A;
const Icon = window.SegIcon;

// ── Formatting helpers ─────────────────────────────────────────
const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
const fmtEur = (n) => "€" + n.toLocaleString("en-IE", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const fmtDate = (iso) => { const [y, m, d] = iso.split("-"); return `${+d} ${MONTHS[+m - 1]} ${y}`; };

const STATUS_TONE = { Reconciled: "success", Pending: "gold", Draft: "neutral" };

// ── Simulated entities ─────────────────────────────────────────
// Target module: Capex (atomic income / expense records). ~34 rows so
// filtering + numbered pagination are meaningful.
const CAPEX = [
  { id: "CPX-0142", name: "Oak dining table & 6 chairs", category: "Furniture",        status: "Reconciled", date: "2026-02-14", supplier: "Ercol Studio",       amount: 1240.00 },
  { id: "CPX-0141", name: "Robot vacuum cleaner",        category: "Appliances",       status: "Reconciled", date: "2026-02-09", supplier: "Media Markt",       amount: 389.00 },
  { id: "CPX-0139", name: "Reading floor lamp",          category: "Decor",            status: "Draft",      date: "2026-02-03", supplier: "IKEA",              amount: 64.99 },
  { id: "CPX-0137", name: "Espresso machine",            category: "Kitchen",          status: "Reconciled", date: "2026-01-28", supplier: "El Corte Inglés",   amount: 549.00 },
  { id: "CPX-0136", name: "Bathroom tile renovation",    category: "Home improvement", status: "Pending",    date: "2026-01-22", supplier: "Leroy Merlin",      amount: 2180.00 },
  { id: "CPX-0134", name: "Smart thermostat",            category: "Electronics",      status: "Reconciled", date: "2026-01-19", supplier: "Amazon",            amount: 219.00 },
  { id: "CPX-0133", name: "Wool living-room rug",        category: "Decor",            status: "Reconciled", date: "2026-01-15", supplier: "Zara Home",         amount: 312.50 },
  { id: "CPX-0131", name: "Cordless drill set",          category: "Tools",            status: "Draft",      date: "2026-01-11", supplier: "Bauhaus",           amount: 148.90 },
  { id: "CPX-0130", name: "Memory-foam mattress, king",  category: "Furniture",        status: "Reconciled", date: "2026-01-07", supplier: "Conforama",         amount: 879.00 },
  { id: "CPX-0128", name: "Garden parasol & base",       category: "Garden",           status: "Pending",    date: "2026-01-04", supplier: "Leroy Merlin",      amount: 156.00 },
  { id: "CPX-0127", name: "55\" OLED television",         category: "Electronics",      status: "Reconciled", date: "2025-12-29", supplier: "Worten",            amount: 1349.00 },
  { id: "CPX-0125", name: "Stand mixer",                 category: "Kitchen",          status: "Reconciled", date: "2025-12-22", supplier: "El Corte Inglés",   amount: 429.00 },
  { id: "CPX-0124", name: "Bookshelf, walnut",           category: "Furniture",        status: "Reconciled", date: "2025-12-18", supplier: "IKEA",              amount: 189.00 },
  { id: "CPX-0122", name: "Window blinds, set of 4",     category: "Home improvement", status: "Draft",      date: "2025-12-14", supplier: "Leroy Merlin",      amount: 268.40 },
  { id: "CPX-0121", name: "Air purifier",                category: "Appliances",       status: "Reconciled", date: "2025-12-09", supplier: "Media Markt",       amount: 174.99 },
  { id: "CPX-0119", name: "Ceramic dinnerware set",      category: "Kitchen",          status: "Reconciled", date: "2025-12-02", supplier: "Zara Home",         amount: 96.00 },
  { id: "CPX-0118", name: "Outdoor lounge sofa",         category: "Garden",           status: "Pending",    date: "2025-11-27", supplier: "Conforama",         amount: 740.00 },
  { id: "CPX-0116", name: "Noise-cancelling headphones", category: "Electronics",      status: "Reconciled", date: "2025-11-21", supplier: "Amazon",            amount: 299.00 },
  { id: "CPX-0115", name: "Office desk, sit-stand",      category: "Furniture",        status: "Reconciled", date: "2025-11-16", supplier: "IKEA",              amount: 459.00 },
  { id: "CPX-0113", name: "Pressure washer",             category: "Tools",            status: "Draft",      date: "2025-11-10", supplier: "Bauhaus",           amount: 132.00 },
  { id: "CPX-0112", name: "Wall paint & primer",         category: "Home improvement", status: "Reconciled", date: "2025-11-04", supplier: "Leroy Merlin",      amount: 88.70 },
  { id: "CPX-0110", name: "Dishwasher, built-in",        category: "Appliances",       status: "Reconciled", date: "2025-10-29", supplier: "Worten",            amount: 629.00 },
  { id: "CPX-0109", name: "Framed print, coastal",       category: "Decor",            status: "Draft",      date: "2025-10-23", supplier: "Local gallery",     amount: 240.00 },
  { id: "CPX-0107", name: "Raised garden beds, pair",    category: "Garden",           status: "Reconciled", date: "2025-10-17", supplier: "Leroy Merlin",      amount: 119.00 },
  { id: "CPX-0106", name: "Tablet, 11\"",                 category: "Electronics",      status: "Reconciled", date: "2025-10-11", supplier: "Media Markt",       amount: 679.00 },
  { id: "CPX-0104", name: "Velvet accent armchair",      category: "Furniture",        status: "Reconciled", date: "2025-10-05", supplier: "Conforama",         amount: 528.00 },
  { id: "CPX-0103", name: "Knife block set",             category: "Kitchen",          status: "Reconciled", date: "2025-09-29", supplier: "El Corte Inglés",   amount: 159.90 },
  { id: "CPX-0101", name: "Toolbox & hand-tool kit",     category: "Tools",            status: "Reconciled", date: "2025-09-22", supplier: "Bauhaus",           amount: 97.50 },
  { id: "CPX-0100", name: "Bedside tables, pair",        category: "Furniture",        status: "Reconciled", date: "2025-09-16", supplier: "IKEA",              amount: 138.00 },
  { id: "CPX-0098", name: "Coffee grinder",              category: "Kitchen",          status: "Draft",      date: "2025-09-10", supplier: "Amazon",            amount: 79.00 },
  { id: "CPX-0097", name: "Floor-standing mirror",       category: "Decor",            status: "Reconciled", date: "2025-09-03", supplier: "Zara Home",         amount: 142.00 },
  { id: "CPX-0095", name: "Washer-dryer, 9kg",           category: "Appliances",       status: "Reconciled", date: "2025-08-28", supplier: "Worten",            amount: 812.00 },
  { id: "CPX-0094", name: "Patio string lights",         category: "Garden",           status: "Reconciled", date: "2025-08-21", supplier: "Amazon",            amount: 44.99 },
  { id: "CPX-0092", name: "Smart doorbell",              category: "Electronics",      status: "Pending",    date: "2025-08-14", supplier: "Media Markt",       amount: 189.00 },
];

// Source module: Assets (durable objects). The first row is the asset
// currently being edited; the rest give the table context.
const ASSETS = [
  { id: "AST-0061", name: "Oak dining table",   status: "In use",    category: "Furniture",   location: "Dining room",  model: "Ercol Windsor",  link: null,                               editing: true },
  { id: "AST-0058", name: "Espresso machine",   status: "In use",    category: "Kitchen",     location: "Kitchen",      model: "De'Longhi La Specialista", link: "CPX-0137 · €549.00" },
  { id: "AST-0054", name: "Living-room sofa",   status: "In use",    category: "Furniture",   location: "Living room",  model: "Conforama Velvet",        link: "CPX-0104 · €528.00" },
  { id: "AST-0049", name: "OLED television",    status: "In use",    category: "Electronics", location: "Living room",  model: "LG C4 55\"",              link: "CPX-0127 · €1,349.00" },
  { id: "AST-0041", name: "Office desk",        status: "On loan",   category: "Furniture",   location: "Study",        model: "IKEA Bekant",             link: "CPX-0115 · €459.00" },
  { id: "AST-0036", name: "Patio parasol",      status: "In storage",category: "Garden",      location: "Garage",       model: "Leroy Sombra 3m",         link: null },
];

const ASSET_STATUS_TONE = { "In use": "success", "On loan": "azure", "In storage": "neutral", "Disposed": "danger" };

const CATEGORIES = Array.from(new Set(CAPEX.map((c) => c.category))).sort();
const STATUSES = ["Reconciled", "Pending", "Draft"];
const AMOUNT_BRACKETS = [
  { value: "any",   label: "Any amount",     range: [0, Infinity] },
  { value: "u100",  label: "Under €100",     range: [0, 100] },
  { value: "m100",  label: "€100 – €500",    range: [100, 500] },
  { value: "m500",  label: "€500 – €2,000",  range: [500, 2000] },
  { value: "o2000", label: "Over €2,000",    range: [2000, Infinity] },
];
const PAGE_SIZE = 8;

// ── File-selector-style reference control ──────────────────────
function RefControl({ value, onBrowse, onClear }) {
  if (!value) {
    return (
      <div className="seg-ref is-empty">
        <div className="seg-ref__icon"><Icon n="link" size={19} /></div>
        <div className="seg-ref__body">
          <span className="seg-ref__placeholder">No expense linked</span>
          <span className="seg-ref__hint">Link the Capex entry this asset was purchased with.</span>
        </div>
        <div className="seg-ref__act">
          <Button variant="outline" size="sm" iconLeft={<Icon n="search" size={15} />} onClick={onBrowse}>Browse</Button>
        </div>
      </div>
    );
  }
  return (
    <div className="seg-ref is-filled">
      <div className="seg-ref__icon"><Icon n="receipt-text" size={19} /></div>
      <div className="seg-ref__body">
        <span className="seg-ref__name">{value.name}</span>
        <span className="seg-ref__meta">
          <b>{value.id}</b><span className="seg-ref__sep"></span>{value.category}
          <span className="seg-ref__sep"></span><b>{fmtEur(value.amount)}</b>
        </span>
      </div>
      <div className="seg-ref__act">
        <Tooltip label="Clear" side="top"><IconButton size="sm" variant="ghost" label="Clear link" icon={<Icon n="x" size={15} />} onClick={onClear} /></Tooltip>
        <Button variant="outline" size="sm" iconLeft={<Icon n="repeat-2" size={15} />} onClick={onBrowse}>Change</Button>
      </div>
    </div>
  );
}

// ── Sort header cell ───────────────────────────────────────────
function SortHeader({ label, sortKey, sort, onSort, align }) {
  const active = sort.key === sortKey;
  return (
    <span className={"seg-th" + (align === "right" ? " seg-th--right" : "")} style={align === "right" ? { display: "flex", justifyContent: "flex-end" } : undefined}>
      <button className={"seg-sorth" + (active ? " is-active" : "") + (active && sort.dir === "desc" ? " is-desc" : "")} onClick={() => onSort(sortKey)}>
        {label}
        <span className="seg-sorth__chev"><Icon n="chevron-up" size={13} /></span>
      </button>
    </span>
  );
}

// ── Numbered pager ─────────────────────────────────────────────
function pageItems(page, pages) {
  if (pages <= 7) return Array.from({ length: pages }, (_, i) => i + 1);
  const out = [1];
  const lo = Math.max(2, page - 1), hi = Math.min(pages - 1, page + 1);
  if (lo > 2) out.push("…");
  for (let p = lo; p <= hi; p++) out.push(p);
  if (hi < pages - 1) out.push("…");
  out.push(pages);
  return out;
}
function Pager({ page, pages, onPage }) {
  return (
    <div className="seg-pager">
      <button className="seg-pager__btn" disabled={page <= 1} onClick={() => onPage(page - 1)} aria-label="Previous page"><Icon n="chevron-left" size={16} /></button>
      {pageItems(page, pages).map((p, i) =>
        p === "…"
          ? <span key={"g" + i} className="seg-pager__gap">…</span>
          : <button key={p} className={"seg-pager__btn" + (p === page ? " is-active" : "")} onClick={() => onPage(p)}>{p}</button>
      )}
      <button className="seg-pager__btn" disabled={page >= pages} onClick={() => onPage(page + 1)} aria-label="Next page"><Icon n="chevron-right" size={16} /></button>
    </div>
  );
}

// ── The selector table popup ───────────────────────────────────
function CapexSelector({ variant, currentId, onClose, onSelect }) {
  const [search, setSearch] = React.useState("");
  const [cat, setCat] = React.useState("all");
  const [status, setStatus] = React.useState("all");
  const [amount, setAmount] = React.useState("any");
  const [sort, setSort] = React.useState({ key: "date", dir: "desc" });
  const [page, setPage] = React.useState(1);

  const onSort = (key) => {
    setSort((s) => s.key === key ? { key, dir: s.dir === "asc" ? "desc" : "asc" } : { key, dir: key === "amount" || key === "date" ? "desc" : "asc" });
    setPage(1);
  };
  const setF = (fn) => (v) => { fn(v); setPage(1); };

  const filtered = React.useMemo(() => {
    const q = search.trim().toLowerCase();
    const [min, max] = AMOUNT_BRACKETS.find((b) => b.value === amount).range;
    let rows = CAPEX.filter((c) => {
      if (q && !(c.name.toLowerCase().includes(q) || c.id.toLowerCase().includes(q) || c.supplier.toLowerCase().includes(q))) return false;
      if (cat !== "all" && c.category !== cat) return false;
      if (status !== "all" && c.status !== status) return false;
      if (c.amount < min || c.amount > max) return false;
      return true;
    });
    const dir = sort.dir === "asc" ? 1 : -1;
    rows = rows.slice().sort((a, b) => {
      let av = a[sort.key], bv = b[sort.key];
      if (typeof av === "string") { av = av.toLowerCase(); bv = bv.toLowerCase(); }
      return av < bv ? -dir : av > bv ? dir : 0;
    });
    return rows;
  }, [search, cat, status, amount, sort]);

  const total = filtered.length;
  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE));
  const safePage = Math.min(page, pages);
  const start = (safePage - 1) * PAGE_SIZE;
  const rows = filtered.slice(start, start + PAGE_SIZE);

  const activeFilters = [
    cat !== "all" && { key: "cat", label: cat, clear: () => setF(setCat)("all") },
    status !== "all" && { key: "status", label: status, clear: () => setF(setStatus)("all") },
    amount !== "any" && { key: "amount", label: AMOUNT_BRACKETS.find((b) => b.value === amount).label, clear: () => setF(setAmount)("any") },
    search.trim() && { key: "q", label: `“${search.trim()}”`, clear: () => setF(setSearch)("") },
  ].filter(Boolean);
  const clearAll = () => { setSearch(""); setCat("all"); setStatus("all"); setAmount("any"); setPage(1); };

  const countNode = (
    <span className="seg-selector__count"><b>{total}</b> {total === 1 ? "expense" : "expenses"} match</span>
  );

  const tableHead = (
    <div className="seg-selhead">
      <SortHeader label="Expense" sortKey="name" sort={sort} onSort={onSort} />
      <SortHeader label="Category" sortKey="category" sort={sort} onSort={onSort} />
      <SortHeader label="Supplier" sortKey="supplier" sort={sort} onSort={onSort} />
      <SortHeader label="Date" sortKey="date" sort={sort} onSort={onSort} />
      <SortHeader label="Amount" sortKey="amount" sort={sort} onSort={onSort} align="right" />
      <SortHeader label="Status" sortKey="status" sort={sort} onSort={onSort} />
      <span></span>
    </div>
  );

  const tableBody = rows.length === 0 ? (
    <div className="seg-selempty">
      <span className="seg-selempty__icon"><Icon n="search-x" size={26} /></span>
      <p>No expenses match these filters. Try a broader search or clear a filter.</p>
      <Button variant="outline" size="sm" iconLeft={<Icon n="rotate-ccw" size={15} />} onClick={clearAll}>Clear filters</Button>
    </div>
  ) : rows.map((c) => {
    const current = c.id === currentId;
    return (
      <div key={c.id} className={"seg-selrow" + (current ? " is-current" : "")}>
        <div className="seg-seln"><strong>{c.name}</strong><em>{c.id}</em></div>
        <span className="seg-selcell">{c.category}</span>
        <span className="seg-selcell">{c.supplier}</span>
        <span className="seg-selcell">{fmtDate(c.date)}</span>
        <span className="seg-selamt">{fmtEur(c.amount)}</span>
        <span><Badge tone={STATUS_TONE[c.status]} dot>{c.status}</Badge></span>
        <div className="seg-selrow__act">
          {current
            ? <span className="seg-current-tag"><Icon n="check" size={14} /> Linked</span>
            : <Button variant="primary" size="sm" onClick={() => onSelect(c)}>Select</Button>}
        </div>
      </div>
    );
  });

  return (
    <div className="seg-selector" onClick={onClose}>
      <div className="seg-selector__card" onClick={(e) => e.stopPropagation()}>
        <div className="seg-selector__head">
          <div className="seg-selector__head-txt">
            <div className="armali-eyebrow">Link · Capex</div>
            <h3>Select an expense</h3>
            <p>Choose the Capex record this asset was purchased with. One reference per asset.</p>
          </div>
          <IconButton label="Close" variant="ghost" icon={<Icon n="x" size={18} />} onClick={onClose} />
        </div>

        {variant === "top" ? (
          <React.Fragment>
            <div className="seg-selector__filters">
              <div className="seg-selector__search">
                <Input placeholder="Search by name, code or supplier" value={search}
                  onChange={(e) => setF(setSearch)(e.target.value)} iconLeft={<Icon n="search" size={16} />} />
              </div>
              <Select value={cat} onChange={(e) => setF(setCat)(e.target.value)}
                options={[{ value: "all", label: "All categories" }, ...CATEGORIES.map((c) => ({ value: c, label: c }))]} />
              <Select value={status} onChange={(e) => setF(setStatus)(e.target.value)}
                options={[{ value: "all", label: "All statuses" }, ...STATUSES.map((s) => ({ value: s, label: s }))]} />
              <Select value={amount} onChange={(e) => setF(setAmount)(e.target.value)} options={AMOUNT_BRACKETS} />
            </div>
            <div className="seg-selector__strip">
              {countNode}
              <div className="seg-chips">
                {activeFilters.map((f) => (
                  <span key={f.key} className="seg-chip">{f.label}
                    <button className="seg-chip__x" onClick={f.clear} aria-label="Remove filter"><Icon n="x" size={11} /></button>
                  </span>
                ))}
              </div>
              {activeFilters.length > 0 && (
                <button className="seg-linkbtn" onClick={clearAll}><Icon n="rotate-ccw" size={14} /> Clear all</button>
              )}
            </div>
            <div className="seg-selector__scroll">
              <div className="seg-seltable">{tableHead}{tableBody}</div>
            </div>
          </React.Fragment>
        ) : (
          <div className="seg-selector__body">
            <div className="seg-selector__rail">
              <div className="seg-selector__search">
                <Input placeholder="Search expenses" value={search}
                  onChange={(e) => setF(setSearch)(e.target.value)} iconLeft={<Icon n="search" size={16} />} />
              </div>
              <div className="seg-facet">
                <span className="seg-facet__label">Category</span>
                <div className="seg-facet__opts">
                  <button className={"seg-facet__opt" + (cat === "all" ? " is-active" : "")} onClick={() => setF(setCat)("all")}>
                    All categories<span className="seg-facet__n">{CAPEX.length}</span>
                  </button>
                  {CATEGORIES.map((c) => (
                    <button key={c} className={"seg-facet__opt" + (cat === c ? " is-active" : "")} onClick={() => setF(setCat)(c)}>
                      {c}<span className="seg-facet__n">{CAPEX.filter((x) => x.category === c).length}</span>
                    </button>
                  ))}
                </div>
              </div>
              <div className="seg-facet">
                <span className="seg-facet__label">Status</span>
                <div className="seg-facet__opts">
                  <button className={"seg-facet__opt" + (status === "all" ? " is-active" : "")} onClick={() => setF(setStatus)("all")}>All statuses</button>
                  {STATUSES.map((s) => (
                    <button key={s} className={"seg-facet__opt" + (status === s ? " is-active" : "")} onClick={() => setF(setStatus)(s)}>
                      {s}<span className="seg-facet__n">{CAPEX.filter((x) => x.status === s).length}</span>
                    </button>
                  ))}
                </div>
              </div>
              <div className="seg-facet">
                <span className="seg-facet__label">Amount</span>
                <div className="seg-facet__opts">
                  {AMOUNT_BRACKETS.map((b) => (
                    <button key={b.value} className={"seg-facet__opt" + (amount === b.value ? " is-active" : "")} onClick={() => setF(setAmount)(b.value)}>{b.label}</button>
                  ))}
                </div>
              </div>
            </div>
            <div className="seg-selector__main">
              <div className="seg-selector__strip">
                {countNode}
                {activeFilters.length > 0 && (
                  <button className="seg-linkbtn" onClick={clearAll}><Icon n="rotate-ccw" size={14} /> Clear all</button>
                )}
              </div>
              <div className="seg-selector__scroll">
                <div className="seg-seltable">{tableHead}{tableBody}</div>
              </div>
            </div>
          </div>
        )}

        <div className="seg-selector__foot">
          <span className="seg-pageinfo">{total === 0 ? "No results" : <React.Fragment>Showing <b>{start + 1}–{Math.min(start + PAGE_SIZE, total)}</b> of <b>{total}</b></React.Fragment>}</span>
          <Pager page={safePage} pages={pages} onPage={setPage} />
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
        </div>
      </div>
    </div>
  );
}

// ── The asset edit popup (the "main" popup) ────────────────────
function AssetEditDialog({ asset, linked, behind, onBrowse, onClear, onClose }) {
  return (
    <div className={"seg-modal" + (behind ? " is-under" : "")} onClick={behind ? undefined : onClose}>
      <div className={"seg-modal__card is-asset" + (behind ? " is-behind" : "")} onClick={(e) => e.stopPropagation()} style={{ maxWidth: 540 }}>
        <div className="seg-modal__head">
          <h3>Edit asset</h3>
          <p>Update this durable object and link the expense it came from.</p>
        </div>

        <div className="seg-modal__grid">
          <Input label="Name" defaultValue={asset.name} />
          <div>
            <span className="seg-field-label">Status</span>
            <Select defaultValue={asset.status} options={["In use", "On loan", "In storage", "Disposed"]} />
          </div>
          <div>
            <span className="seg-field-label">Category</span>
            <Select defaultValue={asset.category} options={["Furniture", "Kitchen", "Electronics", "Appliances", "Garden", "Decor", "Tools"]} />
          </div>
          <Input label="Location" defaultValue={asset.location} iconLeft={<Icon n="map-pin" size={15} />} />
        </div>
        <Input label="Model name" defaultValue={asset.model} />

        <div>
          <span className="seg-field-label">Linked expense · Capex</span>
          <RefControl value={linked} onBrowse={onBrowse} onClear={onClear} />
        </div>

        <div className="seg-modal__foot">
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
          <Button variant="primary" iconLeft={<Icon n="check" size={17} />} onClick={onClose}>Save changes</Button>
        </div>
      </div>
    </div>
  );
}

// ── Assets table context behind the editor ─────────────────────
function AssetsContext({ linked }) {
  return (
    <div className="seg-users">
      <div className="seg-asset__bar">
        <div>
          <div className="armali-eyebrow">Assets</div>
          <h2>Durable objects</h2>
          <p>Furniture, appliances and equipment — each can reference the expense it came from.</p>
        </div>
        <Button variant="primary" iconLeft={<Icon n="plus" size={17} />}>New asset</Button>
      </div>
      <div className="seg-tablecard">
        <div className="seg-table seg-atable">
          <div className="seg-thead">
            <span>Asset</span><span>Status</span><span>Category</span><span>Location</span><span>Linked expense</span><span style={{ textAlign: "right" }}>Edit</span>
          </div>
          {ASSETS.map((a) => {
            const link = a.editing ? (linked ? `${linked.id} · ${fmtEur(linked.amount)}` : null) : a.link;
            return (
              <div key={a.id} className="seg-trow">
                <div className="seg-aname"><strong>{a.name}</strong><em>{a.id}</em></div>
                <span><Badge tone={ASSET_STATUS_TONE[a.status]} dot>{a.status}</Badge></span>
                <span className="seg-acell">{a.category}</span>
                <span className="seg-acell">{a.location}</span>
                <span className={"seg-aref" + (link ? "" : " is-empty")}>
                  <Icon n={link ? "receipt-text" : "minus"} size={14} />{link || "Not linked"}
                </span>
                <div className="seg-trow__act" style={{ justifyContent: "flex-end" }}>
                  <IconButton size="sm" label="Edit" icon={<Icon n="pencil" size={15} />} />
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}

// ── Full flow ──────────────────────────────────────────────────
function Flow({ selectorVariant = "top", startSelector = false }) {
  const asset = ASSETS[0];
  const [linked, setLinked] = React.useState(null);
  const [selectorOpen, setSelectorOpen] = React.useState(startSelector);

  return (
    <div className="seg-screen">
      <div className="seg-page" style={{ paddingTop: "var(--space-7)" }}>
        <div className="seg-page__inner">
          <AssetsContext linked={linked} />
        </div>
      </div>
      <AssetEditDialog asset={asset} linked={linked} behind={selectorOpen}
        onBrowse={() => setSelectorOpen(true)} onClear={() => setLinked(null)} onClose={() => {}} />
      {selectorOpen && (
        <CapexSelector variant={selectorVariant} currentId={linked && linked.id}
          onClose={() => setSelectorOpen(false)}
          onSelect={(c) => { setLinked(c); setSelectorOpen(false); }} />
      )}
    </div>
  );
}

const EntitySelectorEditor = () => <Flow selectorVariant="top" startSelector={false} />;
const EntitySelectorTop = () => <Flow selectorVariant="top" startSelector={true} />;
const EntitySelectorRail = () => <Flow selectorVariant="rail" startSelector={true} />;

Object.assign(window, { EntitySelectorEditor, EntitySelectorTop, EntitySelectorRail });
})();
