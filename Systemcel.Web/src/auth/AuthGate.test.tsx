import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { RequireAuth } from "./AuthGate";

const auth = vi.hoisted(() => ({
  clerkEnabled: true,
  isLoaded: false,
  isSignedIn: false,
  error: ""
}));

vi.mock("./SystemcelAuthProvider", () => ({ useSystemcelAuth: () => auth }));

describe("RequireAuth", () => {
  afterEach(cleanup);

  beforeEach(() => {
    auth.clerkEnabled = true;
    auth.isLoaded = false;
    auth.isSignedIn = false;
    auth.error = "";
  });

  it("shows a branded workspace loading state until auth is ready", () => {
    render(<RequireAuth><p>Özel çalışma alanı</p></RequireAuth>);

    expect(screen.getByRole("status")).toHaveTextContent("Çalışma alanın yükleniyor");
    expect(screen.queryByText("Özel çalışma alanı")).not.toBeInTheDocument();
  });

  it("keeps an actionable error when auth initialization fails", () => {
    auth.isLoaded = true;
    auth.error = "Oturum ayarları alınamadı.";

    render(<RequireAuth><p>Özel çalışma alanı</p></RequireAuth>);

    expect(screen.getByRole("heading", { name: "Çalışma alanı açılamadı" })).toBeInTheDocument();
    expect(screen.getByText(auth.error)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Giriş ekranına git" })).toHaveAttribute("href", "/giris");
  });

  it("shows the workspace after sign-in", () => {
    auth.isLoaded = true;
    auth.isSignedIn = true;

    render(<RequireAuth><p>Özel çalışma alanı</p></RequireAuth>);

    expect(screen.getByText("Özel çalışma alanı")).toBeInTheDocument();
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
  });
});
