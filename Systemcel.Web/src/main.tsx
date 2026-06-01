import React from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import { SystemcelAuthProvider } from "./auth/SystemcelAuthProvider";
import { ThemeProvider } from "./theme/ThemeProvider";
import "./styles.css";

createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <ThemeProvider>
      <SystemcelAuthProvider>
        <App />
      </SystemcelAuthProvider>
    </ThemeProvider>
  </React.StrictMode>
);
