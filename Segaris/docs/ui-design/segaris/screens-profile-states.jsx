/* global React */
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Avatar, Badge, Button, Input, Select, IconButton, Tooltip } = A;
const Icon = window.SegIcon;

// ── My profile (self-service) ──────────────────────────────────
function Profile() {
  return (
    <div className="seg-screen">
      {window.SegShellTopBar ? <window.SegShellTopBar eyebrow="Account" title="My profile" /> : null}
      <div className="seg-page">
        <div className="seg-page__inner">
          <div className="seg-profile">
            <aside className="seg-profile__side">
              <div className="seg-profile__photo">
                <Avatar name="Marina Velasco" size="lg" status="online" />
              </div>
              <div>
                <div className="seg-profile__name">Marina Velasco</div>
                <div style={{ color: "var(--text-muted)", fontSize: "var(--text-sm)", fontWeight: 600 }}>@marina</div>
              </div>
              <div className="seg-profile__role">
                <Badge tone="azure">Admin</Badge>
                <Badge tone="success" dot>Active</Badge>
              </div>
              <Button variant="outline" size="sm" iconLeft={<Icon n="image-up" size={16} />}>Change photo</Button>
            </aside>

            <div className="seg-profile__main">
              <section className="seg-card">
                <div className="seg-card__title">Profile details</div>
                <div className="seg-card__sub">Your display name and language are visible across the household.</div>
                <div className="seg-form-grid">
                  <Input label="Display name" defaultValue="Marina Velasco" />
                  <Input label="Username" defaultValue="marina" disabled hint="Usernames are set by an administrator." />
                  <div>
                    <span className="seg-field-label">Interface language</span>
                    <Select defaultValue="en" options={[{ value: "en", label: "English (en-GB)" }, { value: "es", label: "Español (es-ES)" }]} />
                  </div>
                  <Input label="Email for receipts" type="email" defaultValue="marina@home.local" iconLeft={<Icon n="mail" size={16} />} />
                </div>
              </section>

              <section className="seg-card">
                <div className="seg-card__title">Password</div>
                <div className="seg-card__sub">Choose a strong passphrase. You'll stay signed in on this device.</div>
                <div className="seg-form-grid">
                  <Input label="Current password" type="password" placeholder="••••••••••" iconLeft={<Icon n="lock" size={16} />} />
                  <div />
                  <Input label="New password" type="password" placeholder="••••••••••" />
                  <Input label="Confirm new password" type="password" placeholder="••••••••••" />
                </div>
              </section>

              <div className="seg-save-bar">
                <span>Last saved 12 Jun 2026 · changes apply immediately.</span>
                <div style={{ display: "flex", gap: "var(--space-3)" }}>
                  <Button variant="ghost">Discard</Button>
                  <Button variant="primary" iconLeft={<Icon n="check" size={17} />}>Save changes</Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// ── Service unavailable (backend unreachable) ──────────────────
function ServiceUnavailable() {
  return (
    <div className="seg-screen seg-state armali-aurora">
      <div className="seg-state__card">
        <span className="seg-state__icon seg-state__icon--danger"><Icon n="cloud-off" size={36} /></span>
        <div className="armali-eyebrow">Service unavailable</div>
        <h2 className="seg-state__title">Segaris can't reach the household server</h2>
        <p className="seg-state__body">Your modules are paused until the connection returns. Nothing has been lost — this view will refresh once the server is back.</p>
        <div className="seg-state__actions">
          <Button variant="primary" iconLeft={<Icon n="refresh-cw" size={17} />}>Try again</Button>
          <Button variant="outline" iconLeft={<Icon n="life-buoy" size={17} />}>Get help</Button>
        </div>
        <div className="seg-state__meta">Last reached 4 minutes ago · ref SVR-503</div>
      </div>
    </div>
  );
}

// ── Not found (404) ────────────────────────────────────────────
function NotFound() {
  return (
    <div className="seg-screen seg-state armali-aurora">
      <div className="seg-state__card">
        <span className="seg-state__icon seg-state__icon--gold"><Icon n="compass" size={36} /></span>
        <div className="seg-state__code">404</div>
        <h2 className="seg-state__title">We can't find that page</h2>
        <p className="seg-state__body">The link may be old, or the module it pointed to isn't installed in this household. Head back to the launcher to choose a module.</p>
        <div className="seg-state__actions">
          <Button variant="primary" iconLeft={<Icon n="arrow-left" size={17} />}>Return to launcher</Button>
        </div>
        <div className="seg-state__meta">/modules/unknown</div>
      </div>
    </div>
  );
}

Object.assign(window, { Profile, ServiceUnavailable, NotFound });
})();
