import { Globe2, History, LayoutGrid, SlidersHorizontal, Waves, type LucideIcon } from "lucide-react";
import { useLocation, useNavigate } from "react-router-dom";
import { useEraData } from "../state/EraDataContext";

interface NavItem {
  label: string;
  icon: LucideIcon;
  to: string;
  isActive: (path: string) => boolean;
}

export function LeftRail() {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const { areas } = useEraData();

  const firstAreaPath = areas.length > 0 ? `/area/${areas[0].areaId}` : "/";

  const items: NavItem[] = [
    { label: "World", icon: Globe2, to: "/", isActive: (path) => path === "/" },
    { label: "Areas", icon: LayoutGrid, to: firstAreaPath, isActive: (path) => path.startsWith("/area") },
    { label: "History", icon: History, to: "/history", isActive: (path) => path.startsWith("/history") },
    { label: "Admin", icon: SlidersHorizontal, to: "/admin", isActive: (path) => path.startsWith("/admin") },
  ];

  return (
    <nav
      style={{
        width: 84,
        flex: "0 0 84px",
        background: "linear-gradient(180deg,#FCF8F0,#F6EFE1)",
        borderRight: "1px solid var(--border-subtle)",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        padding: "20px 0",
        gap: 10,
        zIndex: 20,
      }}
    >
      <div
        style={{
          width: 46,
          height: 46,
          borderRadius: 14,
          background: "linear-gradient(150deg,var(--aqua-400),var(--aqua-600))",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          boxShadow: "var(--glow-aqua)",
          marginBottom: 14,
        }}
      >
        <Waves size={24} color="#fff" aria-hidden />
      </div>

      {items.map(({ label, icon: Icon, to, isActive }) => (
        <button
          key={label}
          type="button"
          className="belf-nav-btn"
          data-active={isActive(pathname)}
          title={label}
          onClick={() => navigate(to)}
        >
          <Icon size={21} aria-hidden />
          <span className="belf-nav-label">{label}</span>
        </button>
      ))}

      <div style={{ flex: 1 }} />

      <div
        title="Player"
        style={{
          width: 42,
          height: 42,
          borderRadius: "50%",
          background: "linear-gradient(140deg,var(--gold-300),var(--gold-500))",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          color: "#fff",
          fontFamily: "var(--font-display)",
          fontWeight: 600,
          boxShadow: "var(--glow-soft)",
        }}
      >
        LM
      </div>
    </nav>
  );
}
