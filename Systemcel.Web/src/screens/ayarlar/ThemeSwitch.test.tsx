import React from "react";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, expect, it } from "vitest";
import { ThemeProvider } from "../../theme/ThemeProvider";
import { ThemeSwitch } from "./ThemeSwitch";

afterEach(() => { cleanup(); localStorage.clear(); delete document.documentElement.dataset.theme; });

function mount() {
  localStorage.setItem("systemcel.theme", "light");
  render(<ThemeProvider><ThemeSwitch label="Tema seçimi" lightLabel="Açık" darkLabel="Koyu" /></ThemeProvider>);
  return screen.getByRole("switch", { name: "Tema seçimi: Koyu" });
}

it("switch state follows and persists the selected theme", async () => {
  const user = userEvent.setup();
  const control = mount();
  expect(control).not.toBeChecked();
  await user.click(control);
  expect(control).toBeChecked();
  expect(localStorage.getItem("systemcel.theme")).toBe("dark");
  await user.click(control);
  expect(control).not.toBeChecked();
  expect(document.documentElement.dataset.theme).toBe("light");
});

it("supports arrows, Space and Enter without keyboard motion", async () => {
  const user = userEvent.setup();
  const control = mount();
  await user.tab();
  expect(control).toHaveFocus();
  await user.keyboard("{ArrowRight}");
  expect(control).toBeChecked();
  expect(control).toHaveAttribute("data-keyboard", "true");
  await user.keyboard("{ArrowLeft}");
  expect(control).not.toBeChecked();
  await user.keyboard(" ");
  expect(control).toBeChecked();
  await user.keyboard("{Enter}");
  expect(control).not.toBeChecked();
});
