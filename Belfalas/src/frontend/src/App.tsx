import { Navigate, Route, Routes } from "react-router-dom";
import { AppShell } from "./components/AppShell";
import { AdminScreen } from "./screens/AdminScreen";
import { AreaScreen } from "./screens/AreaScreen";
import { HistoryScreen } from "./screens/HistoryScreen";
import { HomeScreen } from "./screens/HomeScreen";

export default function App() {
  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route index element={<HomeScreen />} />
        <Route path="area/:areaId" element={<AreaScreen />} />
        <Route path="admin" element={<AdminScreen />} />
        <Route path="history" element={<HistoryScreen />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
