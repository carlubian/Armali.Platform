import { Outlet } from "react-router-dom";
import { useEraData } from "../state/EraDataContext";
import { Celebration } from "./Celebration";
import { LeftRail } from "./LeftRail";
import { Topbar } from "./Topbar";

export function AppShell() {
  const { loading } = useEraData();

  return (
    <div style={{ height: "100vh", width: "100%", display: "flex", overflow: "hidden" }}>
      <LeftRail />
      <main style={{ flex: 1, minWidth: 0, display: "flex", flexDirection: "column", position: "relative" }}>
        <Topbar />
        <div style={{ flex: 1, minHeight: 0, position: "relative" }}>
          {loading ? <ShellLoader /> : <Outlet />}
        </div>
      </main>
      <Celebration />
    </div>
  );
}

function ShellLoader() {
  return (
    <div
      style={{
        position: "absolute",
        inset: 0,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        color: "var(--text-muted)",
        fontFamily: "var(--font-display)",
        fontWeight: 600,
        fontSize: 14,
      }}
    >
      Loading your world…
    </div>
  );
}
