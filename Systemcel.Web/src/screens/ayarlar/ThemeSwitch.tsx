import React from "react";
import { useTheme } from "../../theme/ThemeProvider";
import "./theme-switch.css";

export function ThemeSwitch({ darkLabel, label }: {
  lightLabel: string; darkLabel: string; label: string;
}) {
  const { theme, setTheme } = useTheme();
  const maskId = React.useId();
  const [keyboard, setKeyboard] = React.useState(false);
  const dark = theme === "dark";

  return (
    <button
      className="settings-theme-switch"
      type="button"
      role="switch"
      aria-label={`${label}: ${darkLabel}`}
      aria-checked={dark}
      data-keyboard={keyboard}
      onPointerDown={() => setKeyboard(false)}
      onClick={() => setTheme(dark ? "light" : "dark")}
      onKeyDown={(event) => {
        setKeyboard(true);
        if (event.key === "ArrowLeft" || event.key === "ArrowRight") {
          event.preventDefault();
          setTheme(event.key === "ArrowRight" ? "dark" : "light");
        }
      }}
    >
      <span className="settings-theme-switch__thumb" aria-hidden="true">
        <svg viewBox="0 0 24 24" className="settings-theme-switch__symbol">
          <defs>
            <mask id={maskId}>
              <rect width="24" height="24" fill="white" />
              <circle className="settings-theme-switch__cutout" cx="19" cy="5" r="7" fill="black" />
            </mask>
          </defs>
          <circle className="settings-theme-switch__orb" cx="12" cy="12" r="6" fill="currentColor" mask={`url(#${maskId})`} />
          <g className="settings-theme-switch__rays" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round">
            <path d="M12 2v2m0 16v2M2 12h2m16 0h2M4.93 4.93l1.42 1.42m11.3 11.3 1.42 1.42M4.93 19.07l1.42-1.42m11.3-11.3 1.42-1.42" />
          </g>
        </svg>
      </span>
    </button>
  );
}
