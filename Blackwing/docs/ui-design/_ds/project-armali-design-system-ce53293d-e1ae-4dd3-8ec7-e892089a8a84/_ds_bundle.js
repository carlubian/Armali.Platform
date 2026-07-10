/* @ds-bundle: {"format":3,"namespace":"ProjectArmaliDesignSystem_ce5329","components":[{"name":"Button","sourcePath":"components/buttons/Button.jsx"},{"name":"IconButton","sourcePath":"components/buttons/IconButton.jsx"},{"name":"Spinner","sourcePath":"components/feedback/Spinner.jsx"},{"name":"Toast","sourcePath":"components/feedback/Toast.jsx"},{"name":"Tooltip","sourcePath":"components/feedback/Tooltip.jsx"},{"name":"Checkbox","sourcePath":"components/forms/Checkbox.jsx"},{"name":"Input","sourcePath":"components/forms/Input.jsx"},{"name":"Select","sourcePath":"components/forms/Select.jsx"},{"name":"Switch","sourcePath":"components/forms/Switch.jsx"},{"name":"Tabs","sourcePath":"components/navigation/Tabs.jsx"},{"name":"Dialog","sourcePath":"components/overlay/Dialog.jsx"},{"name":"Avatar","sourcePath":"components/surfaces/Avatar.jsx"},{"name":"Badge","sourcePath":"components/surfaces/Badge.jsx"},{"name":"Card","sourcePath":"components/surfaces/Card.jsx"}],"sourceHashes":{"components/buttons/Button.jsx":"08dc6b63de0e","components/buttons/IconButton.jsx":"38010fb2960a","components/feedback/Spinner.jsx":"f123844b5613","components/feedback/Toast.jsx":"cbcee2e397e9","components/feedback/Tooltip.jsx":"f9ae131d8d7b","components/forms/Checkbox.jsx":"f703f495a0e1","components/forms/Input.jsx":"d3cd36fcc720","components/forms/Select.jsx":"af4b39f0e2d9","components/forms/Switch.jsx":"f20dc4f1b019","components/navigation/Tabs.jsx":"e99429397c21","components/overlay/Dialog.jsx":"1caa5454cd38","components/surfaces/Avatar.jsx":"03b36ca8776c","components/surfaces/Badge.jsx":"76bbcb8432a5","components/surfaces/Card.jsx":"b3de93d1fa84","ui_kits/console/App.jsx":"e81c77bf3d85","ui_kits/console/LoginScreen.jsx":"8a1243b87a6a","ui_kits/console/OverviewScreen.jsx":"930e1f58f970","ui_kits/console/RegionsScreen.jsx":"abe827b5aa51","ui_kits/console/SettingsScreen.jsx":"d5eb43cd465d","ui_kits/console/Sidebar.jsx":"09f5a23ebf4c","ui_kits/console/TopBar.jsx":"867d9c605b80","ui_kits/console/data.jsx":"8ef3bae7b677"},"inlinedExternals":[],"unexposedExports":[]} */

