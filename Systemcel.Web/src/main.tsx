import React from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import { SystemcelAuthProvider } from "./auth/SystemcelAuthProvider";
import { ThemeProvider } from "./theme/ThemeProvider";
import { I18nProvider } from "./shared/i18n";
import "./styles.css";
import "./app-theme.css";

createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <ThemeProvider>
      <I18nProvider>
        <SystemcelAuthProvider>
          <App />
        </SystemcelAuthProvider>
      </I18nProvider>
    </ThemeProvider>
  </React.StrictMode>
);
