import React from "react";

export type Theme = "light" | "dark";

interface ThemeContextValue {
  theme: Theme;
}

const DEFAULT_THEME: Theme = "dark";

const ThemeContext = React.createContext<ThemeContextValue | undefined>(undefined);

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [theme] = React.useState<Theme>(DEFAULT_THEME);

  React.useEffect(() => {
    document.documentElement.dataset.theme = DEFAULT_THEME;
  }, [theme]);

  const value = React.useMemo<ThemeContextValue>(() => {
    return {
      theme
    };
  }, [theme]);

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const context = React.useContext(ThemeContext);
  if (!context) {
    throw new Error("useTheme must be used within ThemeProvider.");
  }

  return context;
}