(() => {

const __ds_ns = (window.ProjectArmaliDesignSystem_ce5329 = window.ProjectArmaliDesignSystem_ce5329 || {});

const __ds_scope = {};

(__ds_ns.__errors = __ds_ns.__errors || []);

// components/buttons/Button.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const STYLE_ID = "arm-button-css";
function ensureStyle() {
  if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
  const el = document.createElement("style");
  el.id = STYLE_ID;
  el.textContent = `
  .arm-btn {
    --_bg: var(--accent);
    --_fg: var(--text-on-accent);
    --_glow: var(--glow-aqua);
    display: inline-flex; align-items: center; justify-content: center; gap: 0.5em;
    font-family: var(--font-display); font-weight: 600;
    letter-spacing: 0.01em; line-height: 1; white-space: nowrap;
    border: 1px solid transparent; border-radius: var(--radius-control);
    padding: 0.72em 1.25em; font-size: var(--text-base);
    cursor: pointer; user-select: none; position: relative;
    background: var(--_bg); color: var(--_fg);
    box-shadow: var(--glow-soft);
    transition: transform var(--dur-fast) var(--ease-out),
                box-shadow var(--dur-base) var(--ease-out),
                background var(--dur-base) var(--ease-soft),
                filter var(--dur-base) var(--ease-soft);
  }
  .arm-btn:hover { box-shadow: var(--_glow); filter: brightness(1.04); }
  .arm-btn:active { transform: translateY(1px) scale(0.985); filter: brightness(0.97); }
  .arm-btn:focus-visible { outline: none; box-shadow: var(--ring), var(--_glow); }
  .arm-btn[disabled] { cursor: not-allowed; opacity: 0.5; box-shadow: none; filter: none; transform: none; }

  .arm-btn--sm { font-size: var(--text-sm); padding: 0.55em 1em; border-radius: var(--radius-sm); }
  .arm-btn--lg { font-size: var(--text-lg); padding: 0.85em 1.6em; }
  .arm-btn--block { display: flex; width: 100%; }

  /* primary (aqua) is default */
  .arm-btn--secondary { --_bg: var(--accent-secondary); --_fg: #3a2c08; --_glow: var(--glow-gold); }
  .arm-btn--action { --_bg: var(--action); --_fg: var(--text-on-accent); --_glow: var(--glow-azure); }
  .arm-btn--danger { --_bg: var(--danger); --_fg: var(--text-on-accent); --_glow: var(--glow-danger); }

  .arm-btn--outline {
    background: var(--surface-frost); color: var(--accent-press);
    border-color: color-mix(in srgb, var(--accent) 45%, transparent);
    -webkit-backdrop-filter: var(--blur-chip); backdrop-filter: var(--blur-chip);
    box-shadow: none;
  }
  .arm-btn--outline:hover { background: var(--accent-soft); box-shadow: var(--glow-aqua); filter: none; }

  .arm-btn--ghost { background: transparent; color: var(--text-secondary); box-shadow: none; }
  .arm-btn--ghost:hover { background: var(--surface-sunken); color: var(--ink-900); box-shadow: none; filter: none; }
  `;
  document.head.appendChild(el);
}
function Button({
  children,
  variant = "primary",
  size = "md",
  block = false,
  iconLeft = null,
  iconRight = null,
  className = "",
  ...rest
}) {
  ensureStyle();
  const cls = ["arm-btn", variant !== "primary" ? `arm-btn--${variant}` : "", size !== "md" ? `arm-btn--${size}` : "", block ? "arm-btn--block" : "", className].filter(Boolean).join(" ");
  return /*#__PURE__*/React.createElement("button", _extends({
    className: cls
  }, rest), iconLeft, children != null && /*#__PURE__*/React.createElement("span", null, children), iconRight);
}
Object.assign(__ds_scope, { Button });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/buttons/Button.jsx", error: String((e && e.message) || e) }); }

// components/buttons/IconButton.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const STYLE_ID = "arm-iconbutton-css";
function ensureStyle() {
  if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
  const el = document.createElement("style");
  el.id = STYLE_ID;
  el.textContent = `
  .arm-iconbtn {
    display: inline-flex; align-items: center; justify-content: center;
    width: 40px; height: 40px; padding: 0; flex: none;
    border-radius: var(--radius-control); border: 1px solid transparent;
    background: var(--surface-frost); color: var(--text-secondary);
    -webkit-backdrop-filter: var(--blur-chip); backdrop-filter: var(--blur-chip);
    cursor: pointer; position: relative;
    transition: transform var(--dur-fast) var(--ease-out),
                box-shadow var(--dur-base) var(--ease-out),
                background var(--dur-base) var(--ease-soft), color var(--dur-base) var(--ease-soft);
  }
  .arm-iconbtn:hover { background: var(--accent-soft); color: var(--accent-press); box-shadow: var(--glow-aqua); }
  .arm-iconbtn:active { transform: scale(0.92); }
  .arm-iconbtn:focus-visible { outline: none; box-shadow: var(--ring); }
  .arm-iconbtn[disabled] { cursor: not-allowed; opacity: 0.45; box-shadow: none; }
  .arm-iconbtn--sm { width: 32px; height: 32px; border-radius: var(--radius-sm); }
  .arm-iconbtn--lg { width: 48px; height: 48px; }
  .arm-iconbtn--solid { background: var(--accent); color: var(--text-on-accent); }
  .arm-iconbtn--solid:hover { color: var(--text-on-accent); filter: brightness(1.05); }
  .arm-iconbtn--bare { background: transparent; -webkit-backdrop-filter: none; backdrop-filter: none; }
  .arm-iconbtn--bare:hover { background: var(--surface-sunken); box-shadow: none; }
  `;
  document.head.appendChild(el);
}
function IconButton({
  children,
  icon,
  size = "md",
  variant = "frost",
  label,
  className = "",
  ...rest
}) {
  ensureStyle();
  const cls = ["arm-iconbtn", size !== "md" ? `arm-iconbtn--${size}` : "", variant !== "frost" ? `arm-iconbtn--${variant}` : "", className].filter(Boolean).join(" ");
  return /*#__PURE__*/React.createElement("button", _extends({
    className: cls,
    "aria-label": label,
    title: label
  }, rest), icon || children);
}
Object.assign(__ds_scope, { IconButton });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/buttons/IconButton.jsx", error: String((e && e.message) || e) }); }

// components/feedback/Spinner.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const STYLE_ID = "arm-spinner-css";
function ensureStyle() {
  if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
  const el = document.createElement("style");
  el.id = STYLE_ID;
  el.textContent = `
  .arm-spinner { display: inline-block; width: var(--_s, 28px); height: var(--_s, 28px); }
  .arm-spinner svg { width: 100%; height: 100%; animation: arm-spin 900ms linear infinite; }
  .arm-spinner circle { fill: none; stroke-width: 3; stroke-linecap: round; }
  .arm-spinner__track { stroke: var(--border-default); }
  .arm-spinner__head { stroke: var(--accent); stroke-dasharray: 64; stroke-dashoffset: 44; }
  @keyframes arm-spin { to { transform: rotate(360deg); } }
  `;
  document.head.appendChild(el);
}
function Spinner({
  size = 28,
  label = "Loading",
  className = "",
  ...rest
}) {
  ensureStyle();
  return /*#__PURE__*/React.createElement("span", _extends({
    className: ["arm-spinner", className].filter(Boolean).join(" "),
    role: "status",
    "aria-label": label,
    style: {
      "--_s": typeof size === "number" ? `${size}px` : size
    }
  }, rest), /*#__PURE__*/React.createElement("svg", {
    viewBox: "0 0 24 24"
  }, /*#__PURE__*/React.createElement("circle", {
    className: "arm-spinner__track",
    cx: "12",
    cy: "12",
    r: "9"
  }), /*#__PURE__*/React.createElement("circle", {
    className: "arm-spinner__head",
    cx: "12",
    cy: "12",
    r: "9"
  })));
}
Object.assign(__ds_scope, { Spinner });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/Spinner.jsx", error: String((e && e.message) || e) }); }

// components/feedback/Toast.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const STYLE_ID = "arm-toast-css";
function ensureStyle() {
  if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
  const el = document.createElement("style");
  el.id = STYLE_ID;
  el.textContent = `
  .arm-toast {
    display: flex; align-items: flex-start; gap: 0.75em;
    min-width: 280px; max-width: 380px; padding: 0.95em 1.05em;
    background: var(--surface-overlay); -webkit-backdrop-filter: var(--blur-glass); backdrop-filter: var(--blur-glass);
    border: 1px solid var(--border-glass); border-radius: var(--radius-md);
    box-shadow: var(--glow-card); position: relative; overflow: hidden;
    font-family: var(--font-body);
  }
  .arm-toast::before { content: ""; position: absolute; left: 0; top: 0; bottom: 0; width: 4px; background: var(--accent); }
  .arm-toast--success::before { background: var(--success); }
  .arm-toast--danger::before  { background: var(--danger); }
  .arm-toast--gold::before    { background: var(--accent-secondary); }
  .arm-toast__icon { display: inline-flex; flex: none; margin-top: 1px; color: var(--accent); }
  .arm-toast--success .arm-toast__icon { color: var(--success); }
  .arm-toast--danger  .arm-toast__icon { color: var(--danger); }
  .arm-toast--gold    .arm-toast__icon { color: var(--gold-600); }
  .arm-toast__body { flex: 1; min-width: 0; }
  .arm-toast__title { font-family: var(--font-display); font-weight: 600; font-size: var(--text-base); color: var(--text-primary); }
  .arm-toast__msg { font-weight: 500; font-size: var(--text-sm); color: var(--text-secondary); margin-top: 2px; line-height: var(--leading-snug); }
  .arm-toast__close {
    flex: none; border: none; background: transparent; cursor: pointer; color: var(--text-muted);
    width: 24px; height: 24px; border-radius: var(--radius-xs); display: inline-flex; align-items: center; justify-content: center;
    transition: background var(--dur-fast) var(--ease-soft), color var(--dur-fast) var(--ease-soft);
  }
  .arm-toast__close:hover { background: var(--surface-sunken); color: var(--ink-900); }
  .arm-toast--enter { animation: arm-toast-in var(--dur-slow) var(--ease-spring); }
  @keyframes arm-toast-in { from { opacity: 0; transform: translateY(10px) scale(0.98); } to { opacity: 1; transform: none; } }
  `;
  document.head.appendChild(el);
}
const ICONS = {
  info: "M12 8h.01M11 12h1v4h1",
  success: "m5 13 4 4L19 7",
  danger: "M12 9v4m0 4h.01",
  gold: "M12 9v4m0 4h.01"
};
function Toast({
  title,
  children,
  tone = "info",
  icon,
  onClose,
  className = "",
  ...rest
}) {
  ensureStyle();
  const toneCls = tone !== "info" ? `arm-toast--${tone}` : "";
  return /*#__PURE__*/React.createElement("div", _extends({
    className: ["arm-toast", "arm-toast--enter", toneCls, className].filter(Boolean).join(" "),
    role: "status"
  }, rest), /*#__PURE__*/React.createElement("span", {
    className: "arm-toast__icon"
  }, icon || /*#__PURE__*/React.createElement("svg", {
    width: "20",
    height: "20",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "2",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("circle", {
    cx: "12",
    cy: "12",
    r: "9",
    opacity: "0.35"
  }), /*#__PURE__*/React.createElement("path", {
    d: ICONS[tone] || ICONS.info
  }))), /*#__PURE__*/React.createElement("div", {
    className: "arm-toast__body"
  }, title && /*#__PURE__*/React.createElement("div", {
    className: "arm-toast__title"
  }, title), children && /*#__PURE__*/React.createElement("div", {
    className: "arm-toast__msg"
  }, children)), onClose && /*#__PURE__*/React.createElement("button", {
    className: "arm-toast__close",
    "aria-label": "Dismiss",
    onClick: onClose
  }, /*#__PURE__*/React.createElement("svg", {
    width: "15",
    height: "15",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "2.2",
    strokeLinecap: "round"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M18 6 6 18M6 6l12 12"
  }))));
}
Object.assign(__ds_scope, { Toast });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/Toast.jsx", error: String((e && e.message) || e) }); }

// components/feedback/Tooltip.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const STYLE_ID = "arm-tooltip-css";
function ensureStyle() {
  if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
  const el = document.createElement("style");
  el.id = STYLE_ID;
  el.textContent = `
  .arm-tooltip { position: relative; display: inline-flex; }
  .arm-tooltip__bubble {
    position: absolute; z-index: 50; left: 50%; bottom: calc(100% + 10px); transform: translateX(-50%) translateY(4px);
    background: var(--surface-overlay); -webkit-backdrop-filter: var(--blur-glass); backdrop-filter: var(--blur-glass);
    border: 1px solid var(--border-glass); border-radius: var(--radius-sm);
    padding: 0.5em 0.75em; white-space: nowrap;
    font-family: var(--font-display); font-weight: 600; font-size: var(--text-xs); letter-spacing: 0.01em;
    color: var(--text-primary); box-shadow: var(--glow-soft);
    opacity: 0; pointer-events: none;
    transition: opacity var(--dur-base) var(--ease-out), transform var(--dur-base) var(--ease-out);
  }
  .arm-tooltip__bubble::after {
    content: ""; position: absolute; top: 100%; left: 50%; transform: translateX(-50%) rotate(45deg);
    width: 9px; height: 9px; margin-top: -5px; background: var(--surface-overlay);
    border-right: 1px solid var(--border-glass); border-bottom: 1px solid var(--border-glass);
  }
  .arm-tooltip:hover .arm-tooltip__bubble,
  .arm-tooltip:focus-within .arm-tooltip__bubble { opacity: 1; transform: translateX(-50%) translateY(0); }
  .arm-tooltip--bottom .arm-tooltip__bubble { bottom: auto; top: calc(100% + 10px); transform: translateX(-50%) translateY(-4px); }
  .arm-tooltip--bottom .arm-tooltip__bubble::after { top: -5px; border: none; border-left: 1px solid var(--border-glass); border-top: 1px solid var(--border-glass); }
  .arm-tooltip--bottom:hover .arm-tooltip__bubble { transform: translateX(-50%) translateY(0); }
  `;
  document.head.appendChild(el);
}
function Tooltip({
  label,
  side = "top",
  children,
  className = "",
  ...rest
}) {
  ensureStyle();
  return /*#__PURE__*/React.createElement("span", _extends({
    className: ["arm-tooltip", side === "bottom" ? "arm-tooltip--bottom" : "", className].filter(Boolean).join(" ")
  }, rest), children, /*#__PURE__*/React.createElement("span", {
    className: "arm-tooltip__bubble",
    role: "tooltip"
  }, label));
}
Object.assign(__ds_scope, { Tooltip });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/Tooltip.jsx", error: String((e && e.message) || e) }); }

// components/forms/Checkbox.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const STYLE_ID = "arm-checkbox-css";
function ensureStyle() {
  if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
  const el = document.createElement("style");
  el.id = STYLE_ID;
  el.textContent = `
  .arm-check { display: inline-flex; align-items: center; gap: 0.6em; cursor: pointer; font-family: var(--font-body); user-select: none; }
  .arm-check input { position: absolute; opacity: 0; width: 0; height: 0; }
  .arm-check__box {
    width: 20px; height: 20px; flex: none; border-radius: var(--radius-xs);
    background: var(--surface-card-solid); border: 1.5px solid var(--border-strong);
    display: inline-flex; align-items: center; justify-content: center;
    transition: background var(--dur-fast) var(--ease-soft), border-color var(--dur-fast) var(--ease-soft), box-shadow var(--dur-base) var(--ease-out);
  }
  .arm-check__box svg { width: 13px; height: 13px; stroke: var(--text-on-accent); stroke-width: 3.2; fill: none; stroke-linecap: round; stroke-linejoin: round;
    stroke-dasharray: 18; stroke-dashoffset: 18; transition: stroke-dashoffset var(--dur-base) var(--ease-out); }
  .arm-check input:checked + .arm-check__box { background: var(--accent); border-color: var(--accent); box-shadow: var(--glow-aqua); }
  .arm-check input:checked + .arm-check__box svg { stroke-dashoffset: 0; }
  .arm-check input:focus-visible + .arm-check__box { box-shadow: var(--ring); }
  .arm-check--disabled { opacity: 0.5; cursor: not-allowed; }
  .arm-check__label { font-weight: 500; font-size: var(--text-base); color: var(--text-primary); }
  `;
  document.head.appendChild(el);
}
function Checkbox({
  checked,
  defaultChecked,
  onChange,
  label,
  disabled = false,
  className = "",
  ...rest
}) {
  ensureStyle();
  return /*#__PURE__*/React.createElement("label", {
    className: ["arm-check", disabled ? "arm-check--disabled" : "", className].filter(Boolean).join(" ")
  }, /*#__PURE__*/React.createElement("input", _extends({
    type: "checkbox",
    checked: checked,
    defaultChecked: defaultChecked,
    onChange: onChange,
    disabled: disabled
  }, rest)), /*#__PURE__*/React.createElement("span", {
    className: "arm-check__box"
  }, /*#__PURE__*/React.createElement("svg", {
    viewBox: "0 0 16 16"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M3 8.5 L6.5 12 L13 4"
  }))), label && /*#__PURE__*/React.createElement("span", {
    className: "arm-check__label"
  }, label));
}
Object.assign(__ds_scope, { Checkbox });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Checkbox.jsx", error: String((e && e.message) || e) }); }

// components/forms/Input.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const STYLE_ID = "arm-input-css";
function ensureStyle() {
  if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
  const el = document.createElement("style");
  el.id = STYLE_ID;
  el.textContent = `
  .arm-field { display: flex; flex-direction: column; gap: 6px; font-family: var(--font-body); }
  .arm-field__label {
    font-family: var(--font-body); font-weight: 600; font-size: var(--text-sm);
    color: var(--text-primary); letter-spacing: 0.005em;
  }
  .arm-field__req { color: var(--danger); margin-left: 2px; }
  .arm-input-wrap {
    display: flex; align-items: center; gap: 0.6em;
    background: var(--surface-card-solid);
    border: 1px solid var(--border-default); border-radius: var(--radius-control);
    padding: 0 0.9em; min-height: 42px;
    transition: border-color var(--dur-base) var(--ease-soft),
                box-shadow var(--dur-base) var(--ease-out), background var(--dur-base) var(--ease-soft);
  }
  .arm-input-wrap:hover { border-color: var(--border-strong); }
  .arm-input-wrap:focus-within { border-color: var(--accent); box-shadow: var(--ring); background: var(--bone-50); }
  .arm-input-wrap--error { border-color: var(--danger); }
  .arm-input-wrap--error:focus-within { box-shadow: 0 0 0 3px rgba(203,87,66,0.30); }
  .arm-input-wrap--disabled { opacity: 0.55; pointer-events: none; background: var(--surface-sunken); }
  .arm-input {
    flex: 1; border: none; outline: none; background: transparent;
    font-family: var(--font-body); font-weight: 500; font-size: var(--text-base);
    color: var(--text-primary); padding: 0.6em 0; min-width: 0;
  }
  .arm-input:focus, .arm-input:focus-visible { outline: none; box-shadow: none; }
  .arm-input::placeholder { color: var(--text-muted); }
  .arm-input-wrap .arm-icon { color: var(--text-muted); display: inline-flex; flex: none; }
  .arm-input-wrap:focus-within .arm-icon { color: var(--accent); }
  .arm-field__hint { font-size: var(--text-sm); color: var(--text-secondary); font-weight: 500; }
  .arm-field__hint--error { color: var(--danger); }
  `;
  document.head.appendChild(el);
}
function Input({
  label,
  hint,
  error,
  required = false,
  iconLeft = null,
  iconRight = null,
  disabled = false,
  className = "",
  id,
  ...rest
}) {
  ensureStyle();
  const fieldId = id || (label ? `arm-${label.replace(/\s+/g, "-").toLowerCase()}` : undefined);
  const msg = error || hint;
  return /*#__PURE__*/React.createElement("div", {
    className: ["arm-field", className].filter(Boolean).join(" ")
  }, label && /*#__PURE__*/React.createElement("label", {
    className: "arm-field__label",
    htmlFor: fieldId
  }, label, required && /*#__PURE__*/React.createElement("span", {
    className: "arm-field__req"
  }, "*")), /*#__PURE__*/React.createElement("div", {
    className: ["arm-input-wrap", error ? "arm-input-wrap--error" : "", disabled ? "arm-input-wrap--disabled" : ""].filter(Boolean).join(" ")
  }, iconLeft && /*#__PURE__*/React.createElement("span", {
    className: "arm-icon"
  }, iconLeft), /*#__PURE__*/React.createElement("input", _extends({
    id: fieldId,
    className: "arm-input",
    disabled: disabled,
    "aria-invalid": !!error
  }, rest)), iconRight && /*#__PURE__*/React.createElement("span", {
    className: "arm-icon"
  }, iconRight)), msg && /*#__PURE__*/React.createElement("span", {
    className: ["arm-field__hint", error ? "arm-field__hint--error" : ""].filter(Boolean).join(" ")
  }, msg));
}
Object.assign(__ds_scope, { Input });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Input.jsx", error: String((e && e.message) || e) }); }

// components/forms/Select.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const STYLE_ID = "arm-select-css";
function ensureStyle() {
  if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
  const el = document.createElement("style");
  el.id = STYLE_ID;
  el.textContent = `
  .arm-select-wrap {
    position: relative; display: inline-flex; align-items: center; width: 100%;
    background: var(--surface-card-solid); border: 1px solid var(--border-default);
    border-radius: var(--radius-control); min-height: 42px;
    transition: border-color var(--dur-base) var(--ease-soft), box-shadow var(--dur-base) var(--ease-out);
  }
  .arm-select-wrap:hover { border-color: var(--border-strong); }
  .arm-select-wrap:focus-within { border-color: var(--accent); box-shadow: var(--ring); }
  .arm-select {
    appearance: none; -webkit-appearance: none; border: none; outline: none; background: transparent;
    font-family: var(--font-body); font-weight: 500; font-size: var(--text-base); color: var(--text-primary);
    padding: 0.6em 2.4em 0.6em 0.95em; width: 100%; cursor: pointer; border-radius: var(--radius-control);
  }
  .arm-select__chev {
    position: absolute; right: 0.85em; pointer-events: none; color: var(--text-muted);
    display: inline-flex; transition: transform var(--dur-base) var(--ease-out);
  }
  .arm-select-wrap:focus-within .arm-select__chev { color: var(--accent); transform: translateY(1px); }
  .arm-select-wrap--disabled { opacity: 0.55; pointer-events: none; background: var(--surface-sunken); }
  `;
  document.head.appendChild(el);
}
function Select({
  options = [],
  value,
  defaultValue,
  onChange,
  placeholder,
  disabled = false,
  className = "",
  children,
  ...rest
}) {
  ensureStyle();
  return /*#__PURE__*/React.createElement("div", {
    className: ["arm-select-wrap", disabled ? "arm-select-wrap--disabled" : "", className].filter(Boolean).join(" ")
  }, /*#__PURE__*/React.createElement("select", _extends({
    className: "arm-select",
    value: value,
    defaultValue: defaultValue,
    onChange: onChange,
    disabled: disabled
  }, rest), placeholder && /*#__PURE__*/React.createElement("option", {
    value: "",
    disabled: true
  }, placeholder), options.map(o => {
    const opt = typeof o === "string" ? {
      value: o,
      label: o
    } : o;
    return /*#__PURE__*/React.createElement("option", {
      key: opt.value,
      value: opt.value
    }, opt.label);
  }), children), /*#__PURE__*/React.createElement("span", {
    className: "arm-select__chev"
  }, /*#__PURE__*/React.createElement("svg", {
    width: "16",
    height: "16",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "2.2",
    strokeLinecap: "round",
    strokeLinejoin: "round"
  }, /*#__PURE__*/React.createElement("path", {
    d: "m6 9 6 6 6-6"
  }))));
}
Object.assign(__ds_scope, { Select });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Select.jsx", error: String((e && e.message) || e) }); }

// components/forms/Switch.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const STYLE_ID = "arm-switch-css";
function ensureStyle() {
  if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
  const el = document.createElement("style");
  el.id = STYLE_ID;
  el.textContent = `
  .arm-switch { display: inline-flex; align-items: center; gap: 0.65em; cursor: pointer; font-family: var(--font-body); user-select: none; }
  .arm-switch__track {
    --_w: 44px; --_h: 26px;
    position: relative; width: var(--_w); height: var(--_h); flex: none;
    background: var(--sand-400); border-radius: var(--radius-pill);
    transition: background var(--dur-base) var(--ease-soft), box-shadow var(--dur-base) var(--ease-out);
  }
  .arm-switch__thumb {
    position: absolute; top: 3px; left: 3px; width: 20px; height: 20px;
    background: var(--bone-50); border-radius: 50%;
    box-shadow: 0 2px 6px rgba(86,74,54,0.30);
    transition: transform var(--dur-base) var(--ease-spring);
  }
  .arm-switch input { position: absolute; opacity: 0; width: 0; height: 0; }
  .arm-switch input:checked + .arm-switch__track { background: var(--accent); box-shadow: var(--glow-aqua); }
  .arm-switch input:checked + .arm-switch__track .arm-switch__thumb { transform: translateX(18px); }
  .arm-switch input:focus-visible + .arm-switch__track { box-shadow: var(--ring); }
  .arm-switch--disabled { opacity: 0.5; cursor: not-allowed; }
  .arm-switch__label { font-weight: 500; font-size: var(--text-base); color: var(--text-primary); }
  /* ambient breathing when on + 'live' */
  .arm-switch--live input:checked + .arm-switch__track { animation: armali-breathe var(--dur-breathe) var(--ease-soft) infinite; }
  `;
  document.head.appendChild(el);
}
function Switch({
  checked,
  defaultChecked,
  onChange,
  label,
  disabled = false,
  live = false,
  className = "",
  ...rest
}) {
  ensureStyle();
  return /*#__PURE__*/React.createElement("label", {
    className: ["arm-switch", disabled ? "arm-switch--disabled" : "", live ? "arm-switch--live" : "", className].filter(Boolean).join(" ")
  }, /*#__PURE__*/React.createElement("input", _extends({
    type: "checkbox",
    checked: checked,
    defaultChecked: defaultChecked,
    onChange: onChange,
    disabled: disabled
  }, rest)), /*#__PURE__*/React.createElement("span", {
    className: "arm-switch__track"
  }, /*#__PURE__*/React.createElement("span", {
    className: "arm-switch__thumb"
  })), label && /*#__PURE__*/React.createElement("span", {
    className: "arm-switch__label"
  }, label));
}
Object.assign(__ds_scope, { Switch });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Switch.jsx", error: String((e && e.message) || e) }); }

// components/navigation/Tabs.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const STYLE_ID = "arm-tabs-css";
function ensureStyle() {
  if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
  const el = document.createElement("style");
  el.id = STYLE_ID;
  el.textContent = `
  .arm-tabs {
    display: inline-flex; gap: 4px; padding: 5px;
    background: var(--surface-sunken); border: 1px solid var(--border-subtle);
    border-radius: var(--radius-pill);
  }
  .arm-tab {
    position: relative; border: none; background: transparent; cursor: pointer;
    font-family: var(--font-display); font-weight: 600; font-size: var(--text-sm);
    letter-spacing: 0.01em; color: var(--text-secondary);
    padding: 0.55em 1.1em; border-radius: var(--radius-pill);
    display: inline-flex; align-items: center; gap: 0.45em; white-space: nowrap;
    transition: color var(--dur-base) var(--ease-soft), background var(--dur-base) var(--ease-soft), box-shadow var(--dur-base) var(--ease-out);
  }
  .arm-tab:hover { color: var(--ink-900); }
  .arm-tab--active {
    color: var(--aqua-700); background: var(--surface-card-solid);
    box-shadow: var(--glow-soft); 
  }
  .arm-tab:focus-visible { outline: none; box-shadow: var(--ring); }
  .arm-tab__count {
    font-family: var(--font-body); font-weight: 600; font-size: 11px;
    background: var(--accent-soft); color: var(--aqua-700); border-radius: var(--radius-pill);
    padding: 1px 7px; line-height: 1.5;
  }
  /* underline variant */
  .arm-tabs--line { background: transparent; border: none; border-bottom: 1px solid var(--border-subtle); border-radius: 0; padding: 0; gap: 6px; }
  .arm-tabs--line .arm-tab { border-radius: 0; padding: 0.7em 0.4em; margin-bottom: -1px; }
  .arm-tabs--line .arm-tab--active { background: transparent; box-shadow: none; color: var(--aqua-700); }
  .arm-tabs--line .arm-tab--active::after {
    content: ""; position: absolute; left: 0.4em; right: 0.4em; bottom: 0; height: 2.5px;
    background: var(--accent); border-radius: var(--radius-pill); box-shadow: 0 0 8px rgba(22,166,166,0.6);
  }
  `;
  document.head.appendChild(el);
}
function Tabs({
  tabs = [],
  value,
  defaultValue,
  onChange,
  variant = "pill",
  className = "",
  ...rest
}) {
  ensureStyle();
  const norm = tabs.map(t => typeof t === "string" ? {
    value: t,
    label: t
  } : t);
  const [internal, setInternal] = React.useState(defaultValue ?? (norm[0] && norm[0].value));
  const active = value !== undefined ? value : internal;
  const select = v => {
    if (value === undefined) setInternal(v);
    onChange && onChange(v);
  };
  return /*#__PURE__*/React.createElement("div", _extends({
    role: "tablist",
    className: ["arm-tabs", variant === "line" ? "arm-tabs--line" : "", className].filter(Boolean).join(" ")
  }, rest), norm.map(t => /*#__PURE__*/React.createElement("button", {
    key: t.value,
    role: "tab",
    "aria-selected": active === t.value,
    className: ["arm-tab", active === t.value ? "arm-tab--active" : ""].filter(Boolean).join(" "),
    onClick: () => select(t.value)
  }, t.icon, t.label, t.count != null && /*#__PURE__*/React.createElement("span", {
    className: "arm-tab__count"
  }, t.count))));
}
Object.assign(__ds_scope, { Tabs });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/navigation/Tabs.jsx", error: String((e && e.message) || e) }); }

// components/overlay/Dialog.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const STYLE_ID = "arm-dialog-css";
function ensureStyle() {
  if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
  const el = document.createElement("style");
  el.id = STYLE_ID;
  el.textContent = `
  .arm-dialog__scrim {
    position: fixed; inset: 0; z-index: 100; background: var(--scrim);
    -webkit-backdrop-filter: blur(3px); backdrop-filter: blur(3px);
    display: flex; align-items: center; justify-content: center; padding: 24px;
    animation: arm-scrim-in var(--dur-base) var(--ease-soft);
  }
  @keyframes arm-scrim-in { from { opacity: 0; } to { opacity: 1; } }
  .arm-dialog {
    position: relative; width: 100%; max-width: var(--_w, 460px);
    background: var(--surface-overlay); -webkit-backdrop-filter: var(--blur-strong); backdrop-filter: var(--blur-strong);
    border: 1px solid var(--border-glass); border-radius: var(--radius-xl);
    box-shadow: var(--glow-card), 0 40px 80px -40px rgba(44,40,35,0.5);
    padding: var(--space-8); font-family: var(--font-body);
    animation: arm-dialog-in var(--dur-slow) var(--ease-spring);
  }
  @keyframes arm-dialog-in { from { opacity: 0; transform: translateY(14px) scale(0.97); } to { opacity: 1; transform: none; } }
  .arm-dialog__close {
    position: absolute; top: 18px; right: 18px; width: 32px; height: 32px;
    border: none; background: var(--surface-frost); border-radius: var(--radius-sm); cursor: pointer;
    color: var(--text-secondary); display: inline-flex; align-items: center; justify-content: center;
    transition: background var(--dur-fast) var(--ease-soft), color var(--dur-fast) var(--ease-soft);
  }
  .arm-dialog__close:hover { background: var(--accent-soft); color: var(--accent-press); }
  .arm-dialog__title { font-family: var(--font-display); font-weight: 600; font-size: var(--text-2xl); color: var(--text-primary); letter-spacing: var(--tracking-title); padding-right: 32px; }
  .arm-dialog__desc { font-weight: 500; font-size: var(--text-base); color: var(--text-secondary); line-height: var(--leading-relaxed); margin-top: 0.6em; }
  .arm-dialog__body { margin-top: 1.25em; }
  .arm-dialog__footer { display: flex; justify-content: flex-end; gap: 0.7em; margin-top: var(--space-7); }
  `;
  document.head.appendChild(el);
}
function Dialog({
  open = true,
  title,
  description,
  onClose,
  footer = null,
  width,
  className = "",
  children,
  ...rest
}) {
  ensureStyle();
  if (!open) return null;
  const onScrim = e => {
    if (e.target === e.currentTarget && onClose) onClose();
  };
  return /*#__PURE__*/React.createElement("div", {
    className: "arm-dialog__scrim",
    onClick: onScrim
  }, /*#__PURE__*/React.createElement("div", _extends({
    role: "dialog",
    "aria-modal": "true",
    "aria-label": typeof title === "string" ? title : undefined,
    className: ["arm-dialog", className].filter(Boolean).join(" "),
    style: width ? {
      "--_w": typeof width === "number" ? `${width}px` : width
    } : undefined
  }, rest), onClose && /*#__PURE__*/React.createElement("button", {
    className: "arm-dialog__close",
    "aria-label": "Close",
    onClick: onClose
  }, /*#__PURE__*/React.createElement("svg", {
    width: "16",
    height: "16",
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: "2.2",
    strokeLinecap: "round"
  }, /*#__PURE__*/React.createElement("path", {
    d: "M18 6 6 18M6 6l12 12"
  }))), title && /*#__PURE__*/React.createElement("div", {
    className: "arm-dialog__title"
  }, title), description && /*#__PURE__*/React.createElement("div", {
    className: "arm-dialog__desc"
  }, description), children && /*#__PURE__*/React.createElement("div", {
    className: "arm-dialog__body"
  }, children), footer && /*#__PURE__*/React.createElement("div", {
    className: "arm-dialog__footer"
  }, footer)));
}
Object.assign(__ds_scope, { Dialog });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/overlay/Dialog.jsx", error: String((e && e.message) || e) }); }

// components/surfaces/Avatar.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const STYLE_ID = "arm-avatar-css";
function ensureStyle() {
  if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
  const el = document.createElement("style");
  el.id = STYLE_ID;
  el.textContent = `
  .arm-avatar {
    --_s: 40px;
    position: relative; width: var(--_s); height: var(--_s); flex: none;
    border-radius: 50%; display: inline-flex; align-items: center; justify-content: center;
    font-family: var(--font-display); font-weight: 600; color: var(--text-on-accent);
    background: linear-gradient(140deg, var(--aqua-400), var(--azure-500));
    box-shadow: 0 0 0 2px var(--bone-50), var(--glow-soft);
    font-size: calc(var(--_s) * 0.4);
  }
  .arm-avatar img { width: 100%; height: 100%; object-fit: cover; border-radius: 50%; }
  .arm-avatar--sm { --_s: 28px; }
  .arm-avatar--lg { --_s: 56px; }
  .arm-avatar--gold { background: linear-gradient(140deg, var(--gold-400), var(--gold-600)); color: #3a2c08; }
  .arm-avatar--sea  { background: linear-gradient(140deg, var(--sea-400), var(--sea-600)); }
  .arm-avatar__status {
    position: absolute; right: -3px; bottom: -3px; width: 30%; height: 30%; min-width: 9px; min-height: 9px;
    border-radius: 50%; border: 2px solid var(--bone-50); background: var(--success);
    z-index: 1;
  }
  .arm-avatar__status--away { background: var(--gold-500); }
  .arm-avatar__status--busy { background: var(--danger); }
  `;
  document.head.appendChild(el);
}
const GRADS = ["", "arm-avatar--gold", "arm-avatar--sea"];
function pick(str = "") {
  let h = 0;
  for (let i = 0; i < str.length; i++) h = h * 31 + str.charCodeAt(i) >>> 0;
  return GRADS[h % GRADS.length];
}
function Avatar({
  name = "",
  src,
  size = "md",
  status,
  className = "",
  ...rest
}) {
  ensureStyle();
  const initials = name.split(/\s+/).filter(Boolean).slice(0, 2).map(w => w[0]).join("").toUpperCase();
  const cls = ["arm-avatar", size !== "md" ? `arm-avatar--${size}` : "", src ? "" : pick(name), className].filter(Boolean).join(" ");
  return /*#__PURE__*/React.createElement("span", _extends({
    className: cls
  }, rest), src ? /*#__PURE__*/React.createElement("img", {
    src: src,
    alt: name
  }) : initials || "?", status && /*#__PURE__*/React.createElement("span", {
    className: `arm-avatar__status arm-avatar__status--${status}`
  }));
}
Object.assign(__ds_scope, { Avatar });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/surfaces/Avatar.jsx", error: String((e && e.message) || e) }); }

// components/surfaces/Badge.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const STYLE_ID = "arm-badge-css";
function ensureStyle() {
  if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
  const el = document.createElement("style");
  el.id = STYLE_ID;
  el.textContent = `
  .arm-badge {
    display: inline-flex; align-items: center; gap: 0.4em;
    font-family: var(--font-display); font-weight: 600; font-size: var(--text-xs);
    letter-spacing: 0.02em; line-height: 1; padding: 0.42em 0.75em;
    border-radius: var(--radius-pill); white-space: nowrap;
    background: var(--accent-soft); color: var(--aqua-700);
    border: 1px solid color-mix(in srgb, var(--accent) 24%, transparent);
  }
  .arm-badge--gold   { background: var(--gold-100);  color: var(--gold-600);  border-color: color-mix(in srgb, var(--gold-500) 28%, transparent); }
  .arm-badge--azure  { background: var(--azure-100); color: var(--azure-600); border-color: color-mix(in srgb, var(--azure-500) 26%, transparent); }
  .arm-badge--success{ background: var(--sea-100);   color: var(--sea-600);   border-color: color-mix(in srgb, var(--sea-500) 28%, transparent); }
  .arm-badge--danger { background: var(--danger-soft);color: var(--terracotta-600); border-color: color-mix(in srgb, var(--danger) 28%, transparent); }
  .arm-badge--neutral{ background: var(--surface-sunken); color: var(--text-secondary); border-color: var(--border-default); }
  .arm-badge--solid  { background: var(--accent); color: var(--text-on-accent); border-color: transparent; }
  .arm-badge__dot { width: 7px; height: 7px; border-radius: 50%; background: currentColor; }
  .arm-badge__dot--pulse { animation: armali-pulse-dot 1800ms var(--ease-soft) infinite; }
  `;
  document.head.appendChild(el);
}
function Badge({
  children,
  tone = "aqua",
  dot = false,
  pulse = false,
  className = "",
  ...rest
}) {
  ensureStyle();
  const toneCls = tone === "aqua" ? "" : `arm-badge--${tone}`;
  return /*#__PURE__*/React.createElement("span", _extends({
    className: ["arm-badge", toneCls, className].filter(Boolean).join(" ")
  }, rest), dot && /*#__PURE__*/React.createElement("span", {
    className: ["arm-badge__dot", pulse ? "arm-badge__dot--pulse" : ""].filter(Boolean).join(" ")
  }), children);
}
Object.assign(__ds_scope, { Badge });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/surfaces/Badge.jsx", error: String((e && e.message) || e) }); }

// components/surfaces/Card.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const STYLE_ID = "arm-card-css";
function ensureStyle() {
  if (typeof document === "undefined" || document.getElementById(STYLE_ID)) return;
  const el = document.createElement("style");
  el.id = STYLE_ID;
  el.textContent = `
  .arm-card {
    position: relative; border-radius: var(--radius-card);
    background: var(--surface-card-solid); border: 1px solid var(--border-subtle);
    box-shadow: var(--glow-card); padding: var(--pad-card);
    transition: transform var(--dur-base) var(--ease-out), box-shadow var(--dur-base) var(--ease-out);
  }
  .arm-card--glass {
    background: var(--surface-card);
    -webkit-backdrop-filter: var(--blur-glass); backdrop-filter: var(--blur-glass);
    border-color: var(--border-glass);
  }
  .arm-card--interactive { cursor: pointer; }
  .arm-card--interactive:hover { transform: translateY(-3px); box-shadow: var(--glow-aqua), var(--glow-card); }
  .arm-card--accent { border-color: color-mix(in srgb, var(--accent) 40%, transparent); }
  .arm-card__header { display: flex; align-items: flex-start; justify-content: space-between; gap: 1em; margin-bottom: 0.75em; }
  .arm-card__title { font-family: var(--font-display); font-weight: 600; font-size: var(--text-xl); color: var(--text-primary); letter-spacing: var(--tracking-title); }
  .arm-card__subtitle { font-family: var(--font-body); font-weight: 500; font-size: var(--text-sm); color: var(--text-secondary); margin-top: 3px; }
  .arm-card__body { font-family: var(--font-body); font-weight: 500; font-size: var(--text-base); color: var(--text-secondary); line-height: var(--leading-relaxed); }
  .arm-card__footer { display: flex; align-items: center; gap: 0.6em; margin-top: 1.25em; }
  `;
  document.head.appendChild(el);
}
function Card({
  title,
  subtitle,
  action = null,
  footer = null,
  glass = false,
  accent = false,
  interactive = false,
  className = "",
  children,
  ...rest
}) {
  ensureStyle();
  const cls = ["arm-card", glass ? "arm-card--glass" : "", accent ? "arm-card--accent" : "", interactive ? "arm-card--interactive" : "", className].filter(Boolean).join(" ");
  const hasHeader = title || subtitle || action;
  return /*#__PURE__*/React.createElement("div", _extends({
    className: cls
  }, rest), hasHeader && /*#__PURE__*/React.createElement("div", {
    className: "arm-card__header"
  }, /*#__PURE__*/React.createElement("div", null, title && /*#__PURE__*/React.createElement("div", {
    className: "arm-card__title"
  }, title), subtitle && /*#__PURE__*/React.createElement("div", {
    className: "arm-card__subtitle"
  }, subtitle)), action), children && /*#__PURE__*/React.createElement("div", {
    className: "arm-card__body"
  }, children), footer && /*#__PURE__*/React.createElement("div", {
    className: "arm-card__footer"
  }, footer));
}
Object.assign(__ds_scope, { Card });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/surfaces/Card.jsx", error: String((e && e.message) || e) }); }

// ui_kits/console/App.jsx
try { (() => {
/* global React */
(() => {
  const {
    Toast
  } = window.ProjectArmaliDesignSystem_ce5329;
  const TITLES = {
    overview: "Overview",
    regions: "Regions",
    activity: "Activity",
    keys: "Access keys",
    settings: "Settings"
  };
  function ToastStack({
    toasts,
    onClose
  }) {
    return /*#__PURE__*/React.createElement("div", {
      className: "con-toaststack"
    }, toasts.map(t => /*#__PURE__*/React.createElement(Toast, {
      key: t.id,
      tone: t.tone,
      title: t.title,
      onClose: () => onClose(t.id)
    }, t.msg)));
  }
  function App() {
    const [signedIn, setSignedIn] = React.useState(false);
    const [nav, setNav] = React.useState("overview");
    const [toasts, setToasts] = React.useState([]);
    const pushToast = t => {
      const id = Date.now() + Math.random();
      setToasts(ts => [...ts, {
        ...t,
        id
      }]);
      setTimeout(() => setToasts(ts => ts.filter(x => x.id !== id)), 4200);
    };
    const closeToast = id => setToasts(ts => ts.filter(x => x.id !== id));
    React.useEffect(() => {
      window.lucide && window.lucide.createIcons();
    });
    if (!signedIn) {
      return /*#__PURE__*/React.createElement(window.LoginScreen, {
        onSignIn: () => {
          setSignedIn(true);
          setTimeout(() => pushToast({
            tone: "success",
            title: "Signed in",
            msg: "Welcome back, Lina."
          }), 350);
        }
      });
    }
    let screen = null;
    if (nav === "overview") screen = /*#__PURE__*/React.createElement(window.OverviewScreen, null);else if (nav === "regions") screen = /*#__PURE__*/React.createElement(window.RegionsScreen, {
      onToast: pushToast
    });else if (nav === "settings") screen = /*#__PURE__*/React.createElement(window.SettingsScreen, {
      onToast: pushToast
    });else screen = /*#__PURE__*/React.createElement("div", {
      className: "con-empty"
    }, /*#__PURE__*/React.createElement("i", {
      "data-lucide": "wind"
    }), /*#__PURE__*/React.createElement("p", null, "Nothing here yet \u2014 this view is part of the demo shell."));
    const subtitles = {
      overview: "A calm view of every connected region",
      regions: "Manage replication across your coast",
      settings: "Tune your workspace"
    };
    return /*#__PURE__*/React.createElement("div", {
      className: "con-shell"
    }, /*#__PURE__*/React.createElement(window.Sidebar, {
      active: nav,
      onNavigate: setNav
    }), /*#__PURE__*/React.createElement("div", {
      className: "con-main"
    }, /*#__PURE__*/React.createElement(window.TopBar, {
      title: TITLES[nav],
      subtitle: subtitles[nav],
      onSignOut: () => {
        setSignedIn(false);
        setNav("overview");
      }
    }), /*#__PURE__*/React.createElement("div", {
      className: "con-content"
    }, screen)), /*#__PURE__*/React.createElement(ToastStack, {
      toasts: toasts,
      onClose: closeToast
    }));
  }
  Object.assign(window, {
    ConsoleApp: App
  });
  ReactDOM.createRoot(document.getElementById("root")).render(/*#__PURE__*/React.createElement(App, null));
  setTimeout(() => window.lucide && window.lucide.createIcons(), 120);
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/console/App.jsx", error: String((e && e.message) || e) }); }

// ui_kits/console/LoginScreen.jsx
try { (() => {
/* global React */
(() => {
  const {
    Card,
    Input,
    Button,
    Checkbox
  } = window.ProjectArmaliDesignSystem_ce5329;
  const Icon = window.ConIcon;
  function LoginScreen({
    onSignIn
  }) {
    const [email, setEmail] = React.useState("lina@harbour.co");
    const [pw, setPw] = React.useState("");
    const submit = e => {
      e.preventDefault();
      onSignIn();
    };
    return /*#__PURE__*/React.createElement("div", {
      className: "con-login armali-aurora"
    }, /*#__PURE__*/React.createElement("form", {
      className: "con-login__card",
      onSubmit: submit
    }, /*#__PURE__*/React.createElement("div", {
      className: "con-brand con-brand--lg"
    }, /*#__PURE__*/React.createElement("span", {
      className: "con-brand__mark"
    }, /*#__PURE__*/React.createElement(Icon, {
      n: "waves",
      size: 24
    })), /*#__PURE__*/React.createElement("span", {
      className: "con-brand__name"
    }, "Armali")), /*#__PURE__*/React.createElement("h2", {
      className: "con-login__title"
    }, "Welcome back"), /*#__PURE__*/React.createElement("p", {
      className: "con-login__sub"
    }, "Sign in to your console to check on your coastal regions."), /*#__PURE__*/React.createElement("div", {
      className: "con-login__fields"
    }, /*#__PURE__*/React.createElement(Input, {
      label: "Work email",
      type: "email",
      value: email,
      onChange: e => setEmail(e.target.value),
      iconLeft: /*#__PURE__*/React.createElement(Icon, {
        n: "mail",
        size: 17
      })
    }), /*#__PURE__*/React.createElement(Input, {
      label: "Password",
      type: "password",
      value: pw,
      placeholder: "\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022",
      onChange: e => setPw(e.target.value),
      iconLeft: /*#__PURE__*/React.createElement(Icon, {
        n: "lock",
        size: 17
      })
    }), /*#__PURE__*/React.createElement("div", {
      className: "con-login__row"
    }, /*#__PURE__*/React.createElement(Checkbox, {
      label: "Keep me signed in",
      defaultChecked: true
    }), /*#__PURE__*/React.createElement("a", {
      href: "#",
      onClick: e => e.preventDefault()
    }, "Forgot password?"))), /*#__PURE__*/React.createElement(Button, {
      type: "submit",
      block: true,
      size: "lg",
      iconRight: /*#__PURE__*/React.createElement(Icon, {
        n: "arrow-right",
        size: 18
      })
    }, "Sign in"), /*#__PURE__*/React.createElement("p", {
      className: "con-login__foot"
    }, "New to Armali? ", /*#__PURE__*/React.createElement("a", {
      href: "#",
      onClick: e => e.preventDefault()
    }, "Request access"))));
  }
  Object.assign(window, {
    LoginScreen
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/console/LoginScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/console/OverviewScreen.jsx
try { (() => {
/* global React */
(() => {
  const {
    Card,
    Badge,
    Button,
    Tabs
  } = window.ProjectArmaliDesignSystem_ce5329;
  const Icon = window.ConIcon;
  const STATS = [{
    label: "Connected regions",
    value: "12",
    delta: "+2 this week",
    icon: "waves",
    tone: "aqua"
  }, {
    label: "Avg. uptime",
    value: "99.8%",
    delta: "30-day",
    icon: "gauge",
    tone: "sea"
  }, {
    label: "Events today",
    value: "1,284",
    delta: "+6%",
    icon: "activity",
    tone: "gold"
  }, {
    label: "Open alerts",
    value: "3",
    delta: "2 acknowledged",
    icon: "bell",
    tone: "danger"
  }];
  function StatCard({
    s
  }) {
    return /*#__PURE__*/React.createElement("div", {
      className: "con-stat con-stat--" + s.tone
    }, /*#__PURE__*/React.createElement("div", {
      className: "con-stat__top"
    }, /*#__PURE__*/React.createElement("span", {
      className: "con-stat__icon"
    }, /*#__PURE__*/React.createElement(Icon, {
      n: s.icon,
      size: 20
    })), /*#__PURE__*/React.createElement("span", {
      className: "con-stat__label"
    }, s.label)), /*#__PURE__*/React.createElement("div", {
      className: "con-stat__value"
    }, s.value), /*#__PURE__*/React.createElement("div", {
      className: "con-stat__delta"
    }, s.delta));
  }
  function ActivityRow({
    a
  }) {
    return /*#__PURE__*/React.createElement("div", {
      className: "con-act"
    }, /*#__PURE__*/React.createElement("span", {
      className: "con-act__icon con-act__icon--" + a.tone
    }, /*#__PURE__*/React.createElement(Icon, {
      n: a.icon,
      size: 16
    })), /*#__PURE__*/React.createElement("span", {
      className: "con-act__text"
    }, a.text), /*#__PURE__*/React.createElement("span", {
      className: "con-act__time"
    }, a.time));
  }
  function OverviewScreen() {
    return /*#__PURE__*/React.createElement("div", {
      className: "con-overview"
    }, /*#__PURE__*/React.createElement("div", {
      className: "con-stats"
    }, STATS.map(s => /*#__PURE__*/React.createElement(StatCard, {
      key: s.label,
      s: s
    }))), /*#__PURE__*/React.createElement("div", {
      className: "con-overview__grid"
    }, /*#__PURE__*/React.createElement(Card, {
      className: "con-chart-card",
      title: "Replication throughput",
      subtitle: "Events synced per minute \xB7 last 24h",
      action: /*#__PURE__*/React.createElement(Tabs, {
        tabs: ["24h", "7d", "30d"],
        defaultValue: "24h"
      })
    }, /*#__PURE__*/React.createElement("div", {
      className: "con-chart"
    }, [38, 52, 47, 63, 58, 71, 66, 80, 74, 88, 69, 92, 84, 78, 90, 73].map((h, i) => /*#__PURE__*/React.createElement("span", {
      key: i,
      className: "con-chart__bar",
      style: {
        height: h + "%",
        animationDelay: i * 60 + "ms"
      }
    })))), /*#__PURE__*/React.createElement(Card, {
      title: "Recent activity",
      subtitle: "Across all regions"
    }, /*#__PURE__*/React.createElement("div", {
      className: "con-activity"
    }, window.ARMALI_ACTIVITY.map((a, i) => /*#__PURE__*/React.createElement(ActivityRow, {
      key: i,
      a: a
    }))))));
  }
  Object.assign(window, {
    OverviewScreen
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/console/OverviewScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/console/RegionsScreen.jsx
try { (() => {
/* global React */
(() => {
  const {
    Card,
    Badge,
    Button,
    IconButton,
    Dialog,
    Tabs,
    Tooltip
  } = window.ProjectArmaliDesignSystem_ce5329;
  const Icon = window.ConIcon;
  const STATUS = {
    online: {
      tone: "success",
      label: "Online"
    },
    away: {
      tone: "gold",
      label: "Degraded"
    },
    busy: {
      tone: "azure",
      label: "Syncing"
    }
  };
  function RegionsScreen({
    onToast
  }) {
    const [regions, setRegions] = React.useState(window.ARMALI_REGIONS);
    const [target, setTarget] = React.useState(null);
    const disconnect = () => {
      setRegions(rs => rs.filter(r => r.code !== target.code));
      onToast({
        tone: "danger",
        title: target.name + " disconnected",
        msg: "Replication paused. Reconnect anytime."
      });
      setTarget(null);
    };
    return /*#__PURE__*/React.createElement("div", {
      className: "con-regions"
    }, /*#__PURE__*/React.createElement(Card, {
      className: "con-table-card",
      title: "Coastal regions",
      subtitle: regions.length + " connected · 2 with alerts",
      action: /*#__PURE__*/React.createElement(Button, {
        size: "sm",
        iconLeft: /*#__PURE__*/React.createElement(Icon, {
          n: "plus",
          size: 16
        })
      }, "Connect region")
    }, /*#__PURE__*/React.createElement("div", {
      className: "con-table"
    }, /*#__PURE__*/React.createElement("div", {
      className: "con-table__head"
    }, /*#__PURE__*/React.createElement("span", null, "Region"), /*#__PURE__*/React.createElement("span", null, "Status"), /*#__PURE__*/React.createElement("span", null, "Uptime"), /*#__PURE__*/React.createElement("span", null, "Lag"), /*#__PURE__*/React.createElement("span", null, "Last run"), /*#__PURE__*/React.createElement("span", null)), regions.map(r => /*#__PURE__*/React.createElement("div", {
      className: "con-table__row",
      key: r.code
    }, /*#__PURE__*/React.createElement("span", {
      className: "con-table__name"
    }, /*#__PURE__*/React.createElement("span", {
      className: "con-table__mark"
    }, /*#__PURE__*/React.createElement(Icon, {
      n: "waves",
      size: 15
    })), /*#__PURE__*/React.createElement("span", null, /*#__PURE__*/React.createElement("strong", null, r.name), /*#__PURE__*/React.createElement("em", null, r.code))), /*#__PURE__*/React.createElement("span", null, /*#__PURE__*/React.createElement(Badge, {
      tone: STATUS[r.status].tone,
      dot: true,
      pulse: r.status === "busy"
    }, STATUS[r.status].label)), /*#__PURE__*/React.createElement("span", {
      className: "con-mono"
    }, r.uptime), /*#__PURE__*/React.createElement("span", {
      className: "con-mono"
    }, r.lag), /*#__PURE__*/React.createElement("span", {
      className: "con-table__muted"
    }, r.lastRun), /*#__PURE__*/React.createElement("span", {
      className: "con-table__rowact"
    }, /*#__PURE__*/React.createElement(Tooltip, {
      label: "Disconnect",
      side: "bottom"
    }, /*#__PURE__*/React.createElement(IconButton, {
      size: "sm",
      variant: "bare",
      label: "Disconnect",
      icon: /*#__PURE__*/React.createElement(Icon, {
        n: "unplug",
        size: 15
      }),
      onClick: () => setTarget(r)
    }))))))), /*#__PURE__*/React.createElement(Dialog, {
      open: !!target,
      title: target ? "Disconnect " + target.name + "?" : "",
      description: "Replication for this region will pause until you reconnect. In-flight changes are preserved.",
      onClose: () => setTarget(null),
      footer: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
        variant: "ghost",
        onClick: () => setTarget(null)
      }, "Cancel"), /*#__PURE__*/React.createElement(Button, {
        variant: "danger",
        iconLeft: /*#__PURE__*/React.createElement(Icon, {
          n: "unplug",
          size: 16
        }),
        onClick: disconnect
      }, "Disconnect"))
    }));
  }
  Object.assign(window, {
    RegionsScreen
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/console/RegionsScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/console/SettingsScreen.jsx
try { (() => {
/* global React */
(() => {
  const {
    Card,
    Input,
    Select,
    Switch,
    Button,
    Badge
  } = window.ProjectArmaliDesignSystem_ce5329;
  const Icon = window.ConIcon;
  function SettingsScreen({
    onToast
  }) {
    return /*#__PURE__*/React.createElement("div", {
      className: "con-settings"
    }, /*#__PURE__*/React.createElement(Card, {
      title: "Workspace",
      subtitle: "How your console behaves and looks"
    }, /*#__PURE__*/React.createElement("div", {
      className: "con-form"
    }, /*#__PURE__*/React.createElement(Input, {
      label: "Workspace name",
      defaultValue: "Harbour Operations"
    }), /*#__PURE__*/React.createElement("div", {
      className: "con-form__field"
    }, /*#__PURE__*/React.createElement("label", null, "Default region"), /*#__PURE__*/React.createElement(Select, {
      defaultValue: "ion-3",
      options: [{
        value: "aeg-1",
        label: "Aegean"
      }, {
        value: "adr-2",
        label: "Adriatic"
      }, {
        value: "ion-3",
        label: "Ionian"
      }]
    })))), /*#__PURE__*/React.createElement(Card, {
      title: "Replication",
      subtitle: "Automatic behaviour for connected regions"
    }, /*#__PURE__*/React.createElement("div", {
      className: "con-toggles"
    }, /*#__PURE__*/React.createElement("div", {
      className: "con-toggle"
    }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("strong", null, "Continuous sync"), /*#__PURE__*/React.createElement("span", null, "Keep regions in step in the background")), /*#__PURE__*/React.createElement(Switch, {
      live: true,
      defaultChecked: true
    })), /*#__PURE__*/React.createElement("div", {
      className: "con-toggle"
    }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("strong", null, "Pause on high latency"), /*#__PURE__*/React.createElement("span", null, "Hold replication above 200\xA0ms lag")), /*#__PURE__*/React.createElement(Switch, {
      defaultChecked: true
    })), /*#__PURE__*/React.createElement("div", {
      className: "con-toggle"
    }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("strong", null, "Email me weekly reports"), /*#__PURE__*/React.createElement("span", null, "Every Monday at 08:00 local time")), /*#__PURE__*/React.createElement(Switch, null)))), /*#__PURE__*/React.createElement("div", {
      className: "con-settings__save"
    }, /*#__PURE__*/React.createElement(Badge, {
      tone: "neutral"
    }, "Unsaved changes"), /*#__PURE__*/React.createElement("div", {
      style: {
        flex: 1
      }
    }), /*#__PURE__*/React.createElement(Button, {
      variant: "ghost"
    }, "Discard"), /*#__PURE__*/React.createElement(Button, {
      onClick: () => onToast({
        tone: "success",
        title: "Settings saved",
        msg: "Your workspace is up to date."
      })
    }, "Save changes")));
  }
  Object.assign(window, {
    SettingsScreen
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/console/SettingsScreen.jsx", error: String((e && e.message) || e) }); }

// ui_kits/console/Sidebar.jsx
try { (() => {
/* global React */
(() => {
  const {
    IconButton,
    Badge
  } = window.ProjectArmaliDesignSystem_ce5329;
  const NAV = [{
    id: "overview",
    label: "Overview",
    icon: "layout-dashboard"
  }, {
    id: "regions",
    label: "Regions",
    icon: "waves",
    count: 12
  }, {
    id: "activity",
    label: "Activity",
    icon: "activity"
  }, {
    id: "keys",
    label: "Access keys",
    icon: "key-round"
  }, {
    id: "settings",
    label: "Settings",
    icon: "settings"
  }];
  function Icon({
    n,
    size = 18
  }) {
    return /*#__PURE__*/React.createElement("i", {
      "data-lucide": n,
      style: {
        width: size,
        height: size
      }
    });
  }
  function Sidebar({
    active,
    onNavigate
  }) {
    return /*#__PURE__*/React.createElement("aside", {
      className: "con-sidebar"
    }, /*#__PURE__*/React.createElement("div", {
      className: "con-brand"
    }, /*#__PURE__*/React.createElement("span", {
      className: "con-brand__mark"
    }, /*#__PURE__*/React.createElement(Icon, {
      n: "waves",
      size: 20
    })), /*#__PURE__*/React.createElement("span", {
      className: "con-brand__name"
    }, "Armali")), /*#__PURE__*/React.createElement("nav", {
      className: "con-nav"
    }, NAV.map(item => /*#__PURE__*/React.createElement("button", {
      key: item.id,
      className: "con-nav__item" + (active === item.id ? " is-active" : ""),
      onClick: () => onNavigate(item.id)
    }, /*#__PURE__*/React.createElement(Icon, {
      n: item.icon
    }), /*#__PURE__*/React.createElement("span", null, item.label), item.count != null && /*#__PURE__*/React.createElement("span", {
      className: "con-nav__count"
    }, item.count)))), /*#__PURE__*/React.createElement("div", {
      className: "con-sidebar__foot"
    }, /*#__PURE__*/React.createElement("div", {
      className: "con-region-pill"
    }, /*#__PURE__*/React.createElement(Badge, {
      tone: "success",
      dot: true,
      pulse: true
    }, "All systems calm"))));
  }
  Object.assign(window, {
    Sidebar,
    ConIcon: Icon
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/console/Sidebar.jsx", error: String((e && e.message) || e) }); }

// ui_kits/console/TopBar.jsx
try { (() => {
/* global React */
(() => {
  const {
    Input,
    IconButton,
    Avatar,
    Tooltip
  } = window.ProjectArmaliDesignSystem_ce5329;
  const Icon = window.ConIcon;
  function TopBar({
    title,
    subtitle,
    onSignOut
  }) {
    return /*#__PURE__*/React.createElement("header", {
      className: "con-topbar"
    }, /*#__PURE__*/React.createElement("div", {
      className: "con-topbar__title"
    }, /*#__PURE__*/React.createElement("div", {
      className: "armali-eyebrow"
    }, "Console"), /*#__PURE__*/React.createElement("h1", null, title)), /*#__PURE__*/React.createElement("div", {
      className: "con-topbar__actions"
    }, /*#__PURE__*/React.createElement("div", {
      className: "con-search"
    }, /*#__PURE__*/React.createElement(Input, {
      placeholder: "Search regions, keys, events\u2026",
      iconLeft: /*#__PURE__*/React.createElement(Icon, {
        n: "search",
        size: 17
      })
    })), /*#__PURE__*/React.createElement(Tooltip, {
      label: "Notifications",
      side: "bottom"
    }, /*#__PURE__*/React.createElement(IconButton, {
      label: "Notifications",
      icon: /*#__PURE__*/React.createElement(Icon, {
        n: "bell"
      })
    })), /*#__PURE__*/React.createElement(Tooltip, {
      label: "Help",
      side: "bottom"
    }, /*#__PURE__*/React.createElement(IconButton, {
      label: "Help",
      icon: /*#__PURE__*/React.createElement(Icon, {
        n: "life-buoy"
      })
    })), /*#__PURE__*/React.createElement(Tooltip, {
      label: "Sign out",
      side: "bottom"
    }, /*#__PURE__*/React.createElement("span", {
      onClick: onSignOut,
      style: {
        cursor: "pointer"
      }
    }, /*#__PURE__*/React.createElement(Avatar, {
      name: "Lina Mar\xF2",
      status: "online"
    })))));
  }
  Object.assign(window, {
    TopBar
  });
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/console/TopBar.jsx", error: String((e && e.message) || e) }); }

// ui_kits/console/data.jsx
try { (() => {
const ARMALI_REGIONS = [{
  name: "Aegean",
  code: "aeg-1",
  status: "online",
  uptime: "99.98%",
  lag: "12 ms",
  lastRun: "4m ago"
}, {
  name: "Adriatic",
  code: "adr-2",
  status: "online",
  uptime: "99.95%",
  lag: "18 ms",
  lastRun: "2m ago"
}, {
  name: "Tyrrhenian",
  code: "tyr-1",
  status: "away",
  uptime: "99.40%",
  lag: "61 ms",
  lastRun: "21m ago"
}, {
  name: "Ionian",
  code: "ion-3",
  status: "online",
  uptime: "99.99%",
  lag: "9 ms",
  lastRun: "1m ago"
}, {
  name: "Ligurian",
  code: "lig-1",
  status: "busy",
  uptime: "97.10%",
  lag: "—",
  lastRun: "syncing…"
}, {
  name: "Balearic",
  code: "bal-2",
  status: "online",
  uptime: "99.92%",
  lag: "22 ms",
  lastRun: "6m ago"
}];
const ARMALI_ACTIVITY = [{
  icon: "check-circle-2",
  tone: "success",
  text: "Ionian completed a full sync",
  time: "1m ago"
}, {
  icon: "key-round",
  tone: "azure",
  text: "New access key issued for adr-2",
  time: "18m ago"
}, {
  icon: "alert-triangle",
  tone: "gold",
  text: "Tyrrhenian latency above threshold",
  time: "21m ago"
}, {
  icon: "waves",
  tone: "aqua",
  text: "Balearic region connected",
  time: "1h ago"
}];
window.ARMALI_REGIONS = ARMALI_REGIONS;
window.ARMALI_ACTIVITY = ARMALI_ACTIVITY;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/console/data.jsx", error: String((e && e.message) || e) }); }

__ds_ns.Button = __ds_scope.Button;

__ds_ns.IconButton = __ds_scope.IconButton;

__ds_ns.Spinner = __ds_scope.Spinner;

__ds_ns.Toast = __ds_scope.Toast;

__ds_ns.Tooltip = __ds_scope.Tooltip;

__ds_ns.Checkbox = __ds_scope.Checkbox;

__ds_ns.Input = __ds_scope.Input;

__ds_ns.Select = __ds_scope.Select;

__ds_ns.Switch = __ds_scope.Switch;

__ds_ns.Tabs = __ds_scope.Tabs;

__ds_ns.Dialog = __ds_scope.Dialog;

__ds_ns.Avatar = __ds_scope.Avatar;

__ds_ns.Badge = __ds_scope.Badge;

__ds_ns.Card = __ds_scope.Card;

})();
