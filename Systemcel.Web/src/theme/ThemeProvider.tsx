import React from "react";
import "./theme-transition.css";

export type Theme = "light" | "dark";

interface ThemeContextValue {
  theme: Theme;
  setTheme: (theme: Theme) => void;
  toggleTheme: () => void;
}

const THEME_STORAGE_KEY = "systemcel.theme";

function initialTheme(): Theme {
  try {
    const stored = window.localStorage.getItem(THEME_STORAGE_KEY);
    if (stored === "light" || stored === "dark") return stored;
  } catch {
    // Depolama kapalıysa işletim sistemi tercihiyle devam edilir.
  }

  return window.matchMedia?.("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

const ThemeContext = React.createContext<ThemeContextValue | undefined>(undefined);

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [theme, setTheme] = React.useState<Theme>(initialTheme);
  const previousTheme = React.useRef(theme);

  React.useLayoutEffect(() => {
    const root = document.documentElement;
    let transitionTimer: ReturnType<typeof setTimeout> | undefined;
    if (previousTheme.current !== theme && !window.matchMedia?.("(prefers-reduced-motion: reduce)").matches) {
      root.dataset.themeTransitioning = "true";
      // Commit the transition rules before swapping the theme's colors.
      void root.offsetWidth;
      transitionTimer = setTimeout(() => delete root.dataset.themeTransitioning, 320);
    }
    previousTheme.current = theme;
    document.documentElement.dataset.theme = theme;
    document.documentElement.style.colorScheme = theme;
    document.querySelector<HTMLMetaElement>('meta[name="theme-color"]')?.setAttribute("content", theme === "dark" ? "#11120e" : "#f2f0e7");
    try {
      window.localStorage.setItem(THEME_STORAGE_KEY, theme);
    } catch {
      // Tema yine de bu oturum için uygulanır.
    }
    return () => {
      clearTimeout(transitionTimer);
      delete root.dataset.themeTransitioning;
    };
  }, [theme]);

  React.useEffect(() => {
    const syncTheme = (event: StorageEvent) => {
      if (event.key === THEME_STORAGE_KEY && (event.newValue === "light" || event.newValue === "dark")) {
        setTheme(event.newValue);
      }
    };
    window.addEventListener("storage", syncTheme);
    return () => window.removeEventListener("storage", syncTheme);
  }, []);

  const value = React.useMemo<ThemeContextValue>(() => {
    return {
      theme,
      setTheme,
      toggleTheme: () => setTheme((current) => current === "light" ? "dark" : "light")
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
