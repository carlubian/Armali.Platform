/* global React */
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Input, Button, Card } = A;
const Icon = window.SegIcon;

function BrandMark({ lg }) {
  const uid = React.useId().replace(/[^a-z0-9]/gi, '');
  return (
    <div className={"seg-brand" + (lg ? " seg-brand--lg" : "")}>
      <span className="seg-brand__mark">
        <svg width="100%" height="100%" viewBox="0 0 100 100" fill="none" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <linearGradient id={`arch${uid}`} x1="50" y1="18" x2="50" y2="90" gradientUnits="userSpaceOnUse">
              <stop offset="0%" stopColor="#1F5F8E"/>
              <stop offset="55%" stopColor="#3298B0"/>
              <stop offset="100%" stopColor="#52BEC8"/>
            </linearGradient>
          </defs>

          {/* Arch */}
          <path d="M 21 91 L 21 52 A 29 29 0 0 0 79 52 L 79 91"
            fill="none" stroke={`url(#arch${uid})`} strokeWidth="8" strokeLinecap="round"/>

          {/* Sun (behind house) */}
          <circle cx="68" cy="46" r="12" fill="#E8A828"/>

          {/* House */}
          <path d="M 37 84 L 37 63 L 51 51 L 65 63 L 65 84 Z" fill="#F3EDE3"/>

          {/* Windows 2×2 */}
          <rect x="41" y="66" width="7.5" height="5.5" rx="1.5" fill="#C29040"/>
          <rect x="51.5" y="66" width="7.5" height="5.5" rx="1.5" fill="#C29040"/>
          <rect x="41" y="74" width="7.5" height="5.5" rx="1.5" fill="#C29040"/>
          <rect x="51.5" y="74" width="7.5" height="5.5" rx="1.5" fill="#C29040"/>

          {/* Plant stem */}
          <line x1="31" y1="82" x2="31" y2="49" stroke="#59BCBA" strokeWidth="2.5" strokeLinecap="round"/>
          {/* Plant leaves */}
          <ellipse cx="24" cy="72" rx="7.5" ry="3.2" transform="rotate(-40 24 72)" fill="#59BCBA"/>
          <ellipse cx="38" cy="66" rx="7.5" ry="3.2" transform="rotate(35 38 66)" fill="#59BCBA"/>
          <ellipse cx="24" cy="61" rx="7.5" ry="3.2" transform="rotate(-35 24 61)" fill="#59BCBA"/>
          <ellipse cx="37" cy="55" rx="7.5" ry="3.2" transform="rotate(30 37 55)" fill="#59BCBA"/>
          <ellipse cx="29" cy="50" rx="5.5" ry="2.8" transform="rotate(-15 29 50)" fill="#59BCBA"/>

          {/* Wave */}
          <path d="M 6 80 Q 22 68 38 78 Q 52 87 62 76 Q 69 68 76 73 C 85 79 83 95 72 95 L 6 95 Q 0 95 0 88 Q 0 80 6 80 Z" fill="#52BEC8"/>
        </svg>
      </span>
      <span className="seg-brand__name">Segaris</span>
    </div>
  );
}
window.SegBrandMark = BrandMark;

// ── A · Centered glass card over the aurora ────────────────────
function LoginCentered() {
  return (
    <div className="seg-screen seg-login armali-aurora">
      <form className="seg-login__card" onSubmit={(e) => e.preventDefault()}>
        <BrandMark lg />
        <h2 className="seg-login__title">Welcome home</h2>
        <p className="seg-login__sub">Sign in to your household to open Segaris.</p>

        <div className="seg-login__fields">
          <Input label="Username" placeholder="marina" defaultValue="marina"
            iconLeft={<Icon n="user-round" size={17} />} />
          <Input label="Password" type="password" placeholder="••••••••••" defaultValue="passphrase"
            iconLeft={<Icon n="lock" size={17} />} />
        </div>

        <Button type="submit" block size="lg" iconRight={<Icon n="arrow-right" size={18} />}>Sign in</Button>

        <p className="seg-login__foot">Accounts are created by your household administrator.</p>
      </form>
    </div>
  );
}

// ── B · Split panel — aurora aside + solid form pane ───────────
function LoginSplit() {
  return (
    <div className="seg-screen seg-login seg-login--split">
      <div className="seg-login__split">
        <aside className="seg-login__aside armali-aurora">
          <BrandMark lg />
          <div className="seg-login__aside-quote">A calm place for everything your home keeps track of.</div>
          <div className="seg-login__aside-meta">
            <Icon n="map-pin" size={15} /> One household · Europe/Madrid · EUR
          </div>
        </aside>

        <div className="seg-login__pane">
          <form className="seg-login__pane-inner" onSubmit={(e) => e.preventDefault()}>
            <div className="armali-eyebrow" style={{ marginBottom: 4 }}>Sign in</div>
            <h2 className="seg-login__title" style={{ marginTop: 0 }}>Welcome back</h2>
            <p className="seg-login__sub">Enter your household credentials to continue.</p>

            <div className="seg-login__fields">
              <Input label="Username" placeholder="marina" defaultValue="marina"
                iconLeft={<Icon n="user-round" size={17} />} />
              <Input label="Password" type="password" placeholder="••••••••••" defaultValue="passphrase"
                iconLeft={<Icon n="lock" size={17} />} />
            </div>

            <Button type="submit" block size="lg" iconRight={<Icon n="arrow-right" size={18} />}>Sign in</Button>
            <p className="seg-login__foot">Accounts are created by your household administrator.</p>
          </form>
        </div>
      </div>
    </div>
  );
}

Object.assign(window, { LoginCentered, LoginSplit });
})();
