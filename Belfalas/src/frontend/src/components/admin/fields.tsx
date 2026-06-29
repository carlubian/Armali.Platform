// Small inline-styled form primitives for the admin (era studio) screens.
// They lean on the `.belf-input`/`.belf-select`/`.belf-btn*` classes in
// styles/global.css for interactive states, and the design-system tokens for
// colour, so the admin UI stays consistent with the rest of Belfalas.

import type { ButtonHTMLAttributes, ReactNode, SelectHTMLAttributes } from "react";

export function Field({ label, hint, children }: { label: string; hint?: string; children: ReactNode }) {
  return (
    <label style={{ display: "flex", flexDirection: "column", gap: 6 }}>
      <span className="armali-eyebrow" style={{ fontSize: 9.5 }}>
        {label}
      </span>
      {children}
      {hint ? <span style={{ fontSize: 11.5, color: "var(--text-muted)" }}>{hint}</span> : null}
    </label>
  );
}

export function TextField({
  value,
  onChange,
  placeholder,
  disabled,
  maxLength,
}: {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  disabled?: boolean;
  maxLength?: number;
}) {
  return (
    <input
      className="belf-input"
      type="text"
      value={value}
      placeholder={placeholder}
      disabled={disabled}
      maxLength={maxLength}
      onChange={(event) => onChange(event.target.value)}
    />
  );
}

export function NumberField({
  value,
  onChange,
  min,
  max,
  disabled,
}: {
  value: number | "";
  onChange: (value: number | "") => void;
  min?: number;
  max?: number;
  disabled?: boolean;
}) {
  return (
    <input
      className="belf-input"
      type="number"
      value={value}
      min={min}
      max={max}
      disabled={disabled}
      onChange={(event) => {
        const raw = event.target.value;
        onChange(raw === "" ? "" : Number(raw));
      }}
    />
  );
}

export function DateField({
  value,
  onChange,
  disabled,
}: {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  return (
    <input
      className="belf-input"
      type="date"
      value={value}
      disabled={disabled}
      onChange={(event) => onChange(event.target.value)}
    />
  );
}

interface SelectFieldProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, "onChange"> {
  value: string;
  onChange: (value: string) => void;
  options: { value: string; label: string }[];
}

export function SelectField({ value, onChange, options, ...rest }: SelectFieldProps) {
  return (
    <select
      className="belf-select"
      value={value}
      onChange={(event) => onChange(event.target.value)}
      {...rest}
    >
      {options.map((option) => (
        <option key={option.value} value={option.value}>
          {option.label}
        </option>
      ))}
    </select>
  );
}

type ButtonVariant = "primary" | "ghost" | "danger";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
}

export function Button({ variant = "primary", children, ...rest }: ButtonProps) {
  return (
    <button type="button" className={`belf-btn belf-btn-${variant}`} {...rest}>
      {children}
    </button>
  );
}

export function IconButton({
  title,
  onClick,
  danger,
  disabled,
  children,
}: {
  title: string;
  onClick: () => void;
  danger?: boolean;
  disabled?: boolean;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      title={title}
      aria-label={title}
      disabled={disabled}
      onClick={onClick}
      className={`belf-icon-action${danger ? " is-danger" : ""}`}
    >
      {children}
    </button>
  );
}

export function AdminCard({ children, style }: { children: ReactNode; style?: React.CSSProperties }) {
  return (
    <section
      style={{
        padding: "22px 24px",
        borderRadius: "var(--radius-card)",
        background: "var(--surface-card-solid)",
        border: "1px solid var(--border-subtle)",
        boxShadow: "var(--glow-soft)",
        ...style,
      }}
    >
      {children}
    </section>
  );
}

export function CardHeader({ title, subtitle, action }: { title: string; subtitle?: string; action?: ReactNode }) {
  return (
    <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 14 }}>
      <div>
        <h2 style={{ fontSize: 18 }}>{title}</h2>
        {subtitle ? (
          <p style={{ marginTop: 4, fontSize: 13, color: "var(--text-secondary)" }}>{subtitle}</p>
        ) : null}
      </div>
      {action}
    </div>
  );
}
