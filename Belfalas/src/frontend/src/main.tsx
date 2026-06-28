import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import App from "./App";
import { EraDataProvider } from "./state/EraDataContext";
import "./design-system/styles.css";
import "./styles/global.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <BrowserRouter>
      <EraDataProvider>
        <App />
      </EraDataProvider>
    </BrowserRouter>
  </StrictMode>,
);
