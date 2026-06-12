/* global React */
(() => {
const A = window.ProjectArmaliDesignSystem_ce5329;
const { Avatar, Badge, Button, IconButton, Tooltip, Input, Select, Switch } = A;
const Icon = window.SegIcon;

const roleTone = (r) => (r === "Admin" ? "azure" : "neutral");

// ── Contained create / edit user dialog (lives inside the artboard) ──
function UserDialog({ mode, user, onClose }) {
  const editing = mode === "edit";
  return (
    <div className="seg-modal" onClick={onClose}>
      <div className="seg-modal__card" onClick={(e) => e.stopPropagation()}>
        <div className="seg-modal__head">
          <h3>{editing ? "Edit user" : "New user"}</h3>
          <p>{editing ? "Update this household member's details." : "Add a member to your household. They sign in with a username and password."}</p>
        </div>

        <div className="seg-modal__grid">
          <Input label="Display name" defaultValue={editing ? user.name : ""} placeholder="Full name" />
          <Input label="Username" defaultValue={editing ? user.username : ""} placeholder="username" iconLeft={<Icon n="at-sign" size={16} />} />
        </div>

        <div>
          <span className="seg-field-label">Role</span>
          <Select defaultValue={editing ? user.role : "User"} options={["User", "Admin"]} />
        </div>

        {!editing && (
          <Input label="Temporary password" type="password" placeholder="••••••••••"
            hint="The member can change this from their profile after signing in." />
        )}

        <Switch label="Account active" defaultChecked={editing ? user.status === "active" : true} />

        <div className="seg-modal__foot">
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
          <Button variant="primary" onClick={onClose} iconLeft={<Icon n={editing ? "check" : "user-plus"} size={17} />}>
            {editing ? "Save changes" : "Create user"}
          </Button>
        </div>
      </div>
    </div>
  );
}

function UsersHeader({ onNew }) {
  const users = window.SEG_USERS;
  const active = users.filter((u) => u.status === "active").length;
  const admins = users.filter((u) => u.role === "Admin").length;
  return (
    <div className="seg-users__bar">
      <div>
        <div className="armali-eyebrow">Administration</div>
        <h2>Household users</h2>
        <p>Create members, set roles, and activate or deactivate accounts.</p>
      </div>
      <div className="seg-users__stats">
        <div className="seg-stat-pill"><strong>{users.length}</strong><span>Members</span></div>
        <div className="seg-stat-pill"><strong>{active}</strong><span>Active</span></div>
        <div className="seg-stat-pill"><strong>{admins}</strong><span>Admins</span></div>
        <Button variant="primary" iconLeft={<Icon n="user-plus" size={17} />} onClick={onNew}>New user</Button>
      </div>
    </div>
  );
}

// ── Variant A · Table ──────────────────────────────────────────
function UserMgmtTable() {
  const users = window.SEG_USERS;
  const [dialog, setDialog] = React.useState(null);
  return (
    <div className="seg-screen">
      {window.SegShellTopBar ? <window.SegShellTopBar eyebrow="Administration" title="Users" /> : null}
      <div className="seg-page">
        <div className="seg-page__inner">
          <div className="seg-users">
            <UsersHeader onNew={() => setDialog({ mode: "new" })} />
            <div className="seg-tablecard">
              <div className="seg-table">
                <div className="seg-thead">
                  <span>Member</span><span>Role</span><span>Status</span><span>Last active</span><span style={{ textAlign: "right" }}>Manage</span>
                </div>
                {users.map((u) => (
                  <div key={u.username} className={"seg-trow" + (u.status === "inactive" ? " is-inactive" : "")}>
                    <div className="seg-uname">
                      <Avatar name={u.name} size="sm" status={u.status === "active" ? "online" : undefined} />
                      <div><strong>{u.name}</strong><em>@{u.username}</em></div>
                    </div>
                    <span><Badge tone={roleTone(u.role)}>{u.role}</Badge></span>
                    <span>
                      {u.status === "active"
                        ? <Badge tone="success" dot>Active</Badge>
                        : <Badge tone="neutral" dot>Inactive</Badge>}
                    </span>
                    <span className="seg-uname"><em style={{ fontSize: "var(--text-sm)", color: "var(--text-secondary)" }}>{u.last}</em></span>
                    <div className="seg-trow__act">
                      <Tooltip label="Edit" side="top"><IconButton size="sm" label="Edit" icon={<Icon n="pencil" size={15} />} onClick={() => setDialog({ mode: "edit", user: u })} /></Tooltip>
                      <Tooltip label={u.status === "active" ? "Deactivate" : "Activate"} side="top">
                        <IconButton size="sm" label="Toggle active" icon={<Icon n={u.status === "active" ? "user-x" : "user-check"} size={15} />} />
                      </Tooltip>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
      {dialog && <UserDialog mode={dialog.mode} user={dialog.user} onClose={() => setDialog(null)} />}
    </div>
  );
}

// ── Variant B · Cards ──────────────────────────────────────────
function UserMgmtCards() {
  const users = window.SEG_USERS;
  const [dialog, setDialog] = React.useState(null);
  return (
    <div className="seg-screen">
      {window.SegShellTopBar ? <window.SegShellTopBar eyebrow="Administration" title="Users" /> : null}
      <div className="seg-page">
        <div className="seg-page__inner">
          <div className="seg-users">
            <UsersHeader onNew={() => setDialog({ mode: "new" })} />
            <div className="seg-ucards">
              {users.map((u) => (
                <div key={u.username} className={"seg-ucard" + (u.status === "inactive" ? " is-inactive" : "")}>
                  <div className="seg-ucard__top">
                    <Avatar name={u.name} size="lg" status={u.status === "active" ? "online" : undefined} />
                    <div className="seg-ucard__id">
                      <strong>{u.name}</strong><em>@{u.username}</em>
                    </div>
                  </div>
                  <div className="seg-ucard__meta">
                    <Badge tone={roleTone(u.role)}>{u.role}</Badge>
                    {u.status === "active" ? <Badge tone="success" dot>Active</Badge> : <Badge tone="neutral" dot>Inactive</Badge>}
                  </div>
                  <div className="seg-ucard__meta" style={{ color: "var(--text-muted)", fontSize: "var(--text-sm)", gap: "0.4em" }}>
                    <Icon n="clock" size={14} /> Last active {u.last}
                  </div>
                  <div className="seg-ucard__foot">
                    <Button size="sm" variant="outline" iconLeft={<Icon n="pencil" size={15} />} onClick={() => setDialog({ mode: "edit", user: u })}>Edit</Button>
                    <Button size="sm" variant="ghost" iconLeft={<Icon n={u.status === "active" ? "user-x" : "user-check"} size={15} />}>
                      {u.status === "active" ? "Deactivate" : "Activate"}
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
      {dialog && <UserDialog mode={dialog.mode} user={dialog.user} onClose={() => setDialog(null)} />}
    </div>
  );
}

Object.assign(window, { UserMgmtTable, UserMgmtCards });
})();
