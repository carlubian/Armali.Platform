/* global React */
// Calendar — manual daily-note editor. A URL-aware popup (over the calendar
// view) for creating, editing and deleting Calendar-owned daily notes.
//
//   • New notes default to today / the selected day and Private visibility.
//   • Body required (≤ 4,000 chars); optional title (≤ 200 chars).
//   • Only the creator may change visibility; any user may edit a public note.
//   • Notes attach to one civil date — no ranges, recurrence, alarms or links.
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Button, IconButton, Input } = A;
const Icon = window.SegIcon;
const C = window.CalData;

function VisibilitySeg({ value, onChange }) {
  const opts = [
    { v: "Private", icon: "lock", priv: true },
    { v: "Public", icon: "users", priv: false },
  ];
  return (
    <div className="cal-visseg" role="group" aria-label="Visibility">
      {opts.map((o) => (
        <button key={o.v} type="button"
          className={"cal-visseg__btn" + (value === o.v ? " is-active" : "") + (o.priv ? " is-private" : "")}
          onClick={() => onChange(o.v)}>
          <Icon n={o.icon} size={15} /> {o.v}
        </button>
      ))}
    </div>
  );
}

function NoteEditor({ mode = "new", note, date, onClose }) {
  const editing = mode === "edit";
  const initialDate = editing ? note.start : (date || C.TODAY);
  const [title, setTitle] = React.useState(editing ? (note.title || "") : "");
  const [body, setBody] = React.useState(editing ? (note.supporting || "") : "");
  const [vis, setVis] = React.useState(editing ? note.visibility : "Private");

  const dateStr = C.ymd(initialDate);
  const valid = body.trim().length > 0 && body.length <= 4000 && title.length <= 200;

  return (
    <div className="seg-modal cal-notedlg" onClick={onClose}>
      <div className="seg-modal__card" onClick={(e) => e.stopPropagation()}>
        <div className="cal-notedlg__head">
          <div>
            <div className="armali-eyebrow" style={{ color: "var(--gold-600)" }}>Daily note</div>
            <h3>{editing ? "Edit note" : "New note"}</h3>
            <p>{editing
              ? "Update this note. Notes attach to a single day."
              : "A private or shared note pinned to one day. Only Calendar owns these."}</p>
          </div>
          <IconButton label="Close" variant="ghost" icon={<Icon n="x" size={18} />} onClick={onClose} />
        </div>

        <div className="cal-notedlg__body">
          <div className="cal-grid2">
            <div className="cal-field">
              <label className="cal-field__label">Date <span className="cal-field__req">*</span></label>
              <Input type="date" defaultValue={dateStr} iconLeft={<Icon n="calendar" size={16} />} />
            </div>
            <div className="cal-field">
              <span className="cal-field__label">Visibility</span>
              <VisibilitySeg value={vis} onChange={setVis} />
              <span className="cal-vishint">
                {vis === "Private"
                  ? "Only you can see this — hidden even from administrators."
                  : "Any household member can see and edit this note."}
              </span>
            </div>
          </div>

          <div className="cal-field">
            <label className="cal-field__label">Title <span className="cal-field__opt">· optional</span></label>
            <Input placeholder="A short label for the day" defaultValue={title}
              maxLength={200} onChange={(e) => setTitle(e.target.value)} />
          </div>

          <div className="cal-field">
            <label className="cal-field__label">Note <span className="cal-field__req">*</span></label>
            <textarea className="cal-textarea" maxLength={4000} placeholder="Write the note…"
              value={body} onChange={(e) => setBody(e.target.value)} />
            <span className="cal-field__count">{body.length} / 4,000</span>
          </div>
        </div>

        <div className="cal-notedlg__foot">
          {editing && (
            <Button variant="danger" iconLeft={<Icon n="trash-2" size={16} />} onClick={onClose}>Delete</Button>
          )}
          <div className="cal-notedlg__foot-right">
            <Button variant="ghost" onClick={onClose}>Cancel</Button>
            <Button variant="primary" disabled={!valid} iconLeft={<Icon n="check" size={17} />} onClick={onClose}>
              {editing ? "Save changes" : "Save note"}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { CalNoteEditor: NoteEditor });
})();
