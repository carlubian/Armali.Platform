/* global React */
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Avatar, IconButton, Tooltip, Badge } = A;
const Icon = window.SegIcon;
const MODULES = window.SEG_MODULES;

// Shared admin / module top bar with a "return to launcher" action,
// the eyebrow+title block, and the current-user avatar.
function ShellTopBar({ eyebrow, title, withBack = true, currentUser = "Marina Velasco" }) {
  return (
    <header className="seg-topbar">
      <div className="seg-topbar__left">
        {withBack && (
          <button className="seg-back" type="button">
            <Icon n="arrow-left" size={16} /> Launcher
          </button>
        )}
        {withBack && <span className="seg-divider" />}
        <div className="seg-topbar__title">
          <div className="armali-eyebrow">{eyebrow}</div>
          <h1>{title}</h1>
        </div>
      </div>
      <div className="seg-topbar__right">
        <Tooltip label="Help" side="bottom"><IconButton label="Help" icon={<Icon n="life-buoy" />} /></Tooltip>
        <span className="seg-divider" />
        <Avatar name={currentUser} status="online" />
      </div>
    </header>
  );
}
window.SegShellTopBar = ShellTopBar;

function ModuleCard({ m }) {
  return (
    <button type="button" className={"seg-mod seg-mod--" + m.tone}>
      {m.attn && (
        <Tooltip label="Needs your attention" side="left">
          <span className="seg-mod__attn" aria-label="Needs your attention" />
        </Tooltip>
      )}
      <span className="seg-mod__icon"><Icon n={m.icon} size={24} /></span>
      <span className="seg-mod__name">{m.name}</span>
      <span className="seg-mod__desc">{m.desc}</span>
      <span className="seg-mod__foot">Open <Icon n="arrow-right" size={13} /></span>
    </button>
  );
}

function Launcher() {
  return (
    <div className="seg-screen seg-launcher armali-aurora">
      {/* Minimal launcher chrome — the launcher is an entry point, not a dashboard */}
      <header className="seg-topbar">
        <div className="seg-topbar__left">
          {window.SegBrandMark ? <window.SegBrandMark /> : null}
        </div>
        <div className="seg-topbar__right">
          <Tooltip label="My profile" side="bottom"><IconButton label="Profile" icon={<Icon n="user-round" />} /></Tooltip>
          <Tooltip label="Sign out" side="bottom"><IconButton label="Sign out" icon={<Icon n="log-out" />} /></Tooltip>
          <span className="seg-divider" />
          <Avatar name="Marina Velasco" status="online" />
        </div>
      </header>

      <div className="seg-launcher__head">
        <div>
          <div className="armali-eyebrow">Good afternoon, Marina</div>
          <h2>Choose a module</h2>
          <p>Open a tool to manage that part of your home. Return here to switch.</p>
        </div>
        <Badge tone="neutral" dot>12 modules · 1 read view</Badge>
      </div>

      <div className="seg-launcher__scroll">
        <div className="seg-modules">
          {MODULES.map((m) => <ModuleCard key={m.key} m={m} />)}
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { Launcher });
})();
